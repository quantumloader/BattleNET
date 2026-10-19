# Migracja z BattleNET 1.3.x do 2.0

v2 to **niekompatybilny rewrite**.

| 1.3.x | 2.0 |
|-------|-----|
| `BattleNET` (netstandard2.0) | `BattleNET.Core` + `BattleNET.Commands` (net9.0) |
| synchroniczne `Connect()` | `ConnectAsync()` |
| `BattlEyeLoginCredentials` | `BattlEyeCredentials` |
| `BattlEyeMessageReceived` | `MessageReceived` |

```csharp
await using var client = new BattlEyeClient(credentials);
await client.ConnectAsync();
```

Czyszczenie UDP w v2 jest niezawodne (bez wycieków portów).
