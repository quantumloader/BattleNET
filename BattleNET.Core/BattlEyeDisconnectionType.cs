namespace BattleNET;

/// <summary>
/// Why the client disconnected.
/// </summary>
public enum BattlEyeDisconnectionType
{
    Manual,
    ConnectionLost,
    SocketError,
    InvalidLogin,
    Disposed,
}
