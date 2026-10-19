<#
.SYNOPSIS
    BattleNET BattlEye RCon console client (PowerShell).
.EXAMPLE
    .\BattleNET.Cli.ps1 -HostName 127.0.0.1 -Port 2302 -Password secret
.EXAMPLE
    .\BattleNET.Cli.ps1 -HostName 127.0.0.1 -Port 2302 -Password secret -Command 'players'
#>
[CmdletBinding()]
param(
    [string]$HostName,
    [int]$Port,
    [string]$Password,
    [string]$Command
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'BattleNET\BattleNET.psd1') -Force

if (-not $HostName) { $HostName = Read-Host 'Enter IP address or hostname' }
if ($Port -le 0) { $Port = [int](Read-Host 'Enter port number') }
if (-not $Password) { $Password = Read-Host 'Enter RCon password' }

$client = New-BattlEyeClient -HostName $HostName -Port $Port -Password $Password
$client.SetOnMessage({
        param($evt)
        if ($evt.Message) { Write-Host $evt.Message }
    })
$client.SetOnConnected({ param($r) Write-Host "connect: $r" -ForegroundColor Green })
$client.SetOnDisconnected({ param($r) Write-Host "disconnected: $r" -ForegroundColor Yellow })

try {
    $result = Connect-BattlEye -Client $client
    if ($result -ne 'Success') { exit 2 }

    if ($Command) {
        $reply = Send-BattlEyeCommandAndWait -Client $client -Command $Command
        if ($reply) { Write-Host $reply }
        exit 0
    }

    Write-Host "Interactive mode. Type a command, or exit/logout to quit." -ForegroundColor Cyan
    while ($client.Connected) {
        $line = Read-Host '>'
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        if ($line -in @('exit', 'logout', 'quit')) { break }
        try {
            $reply = Send-BattlEyeCommandAndWait -Client $client -Command $line
            if ($reply) { Write-Host $reply }
        }
        catch {
            Write-Host $_ -ForegroundColor Red
            if (-not $client.Connected) { break }
        }
    }
}
finally {
    Disconnect-BattlEye -Client $client
    $client.Dispose()
}
