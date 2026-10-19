# Szybki start

## Wymagania

- [.NET 9 SDK](https://dotnet.microsoft.com/download) (kompilacja) lub [.NET 9 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) (CLI)
- Visual Studio 2022 17.12+ (lub CLI `dotnet`). SDK .NET 10 wymaga VS 2026 / MSBuild 18
- Serwer DayZ / Arma z włączonym RCon BattlEye (`RConPort`, `RConPassword`)

## Instalacja pakietów

```bash
dotnet add package BattleNET.Core
dotnet add package BattleNET.Commands
```

Lub pobierz DLL z [Releases](https://github.com/quantumloader/BattleNET/releases): `BattleNET.Libraries-*-net9.0.zip`.

## Minimalny przykład

```csharp
using BattleNET;
using BattleNET.Commands;

var credentials = new BattlEyeCredentials("127.0.0.1", 2302, "secret");
await using var client = new BattlEyeClient(credentials);

client.MessageReceived += (_, e) => Console.WriteLine($"[{e.Id}] {e.Message}");

if (await client.ConnectAsync() != BattlEyeConnectionResult.Success)
{
    Console.WriteLine("Błąd połączenia");
    return;
}

foreach (var player in await client.GetPlayersAsync())
    Console.WriteLine($"{player.Id}: {player.Name}");

await client.SayAllAsync("Witaj z BattleNET");
await client.DisconnectAsync();
```

`await using` zawsze zamyka gniazdo UDP.

| Wynik | Znaczenie |
|-------|-----------|
| `Success` | Zalogowano |
| `InvalidLogin` | Złe hasło RCon |
| `ConnectionFailed` | Host niedostępny / timeout |
| `Cancelled` | Anulowano |

Dalej: [API](api.md) · [CLI](cli.md)
