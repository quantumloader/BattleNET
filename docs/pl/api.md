# Referencja API

Przestrzenie nazw: `BattleNET` (Core), `BattleNET.Commands` (helpery / parsery).

## BattlEyeClient

```csharp
await using var client = new BattlEyeClient(credentials, options);
await client.ConnectAsync();
await client.SendCommandAsync("missions");
string reply = await client.SendCommandAndWaitAsync("players");
```

### Ważne opcje

| Właściwość | Domyślnie | Opis |
|------------|-----------|------|
| `ReceiveTimeout` | 5s | Timeout logowania |
| `KeepAliveInterval` | 5s | Keep-alive przy bezczynności |
| `ConnectionLostTimeout` | 20s | Utrata połączenia |
| `CommandTimeout` | 10s | Oczekiwanie na odpowiedź |
| `ReconnectOnPacketLoss` | `false` | Auto-reconnect |

### Zdarzenia

`Connected` · `MessageReceived` · `Disconnected`

`Id`: `0`–`255` = sekwencja komendy, `-1` = komunikat serwera.

## Commands

```csharp
await client.GetPlayersAsync();
await client.KickAsync(id, reason);
await client.SayAllAsync("Hello");
```

Modele: `BattlEyePlayer`, `BattlEyeBan`. Zawsze używaj `await using`.
