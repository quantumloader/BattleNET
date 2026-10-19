namespace BattleNET.Commands;

/// <summary>
/// A ban entry from the BattlEye <c>bans</c> command.
/// </summary>
public sealed record BattlEyeBan(
    int Id,
    string GuidOrIp,
    int Minutes,
    string Reason);
