# BattleNET for PowerShell

Code: [`/powershell`](../powershell/)

```powershell
Import-Module .\powershell\BattleNET\BattleNET.psd1
$client = New-BattlEyeClient -HostName 127.0.0.1 -Port 2302 -Password secret
Connect-BattlEye -Client $client
Send-BattlEyeCommandAndWait -Client $client -Command 'players'
Disconnect-BattlEye -Client $client
$client.Dispose()
```

CLI:

```powershell
.\powershell\BattleNET.Cli.ps1 -HostName 127.0.0.1 -Port 2302 -Password secret
```

See [powershell/README.md](../powershell/README.md).
