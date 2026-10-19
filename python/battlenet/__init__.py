"""BattleNET — BattlEye RCon client for Python."""

from .client import (
    BattlEyeClient,
    BattlEyeClientOptions,
    ConnectionResult,
    DisconnectionType,
    MessageEvent,
    SERVER_MESSAGE_ID,
)
from .protocol import (
    PacketType,
    ParsedPacket,
    build_acknowledge_packet,
    build_command_packet,
    build_login_packet,
    parse_packet,
)

__all__ = [
    "BattlEyeClient",
    "BattlEyeClientOptions",
    "ConnectionResult",
    "DisconnectionType",
    "MessageEvent",
    "SERVER_MESSAGE_ID",
    "PacketType",
    "ParsedPacket",
    "build_acknowledge_packet",
    "build_command_packet",
    "build_login_packet",
    "parse_packet",
]

__version__ = "2.1.0"
