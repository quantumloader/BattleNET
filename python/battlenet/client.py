"""Async-friendly BattlEye RCon client with proper socket cleanup."""

from __future__ import annotations

import socket
import threading
import time
from dataclasses import dataclass, field
from enum import Enum, auto
from typing import Callable, Optional

from .protocol import (
    PacketType,
    build_acknowledge_packet,
    build_command_packet,
    build_login_packet,
    parse_packet,
)


class ConnectionResult(Enum):
    SUCCESS = auto()
    CONNECTION_FAILED = auto()
    INVALID_LOGIN = auto()
    CANCELLED = auto()


class DisconnectionType(Enum):
    MANUAL = auto()
    CONNECTION_LOST = auto()
    SOCKET_ERROR = auto()
    DISPOSED = auto()


SERVER_MESSAGE_ID = -1


@dataclass(slots=True)
class MessageEvent:
    message: str
    id: int


@dataclass
class BattlEyeClientOptions:
    receive_timeout: float = 5.0
    keep_alive_interval: float = 5.0
    connection_lost_timeout: float = 20.0
    command_timeout: float = 10.0
    reconnect_on_packet_loss: bool = False
    max_reconnect_attempts: int = 10
    initial_reconnect_delay: float = 1.0
    max_reconnect_delay: float = 30.0
    buffer_size: int = 64 * 1024


@dataclass
class _Pending:
    event: threading.Event = field(default_factory=threading.Event)
    message: str = ""
    error: Optional[BaseException] = None


@dataclass
class _Multipart:
    total: int
    parts: list[Optional[str]]
    received: int = 0


