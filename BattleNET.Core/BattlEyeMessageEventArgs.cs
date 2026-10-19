namespace BattleNET;

/// <summary>
/// Raised when a message is received from the RCon server.
/// </summary>
public sealed class BattlEyeMessageEventArgs : EventArgs
{
    public BattlEyeMessageEventArgs(string message, int id)
    {
        Message = message;
        Id = id;
    }

    /// <summary>Decoded message text.</summary>
    public string Message { get; }

    /// <summary>
    /// Command sequence id (0–255), or <see cref="ServerMessageId"/> for unsolicited server messages.
    /// </summary>
    public int Id { get; }

    /// <summary>Id used for unsolicited server messages.</summary>
    public const int ServerMessageId = -1;
}
