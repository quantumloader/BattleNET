# BattleNET

[![.NET](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/)
[![License: LGPL v3](https://img.shields.io/badge/License-LGPL%20v3-blue.svg)](license.txt)

Modern C# (.NET 10) library and CLI for the BattlEye RCon protocol (Arma / DayZ servers).

Maintained by **[quantumloader](https://github.com/quantumloader)** — v2.0 is a breaking rewrite: async/`await`, `IAsyncDisposable`, typed commands, and proper UDP socket cleanup.

## Projects

| Project | Description |
|---------|-------------|
| `BattleNET.Core` | Protocol + `BattlEyeClient` |
| `BattleNET.Commands` | Typed helpers + `players` / `bans` parsers |
| `BattleNET.Cli` | Console client (`System.CommandLine`) |

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Download

- **GitHub Releases:** https://github.com/quantumloader/BattleNET/releases/tag/v2.0.0  
  (`BattleNET-2.0.0-win-x64.zip` + `.nupkg` files)

## Build

```bash
dotnet build BattleNET.sln -c Release
```

## CLI

```bash
dotnet run --project BattleNET.Cli -- --host 127.0.0.1 --port 2302 --password secret
dotnet run --project BattleNET.Cli -- --host 127.0.0.1 --port 2302 --password secret --command "say -1 Hello"
```

Interactive shortcuts: `/players`, `/bans`, `/history`. Type `exit` to quit (socket is always disposed).

Optional: `--reconnect` enables automatic reconnect after connection loss.

## Library usage

```csharp
using BattleNET;
using BattleNET.Commands;

var credentials = new BattlEyeCredentials("127.0.0.1", 2302, "password");
var options = new BattlEyeClientOptions { ReconnectOnPacketLoss = true };

await using var client = new BattlEyeClient(credentials, options);
client.MessageReceived += (_, e) => Console.WriteLine(e.Message);

var result = await client.ConnectAsync();
if (result != BattlEyeConnectionResult.Success)
    return;

var players = await client.GetPlayersAsync();
await client.SayAllAsync("Hello from BattleNET v2");
await client.DisconnectAsync();
```

Fire-and-forget command:

```csharp
byte seq = await client.SendCommandAsync("missions");
```

Wait for reply:

```csharp
string reply = await client.SendCommandAndWaitAsync("players");
```

## Authors

See [authors.txt](authors.txt).

## License

LGPL-3.0-or-later — see [license.txt](license.txt).

Based on the original BattleNET project by Marcel de Vries and Robert van der Boorn.
