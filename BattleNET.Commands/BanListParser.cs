using System.Globalization;
using System.Text.RegularExpressions;

namespace BattleNET.Commands;

/// <summary>
/// Parses the textual response of the BattlEye <c>bans</c> command.
/// </summary>
public static partial class BanListParser
{
    // Examples:
    // 0  abcdef0123456789abcdef0123456789  perm  Cheating
    // 1  127.0.0.1  60  Spam
    [GeneratedRegex(
        @"^\s*(?<id>\d+)\s+(?<target>[0-9a-fA-F]{32}|\d{1,3}(?:\.\d{1,3}){3})\s+(?<minutes>perm|\d+)\s+(?<reason>.*)$",
        RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex BanLineRegex();

    /// <summary>Parses ban rows from a raw RCon reply.</summary>
    public static IReadOnlyList<BattlEyeBan> Parse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return [];
        }

        var bans = new List<BattlEyeBan>();
        foreach (Match match in BanLineRegex().Matches(response))
        {
            string minutesRaw = match.Groups["minutes"].Value;
            int minutes = minutesRaw.Equals("perm", StringComparison.OrdinalIgnoreCase)
                ? 0
                : int.Parse(minutesRaw, CultureInfo.InvariantCulture);

            bans.Add(new BattlEyeBan(
                Id: int.Parse(match.Groups["id"].Value, CultureInfo.InvariantCulture),
                GuidOrIp: match.Groups["target"].Value,
                Minutes: minutes,
                Reason: match.Groups["reason"].Value.Trim()));
        }

        return bans;
    }
}
