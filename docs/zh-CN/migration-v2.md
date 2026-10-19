# 从 BattleNET 1.3.x 迁移到 2.0

v2 是**破坏性重写**，没有 1.3.x 兼容层。

## 包结构

| 1.3.x | 2.0 |
|-------|-----|
| 单个 `BattleNET`（netstandard2.0） | `BattleNET.Core` + `BattleNET.Commands`（net9.0） |
| `BattleNET client`（.NET Framework 4.6.1） | `BattleNET.Cli` |

## 连接

```csharp
// 1.3.x
b.Connect();

// 2.0
await client.ConnectAsync();
```

## 凭据与命令

```csharp
// 1.3.x
new BattlEyeLoginCredentials { Host = ip, Port = 2302, Password = "..." };
b.SendCommand("players");

// 2.0
new BattlEyeCredentials("127.0.0.1", 2302, "...");
await client.SendCommandAndWaitAsync("players");
await client.GetPlayersAsync();
```

## 事件重命名

`BattlEyeMessageReceived` → `MessageReceived`  
`BattlEyeConnected` → `Connected`  
`BattlEyeDisconnected` → `Disconnected`

## 套接字清理

```csharp
await using var client = new BattlEyeClient(credentials);
```

v1 在重连/退出时经常泄漏 UDP；v2 会取消循环并释放 `UdpClient`。
