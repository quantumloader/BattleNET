# Migración desde BattleNET 1.3.x a 2.0

v2 es una **reescritura incompatible**.

| 1.3.x | 2.0 |
|-------|-----|
| `BattleNET` (netstandard2.0) | `BattleNET.Core` + `BattleNET.Commands` (net9.0) |
| `Connect()` síncrono | `ConnectAsync()` |
| `BattlEyeLoginCredentials` | `BattlEyeCredentials` |
| `BattlEyeMessageReceived` | `MessageReceived` |

```csharp
await using var client = new BattlEyeClient(credentials);
await client.ConnectAsync();
```

La limpieza UDP es fiable en v2 (sin fugas de puertos).
