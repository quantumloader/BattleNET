# CLI reference

Project: `BattleNET.Cli`  
Package / tool id: `BattleNET.Cli`  
Binary name: `BattleNET` (tool command: `battlenet`)

## Install

```bash
# as global tool (from nuget.org)
dotnet tool install -g BattleNET.Cli

# or run from source
dotnet run --project BattleNET.Cli -- [options]

# or unzip BattleNET-*-win-x64.zip from GitHub Releases (.NET 10 runtime required)
```

## Options

| Option | Short | Description |
|--------|-------|-------------|
| `--host` | | RCon host or IP |
| `--port` | `-p` | RCon UDP port |
| `--password` | | RCon password |
| `--command` | `-c` | Send one command, print reply, exit |
| `--reconnect` | | Reconnect after connection loss |
| `--help` | `-?` / `-h` | Help |
| `--version` | | Version |

If `--host` / `--port` / `--password` are omitted, the CLI prompts interactively (password is masked).

## Examples

```bash
# Interactive session
BattleNET --host 127.0.0.1 --port 2302 --password secret

# One-shot
BattleNET --host 127.0.0.1 --port 2302 --password secret --command "say -1 Hello World"

# With auto-reconnect
BattleNET --host 127.0.0.1 --port 2302 --password secret --reconnect
```

## Interactive mode

After connect, type RCon commands and press Enter.

| Input | Action |
|-------|--------|
| any RCon command | Sent; reply printed |
| `/players` | Parsed player list |
| `/bans` | Parsed ban list |
| `/history` | Commands sent this session |
| `exit` / `logout` / `quit` | Disconnect and quit |

Ctrl+C cancels and disposes the client (UDP port is released).

Server messages are printed in cyan; command replies in gray; status in green/yellow; errors in red.

## Exit codes

| Code | Meaning |
|------|---------|
| `0` | OK |
| `1` | Missing credentials |
| `2` | Connect / login failed |
| `3` | Command failed (one-shot mode) |
