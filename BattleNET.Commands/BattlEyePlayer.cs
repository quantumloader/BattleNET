namespace BattleNET.Commands;

/// <summary>
/// A player entry from the BattlEye <c>players</c> command.
/// </summary>
public sealed record BattlEyePlayer(
    int Id,
    string Ip,
    int Port,
    int Ping,
    string Guid,
    string Name,
    bool IsLobby);
