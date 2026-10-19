# Démarrage rapide

## Prérequis

- [.NET 9 SDK](https://dotnet.microsoft.com/download) (build) ou [.NET 9 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) (CLI)
- Visual Studio 2022 17.12+ (ou CLI `dotnet`). Le SDK .NET 10 exige VS 2026 / MSBuild 18
- Serveur DayZ / Arma avec RCon BattlEye (`RConPort`, `RConPassword`)

## Installation

```bash
dotnet add package BattleNET.Core
dotnet add package BattleNET.Commands
```

Ou téléchargez les DLL depuis [Releases](https://github.com/quantumloader/BattleNET/releases) : `BattleNET.Libraries-*-net9.0.zip`.

## Exemple minimal

```csharp
using BattleNET;
using BattleNET.Commands;

var credentials = new BattlEyeCredentials("127.0.0.1", 2302, "secret");
await using var client = new BattlEyeClient(credentials);

client.MessageReceived += (_, e) => Console.WriteLine($"[{e.Id}] {e.Message}");

if (await client.ConnectAsync() != BattlEyeConnectionResult.Success)
{
    Console.WriteLine("Échec de connexion");
    return;
}

foreach (var player in await client.GetPlayersAsync())
    Console.WriteLine($"{player.Id}: {player.Name}");

await client.SayAllAsync("Bonjour depuis BattleNET");
await client.DisconnectAsync();
```

`await using` ferme toujours le socket UDP.

| Résultat | Signification |
|----------|---------------|
| `Success` | Connecté |
| `InvalidLogin` | Mot de passe RCon incorrect |
| `ConnectionFailed` | Hôte injoignable / timeout |
| `Cancelled` | Annulé |

Suite : [API](api.md) · [CLI](cli.md)
