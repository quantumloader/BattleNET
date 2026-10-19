# BattleNET 1.3.x から 2.0 への移行

v2 は**互換性のない書き換え**です。

| 1.3.x | 2.0 |
|-------|-----|
| `BattleNET`（netstandard2.0） | `BattleNET.Core` + `BattleNET.Commands`（net9.0） |
| 同期 `Connect()` | `ConnectAsync()` |
| `BattlEyeLoginCredentials` | `BattlEyeCredentials` |
| `BattlEyeMessageReceived` | `MessageReceived` |

```csharp
await using var client = new BattlEyeClient(credentials);
await client.ConnectAsync();
```

v2 では UDP のクリーンアップが確実です（ポートリークなし）。
