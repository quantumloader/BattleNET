"""Console RCon client."""

from __future__ import annotations

import argparse
import sys

from .client import BattlEyeClient, BattlEyeClientOptions, ConnectionResult, DisconnectionType


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="BattleNET v2 — BattlEye RCon client (Python)")
    parser.add_argument("--host", help="RCon host or IP")
    parser.add_argument("--port", type=int, help="RCon UDP port")
    parser.add_argument("--password", help="RCon password")
    parser.add_argument("--command", "-c", help="Send a single command and exit")
    parser.add_argument("--reconnect", action="store_true", help="Reconnect after connection loss")
    args = parser.parse_args(argv)

    host, port, password = args.host, args.port, args.password
    if not host or not port or not password:
        host, port, password = _prompt(host, port, password)
    if not host or not port or not password:
        print("host, port and password are required", file=sys.stderr)
        return 1

    client = BattlEyeClient(
        host,
        int(port),
        password,
        BattlEyeClientOptions(reconnect_on_packet_loss=args.reconnect),
    )
    client.on_message = lambda e: print(e.message) if e.message else None
    client.on_connected = lambda r: print(f"connect: {r.name}", file=sys.stderr)
    client.on_disconnected = lambda t: print(f"disconnected: {t.name}", file=sys.stderr)

    try:
        result = client.connect()
        if result is not ConnectionResult.SUCCESS:
            return 2

        if args.command:
            try:
                reply = client.send_command_and_wait(args.command)
                if reply:
                    print(reply)
            except Exception as ex:
                print(ex, file=sys.stderr)
                return 3
            return 0

        print("Interactive mode. Type a command, or exit/logout to quit.", file=sys.stderr)
        while client.connected:
            try:
                line = input("> ").strip()
            except (EOFError, KeyboardInterrupt):
                print()
                break
            if not line:
                continue
            if line in {"exit", "logout", "quit"}:
                break
            try:
                reply = client.send_command_and_wait(line)
                if reply:
                    print(reply)
            except Exception as ex:
                print(ex, file=sys.stderr)
                if not client.connected:
                    break
        return 0
    finally:
        client.disconnect(DisconnectionType.MANUAL)
        client.close()


def _prompt(host: str | None, port: int | None, password: str | None) -> tuple[str, int, str]:
    while not host:
        host = input("Enter IP address or hostname: ").strip()
    while not port:
        raw = input("Enter port number: ").strip()
        if raw.isdigit():
            port = int(raw)
    while not password:
        password = input("Enter RCon password: ").strip()
    return host, int(port), password


if __name__ == "__main__":
    raise SystemExit(main())
