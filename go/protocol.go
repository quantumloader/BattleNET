package battlenet

import (
	"encoding/binary"
	"errors"
)

// PacketType is the BattlEye RCon payload type byte (after 0xFF).
type PacketType byte

const (
	PacketLogin         PacketType = 0x00
	PacketCommand       PacketType = 0x01
	PacketServerMessage PacketType = 0x02
)

// ParsedPacket is a decoded BattlEye datagram.
type ParsedPacket struct {
	Type           PacketType
	Sequence       *byte
	LoginSuccess   *bool
	Message        string
	IsMultipart    bool
	MultipartTotal byte
	MultipartIndex byte
}

var (
	errShortPacket   = errors.New("battlenet: packet too short")
	errBadHeader     = errors.New("battlenet: invalid header")
	errBadCRC        = errors.New("battlenet: crc mismatch")
	errUnknownType   = errors.New("battlenet: unknown packet type")
)

// BuildLoginPacket builds a login datagram for password.
func BuildLoginPacket(password string) []byte {
	return buildPacket(PacketLogin, nil, []byte(password))
}

// BuildCommandPacket builds a command datagram.
func BuildCommandPacket(sequence byte, command string) []byte {
	seq := sequence
	var body []byte
	if command != "" {
		body = []byte(command)
	}
	return buildPacket(PacketCommand, &seq, body)
}

// BuildAcknowledgePacket builds an ACK for a server message sequence.
func BuildAcknowledgePacket(sequence byte) []byte {
	seq := sequence
	return buildPacket(PacketServerMessage, &seq, nil)
}

func buildPacket(typ PacketType, sequence *byte, body []byte) []byte {
	payloadLen := 2
	if sequence != nil {
		payloadLen++
	}
	payloadLen += len(body)

	payload := make([]byte, payloadLen)
	payload[0] = 0xFF
	payload[1] = byte(typ)
	offset := 2
	if sequence != nil {
		payload[offset] = *sequence
		offset++
	}
	copy(payload[offset:], body)

	packet := make([]byte, 2+4+payloadLen)
	packet[0] = 'B'
	packet[1] = 'E'
	binary.LittleEndian.PutUint32(packet[2:6], CRC32(payload))
	copy(packet[6:], payload)
	return packet
}

// ParsePacket parses a received BattlEye datagram.
func ParsePacket(datagram []byte) (ParsedPacket, error) {
	var out ParsedPacket
	if len(datagram) < 8 {
		return out, errShortPacket
	}
	if datagram[0] != 'B' || datagram[1] != 'E' {
		return out, errBadHeader
	}
	if datagram[6] != 0xFF {
		return out, errBadHeader
	}

	expected := binary.LittleEndian.Uint32(datagram[2:6])
	actual := CRC32(datagram[6:])
	if expected != actual {
		return out, errBadCRC
	}

	typ := PacketType(datagram[7])
	switch typ {
	case PacketLogin:
		if len(datagram) < 9 {
			return out, errShortPacket
		}
		ok := datagram[8] == 0x01
		out.Type = typ
		out.LoginSuccess = &ok
		return out, nil

	case PacketCommand:
		if len(datagram) < 9 {
			return out, errShortPacket
		}
		seq := datagram[8]
		out.Type = typ
		out.Sequence = &seq

		if len(datagram) >= 12 && datagram[9] == 0x00 {
			out.IsMultipart = true
			out.MultipartTotal = datagram[10]
			out.MultipartIndex = datagram[11]
			out.Message = string(datagram[12:])
			return out, nil
		}

		if len(datagram) > 9 {
			out.Message = string(datagram[9:])
		}
		return out, nil

	case PacketServerMessage:
		if len(datagram) < 9 {
			return out, errShortPacket
		}
		seq := datagram[8]
		out.Type = typ
		out.Sequence = &seq
		if len(datagram) > 9 {
			out.Message = string(datagram[9:])
		}
		return out, nil

	default:
		return out, errUnknownType
	}
}
