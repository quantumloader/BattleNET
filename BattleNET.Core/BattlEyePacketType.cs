namespace BattleNET;

/// <summary>
/// BattlEye RCon payload type byte (after 0xFF).
/// </summary>
public enum BattlEyePacketType : byte
{
    Login = 0x00,
    Command = 0x01,
    ServerMessage = 0x02,
}
