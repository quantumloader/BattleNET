"""BattlEye RCon packet build/parse."""

from __future__ import annotations

from dataclasses import dataclass
from enum import IntEnum
from typing import Optional

from .crc32 import crc32


class PacketType(IntEnum):
    LOGIN = 0x00
    COMMAND = 0x01
    SERVER_MESSAGE = 0x02


@dataclass(slots=True)
class ParsedPacket:
    type: PacketType
    sequence: Optional[int] = None
    login_success: Optional[bool] = None
    message: str = ""
    is_multipart: bool = False
    multipart_total: int = 0
    multipart_index: int = 0


def build_packet(packet_type: PacketType, sequence: Optional[int], body: bytes) -> bytes:
    payload = bytearray([0xFF, int(packet_type)])
    if sequence is not None:
        payload.append(sequence & 0xFF)
    payload.extend(body)

    packet = bytearray(b"BE")
    packet.extend(crc32(payload).to_bytes(4, "little"))
    packet.extend(payload)
    return bytes(packet)


def build_login_packet(password: str) -> bytes:
    return build_packet(PacketType.LOGIN, None, password.encode("utf-8"))


def build_command_packet(sequence: int, command: str | None = None) -> bytes:
    body = b"" if not command else command.encode("utf-8")
    return build_packet(PacketType.COMMAND, sequence & 0xFF, body)


def build_acknowledge_packet(sequence: int) -> bytes:
    return build_packet(PacketType.SERVER_MESSAGE, sequence & 0xFF, b"")


def parse_packet(datagram: bytes) -> ParsedPacket:
    if len(datagram) < 8 or datagram[0:2] != b"BE" or datagram[6] != 0xFF:
        raise ValueError("invalid battleye packet header")

    expected = int.from_bytes(datagram[2:6], "little")
    actual = crc32(datagram[6:])
    if expected != actual:
        raise ValueError("crc mismatch")

    ptype = PacketType(datagram[7])

    if ptype is PacketType.LOGIN:
        if len(datagram) < 9:
            raise ValueError("short login packet")
        return ParsedPacket(type=ptype, login_success=datagram[8] == 0x01)

    if ptype is PacketType.COMMAND:
        if len(datagram) < 9:
            raise ValueError("short command packet")
        seq = datagram[8]
        if len(datagram) >= 12 and datagram[9] == 0x00:
            return ParsedPacket(
                type=ptype,
                sequence=seq,
                message=datagram[12:].decode("utf-8", errors="replace"),
                is_multipart=True,
                multipart_total=datagram[10],
                multipart_index=datagram[11],
            )
        message = datagram[9:].decode("utf-8", errors="replace") if len(datagram) > 9 else ""
        return ParsedPacket(type=ptype, sequence=seq, message=message)

    if ptype is PacketType.SERVER_MESSAGE:
        if len(datagram) < 9:
            raise ValueError("short server message")
        message = datagram[9:].decode("utf-8", errors="replace") if len(datagram) > 9 else ""
        return ParsedPacket(type=ptype, sequence=datagram[8], message=message)

    raise ValueError(f"unknown packet type: {ptype}")
