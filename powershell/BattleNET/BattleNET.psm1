#Requires -Version 5.1
Set-StrictMode -Version Latest

$script:CrcTable = $null

function Get-BattlEyeCrcTable {
    if ($null -ne $script:CrcTable) { return $script:CrcTable }
    $mask = [uint64]4294967295
    $poly = [uint64]3988292384  # 0xEDB88320
    $table = New-Object 'System.UInt32[]' 256
    for ($i = 0; $i -lt 256; $i++) {
        [uint64]$entry = [uint64]$i
        for ($j = 0; $j -lt 8; $j++) {
            if (($entry -band [uint64]1) -ne [uint64]0) {
                $entry = (($entry -shr 1) -bxor $poly) -band $mask
            }
            else {
                $entry = ($entry -shr 1) -band $mask
            }
        }
        $table[$i] = [Convert]::ToUInt32($entry)
    }
    $script:CrcTable = $table
    return $table
}

function Get-BattlEyeCrc32 {
    param([Parameter(Mandatory)][byte[]]$Data)
    $table = Get-BattlEyeCrcTable
    $mask = [uint64]4294967295
    [uint64]$crc = $mask
    foreach ($b in $Data) {
        $idx = [int](($crc -bxor [uint64]$b) -band [uint64]255)
        $crc = ([uint64]$table[$idx] -bxor ($crc -shr 8)) -band $mask
    }
    $crc = (-bnot $crc) -band $mask
    return [Convert]::ToUInt32($crc)
}

function New-BattlEyePacket {
    param(
        [Parameter(Mandatory)][ValidateSet(0, 1, 2)][int]$Type,
        [object]$Sequence = $null,
        [byte[]]$Body = @()
    )

    $payload = New-Object System.Collections.Generic.List[byte]
    [void]$payload.Add(0xFF)
    [void]$payload.Add([byte]$Type)
    if ($null -ne $Sequence) {
        [void]$payload.Add([byte]$Sequence)
    }
    foreach ($b in $Body) { [void]$payload.Add($b) }

    $payloadBytes = $payload.ToArray()
    [uint32]$crc = Get-BattlEyeCrc32 -Data $payloadBytes
    $packet = New-Object System.Collections.Generic.List[byte]
    [void]$packet.Add([byte][char]'B')
    [void]$packet.Add([byte][char]'E')
    $crcBytes = [BitConverter]::GetBytes($crc)
    foreach ($b in $crcBytes) { [void]$packet.Add($b) }
    foreach ($b in $payloadBytes) { [void]$packet.Add($b) }
    return , $packet.ToArray()
}

function ConvertTo-Utf8Bytes {
    param([string]$Text)
    if ([string]::IsNullOrEmpty($Text)) { return [byte[]]@() }
    return [System.Text.Encoding]::UTF8.GetBytes($Text)
}

function New-BattlEyeLoginPacket {
    param([Parameter(Mandatory)][string]$Password)
    return New-BattlEyePacket -Type 0 -Sequence $null -Body (ConvertTo-Utf8Bytes $Password)
}

function New-BattlEyeCommandPacket {
    param(
        [Parameter(Mandatory)][byte]$Sequence,
        [string]$Command = ''
    )
    return New-BattlEyePacket -Type 1 -Sequence $Sequence -Body (ConvertTo-Utf8Bytes $Command)
}

function New-BattlEyeAcknowledgePacket {
    param([Parameter(Mandatory)][byte]$Sequence)
    return New-BattlEyePacket -Type 2 -Sequence $Sequence -Body @()
}

