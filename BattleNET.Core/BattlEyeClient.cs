using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BattleNET;

/// <summary>
/// Asynchronous BattlEye RCon client with proper UDP socket cleanup.
/// </summary>
public sealed class BattlEyeClient : IAsyncDisposable, IDisposable
{
    private readonly BattlEyeCredentials _credentials;
    private readonly BattlEyeClientOptions _options;
    private readonly ConcurrentDictionary<byte, PendingCommand> _pending = new();
    private readonly ConcurrentDictionary<byte, MultipartBuffer> _multiparts = new();
    private readonly object _socketGate = new();

    private UdpClient? _udp;
    private CancellationTokenSource? _lifetimeCts;
    private Task? _receiveLoop;
    private Task? _keepAliveLoop;
    private int _sequence;
    private DateTime _lastSentUtc = DateTime.UtcNow;
    private DateTime _lastReceivedUtc = DateTime.UtcNow;
    private int _connected;
    private int _disposed;
    private int _disconnectRaised;

    /// <summary>Creates a client for the given credentials.</summary>
    public BattlEyeClient(BattlEyeCredentials credentials, BattlEyeClientOptions? options = null)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        _options = options ?? new BattlEyeClientOptions();
    }

    /// <summary>Whether the client currently considers itself connected.</summary>
    public bool IsConnected => Interlocked.CompareExchange(ref _connected, 0, 0) == 1;

    /// <summary>Number of commands waiting for a reply.</summary>
    public int PendingCommandCount => _pending.Count;

    /// <summary>Credentials used by this client.</summary>
    public BattlEyeCredentials Credentials => _credentials;

    /// <summary>Options used by this client.</summary>
    public BattlEyeClientOptions Options => _options;

    /// <summary>Raised when a message is received.</summary>
    public event EventHandler<BattlEyeMessageEventArgs>? MessageReceived;

    /// <summary>Raised after connect/login completes.</summary>
    public event EventHandler<BattlEyeConnectEventArgs>? Connected;

    /// <summary>Raised when the client disconnects.</summary>
    public event EventHandler<BattlEyeDisconnectEventArgs>? Disconnected;

    /// <summary>Connects and logs in to the RCon server.</summary>
    public async Task<BattlEyeConnectionResult> ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, this);

        await DisconnectInternalAsync(raiseEvent: false, BattlEyeDisconnectionType.Manual).ConfigureAwait(false);

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(_credentials.Host, cancellationToken)
                .ConfigureAwait(false);
            if (addresses.Length == 0)
            {
                RaiseConnected(BattlEyeConnectionResult.ConnectionFailed);
                return BattlEyeConnectionResult.ConnectionFailed;
            }

            IPAddress address = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
                               ?? addresses[0];
            var endpoint = new IPEndPoint(address, _credentials.Port);

            var udp = CreateUdpClient(endpoint);
            lock (_socketGate)
            {
                _udp = udp;
            }

            byte[] loginPacket = BattlEyeProtocol.BuildLoginPacket(_credentials.Password);
            await SendRawAsync(loginPacket, cancellationToken).ConfigureAwait(false);

            using var loginCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            loginCts.CancelAfter(_options.ReceiveTimeout);

            UdpReceiveResult datagram;
            try
            {
                datagram = await udp.ReceiveAsync(loginCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                await DisposeSocketAsync().ConfigureAwait(false);
                RaiseConnected(BattlEyeConnectionResult.ConnectionFailed);
                return BattlEyeConnectionResult.ConnectionFailed;
            }

            if (!BattlEyeProtocol.TryParse(datagram.Buffer, out var parsed)
                || parsed.Type != BattlEyePacketType.Login)
            {
                await DisposeSocketAsync().ConfigureAwait(false);
                RaiseConnected(BattlEyeConnectionResult.ConnectionFailed);
                return BattlEyeConnectionResult.ConnectionFailed;
            }

            if (parsed.LoginSuccess != true)
            {
                await DisposeSocketAsync().ConfigureAwait(false);
                RaiseConnected(BattlEyeConnectionResult.InvalidLogin);
                return BattlEyeConnectionResult.InvalidLogin;
            }

            _lastSentUtc = DateTime.UtcNow;
            _lastReceivedUtc = DateTime.UtcNow;
            Interlocked.Exchange(ref _sequence, 0);
            Interlocked.Exchange(ref _disconnectRaised, 0);
            Interlocked.Exchange(ref _connected, 1);

            _lifetimeCts = new CancellationTokenSource();
            CancellationToken life = _lifetimeCts.Token;
            _receiveLoop = Task.Run(() => ReceiveLoopAsync(life), CancellationToken.None);
            _keepAliveLoop = Task.Run(() => KeepAliveLoopAsync(life), CancellationToken.None);

            RaiseConnected(BattlEyeConnectionResult.Success);
            return BattlEyeConnectionResult.Success;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await DisposeSocketAsync().ConfigureAwait(false);
            RaiseConnected(BattlEyeConnectionResult.Cancelled);
            return BattlEyeConnectionResult.Cancelled;
        }
        catch (Exception)
        {
            await DisposeSocketAsync().ConfigureAwait(false);
            RaiseConnected(BattlEyeConnectionResult.ConnectionFailed);
            return BattlEyeConnectionResult.ConnectionFailed;
        }
    }

    /// <summary>
    /// Sends a command and returns its sequence number without waiting for a reply.
    /// </summary>
    public async Task<byte> SendCommandAsync(string? command, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        byte seq = NextSequence();
        byte[] packet = BattlEyeProtocol.BuildCommandPacket(seq, command);
        await SendRawAsync(packet, cancellationToken).ConfigureAwait(false);
        return seq;
    }

    /// <summary>
    /// Sends a command and waits for the matching reply (including multipart aggregation).
    /// </summary>
    public async Task<string> SendCommandAndWaitAsync(
        string command,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        EnsureConnected();

        byte seq = NextSequence();
        var pending = new PendingCommand();
        if (!_pending.TryAdd(seq, pending))
        {
            throw new BattlEyeException($"Sequence {seq} is already pending.");
        }

        try
        {
            byte[] packet = BattlEyeProtocol.BuildCommandPacket(seq, command);
            await SendRawAsync(packet, cancellationToken).ConfigureAwait(false);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout ?? _options.CommandTimeout);

            return await pending.Completion.Task.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Timed out waiting for reply to command sequence {seq}.");
        }
        finally
        {
            _pending.TryRemove(seq, out _);
            pending.Completion.TrySetCanceled();
        }
    }

    /// <summary>Disconnects and releases the UDP socket.</summary>
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return DisconnectInternalAsync(raiseEvent: true, BattlEyeDisconnectionType.Manual);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        await DisconnectInternalAsync(raiseEvent: true, BattlEyeDisconnectionType.Disposed)
            .ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    private UdpClient CreateUdpClient(IPEndPoint endpoint)
    {
        var udp = new UdpClient(endpoint.AddressFamily);
        udp.Client.ReceiveBufferSize = _options.ReceiveBufferSize;
        udp.Client.SendBufferSize = _options.SendBufferSize;
        udp.Connect(endpoint);
        return udp;
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && IsConnected)
        {
            UdpClient? udp;
            lock (_socketGate)
            {
                udp = _udp;
            }

            if (udp is null)
            {
                break;
            }

            try
            {
                UdpReceiveResult datagram = await udp.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                _lastReceivedUtc = DateTime.UtcNow;
                HandleDatagram(datagram.Buffer);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException)
            {
                await HandleConnectionLostAsync(BattlEyeDisconnectionType.SocketError).ConfigureAwait(false);
                break;
            }
            catch (Exception)
            {
                await HandleConnectionLostAsync(BattlEyeDisconnectionType.SocketError).ConfigureAwait(false);
                break;
            }
        }
    }

    private async Task KeepAliveLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!IsConnected)
                {
                    break;
                }

                var now = DateTime.UtcNow;
                if (now - _lastReceivedUtc >= _options.ConnectionLostTimeout)
                {
                    await HandleConnectionLostAsync(BattlEyeDisconnectionType.ConnectionLost)
                        .ConfigureAwait(false);
                    break;
                }

                if (now - _lastSentUtc >= _options.KeepAliveInterval && _pending.IsEmpty)
                {
                    try
                    {
                        await SendCommandAsync(null, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        await HandleConnectionLostAsync(BattlEyeDisconnectionType.SocketError)
                            .ConfigureAwait(false);
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // expected on shutdown
        }
    }

    private void HandleDatagram(byte[] buffer)
    {
        if (!BattlEyeProtocol.TryParse(buffer, out var parsed))
        {
            return;
        }

        switch (parsed.Type)
        {
            case BattlEyePacketType.ServerMessage:
                if (parsed.Sequence is byte serverSeq)
                {
                    _ = SendAcknowledgeFireAndForget(serverSeq);
                }

                RaiseMessage(parsed.Message ?? string.Empty, BattlEyeMessageEventArgs.ServerMessageId);
                break;

            case BattlEyePacketType.Command:
                HandleCommandReply(parsed);
                break;
        }
    }

    private void HandleCommandReply(BattlEyeParsedPacket parsed)
    {
        if (parsed.Sequence is not byte seq)
        {
            return;
        }

        string message;
        if (parsed.IsMultipart)
        {
            var buffer = _multiparts.GetOrAdd(seq, _ => new MultipartBuffer(parsed.MultipartTotal));
            buffer.Add(parsed.MultipartIndex, parsed.Message ?? string.Empty);
            if (!buffer.IsComplete)
            {
                return;
            }

            _multiparts.TryRemove(seq, out _);
            message = buffer.Combine();
        }
        else
        {
            message = parsed.Message ?? string.Empty;
        }

        if (_pending.TryRemove(seq, out var pending))
        {
            pending.Completion.TrySetResult(message);
        }

        RaiseMessage(message, seq);
    }

    private async Task SendAcknowledgeFireAndForget(byte sequence)
    {
        try
        {
            byte[] packet = BattlEyeProtocol.BuildAcknowledgePacket(sequence);
            await SendRawAsync(packet, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // ignore acknowledge failures; keep-alive / timeout will handle loss
        }
    }

    private async Task HandleConnectionLostAsync(BattlEyeDisconnectionType type)
    {
        if (!IsConnected)
        {
            return;
        }

        await DisconnectInternalAsync(raiseEvent: true, type).ConfigureAwait(false);

        if (!_options.ReconnectOnPacketLoss || Volatile.Read(ref _disposed) == 1)
        {
            return;
        }

        TimeSpan delay = _options.InitialReconnectDelay;
        int attempts = _options.MaxReconnectAttempts;
        int tryCount = 0;

        while (Volatile.Read(ref _disposed) == 0
               && (attempts == 0 || tryCount < attempts))
        {
            tryCount++;
            try
            {
                await Task.Delay(delay).ConfigureAwait(false);
                var result = await ConnectAsync().ConfigureAwait(false);
                if (result == BattlEyeConnectionResult.Success)
                {
                    return;
                }
            }
            catch
            {
                // keep trying
            }

            delay = TimeSpan.FromMilliseconds(
                Math.Min(delay.TotalMilliseconds * 2, _options.MaxReconnectDelay.TotalMilliseconds));
        }
    }

    private async Task DisconnectInternalAsync(bool raiseEvent, BattlEyeDisconnectionType type)
    {
        Interlocked.Exchange(ref _connected, 0);

        CancellationTokenSource? cts = Interlocked.Exchange(ref _lifetimeCts, null);
        if (cts is not null)
        {
            try
            {
                await cts.CancelAsync().ConfigureAwait(false);
            }
            catch
            {
                // ignore
            }
        }

        Task? receive = Interlocked.Exchange(ref _receiveLoop, null);
        Task? keepAlive = Interlocked.Exchange(ref _keepAliveLoop, null);

        await DisposeSocketAsync().ConfigureAwait(false);

        if (receive is not null)
        {
            try { await receive.ConfigureAwait(false); } catch { /* ignore */ }
        }

        if (keepAlive is not null)
        {
            try { await keepAlive.ConfigureAwait(false); } catch { /* ignore */ }
        }

        cts?.Dispose();

        foreach (var pair in _pending)
        {
            if (_pending.TryRemove(pair.Key, out var pending))
            {
                pending.Completion.TrySetException(
                    new BattlEyeException("Disconnected before a reply was received."));
            }
        }

        _multiparts.Clear();

        if (raiseEvent && Interlocked.Exchange(ref _disconnectRaised, 1) == 0)
        {
            Disconnected?.Invoke(this, new BattlEyeDisconnectEventArgs(_credentials, type));
        }
    }

    private Task DisposeSocketAsync()
    {
        UdpClient? udp;
        lock (_socketGate)
        {
            udp = _udp;
            _udp = null;
        }

        if (udp is null)
        {
            return Task.CompletedTask;
        }

        try
        {
            udp.Close();
        }
        catch
        {
            // ignore
        }

        udp.Dispose();
        return Task.CompletedTask;
    }

    private async Task SendRawAsync(byte[] packet, CancellationToken cancellationToken)
    {
        UdpClient? udp;
        lock (_socketGate)
        {
            udp = _udp;
        }

        if (udp is null)
        {
            throw new BattlEyeException("Not connected.");
        }

        await udp.SendAsync(packet, cancellationToken).ConfigureAwait(false);
        _lastSentUtc = DateTime.UtcNow;
    }

    private byte NextSequence()
    {
        int next = Interlocked.Increment(ref _sequence) - 1;
        return (byte)(next & 0xFF);
    }

    private void EnsureConnected()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, this);
        if (!IsConnected)
        {
            throw new BattlEyeException("Not connected.");
        }
    }

    private void RaiseConnected(BattlEyeConnectionResult result)
    {
        Connected?.Invoke(this, new BattlEyeConnectEventArgs(_credentials, result));
    }

    private void RaiseMessage(string message, int id)
    {
        MessageReceived?.Invoke(this, new BattlEyeMessageEventArgs(message, id));
    }

    private sealed class PendingCommand
    {
        public TaskCompletionSource<string> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class MultipartBuffer(byte total)
    {
        private readonly string?[] _parts = new string[Math.Max(total, (byte)1)];
        private int _received;

        public bool IsComplete => _received >= _parts.Length;

        public void Add(byte index, string part)
        {
            if (index >= _parts.Length)
            {
                return;
            }

            if (_parts[index] is null)
            {
                _parts[index] = part;
                Interlocked.Increment(ref _received);
            }
        }

        public string Combine()
        {
            var sb = new StringBuilder();
            foreach (string? part in _parts)
            {
                sb.Append(part);
            }

            return sb.ToString();
        }
    }
}
