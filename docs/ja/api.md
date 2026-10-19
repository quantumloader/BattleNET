# API リファレンス

名前空間: `BattleNET`（Core）、`BattleNET.Commands`（ヘルパー / パーサー）。

## BattlEyeClient

```csharp
await using var client = new BattlEyeClient(credentials, options);
await client.ConnectAsync();
await client.SendCommandAsync("missions");
string reply = await client.SendCommandAndWaitAsync("players");
```

### 主なオプション

| プロパティ | 既定 | 説明 |
|------------|------|------|
| `ReceiveTimeout` | 5s | ログイン受信タイムアウト |
| `KeepAliveInterval` | 5s | アイドル時 keep-alive |
| `ConnectionLostTimeout` | 20s | 接続喪失判定 |
| `CommandTimeout` | 10s | 応答待ち |
| `ReconnectOnPacketLoss` | `false` | 自動再接続 |

### イベント

`Connected` · `MessageReceived` · `Disconnected`

`Id`: `0`–`255` = コマンドシーケンス、`-1` = サーバーメッセージ。

## Commands

```csharp
await client.GetPlayersAsync();
await client.KickAsync(id, reason);
await client.SayAllAsync("Hello");
```

モデル: `BattlEyePlayer`、`BattlEyeBan`。常に `await using` を使用してください。
