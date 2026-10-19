# Быстрый старт

## Требования

- [.NET 9 SDK](https://dotnet.microsoft.com/download) (сборка) или [.NET 9 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) (запуск CLI)
- Visual Studio 2022 17.12+ (или CLI `dotnet`) — SDK .NET 10 требует VS 2026 / MSBuild 18
- Сервер DayZ / Arma с включённым BattlEye RCon (`RConPort`, `RConPassword`)

## Установка пакетов

```bash
dotnet add package BattleNET.Core
dotnet add package BattleNET.Commands
```

Или скачайте DLL из [Releases](https://github.com/quantumloader/BattleNET/releases): `BattleNET.Libraries-*-net9.0.zip`.

## Минимальный пример

```csharp
using BattleNET;
using BattleNET.Commands;

var credentials = new BattlEyeCredentials(
    host: "127.0.0.1",
    port: 2302,          // UDP-порт RCon (не игровой порт)
    password: "secret");

await using var client = new BattlEyeClient(credentials);

client.MessageReceived += (_, e) =>
    Console.WriteLine($"[{e.Id}] {e.Message}");

var result = await client.ConnectAsync();
if (result != BattlEyeConnectionResult.Success)
{
    Console.WriteLine($"Ошибка подключения: {result}");
    return;
}

string playersRaw = await client.SendCommandAndWaitAsync("players");
Console.WriteLine(playersRaw);

foreach (var player in await client.GetPlayersAsync())
    Console.WriteLine($"{player.Id}: {player.Name} ({player.Guid})");

await client.SayAllAsync("Привет от BattleNET");
await client.DisconnectAsync();
```

`await using` / `DisposeAsync` всегда закрывает UDP-сокет — порты не утекают.

## Результаты подключения

| Результат | Значение |
|-----------|----------|
| `Success` | Успешный вход |
| `InvalidLogin` | Неверный пароль RCon |
| `ConnectionFailed` | Хост недоступен / таймаут |
| `Cancelled` | Сработал `CancellationToken` |

## Далее

- [Справка по API](api.md)
- [CLI](cli.md)
