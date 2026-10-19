# 快速开始

## 要求

- [.NET 9 SDK](https://dotnet.microsoft.com/download)（编译）或 [.NET 9 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)（运行 CLI）
- Visual Studio 2022 17.12+（或 `dotnet` CLI）。.NET 10 SDK 需要 VS 2026 / MSBuild 18
- 已启用 BattlEye RCon 的 DayZ / Arma 服务器（`RConPort`、`RConPassword`）

## 安装包

```bash
dotnet add package BattleNET.Core
dotnet add package BattleNET.Commands
```

也可从 [Releases](https://github.com/quantumloader/BattleNET/releases) 下载 `BattleNET.Libraries-*-net9.0.zip`。

## 最小示例

```csharp
using BattleNET;
using BattleNET.Commands;

var credentials = new BattlEyeCredentials(
    host: "127.0.0.1",
    port: 2302,          // RCon UDP 端口（不是游戏端口）
    password: "secret");

await using var client = new BattlEyeClient(credentials);

client.MessageReceived += (_, e) =>
    Console.WriteLine($"[{e.Id}] {e.Message}");

var result = await client.ConnectAsync();
if (result != BattlEyeConnectionResult.Success)
{
    Console.WriteLine($"连接失败: {result}");
    return;
}

string playersRaw = await client.SendCommandAndWaitAsync("players");
Console.WriteLine(playersRaw);

foreach (var player in await client.GetPlayersAsync())
    Console.WriteLine($"{player.Id}: {player.Name} ({player.Guid})");

await client.SayAllAsync("来自 BattleNET 的问候");
await client.DisconnectAsync();
```

`await using` / `DisposeAsync` 会始终关闭 UDP 套接字，避免端口泄漏。

## 连接结果

| 结果 | 含义 |
|------|------|
| `Success` | 登录成功 |
| `InvalidLogin` | RCon 密码错误 |
| `ConnectionFailed` | 主机不可达 / 超时 |
| `Cancelled` | `CancellationToken` 已取消 |

## 下一步

- [API 参考](api.md)
- [CLI](cli.md)
