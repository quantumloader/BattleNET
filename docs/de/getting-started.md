# Erste Schritte

## Voraussetzungen

- [.NET 9 SDK](https://dotnet.microsoft.com/download) (Build) oder [.NET 9 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) (CLI)
- Visual Studio 2022 17.12+ (oder `dotnet` CLI). .NET-10-SDK braucht VS 2026 / MSBuild 18
- DayZ-/Arma-Server mit BattlEye-RCon (`RConPort`, `RConPassword`)

## Pakete installieren

```bash
dotnet add package BattleNET.Core
dotnet add package BattleNET.Commands
```

Oder DLLs aus [Releases](https://github.com/quantumloader/BattleNET/releases): `BattleNET.Libraries-*-net9.0.zip`.

## Minimalbeispiel

```csharp
using BattleNET;
using BattleNET.Commands;

var credentials = new BattlEyeCredentials("127.0.0.1", 2302, "secret");
await using var client = new BattlEyeClient(credentials);

client.MessageReceived += (_, e) => Console.WriteLine($"[{e.Id}] {e.Message}");

if (await client.ConnectAsync() != BattlEyeConnectionResult.Success)
{
    Console.WriteLine("Verbindung fehlgeschlagen");
    return;
}

foreach (var player in await client.GetPlayersAsync())
    Console.WriteLine($"{player.Id}: {player.Name}");

await client.SayAllAsync("Hallo von BattleNET");
await client.DisconnectAsync();
```

`await using` schließt den UDP-Socket zuverlässig.

## Verbindungsergebnisse

| Ergebnis | Bedeutung |
|----------|-----------|
| `Success` | Login OK |
| `InvalidLogin` | Falsches RCon-Passwort |
| `ConnectionFailed` | Host unerreichbar / Timeout |
| `Cancelled` | Abbruch per Token |

Weiter: [API](api.md) · [CLI](cli.md)
