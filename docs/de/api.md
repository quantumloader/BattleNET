# API-Referenz

Namespaces: `BattleNET` (Core), `BattleNET.Commands` (Helper/Parser).

## BattlEyeClient

```csharp
await using var client = new BattlEyeClient(credentials, options);
await client.ConnectAsync();
await client.SendCommandAsync("missions");
string reply = await client.SendCommandAndWaitAsync("players");
await client.DisconnectAsync();
```

### Wichtige Optionen (`BattlEyeClientOptions`)

| Eigenschaft | Default | Beschreibung |
|-------------|---------|--------------|
| `ReceiveTimeout` | 5s | Login-Timeout |
| `KeepAliveInterval` | 5s | Keep-Alive im Idle |
| `ConnectionLostTimeout` | 20s | Verbindungsverlust |
| `CommandTimeout` | 10s | Wartezeit auf Antwort |
| `ReconnectOnPacketLoss` | `false` | Auto-Reconnect |

### Events

`Connected` · `MessageReceived` · `Disconnected`

`MessageReceived`-`Id`: `0`–`255` = Command-Sequenz, `-1` = Servermeldung.

## Commands

```csharp
await client.GetPlayersAsync();
await client.KickAsync(id, reason);
await client.SayAllAsync("Hello");
await client.BanAsync(id, minutes, reason);
```

Modelle: `BattlEyePlayer`, `BattlEyeBan`.

Fehler: `BattlEyeException`, `TimeoutException`, `ObjectDisposedException`. Immer `await using` verwenden.