class BattlEyeClient:
    def __init__(
        self,
        host: str,
        port: int,
        password: str,
        options: BattlEyeClientOptions | None = None,
    ) -> None:
        self.host = host
        self.port = port
        self.password = password
        self.options = options or BattlEyeClientOptions()

        self._sock: Optional[socket.socket] = None
        self._lock = threading.RLock()
        self._connected = False
        self._disposed = False
        self._stop = threading.Event()
        self._seq = 0
        self._last_sent = time.monotonic()
        self._last_received = time.monotonic()
        self._pending: dict[int, _Pending] = {}
        self._parts: dict[int, _Multipart] = {}
        self._threads: list[threading.Thread] = []

        self.on_message: Optional[Callable[[MessageEvent], None]] = None
        self.on_connected: Optional[Callable[[ConnectionResult], None]] = None
        self.on_disconnected: Optional[Callable[[DisconnectionType], None]] = None

    @property
    def connected(self) -> bool:
        return self._connected

    def connect(self) -> ConnectionResult:
        if self._disposed:
            raise RuntimeError("client disposed")

        self.disconnect(DisconnectionType.MANUAL, raise_event=False)

        try:
            sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
            sock.settimeout(self.options.receive_timeout)
            sock.connect((self.host, self.port))
            sock.setsockopt(socket.SOL_SOCKET, socket.SO_RCVBUF, self.options.buffer_size)
            sock.setsockopt(socket.SOL_SOCKET, socket.SO_SNDBUF, self.options.buffer_size)

            with self._lock:
                self._sock = sock

            sock.send(build_login_packet(self.password))
            data = sock.recv(self.options.buffer_size)
            parsed = parse_packet(data)
            if parsed.type is not PacketType.LOGIN:
                self._close_sock()
                self._raise_connected(ConnectionResult.CONNECTION_FAILED)
                return ConnectionResult.CONNECTION_FAILED
            if not parsed.login_success:
                self._close_sock()
                self._raise_connected(ConnectionResult.INVALID_LOGIN)
                return ConnectionResult.INVALID_LOGIN

            sock.settimeout(1.0)
            now = time.monotonic()
            self._last_sent = now
            self._last_received = now
            self._seq = 0
            self._stop.clear()
            self._connected = True

            self._threads = [
                threading.Thread(target=self._receive_loop, name="battlenet-recv", daemon=True),
                threading.Thread(target=self._keepalive_loop, name="battlenet-ka", daemon=True),
            ]
            for t in self._threads:
                t.start()

            self._raise_connected(ConnectionResult.SUCCESS)
            return ConnectionResult.SUCCESS
        except OSError:
            self._close_sock()
            self._raise_connected(ConnectionResult.CONNECTION_FAILED)
            return ConnectionResult.CONNECTION_FAILED

    def send_command(self, command: str | None = None) -> int:
        if not self._connected:
            raise RuntimeError("not connected")
        seq = self._next_seq()
        self._send(build_command_packet(seq, command))
        return seq

    def send_command_and_wait(
        self,
        command: str,
        timeout: float | None = None,
    ) -> str:
        if not self._connected:
            raise RuntimeError("not connected")

        seq = self._next_seq()
        pending = _Pending()
        with self._lock:
            self._pending[seq] = pending

        try:
            self._send(build_command_packet(seq, command))
            wait = self.options.command_timeout if timeout is None else timeout
            if not pending.event.wait(wait):
                raise TimeoutError(f"timed out waiting for sequence {seq}")
            if pending.error is not None:
                raise pending.error
            return pending.message
        finally:
            with self._lock:
                self._pending.pop(seq, None)

    def disconnect(
        self,
        reason: DisconnectionType = DisconnectionType.MANUAL,
        *,
        raise_event: bool = True,
    ) -> None:
        was = self._connected
        self._connected = False
        self._stop.set()
        self._close_sock()

        for t in self._threads:
            if t.is_alive() and t is not threading.current_thread():
                t.join(timeout=2.0)
        self._threads.clear()

        with self._lock:
            for pending in self._pending.values():
                pending.error = RuntimeError("disconnected before reply")
                pending.event.set()
            self._pending.clear()
            self._parts.clear()

        if was and raise_event and self.on_disconnected:
            self.on_disconnected(reason)

    def close(self) -> None:
        if self._disposed:
            return
        self._disposed = True
        self.disconnect(DisconnectionType.DISPOSED)

    def __enter__(self) -> "BattlEyeClient":
        return self

    def __exit__(self, *args: object) -> None:
        self.close()

    def _next_seq(self) -> int:
        with self._lock:
            seq = self._seq & 0xFF
            self._seq = (self._seq + 1) & 0xFF
            return seq

    def _send(self, packet: bytes) -> None:
        with self._lock:
            sock = self._sock
        if sock is None:
            raise RuntimeError("not connected")
        sock.send(packet)
        self._last_sent = time.monotonic()

    def _close_sock(self) -> None:
        with self._lock:
            sock = self._sock
            self._sock = None
        if sock is not None:
            try:
                sock.close()
            except OSError:
                pass

    def _receive_loop(self) -> None:
        while not self._stop.is_set() and self._connected:
            with self._lock:
                sock = self._sock
            if sock is None:
                break
            try:
                data = sock.recv(self.options.buffer_size)
            except socket.timeout:
                continue
            except OSError:
                if self._connected and not self._stop.is_set():
                    self._handle_lost(DisconnectionType.SOCKET_ERROR)
                break

            self._last_received = time.monotonic()
            try:
                self._handle_datagram(data)
            except Exception:
                continue

    def _keepalive_loop(self) -> None:
        while not self._stop.wait(1.0):
            if not self._connected:
                break
            now = time.monotonic()
            if now - self._last_received >= self.options.connection_lost_timeout:
                self._handle_lost(DisconnectionType.CONNECTION_LOST)
                break
            with self._lock:
                idle = len(self._pending) == 0
            if idle and now - self._last_sent >= self.options.keep_alive_interval:
                try:
                    self.send_command(None)
                except Exception:
                    self._handle_lost(DisconnectionType.SOCKET_ERROR)
                    break

    def _handle_datagram(self, data: bytes) -> None:
        parsed = parse_packet(data)
        if parsed.type is PacketType.SERVER_MESSAGE:
            if parsed.sequence is not None:
                try:
                    self._send(build_acknowledge_packet(parsed.sequence))
                except Exception:
                    pass
            self._raise_message(MessageEvent(parsed.message, SERVER_MESSAGE_ID))
            return

        if parsed.type is PacketType.COMMAND and parsed.sequence is not None:
            self._handle_command(parsed.sequence, parsed.message, parsed.is_multipart,
                                 parsed.multipart_total, parsed.multipart_index)

    def _handle_command(
        self,
        seq: int,
        message: str,
        is_multipart: bool,
        total: int,
        index: int,
    ) -> None:
        if is_multipart:
            with self._lock:
                buf = self._parts.get(seq)
                if buf is None:
                    n = max(total, 1)
                    buf = _Multipart(total=n, parts=[None] * n)
                    self._parts[seq] = buf
                if 0 <= index < len(buf.parts) and buf.parts[index] is None:
                    buf.parts[index] = message
                    buf.received += 1
                complete = buf.received >= buf.total
                if complete:
                    message = "".join(p or "" for p in buf.parts)
                    del self._parts[seq]
                else:
                    return

        with self._lock:
            pending = self._pending.pop(seq, None)
        if pending is not None:
            pending.message = message
            pending.event.set()

        self._raise_message(MessageEvent(message, seq))

    def _handle_lost(self, reason: DisconnectionType) -> None:
        if not self._connected:
            return
        self.disconnect(reason)
        if not self.options.reconnect_on_packet_loss or self._disposed:
            return

        delay = self.options.initial_reconnect_delay
        attempts = self.options.max_reconnect_attempts
        try_count = 0
        while not self._disposed and (attempts == 0 or try_count < attempts):
            try_count += 1
            time.sleep(delay)
            if self.connect() is ConnectionResult.SUCCESS:
                return
            delay = min(delay * 2, self.options.max_reconnect_delay)

    def _raise_connected(self, result: ConnectionResult) -> None:
        if self.on_connected:
            self.on_connected(result)

    def _raise_message(self, event: MessageEvent) -> None:
        if self.on_message:
            self.on_message(event)
