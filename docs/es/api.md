# Referencia API

Namespaces: `BattleNET` (Core), `BattleNET.Commands` (helpers / parsers).

## BattlEyeClient

```csharp
await using var client = new BattlEyeClient(credentials, options);
await client.ConnectAsync();
await client.SendCommandAsync("missions");
string reply = await client.SendCommandAndWaitAsync("players");
```

### Opciones importantes

| Propiedad | Default | Descripción |
|-----------|---------|-------------|
| `ReceiveTimeout` | 5s | Timeout de login |
| `KeepAliveInterval` | 5s | Keep-alive en idle |
| `ConnectionLostTimeout` | 20s | Pérdida de conexión |
| `CommandTimeout` | 10s | Espera de respuesta |
| `ReconnectOnPacketLoss` | `false` | Reconexión automática |

### Eventos

`Connected` · `MessageReceived` · `Disconnected`

`Id`: `0`–`255` = secuencia de comando, `-1` = mensaje del servidor.

## Commands

```csharp
await client.GetPlayersAsync();
await client.KickAsync(id, reason);
await client.SayAllAsync("Hello");
```

Modelos: `BattlEyePlayer`, `BattlEyeBan`. Usa siempre `await using`.
