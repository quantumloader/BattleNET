namespace BattleNET.Commands;

/// <summary>
/// Well-known BattlEye / Arma / DayZ RCon command strings.
/// </summary>
public static class BattlEyeCommand
{
    public const string Init = "#init";
    public const string Shutdown = "#shutdown";
    public const string Reassign = "#reassign";
    public const string Restart = "#restart";
    public const string RestartServer = "#restartserver";
    public const string Lock = "#lock";
    public const string Unlock = "#unlock";
    public const string Missions = "missions";
    public const string Players = "players";
    public const string LoadBans = "loadBans";
    public const string LoadScripts = "loadScripts";
    public const string LoadEvents = "loadEvents";
    public const string Bans = "bans";
    public const string WriteBans = "writeBans";
    public const string Admins = "admins";

    public static string Mission(string missionName) => $"#mission {missionName}";
    public static string RConPassword(string password) => $"RConPassword {password}";
    public static string MaxPing(int ping) => $"MaxPing {ping}";
    public static string Kick(int playerId, string? reason = null)
        => reason is null or "" ? $"kick {playerId}" : $"kick {playerId} {reason}";
    public static string Say(int playerId, string message) => $"Say {playerId} {message}";
    public static string SayAll(string message) => Say(-1, message);
    public static string Ban(int playerId, int minutes = 0, string? reason = null)
        => reason is null or ""
            ? $"ban {playerId} {minutes}"
            : $"ban {playerId} {minutes} {reason}";
    public static string AddBan(string guid, int minutes = 0, string? reason = null)
        => reason is null or ""
            ? $"addBan {guid} {minutes}"
            : $"addBan {guid} {minutes} {reason}";
    public static string RemoveBan(int banId) => $"removeBan {banId}";
}
