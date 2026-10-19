namespace BattleNET;

/// <summary>
/// Result of a connection / login attempt.
/// </summary>
public enum BattlEyeConnectionResult
{
    Success,
    ConnectionFailed,
    InvalidLogin,
    Cancelled,
}
