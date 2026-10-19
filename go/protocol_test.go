package battlenet

import "testing"

func TestCommandRoundTrip(t *testing.T) {
	packet := BuildCommandPacket(7, "players")
	parsed, err := ParsePacket(packet)
	if err != nil {
		t.Fatal(err)
	}
	if parsed.Type != PacketCommand {
		t.Fatalf("type=%v", parsed.Type)
	}
	if parsed.Sequence == nil || *parsed.Sequence != 7 {
		t.Fatalf("seq=%v", parsed.Sequence)
	}
	if parsed.Message != "players" {
		t.Fatalf("msg=%q", parsed.Message)
	}
}

func TestAcknowledgeRoundTrip(t *testing.T) {
	packet := BuildAcknowledgePacket(3)
	parsed, err := ParsePacket(packet)
	if err != nil {
		t.Fatal(err)
	}
	if parsed.Type != PacketServerMessage {
		t.Fatalf("type=%v", parsed.Type)
	}
	if parsed.Sequence == nil || *parsed.Sequence != 3 {
		t.Fatalf("seq=%v", parsed.Sequence)
	}
}

func TestLoginPacketHeader(t *testing.T) {
	packet := BuildLoginPacket("secret")
	if len(packet) < 8 || packet[0] != 'B' || packet[1] != 'E' || packet[6] != 0xFF || packet[7] != byte(PacketLogin) {
		t.Fatalf("bad login packet: %v", packet)
	}
	if _, err := ParsePacket(packet); err != nil {
		t.Fatal(err)
	}
}
