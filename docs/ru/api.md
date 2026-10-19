# Справка по API

Пространства имён:

- `BattleNET` — ядро (`BattleNET.Core`)
- `BattleNET.Commands` — хелперы и парсеры (`BattleNET.Commands`)

---

## BattlEyeCredentials

```csharp
new BattlEyeCredentials(string host, int port, string password)
new BattlEyeCredentials(IPAddress host, int port, string password)
```

`host` — имя хоста или IP. DNS резолвится в `ConnectAsync`.

---

## BattlEyeClientOptions

| Свойство | По умолчанию | Описание |
|----------|--------------|----------|
| `ReceiveTimeout` | 5 с | Таймаут приёма при логине |
| `KeepAliveInterval` | 5 с | Keep-alive при простое |
| `ConnectionLostTimeout` | 20 с | Нет входящих пакетов → обрыв |
| `CommandTimeout` | 10 с | Таймаут `SendCommandAndWaitAsync` |
| `ReconnectOnPacketLoss` | `false` | Автопереподключение |
| `MaxReconnectAttempts` | 10 | `0` = без лимита |
| `InitialReconnectDelay` | 1 с | Первая задержка |
| `MaxReconnectDelay` | 30 с | Потолок backoff |
| `ReceiveBufferSize` | 64 КБ | Буфер приёма UDP |
| `SendBufferSize` | 64 КБ | Буфер отправки UDP |

---

## BattlEyeClient

```csharp
public BattlEyeClient(BattlEyeCredentials credentials, BattlEyeClientOptions? options = null)
```

Реализует `IAsyncDisposable` и `IDisposable`.

### Свойства

| Свойство | Описание |
|----------|----------|
| `IsConnected` | Подключён и залогинен |
| `PendingCommandCount` | Команды в ожидании ответа |
| `Credentials` | Учётные данные |
| `Options` | Опции клиента |

### Методы

| Метод | Описание |
|-------|----------|
| `ConnectAsync(ct)` | Подключение + логин |
| `SendCommandAsync(command, ct)` | Без ожидания ответа; возвращает sequence |
| `SendCommandAndWaitAsync(command, timeout?, ct)` | Ждёт ответ (multipart склеивается) |
| `DisconnectAsync(ct)` | Корректное отключение |
| `Dispose` / `DisposeAsync` | То же, что disconnect |

### События

| Событие | Когда |
|---------|-------|
| `Connected` | После попытки логина |
| `MessageReceived` | Ответ на команду или сообщение сервера |
| `Disconnected` | После отключения / обрыва / dispose |

`BattlEyeMessageEventArgs.Id`: `0`–`255` — sequence команды; `ServerMessageId` (`-1`) — сообщение сервера.

---

## BattleNET.Commands

### Строки команд — `BattlEyeCommand`

```csharp
BattlEyeCommand.Players
BattlEyeCommand.Kick(playerId, reason)
BattlEyeCommand.Ban(playerId, minutes, reason)
BattlEyeCommand.SayAll("Hello")
```

### Расширения `BattlEyeClient`

| Метод | Описание |
|-------|----------|
| `GetPlayersAsync` / `GetBansAsync` | Список игроков / банов с парсингом |
| `KickAsync` / `BanAsync` / `AddBanAsync` / `RemoveBanAsync` | Модерация |
| `SayAsync` / `SayAllAsync` | Чат |
| `LockAsync` / `UnlockAsync` / `ShutdownAsync` | Управление сервером |
| `GetAdminsAsync` | Подключённые RCon-клиенты |

### Модели

- `BattlEyePlayer` — `Id`, `Ip`, `Port`, `Ping`, `Guid`, `Name`, `IsLobby`
- `BattlEyeBan` — `Id`, `GuidOrIp`, `Minutes` (`0` = навсегда), `Reason`

---

## Ошибки

| Тип | Когда |
|-----|-------|
| `BattlEyeException` | Нет соединения, обрыв во время ожидания |
| `TimeoutException` | Таймаут `SendCommandAndWaitAsync` |
| `ObjectDisposedException` | Использование после dispose |

Всегда используйте `await using`, чтобы сокет освобождался даже при ошибках.
