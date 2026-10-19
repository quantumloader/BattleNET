# Migration depuis BattleNET 1.3.x vers 2.0

v2 est une **réécriture incompatible**.

| 1.3.x | 2.0 |
|-------|-----|
| `BattleNET` (netstandard2.0) | `BattleNET.Core` + `BattleNET.Commands` (net9.0) |
| `Connect()` synchrone | `ConnectAsync()` |
| `BattlEyeLoginCredentials` | `BattlEyeCredentials` |
| `BattlEyeMessageReceived` | `MessageReceived` |

```csharp
await using var client = new BattlEyeClient(credentials);
await client.ConnectAsync();
```

Le nettoyage UDP est fiable en v2 (plus de fuite de ports).
