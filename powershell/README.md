# BattleNET for PowerShell

PowerShell 5.1+ / PowerShell 7 module for BattlEye RCon.

## Import

```powershell
Import-Module .\powershell\BattleNET\BattleNET.psd1
```

## Library usage

```powershell
$client = New-BattlEyeClient -HostName '127.0.0.1' -Port 2302 -Password 'secret'
$client.SetOnMessage({ param($e) Write-Host $e.Message })

$result = Connect-BattlEye -Client $client
if ($result -ne 'Success') { throw "connect failed: $result" }

$players = Send-BattlEyeCommandAndWait -Client $client -Command 'players'
Write-Host $players

Disconnect-BattlEye -Client $client
$client.Dispose()
```

Always call `Dispose()` / `Disconnect-BattlEye` so the UDP socket is released.

## CLI script

```powershell
.\powershell\BattleNET.Cli.ps1 -HostName 127.0.0.1 -Port 2302 -Password secret
.\powershell\BattleNET.Cli.ps1 -HostName 127.0.0.1 -Port 2302 -Password secret -Command 'say -1 Hello'
```

## Cmdlets

| Command | Description |
|---------|-------------|
| `New-BattlEyeClient` | Create client |
| `Connect-BattlEye` | Connect + login |
| `Send-BattlEyeCommand` | Fire-and-forget |
| `Send-BattlEyeCommandAndWait` | Wait for reply |
| `Disconnect-BattlEye` | Disconnect |
