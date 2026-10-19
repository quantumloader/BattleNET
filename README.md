# BattleNET

[![.NET](https://img.shields.io/badge/.NET-9-512BD4)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/BattleNET.Core.svg)](https://www.nuget.org/packages/BattleNET.Core)
[![License: LGPL v3](https://img.shields.io/badge/License-LGPL%20v3-blue.svg)](license.txt)

Modern C# (.NET 9) library and CLI for the BattlEye RCon protocol (Arma / DayZ servers).

Maintained by **[quantumloader](https://github.com/quantumloader)** — v2.0 is a breaking rewrite: async/`await`, `IAsyncDisposable`, typed commands, and proper UDP socket cleanup.

```bash
dotnet add package BattleNET.Core
dotnet add package BattleNET.Commands
dotnet tool install -g BattleNET.Cli
```

## Documentation

Full docs in [`docs/`](docs/README.md) — **EN · [RU](docs/ru/README.md) · [ZH](docs/zh-CN/README.md) · [DE](docs/de/README.md) · [FR](docs/fr/README.md) · [ES](docs/es/README.md) · [JA](docs/ja/README.md) · [PL](docs/pl/README.md)**

- [Getting started](docs/getting-started.md) · [API](docs/api.md) · [CLI](docs/cli.md) · [Migration](docs/migration-v2.md)

## Projects

| Project | Description |
|---------|-------------|
| `BattleNET.Core` | .NET protocol + `BattlEyeClient` |
| `BattleNET.Commands` | .NET typed helpers + `players` / `bans` parsers |
| `BattleNET.Cli` | .NET console client (`System.CommandLine`) |
| [`go/`](go/) | **Go** port + CLI |
| [`python/`](python/) | **Python** port + CLI |
| [`powershell/`](powershell/) | **PowerShell** module + CLI script |

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download) (Visual Studio 2022 17.12+)

## Download

- **NuGet:** [BattleNET.Core](https://www.nuget.org/packages/BattleNET.Core) · [BattleNET.Commands](https://www.nuget.org/packages/BattleNET.Commands) · [BattleNET.Cli](https://www.nuget.org/packages/BattleNET.Cli)
- **GitHub Releases:** https://github.com/quantumloader/BattleNET/releases/tag/v2.0.0  
  - `BattleNET.Libraries-*-net9.0.zip` — `BattleNET.Core` + `BattleNET.Commands` DLLs  
  - `BattleNET-*-win-x64.zip` — CLI  
  - `*.nupkg` — NuGet packages

## Build

```bash
dotnet build BattleNET.sln -c Release
```

Open `BattleNET.sln` in Visual Studio 2022 (17.12+) or Cursor/VS Code (C# Dev Kit). Set startup project to **BattleNET.Cli**.

## Quick CLI

```bash
dotnet run --project BattleNET.Cli -- --host 127.0.0.1 --port 2302 --password secret
dotnet run --project BattleNET.Cli -- --host 127.0.0.1 --port 2302 --password secret --command "say -1 Hello"
```

Interactive shortcuts: `/players`, `/bans`, `/history`. Type `exit` to quit.

## Quick library usage

```csharp
using BattleNET;
using BattleNET.Commands;

var credentials = new BattlEyeCredentials("127.0.0.1", 2302, "password");
await using var client = new BattlEyeClient(credentials);

client.MessageReceived += (_, e) => Console.WriteLine(e.Message);

if (await client.ConnectAsync() != BattlEyeConnectionResult.Success)
    return;

var players = await client.GetPlayersAsync();
await client.SayAllAsync("Hello from BattleNET v2");
```

More examples: [docs/getting-started.md](docs/getting-started.md) · [docs/api.md](docs/api.md) · [Go](go/README.md)

## Go

```bash
cd go
go test ./...
go run ./cmd/battlenet --host 127.0.0.1 --port 2302 --password secret
```

```go
import battlenet "github.com/quantumloader/BattleNET/go"

client := battlenet.NewClient("127.0.0.1", 2302, "password", nil)
defer client.Close()
```

Details: [go/README.md](go/README.md) · [docs/go.md](docs/go.md)

## Python

```bash
cd python && pip install -e .
battlenet --host 127.0.0.1 --port 2302 --password secret
```

Details: [python/README.md](python/README.md) · [docs/python.md](docs/python.md)

## PowerShell

```powershell
Import-Module .\powershell\BattleNET\BattleNET.psd1
.\powershell\BattleNET.Cli.ps1 -HostName 127.0.0.1 -Port 2302 -Password secret
```

Details: [powershell/README.md](powershell/README.md) · [docs/powershell.md](docs/powershell.md)

## Authors

See [authors.txt](authors.txt).

## License

LGPL-3.0-or-later — see [license.txt](license.txt).

Based on the original BattleNET project by Marcel de Vries and Robert van der Boorn.
