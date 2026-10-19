namespace BattleNET;

/// <summary>
/// Exception thrown by <see cref="BattlEyeClient"/> operations.
/// </summary>
public sealed class BattlEyeException : Exception
{
    public BattlEyeException(string message)
        : base(message)
    {
    }

    public BattlEyeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
