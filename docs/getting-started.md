# Getting started

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (build) or [.NET 10 runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (run published CLI)
- A DayZ / Arma server with BattlEye RCon enabled (`RConPort`, `RConPassword` in BE server config)

## Install packages

```bash
dotnet add package BattleNET.Core
dotnet add package BattleNET.Commands
```

Or reference the DLL zip from [Releases](https://github.com/quantumloader/BattleNET/releases): `BattleNET.Libraries-*-net10.0.zip`.

## Minimal example

```csharp
using BattleNET;
using BattleNET.Commands;

var credentials = new BattlEyeCredentials(
    host: "127.0.0.1",
    port: 2302,          // RCon UDP port (not the game port)
    password: "secret");

await using var client = new BattlEyeClient(credentials);

client.MessageReceived += (_, e) =>
    Console.WriteLine($"[{e.Id}] {e.Message}");

var result = await client.ConnectAsync();
if (result != BattlEyeConnectionResult.Success)
{
    Console.WriteLine($"Connect failed: {result}");
    return;
}

// Wait for reply
string playersRaw = await client.SendCommandAndWaitAsync("players");
Console.WriteLine(playersRaw);

// Typed helper (parses player list)
foreach (var player in await client.GetPlayersAsync())
    Console.WriteLine($"{player.Id}: {player.Name} ({player.Guid})");

await client.SayAllAsync("Hello from BattleNET");
await client.DisconnectAsync();
```

`await using` / `DisposeAsync` always closes the UDP socket — no leaked ports.

## Connect results

| Result | Meaning |
|--------|---------|
| `Success` | Logged in |
| `InvalidLogin` | Wrong RCon password |
| `ConnectionFailed` | Host unreachable / timeout |
| `Cancelled` | `CancellationToken` fired |

## Next

- [API reference](api.md)
- [CLI](cli.md)
