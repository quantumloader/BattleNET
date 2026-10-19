# Référence API

Namespaces : `BattleNET` (Core), `BattleNET.Commands` (helpers / parsers).

## BattlEyeClient

```csharp
await using var client = new BattlEyeClient(credentials, options);
await client.ConnectAsync();
await client.SendCommandAsync("missions");
string reply = await client.SendCommandAndWaitAsync("players");
```

### Options principales

| Propriété | Défaut | Description |
|-----------|--------|-------------|
| `ReceiveTimeout` | 5s | Timeout login |
| `KeepAliveInterval` | 5s | Keep-alive idle |
| `ConnectionLostTimeout` | 20s | Perte de connexion |
| `CommandTimeout` | 10s | Attente de réponse |
| `ReconnectOnPacketLoss` | `false` | Reconnexion auto |

### Événements

`Connected` · `MessageReceived` · `Disconnected`

`Id` : `0`–`255` = séquence commande, `-1` = message serveur.

## Commands

```csharp
await client.GetPlayersAsync();
await client.KickAsync(id, reason);
await client.SayAllAsync("Hello");
```

Modèles : `BattlEyePlayer`, `BattlEyeBan`. Toujours utiliser `await using`.