function ConvertFrom-BattlEyePacket {
    param([Parameter(Mandatory)][byte[]]$Datagram)

    if ($Datagram.Length -lt 8 -or $Datagram[0] -ne [byte][char]'B' -or $Datagram[1] -ne [byte][char]'E' -or $Datagram[6] -ne 0xFF) {
        throw 'Invalid BattlEye packet header'
    }

    $expected = [BitConverter]::ToUInt32($Datagram, 2)
    $payload = New-Object byte[] ($Datagram.Length - 6)
    [Array]::Copy($Datagram, 6, $payload, 0, $payload.Length)
    $actual = Get-BattlEyeCrc32 -Data $payload
    if ($expected -ne $actual) { throw 'CRC mismatch' }

    $type = [int]$Datagram[7]
    $result = [ordered]@{
        Type           = $type
        Sequence       = $null
        LoginSuccess   = $null
        Message        = ''
        IsMultipart    = $false
        MultipartTotal = 0
        MultipartIndex = 0
    }

    switch ($type) {
        0 {
            if ($Datagram.Length -lt 9) { throw 'Short login packet' }
            $result.LoginSuccess = ($Datagram[8] -eq 0x01)
        }
        1 {
            if ($Datagram.Length -lt 9) { throw 'Short command packet' }
            $result.Sequence = [byte]$Datagram[8]
            if ($Datagram.Length -ge 12 -and $Datagram[9] -eq 0x00) {
                $result.IsMultipart = $true
                $result.MultipartTotal = [int]$Datagram[10]
                $result.MultipartIndex = [int]$Datagram[11]
                if ($Datagram.Length -gt 12) {
                    $result.Message = [System.Text.Encoding]::UTF8.GetString($Datagram, 12, $Datagram.Length - 12)
                }
            }
            elseif ($Datagram.Length -gt 9) {
                $result.Message = [System.Text.Encoding]::UTF8.GetString($Datagram, 9, $Datagram.Length - 9)
            }
        }
        2 {
            if ($Datagram.Length -lt 9) { throw 'Short server message' }
            $result.Sequence = [byte]$Datagram[8]
            if ($Datagram.Length -gt 9) {
                $result.Message = [System.Text.Encoding]::UTF8.GetString($Datagram, 9, $Datagram.Length - 9)
            }
        }
        default { throw "Unknown packet type $type" }
    }

    return [pscustomobject]$result
}

class BattlEyeClient {
    [string]$HostName
    [int]$Port
    [string]$Password
    [bool]$Connected
    [double]$ReceiveTimeoutSec = 5
    [double]$CommandTimeoutSec = 10
    [double]$KeepAliveIntervalSec = 5
    [double]$ConnectionLostTimeoutSec = 20

    hidden [System.Net.Sockets.UdpClient]$Udp
    hidden [System.Net.IPEndPoint]$Remote
    hidden [byte]$Sequence
    hidden [System.Collections.Concurrent.ConcurrentDictionary[byte, object]]$Pending
    hidden [datetime]$LastSent
    hidden [datetime]$LastReceived
    hidden [System.Threading.CancellationTokenSource]$Cts
    hidden [System.Threading.Tasks.Task]$ReceiveTask
    hidden [System.Threading.Tasks.Task]$KeepAliveTask
    hidden [scriptblock]$OnMessage
    hidden [scriptblock]$OnConnected
    hidden [scriptblock]$OnDisconnected

    BattlEyeClient([string]$HostName, [int]$Port, [string]$Password) {
        $this.HostName = $HostName
        $this.Port = $Port
        $this.Password = $Password
        $this.Connected = $false
        $this.Pending = [System.Collections.Concurrent.ConcurrentDictionary[byte, object]]::new()
        $this.LastSent = [datetime]::UtcNow
        $this.LastReceived = [datetime]::UtcNow
    }

    [void] SetOnMessage([scriptblock]$Handler) { $this.OnMessage = $Handler }
    [void] SetOnConnected([scriptblock]$Handler) { $this.OnConnected = $Handler }
    [void] SetOnDisconnected([scriptblock]$Handler) { $this.OnDisconnected = $Handler }

