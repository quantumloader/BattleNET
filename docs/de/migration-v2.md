# Migration von BattleNET 1.3.x nach 2.0

v2 ist ein **Breaking Rewrite** ohne Kompatibilitätsschicht.

| 1.3.x | 2.0 |
|-------|-----|
| `BattleNET` (netstandard2.0) | `BattleNET.Core` + `BattleNET.Commands` (net9.0) |
| `Connect()` synchron | `ConnectAsync()` |
| `BattlEyeLoginCredentials` | `BattlEyeCredentials` |
| `BattlEyeMessageReceived` | `MessageReceived` |
| Rückgabe `256` bei Fehler | Exceptions / Result-Enums |

```csharp
await using var client = new BattlEyeClient(credentials);
await client.ConnectAsync();
```

Socket-Cleanup ist in v2 zuverlässig (kein UDP-Leak mehr bei Reconnect/Exit).
