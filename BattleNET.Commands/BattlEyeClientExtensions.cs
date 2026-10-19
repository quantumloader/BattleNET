namespace BattleNET.Commands;

/// <summary>
/// High-level typed helpers on top of <see cref="BattlEyeClient"/>.
/// </summary>
public static class BattlEyeClientExtensions
{
    public static Task<string> GetPlayersRawAsync(
        this BattlEyeClient client,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        => client.SendCommandAndWaitAsync(BattlEyeCommand.Players, timeout, cancellationToken);

    public static async Task<IReadOnlyList<BattlEyePlayer>> GetPlayersAsync(
        this BattlEyeClient client,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        string raw = await client.GetPlayersRawAsync(timeout, cancellationToken).ConfigureAwait(false);
        return PlayerListParser.Parse(raw);
    }

    public static Task<string> GetBansRawAsync(
        this BattlEyeClient client,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        => client.SendCommandAndWaitAsync(BattlEyeCommand.Bans, timeout, cancellationToken);

    public static async Task<IReadOnlyList<BattlEyeBan>> GetBansAsync(
        this BattlEyeClient client,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        string raw = await client.GetBansRawAsync(timeout, cancellationToken).ConfigureAwait(false);
        return BanListParser.Parse(raw);
    }

    public static Task<byte> KickAsync(
        this BattlEyeClient client,
        int playerId,
        string? reason = null,
        CancellationToken cancellationToken = default)
        => client.SendCommandAsync(BattlEyeCommand.Kick(playerId, reason), cancellationToken);

    public static Task<byte> BanAsync(
        this BattlEyeClient client,
        int playerId,
        int minutes = 0,
        string? reason = null,
        CancellationToken cancellationToken = default)
        => client.SendCommandAsync(BattlEyeCommand.Ban(playerId, minutes, reason), cancellationToken);

    public static Task<byte> AddBanAsync(
        this BattlEyeClient client,
        string guid,
        int minutes = 0,
        string? reason = null,
        CancellationToken cancellationToken = default)
        => client.SendCommandAsync(BattlEyeCommand.AddBan(guid, minutes, reason), cancellationToken);

    public static Task<byte> RemoveBanAsync(
        this BattlEyeClient client,
        int banId,
        CancellationToken cancellationToken = default)
        => client.SendCommandAsync(BattlEyeCommand.RemoveBan(banId), cancellationToken);

    public static Task<byte> SayAsync(
        this BattlEyeClient client,
        int playerId,
        string message,
        CancellationToken cancellationToken = default)
        => client.SendCommandAsync(BattlEyeCommand.Say(playerId, message), cancellationToken);

    public static Task<byte> SayAllAsync(
        this BattlEyeClient client,
        string message,
        CancellationToken cancellationToken = default)
        => client.SendCommandAsync(BattlEyeCommand.SayAll(message), cancellationToken);

    public static Task<byte> ShutdownAsync(
        this BattlEyeClient client,
        CancellationToken cancellationToken = default)
        => client.SendCommandAsync(BattlEyeCommand.Shutdown, cancellationToken);

    public static Task<byte> LockAsync(
        this BattlEyeClient client,
        CancellationToken cancellationToken = default)
        => client.SendCommandAsync(BattlEyeCommand.Lock, cancellationToken);

    public static Task<byte> UnlockAsync(
        this BattlEyeClient client,
        CancellationToken cancellationToken = default)
        => client.SendCommandAsync(BattlEyeCommand.Unlock, cancellationToken);

    public static Task<string> GetAdminsAsync(
        this BattlEyeClient client,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        => client.SendCommandAndWaitAsync(BattlEyeCommand.Admins, timeout, cancellationToken);
}