    [string] Connect() {
        $this.DisconnectInternal($false, 'Manual')

        $ips = [System.Net.Dns]::GetHostAddresses($this.HostName)
        if (-not $ips -or $ips.Count -eq 0) {
            $this.RaiseConnected('ConnectionFailed')
            return 'ConnectionFailed'
        }
        $ip = $ips | Where-Object { $_.AddressFamily -eq 'InterNetwork' } | Select-Object -First 1
        if (-not $ip) { $ip = $ips[0] }
        $this.Remote = [System.Net.IPEndPoint]::new($ip, $this.Port)
        $this.Udp = [System.Net.Sockets.UdpClient]::new($ip.AddressFamily)
        $this.Udp.Client.ReceiveTimeout = [int]($this.ReceiveTimeoutSec * 1000)
        $this.Udp.Connect($this.Remote)

        $login = New-BattlEyeLoginPacket -Password $this.Password
        [void]$this.Udp.Send($login, $login.Length)

        try {
            $remoteEp = $null
            $bytes = $this.Udp.Receive([ref]$remoteEp)
            $parsed = ConvertFrom-BattlEyePacket -Datagram $bytes
        }
        catch {
            $this.CloseUdp()
            $this.RaiseConnected('ConnectionFailed')
            return 'ConnectionFailed'
        }

        if ($parsed.Type -ne 0) {
            $this.CloseUdp()
            $this.RaiseConnected('ConnectionFailed')
            return 'ConnectionFailed'
        }
        if (-not $parsed.LoginSuccess) {
            $this.CloseUdp()
            $this.RaiseConnected('InvalidLogin')
            return 'InvalidLogin'
        }

        $this.Udp.Client.ReceiveTimeout = 1000
        $this.Sequence = 0
        $this.LastSent = [datetime]::UtcNow
        $this.LastReceived = [datetime]::UtcNow
        $this.Connected = $true
        $this.Cts = [System.Threading.CancellationTokenSource]::new()
        $token = $this.Cts.Token
        $self = $this
        $this.ReceiveTask = [System.Threading.Tasks.Task]::Run({ $self.ReceiveLoop($token) })
        $this.KeepAliveTask = [System.Threading.Tasks.Task]::Run({ $self.KeepAliveLoop($token) })
        $this.RaiseConnected('Success')
        return 'Success'
    }

    [byte] SendCommand([string]$Command) {
        if (-not $this.Connected) { throw 'Not connected' }
        $seq = $this.NextSequence()
        $packet = New-BattlEyeCommandPacket -Sequence $seq -Command $Command
        [void]$this.Udp.Send($packet, $packet.Length)
        $this.LastSent = [datetime]::UtcNow
        return $seq
    }

    [string] SendCommandAndWait([string]$Command, [double]$TimeoutSec = 0) {
        if (-not $this.Connected) { throw 'Not connected' }
        if ($TimeoutSec -le 0) { $TimeoutSec = $this.CommandTimeoutSec }

        $seq = $this.NextSequence()
        $tcs = [System.Threading.Tasks.TaskCompletionSource[string]]::new()
        [void]$this.Pending.TryAdd($seq, $tcs)

        try {
            $packet = New-BattlEyeCommandPacket -Sequence $seq -Command $Command
            [void]$this.Udp.Send($packet, $packet.Length)
            $this.LastSent = [datetime]::UtcNow

            if (-not $tcs.Task.Wait([TimeSpan]::FromSeconds($TimeoutSec))) {
                throw "Timed out waiting for sequence $seq"
            }
            return $tcs.Task.Result
        }
        finally {
            $removed = $null
            [void]$this.Pending.TryRemove($seq, [ref]$removed)
        }
    }

    [void] Disconnect() {
        $this.DisconnectInternal($true, 'Manual')
    }

    [void] Dispose() {
        $this.DisconnectInternal($true, 'Disposed')
    }

    hidden [byte] NextSequence() {
        $seq = $this.Sequence
        $this.Sequence = [byte](($this.Sequence + 1) -band 0xFF)
        return $seq
    }

    hidden [void] CloseUdp() {
        if ($null -ne $this.Udp) {
            try { $this.Udp.Close() } catch { }
            try { $this.Udp.Dispose() } catch { }
            $this.Udp = $null
        }
    }

    hidden [void] DisconnectInternal([bool]$Raise, [string]$Reason) {
        $was = $this.Connected
        $this.Connected = $false
        if ($null -ne $this.Cts) {
            try { $this.Cts.Cancel() } catch { }
        }
        $this.CloseUdp()
        if ($null -ne $this.ReceiveTask) {
            try { [void]$this.ReceiveTask.Wait(2000) } catch { }
            $this.ReceiveTask = $null
        }
        if ($null -ne $this.KeepAliveTask) {
            try { [void]$this.KeepAliveTask.Wait(2000) } catch { }
            $this.KeepAliveTask = $null
        }
        if ($null -ne $this.Cts) {
            $this.Cts.Dispose()
            $this.Cts = $null
        }
        foreach ($key in @($this.Pending.Keys)) {
            $tcs = $null
            if ($this.Pending.TryRemove($key, [ref]$tcs) -and $null -ne $tcs) {
                [void]$tcs.TrySetException([System.Exception]::new('Disconnected before reply'))
            }
        }
        if ($was -and $Raise) { $this.RaiseDisconnected($Reason) }
    }

