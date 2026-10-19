# API reference

Namespaces:

- `BattleNET` — Core (`BattleNET.Core`)
- `BattleNET.Commands` — helpers & parsers (`BattleNET.Commands`)

---

## BattlEyeCredentials

```csharp
new BattlEyeCredentials(string host, int port, string password)
new BattlEyeCredentials(IPAddress host, int port, string password)
```

`host` may be a hostname or IP. DNS is resolved in `ConnectAsync`.

---

## BattlEyeClientOptions

| Property | Default | Description |
|----------|---------|-------------|
| `ReceiveTimeout` | 5s | Login receive timeout |
| `KeepAliveInterval` | 5s | Empty keep-alive command when idle |
| `ConnectionLostTimeout` | 20s | No inbound packet → connection lost |
| `CommandTimeout` | 10s | Default `SendCommandAndWaitAsync` timeout |
| `ReconnectOnPacketLoss` | `false` | Auto-reconnect with backoff |
| `MaxReconnectAttempts` | 10 | `0` = unlimited while reconnect enabled |
| `InitialReconnectDelay` | 1s | First reconnect delay |
| `MaxReconnectDelay` | 30s | Backoff cap |
| `ReceiveBufferSize` | 64 KB | UDP receive buffer |
| `SendBufferSize` | 64 KB | UDP send buffer |

---

## BattlEyeClient

```csharp
public BattlEyeClient(BattlEyeCredentials credentials, BattlEyeClientOptions? options = null)
```

Implements `IAsyncDisposable` and `IDisposable`.

### Properties

| Property | Description |
|----------|-------------|
| `IsConnected` | Connected and logged in |
| `PendingCommandCount` | Commands waiting for a reply |
| `Credentials` | Login credentials |
| `Options` | Client options |

### Methods

| Method | Description |
|--------|-------------|
| `ConnectAsync(ct)` | Connect + login → `BattlEyeConnectionResult` |
| `SendCommandAsync(command, ct)` | Fire-and-forget; returns sequence `byte` |
| `SendCommandAndWaitAsync(command, timeout?, ct)` | Wait for matching reply (multipart merged) |
| `DisconnectAsync(ct)` | Clean disconnect, dispose UDP socket |
| `Dispose` / `DisposeAsync` | Same cleanup as disconnect |

Empty / `null` command in `SendCommandAsync` is used as keep-alive (BattlEye empty command packet).

### Events

| Event | Args | When |
|-------|------|------|
| `Connected` | `BattlEyeConnectEventArgs` | After login attempt |
| `MessageReceived` | `BattlEyeMessageEventArgs` | Command reply or server message |
| `Disconnected` | `BattlEyeDisconnectEventArgs` | After disconnect / loss / dispose |

`BattlEyeMessageEventArgs.Id`:

- `0`–`255` — command sequence
- `ServerMessageId` (`-1`) — unsolicited server message (chat, BE notices, …)

---

## Protocol helpers

`BattlEyeProtocol` (public):

- `BuildLoginPacket` / `BuildCommandPacket` / `BuildAcknowledgePacket`
- `TryParse` → `BattlEyeParsedPacket`

Normally you do not call these directly; the client uses them internally.

---

## BattleNET.Commands

### Command strings — `BattlEyeCommand`

Constants and builders, e.g.:

```csharp
BattlEyeCommand.Players
BattlEyeCommand.Kick(playerId, reason)
BattlEyeCommand.Ban(playerId, minutes, reason)
BattlEyeCommand.SayAll("Hello")
BattlEyeCommand.Shutdown
```

### Extension methods on `BattlEyeClient`

| Method | Description |
|--------|-------------|
| `GetPlayersAsync` / `GetPlayersRawAsync` | `players` + optional parse |
| `GetBansAsync` / `GetBansRawAsync` | `bans` + optional parse |
| `KickAsync` / `BanAsync` / `AddBanAsync` / `RemoveBanAsync` | Moderation |
| `SayAsync` / `SayAllAsync` | Chat |
| `LockAsync` / `UnlockAsync` / `ShutdownAsync` | Server control |
| `GetAdminsAsync` | Connected RCon clients |

### Models & parsers

- `BattlEyePlayer` — `Id`, `Ip`, `Port`, `Ping`, `Guid`, `Name`, `IsLobby`
- `BattlEyeBan` — `Id`, `GuidOrIp`, `Minutes` (`0` = permanent), `Reason`
- `PlayerListParser.Parse(raw)`
- `BanListParser.Parse(raw)`

Parsers tolerate typical BE `players` / `bans` text layouts; exotic custom formats may need manual parsing of the raw string.

---

## Error handling

| Type | When |
|------|------|
| `BattlEyeException` | Not connected, disconnect mid-wait, etc. |
| `TimeoutException` | `SendCommandAndWaitAsync` timed out |
| `ObjectDisposedException` | Use after dispose |
| `OperationCanceledException` | Cancellation requested |

Always prefer `await using` so the socket is released even on exceptions.
