using System.Globalization;
using System.Text.RegularExpressions;

namespace BattleNET.Commands;

/// <summary>
/// Parses the textual response of the BattlEye <c>players</c> command.
/// </summary>
public static partial class PlayerListParser
{
    // Example:
    // 0   127.0.0.1:2304    48  abcdef0123456789abcdef0123456789(OK) Player Name
    // 1   10.0.0.2:2304     64  deadbeefdeadbeefdeadbeefdeadbeef(OK) Someone (Lobby)
    [GeneratedRegex(
        @"^\s*(?<id>\d+)\s+(?<ip>\d{1,3}(?:\.\d{1,3}){3}):(?<port>\d+)\s+(?<ping>\d+)\s+(?<guid>[0-9a-fA-F]{32})(?:\([^)]*\))?\s+(?<name>.+?)\s*$",
        RegexOptions.CultureInvariant | RegexOptions.Multiline)]
    private static partial Regex PlayerLineRegex();

    /// <summary>Parses player rows from a raw RCon reply.</summary>
    public static IReadOnlyList<BattlEyePlayer> Parse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return [];
        }

        var players = new List<BattlEyePlayer>();
        foreach (Match match in PlayerLineRegex().Matches(response))
        {
            string name = match.Groups["name"].Value.Trim();
            bool lobby = name.EndsWith("(Lobby)", StringComparison.OrdinalIgnoreCase);
            if (lobby)
            {
                name = name[..^"(Lobby)".Length].TrimEnd();
            }

            players.Add(new BattlEyePlayer(
                Id: int.Parse(match.Groups["id"].Value, CultureInfo.InvariantCulture),
                Ip: match.Groups["ip"].Value,
                Port: int.Parse(match.Groups["port"].Value, CultureInfo.InvariantCulture),
                Ping: int.Parse(match.Groups["ping"].Value, CultureInfo.InvariantCulture),
                Guid: match.Groups["guid"].Value,
                Name: name,
                IsLobby: lobby));
        }

        return players;
    }
}
