# Миграция с BattleNET 1.3.x на 2.0

v2 — **ломающий rewrite**. Совместимости с 1.3.x нет.

## Пакеты

| 1.3.x | 2.0 |
|-------|-----|
| Один `BattleNET` (netstandard2.0) | `BattleNET.Core` + `BattleNET.Commands` (net9.0) |
| `BattleNET client` (.NET Framework 4.6.1) | `BattleNET.Cli` |

## Подключение

```csharp
// 1.3.x
var b = new BattlEyeClient(loginCredentials);
b.BattlEyeMessageReceived += ...;
b.Connect();

// 2.0
await using var client = new BattlEyeClient(credentials, options);
client.MessageReceived += ...;
await client.ConnectAsync();
```

## Учётные данные

```csharp
// 1.3.x
new BattlEyeLoginCredentials { Host = ip, Port = 2302, Password = "..." };

// 2.0
new BattlEyeCredentials("127.0.0.1", 2302, "...");
```

## Команды

```csharp
// 1.3.x
b.SendCommand("players");

// 2.0
await client.SendCommandAsync("players");
await client.SendCommandAndWaitAsync("players");
await client.GetPlayersAsync();
```

Магическое значение `256` убрано — используйте исключения и enum результатов.

## События

| 1.3.x | 2.0 |
|-------|-----|
| `BattlEyeMessageReceived` | `MessageReceived` |
| `BattlEyeConnected` | `Connected` |
| `BattlEyeDisconnected` | `Disconnected` |

## Очистка сокетов

```csharp
await using var client = new BattlEyeClient(credentials);
```

В v1 UDP часто «залипал» при reconnect/выходе; в v2 циклы отменяются и `UdpClient` освобождается.
