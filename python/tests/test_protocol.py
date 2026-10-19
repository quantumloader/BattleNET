import unittest

from battlenet.protocol import (
    PacketType,
    build_acknowledge_packet,
    build_command_packet,
    build_login_packet,
    parse_packet,
)


class ProtocolTests(unittest.TestCase):
    def test_command_roundtrip(self) -> None:
        packet = build_command_packet(7, "players")
        parsed = parse_packet(packet)
        self.assertEqual(parsed.type, PacketType.COMMAND)
        self.assertEqual(parsed.sequence, 7)
        self.assertEqual(parsed.message, "players")

    def test_ack_roundtrip(self) -> None:
        packet = build_acknowledge_packet(3)
        parsed = parse_packet(packet)
        self.assertEqual(parsed.type, PacketType.SERVER_MESSAGE)
        self.assertEqual(parsed.sequence, 3)

    def test_login_header(self) -> None:
        packet = build_login_packet("secret")
        self.assertEqual(packet[:2], b"BE")
        self.assertEqual(packet[6], 0xFF)
        self.assertEqual(packet[7], PacketType.LOGIN)
        parse_packet(packet)


if __name__ == "__main__":
    unittest.main()
