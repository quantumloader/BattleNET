using System.Net;

namespace BattleNET;

/// <summary>
/// Login credentials for a BattlEye RCon endpoint.
/// </summary>
public sealed record BattlEyeCredentials
{
    public BattlEyeCredentials(string host, int port, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(port, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, 65535);
        ArgumentNullException.ThrowIfNull(password);

        Host = host;
        Port = port;
        Password = password;
    }

    public BattlEyeCredentials(IPAddress host, int port, string password)
        : this(host.ToString(), port, password)
    {
    }

    /// <summary>Hostname or IP address.</summary>
    public string Host { get; init; }

    /// <summary>RCon UDP port.</summary>
    public int Port { get; init; }

    /// <summary>RCon password.</summary>
    public string Password { get; init; }
}
