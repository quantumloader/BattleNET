using System.Buffers.Binary;
using System.Text;

namespace BattleNET;

/// <summary>
/// Builds and parses BattlEye RCon UDP packets.
/// </summary>
public static class BattlEyeProtocol
{
    private const byte HeaderB = (byte)'B';
    private const byte HeaderE = (byte)'E';
    private const byte PayloadMarker = 0xFF;

    /// <summary>Builds a login packet for the given password.</summary>
    public static byte[] BuildLoginPacket(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        var body = Encoding.UTF8.GetBytes(password);
        return BuildPacket(BattlEyePacketType.Login, sequence: null, body);
    }

    /// <summary>Builds a command packet with the given sequence number.</summary>
    public static byte[] BuildCommandPacket(byte sequence, string? command)
    {
        ReadOnlySpan<byte> body = command is null or ""
            ? ReadOnlySpan<byte>.Empty
            : Encoding.UTF8.GetBytes(command);
        return BuildPacket(BattlEyePacketType.Command, sequence, body);
    }

    /// <summary>Builds an acknowledge packet for a server message sequence.</summary>
    public static byte[] BuildAcknowledgePacket(byte sequence)
    {
        return BuildPacket(BattlEyePacketType.ServerMessage, sequence, ReadOnlySpan<byte>.Empty);
    }

    /// <summary>Builds a complete RCon datagram: BE + CRC32 + payload.</summary>
    public static byte[] BuildPacket(BattlEyePacketType type, byte? sequence, ReadOnlySpan<byte> body)
    {
        int payloadLength = 2 + (sequence.HasValue ? 1 : 0) + body.Length;
        Span<byte> payload = payloadLength <= 256
            ? stackalloc byte[payloadLength]
            : new byte[payloadLength];

        payload[0] = PayloadMarker;
        payload[1] = (byte)type;
        int offset = 2;
        if (sequence.HasValue)
        {
            payload[offset++] = sequence.Value;
        }

        body.CopyTo(payload[offset..]);

        var packet = new byte[2 + 4 + payloadLength];
        packet[0] = HeaderB;
        packet[1] = HeaderE;

        uint crc = BattlEyeCrc32.Compute(payload);
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(2, 4), crc);
        payload.CopyTo(packet.AsSpan(6));
        return packet;
    }

    /// <summary>Tries to parse a received datagram.</summary>
    public static bool TryParse(ReadOnlySpan<byte> datagram, out BattlEyeParsedPacket packet)
    {
        packet = default;
        if (datagram.Length < 8 || datagram[0] != HeaderB || datagram[1] != HeaderE)
        {
            return false;
        }

        if (datagram[6] != PayloadMarker)
        {
            return false;
        }

        uint expectedCrc = BinaryPrimitives.ReadUInt32LittleEndian(datagram.Slice(2, 4));
        uint actualCrc = BattlEyeCrc32.Compute(datagram[6..]);
        if (expectedCrc != actualCrc)
        {
            return false;
        }

        var type = (BattlEyePacketType)datagram[7];
        switch (type)
        {
            case BattlEyePacketType.Login:
                if (datagram.Length < 9)
                {
                    return false;
                }

                packet = new BattlEyeParsedPacket(
                    type,
                    Sequence: null,
                    LoginSuccess: datagram[8] == 0x01,
                    Message: null,
                    IsMultipart: false,
                    MultipartTotal: 0,
                    MultipartIndex: 0);
                return true;

            case BattlEyePacketType.Command:
                return TryParseCommand(datagram, out packet);

            case BattlEyePacketType.ServerMessage:
                if (datagram.Length < 9)
                {
                    return false;
                }

                string serverMessage = Encoding.UTF8.GetString(datagram[9..]);
                packet = new BattlEyeParsedPacket(
                    type,
                    Sequence: datagram[8],
                    LoginSuccess: null,
                    Message: serverMessage,
                    IsMultipart: false,
                    MultipartTotal: 0,
                    MultipartIndex: 0);
                return true;

            default:
                return false;
        }
    }

    private static bool TryParseCommand(ReadOnlySpan<byte> datagram, out BattlEyeParsedPacket packet)
    {
        packet = default;
        if (datagram.Length < 9)
        {
            return false;
        }

        byte sequence = datagram[8];

        // Multipart: FF 01 seq 00 total index data...
        if (datagram.Length >= 12 && datagram[9] == 0x00)
        {
            byte total = datagram[10];
            byte index = datagram[11];
            string part = Encoding.UTF8.GetString(datagram[12..]);
            packet = new BattlEyeParsedPacket(
                BattlEyePacketType.Command,
                sequence,
                LoginSuccess: null,
                Message: part,
                IsMultipart: true,
                MultipartTotal: total,
                MultipartIndex: index);
            return true;
        }

        string message = datagram.Length > 9
            ? Encoding.UTF8.GetString(datagram[9..])
            : string.Empty;

        packet = new BattlEyeParsedPacket(
            BattlEyePacketType.Command,
            sequence,
            LoginSuccess: null,
            Message: message,
            IsMultipart: false,
            MultipartTotal: 0,
            MultipartIndex: 0);
        return true;
    }
}

/// <summary>
/// A parsed BattlEye RCon packet.
/// </summary>
public readonly record struct BattlEyeParsedPacket(
    BattlEyePacketType Type,
    byte? Sequence,
    bool? LoginSuccess,
    string? Message,
    bool IsMultipart,
    byte MultipartTotal,
    byte MultipartIndex);
