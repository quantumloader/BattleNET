namespace BattleNET;

/// <summary>
/// Raised when the client disconnects.
/// </summary>
public sealed class BattlEyeDisconnectEventArgs : EventArgs
{
    public BattlEyeDisconnectEventArgs(BattlEyeCredentials credentials, BattlEyeDisconnectionType type)
    {
        Credentials = credentials;
        Type = type;
    }

    public BattlEyeCredentials Credentials { get; }
    public BattlEyeDisconnectionType Type { get; }

    public string Message => Type switch
    {
        BattlEyeDisconnectionType.Manual => "Disconnected.",
        BattlEyeDisconnectionType.ConnectionLost => "Connection lost (timeout).",
        BattlEyeDisconnectionType.SocketError => "Disconnected due to a socket error.",
        BattlEyeDisconnectionType.InvalidLogin => "Disconnected: invalid login.",
        BattlEyeDisconnectionType.Disposed => "Client disposed.",
        _ => Type.ToString(),
    };
}
