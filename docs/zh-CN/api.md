# API 参考

命名空间：

- `BattleNET` — 核心（`BattleNET.Core`）
- `BattleNET.Commands` — 辅助方法与解析器（`BattleNET.Commands`）

---

## BattlEyeCredentials

```csharp
new BattlEyeCredentials(string host, int port, string password)
new BattlEyeCredentials(IPAddress host, int port, string password)
```

`host` 可为主机名或 IP。DNS 在 `ConnectAsync` 中解析。

---

## BattlEyeClientOptions

| 属性 | 默认值 | 说明 |
|------|--------|------|
| `ReceiveTimeout` | 5s | 登录接收超时 |
| `KeepAliveInterval` | 5s | 空闲保活 |
| `ConnectionLostTimeout` | 20s | 无入站包则判定断线 |
| `CommandTimeout` | 10s | `SendCommandAndWaitAsync` 默认超时 |
| `ReconnectOnPacketLoss` | `false` | 丢包后自动重连 |
| `MaxReconnectAttempts` | 10 | `0` = 不限制 |
| `ReceiveBufferSize` | 64 KB | UDP 接收缓冲 |
| `SendBufferSize` | 64 KB | UDP 发送缓冲 |

---

## BattlEyeClient

实现 `IAsyncDisposable` 与 `IDisposable`。

| 方法 | 说明 |
|------|------|
| `ConnectAsync` | 连接并登录 |
| `SendCommandAsync` | 发送不等待；返回序号 |
| `SendCommandAndWaitAsync` | 等待匹配回复（合并分片） |
| `DisconnectAsync` / `DisposeAsync` | 清理并关闭套接字 |

### 事件

| 事件 | 时机 |
|------|------|
| `Connected` | 登录尝试之后 |
| `MessageReceived` | 命令回复或服务器消息 |
| `Disconnected` | 断开 / 丢失 / dispose |

`Id`：`0`–`255` 为命令序号；`ServerMessageId`（`-1`）为服务器主动消息。

---

## BattleNET.Commands

```csharp
await client.GetPlayersAsync();
await client.KickAsync(playerId, reason);
await client.SayAllAsync("Hello");
await client.BanAsync(playerId, minutes, reason);
```

模型：`BattlEyePlayer`、`BattlEyeBan`。解析器：`PlayerListParser`、`BanListParser`。

请始终使用 `await using`，确保异常时也能释放套接字。
