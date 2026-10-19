# はじめに

## 要件

- [.NET 9 SDK](https://dotnet.microsoft.com/download)（ビルド）または [.NET 9 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)（CLI）
- Visual Studio 2022 17.12+（または `dotnet` CLI）。.NET 10 SDK は VS 2026 / MSBuild 18 が必要
- BattlEye RCon が有効な DayZ / Arma サーバー（`RConPort`、`RConPassword`）

## パッケージのインストール

```bash
dotnet add package BattleNET.Core
dotnet add package BattleNET.Commands
```

または [Releases](https://github.com/quantumloader/BattleNET/releases) の `BattleNET.Libraries-*-net9.0.zip`。

## 最小例

```csharp
using BattleNET;
using BattleNET.Commands;

var credentials = new BattlEyeCredentials("127.0.0.1", 2302, "secret");
await using var client = new BattlEyeClient(credentials);

client.MessageReceived += (_, e) => Console.WriteLine($"[{e.Id}] {e.Message}");

if (await client.ConnectAsync() != BattlEyeConnectionResult.Success)
{
    Console.WriteLine("接続失敗");
    return;
}

foreach (var player in await client.GetPlayersAsync())
    Console.WriteLine($"{player.Id}: {player.Name}");

await client.SayAllAsync("BattleNET からこんにちは");
await client.DisconnectAsync();
```

`await using` は UDP ソケットを確実に閉じます。

| 結果 | 意味 |
|------|------|
| `Success` | ログイン成功 |
| `InvalidLogin` | RCon パスワード不一致 |
| `ConnectionFailed` | 到達不可 / タイムアウト |
| `Cancelled` | キャンセル |

次へ: [API](api.md) · [CLI](cli.md)
