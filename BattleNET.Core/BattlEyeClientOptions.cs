namespace BattleNET;

/// <summary>
/// Tunable options for <see cref="BattlEyeClient"/>.
/// </summary>
public sealed class BattlEyeClientOptions
{
    /// <summary>Receive timeout for a single UDP datagram during login.</summary>
    public TimeSpan ReceiveTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>How often to send a keep-alive empty command when idle.</summary>
    public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>If no packet is received for this long, treat the connection as lost.</summary>
    public TimeSpan ConnectionLostTimeout { get; set; } = TimeSpan.FromSeconds(20);

    /// <summary>Default timeout when waiting for a command reply.</summary>
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Automatically reconnect after connection loss.</summary>
    public bool ReconnectOnPacketLoss { get; set; }

    /// <summary>Maximum reconnect attempts (0 = unlimited while enabled).</summary>
    public int MaxReconnectAttempts { get; set; } = 10;

    /// <summary>Initial reconnect delay; doubles with backoff up to <see cref="MaxReconnectDelay"/>.</summary>
    public TimeSpan InitialReconnectDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Upper bound for reconnect backoff.</summary>
    public TimeSpan MaxReconnectDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>UDP socket receive buffer size.</summary>
    public int ReceiveBufferSize { get; set; } = 64 * 1024;

    /// <summary>UDP socket send buffer size.</summary>
    public int SendBufferSize { get; set; } = 64 * 1024;
}