    hidden [void] ReceiveLoop([System.Threading.CancellationToken]$Token) {
        while (-not $Token.IsCancellationRequested -and $this.Connected -and $null -ne $this.Udp) {
            try {
                $remoteEp = $null
                $bytes = $this.Udp.Receive([ref]$remoteEp)
                $this.LastReceived = [datetime]::UtcNow
                $this.HandleDatagram($bytes)
            }
            catch [System.Net.Sockets.SocketException] {
                if ($Token.IsCancellationRequested -or -not $this.Connected) { return }
            }
            catch {
                if ($Token.IsCancellationRequested -or -not $this.Connected) { return }
            }
        }
    }

    hidden [void] KeepAliveLoop([System.Threading.CancellationToken]$Token) {
        while (-not $Token.WaitHandle.WaitOne(1000)) {
            if (-not $this.Connected) { return }
            $now = [datetime]::UtcNow
            if (($now - $this.LastReceived).TotalSeconds -ge $this.ConnectionLostTimeoutSec) {
                $this.DisconnectInternal($true, 'ConnectionLost')
                return
            }
            if ($this.Pending.Count -eq 0 -and ($now - $this.LastSent).TotalSeconds -ge $this.KeepAliveIntervalSec) {
                try { [void]$this.SendCommand('') } catch {
                    $this.DisconnectInternal($true, 'SocketError')
                    return
                }
            }
        }
    }

    hidden [void] HandleDatagram([byte[]]$Bytes) {
        try { $parsed = ConvertFrom-BattlEyePacket -Datagram $Bytes } catch { return }

        if ($parsed.Type -eq 2) {
            if ($null -ne $parsed.Sequence) {
                $ack = New-BattlEyeAcknowledgePacket -Sequence ([byte]$parsed.Sequence)
                try { [void]$this.Udp.Send($ack, $ack.Length) } catch { }
            }
            $this.RaiseMessage($parsed.Message, -1)
            return
        }

        if ($parsed.Type -eq 1 -and $null -ne $parsed.Sequence) {
            $seq = [byte]$parsed.Sequence
            $msg = [string]$parsed.Message
            $tcs = $null
            if ($this.Pending.TryRemove($seq, [ref]$tcs) -and $null -ne $tcs) {
                [void]$tcs.TrySetResult($msg)
            }
            $this.RaiseMessage($msg, [int]$seq)
        }
    }

    hidden [void] RaiseMessage([string]$Message, [int]$Id) {
        if ($null -ne $this.OnMessage) {
            & $this.OnMessage @{ Message = $Message; Id = $Id }
        }
    }

    hidden [void] RaiseConnected([string]$Result) {
        if ($null -ne $this.OnConnected) { & $this.OnConnected $Result }
    }

    hidden [void] RaiseDisconnected([string]$Reason) {
        if ($null -ne $this.OnDisconnected) { & $this.OnDisconnected $Reason }
    }
}

function New-BattlEyeClient {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$HostName,
        [Parameter(Mandatory)][int]$Port,
        [Parameter(Mandatory)][string]$Password
    )
    return [BattlEyeClient]::new($HostName, $Port, $Password)
}

function Connect-BattlEye {
    [CmdletBinding()]
    param([Parameter(Mandatory)][BattlEyeClient]$Client)
    return $Client.Connect()
}

function Disconnect-BattlEye {
    [CmdletBinding()]
    param([Parameter(Mandatory)][BattlEyeClient]$Client)
    $Client.Disconnect()
}

function Send-BattlEyeCommand {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][BattlEyeClient]$Client,
        [Parameter(Mandatory)][AllowEmptyString()][string]$Command
    )
    return $Client.SendCommand($Command)
}

function Send-BattlEyeCommandAndWait {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][BattlEyeClient]$Client,
        [Parameter(Mandatory)][string]$Command,
        [double]$TimeoutSec = 0
    )
    return $Client.SendCommandAndWait($Command, $TimeoutSec)
}

Export-ModuleMember -Function @(
    'New-BattlEyeClient'
    'Connect-BattlEye'
    'Disconnect-BattlEye'
    'Send-BattlEyeCommand'
    'Send-BattlEyeCommandAndWait'
    'New-BattlEyeLoginPacket'
    'New-BattlEyeCommandPacket'
    'New-BattlEyeAcknowledgePacket'
    'ConvertFrom-BattlEyePacket'
    'Get-BattlEyeCrc32'
)
