<#
  File:        Test-CaptureRelay.ps1
  Project:     KawasakiVSCode (dev-time tooling)
  Description: Checks Start-CaptureRelay.ps1 against a stand-in target on this PC. Bytes must pass
               both ways unchanged, every byte value included. The log must record each chunk with
               its direction in the capture format (specs/001-khi-terminal-load-save/contracts/
               capture-format.md). The relay must exit 0 when the client closes, 2 when the log
               already exists, and 4 when the target is unreachable, as khi's exit codes do.
  Author:      RCH Automation LLC
  Created:     2026-11-03

  Fire demonstrations (Article VII), 2026-11-03:
  - Byte transparency: the relay was changed to flip the low bit of the first byte of every chunk
    before forwarding. Red: the four "reach ... unchanged" checks and the two "log holds every ...
    byte" checks failed (6 failures). The relay was restored, identical by SHA-256; green.
  - Exit codes: with the relay exiting 1 on both refusals, the "exit 2" and "exit 4" checks
    failed (2 failures). Fixed to 2 and 4; green.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$relayScript = Join-Path $PSScriptRoot 'Start-CaptureRelay.ps1'
$loopback = [System.Net.IPAddress]::Loopback
$script:failures = 0

function Test-That([bool]$condition, [string]$what) {
    if ($condition) { Write-Host "  ok   $what" } else { Write-Host "  FAIL $what"; $script:failures++ }
}

function Get-FreePort {
    $probe = New-Object System.Net.Sockets.TcpListener($loopback, 0)
    $probe.Start()
    $port = $probe.LocalEndpoint.Port
    $probe.Stop()
    return $port
}

function Read-Bytes($stream, [int]$count) {
    $buffer = New-Object byte[] $count
    $read = 0
    while ($read -lt $count) {
        $n = $stream.Read($buffer, $read, $count - $read)
        if ($n -eq 0) { break }
        $read += $n
    }
    if ($read -lt $count) { return ,([byte[]]@()) }
    return ,$buffer
}

function Test-SameBytes([byte[]]$a, [byte[]]$b) {
    if ($a.Length -ne $b.Length) { return $false }
    for ($i = 0; $i -lt $a.Length; $i++) { if ($a[$i] -ne $b[$i]) { return $false } }
    return $true
}

function Start-Relay([int]$listenPort, [int]$targetPort, [string]$log, [string]$work) {
    $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$relayScript`"",
        '-ListenPort', $listenPort, '-TargetHost', $loopback.ToString(), '-TargetPort', $targetPort,
        '-OutFile', "`"$log`"")
    $process = Start-Process -FilePath 'powershell.exe' -ArgumentList $arguments -PassThru -NoNewWindow `
        -RedirectStandardOutput (Join-Path $work "relay-$listenPort.out") -RedirectStandardError (Join-Path $work "relay-$listenPort.err")
    $null = $process.Handle   # without this, PowerShell reports an empty exit code once the process ends
    return $process
}

function Connect-WithRetry([int]$port) {
    $deadline = (Get-Date).AddSeconds(15)
    while ((Get-Date) -lt $deadline) {
        try {
            $candidate = New-Object System.Net.Sockets.TcpClient
            $candidate.Connect($loopback, $port)
            return $candidate
        } catch { Start-Sleep -Milliseconds 100 }
    }
    return $null
}

Write-Host 'Test-CaptureRelay'
Test-That (Test-Path -LiteralPath $relayScript) 'Start-CaptureRelay.ps1 exists'
if (-not (Test-Path -LiteralPath $relayScript)) {
    Write-Host "RED: $script:failures failure(s)"
    exit 1
}

$work = Join-Path ([System.IO.Path]::GetTempPath()) ('khi-relay-test-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory -Path $work | Out-Null
$log = Join-Path $work 'exchange.txt'

$target = New-Object System.Net.Sockets.TcpListener($loopback, 0)
$target.Start()
$targetPort = $target.LocalEndpoint.Port
$listenPort = Get-FreePort
$relay = Start-Relay $listenPort $targetPort $log $work

try {
    $client = Connect-WithRetry $listenPort
    Test-That ($null -ne $client) 'relay accepts a connection on its listen port'

    $deadline = (Get-Date).AddSeconds(10)
    while (-not $target.Pending() -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 50 }
    $connected = $target.Pending()
    Test-That $connected 'relay connects to the target'

    if ($null -ne $client -and $connected) {
        $server = $target.AcceptTcpClient()
        $toClient = $client.GetStream(); $toClient.ReadTimeout = 5000
        $toServer = $server.GetStream(); $toServer.ReadTimeout = 5000

        $prompt = [System.Text.Encoding]::ASCII.GetBytes('login: ')
        $typed = [byte[]](0x61, 0x73, 0x0d)
        $every = [byte[]](0..255)

        $toServer.Write($prompt, 0, $prompt.Length)
        Test-That (Test-SameBytes (Read-Bytes $toClient $prompt.Length) $prompt) 'controller bytes reach the client unchanged'
        $toClient.Write($typed, 0, $typed.Length)
        Test-That (Test-SameBytes (Read-Bytes $toServer $typed.Length) $typed) 'client bytes reach the controller unchanged'
        $toServer.Write($every, 0, $every.Length)
        Test-That (Test-SameBytes (Read-Bytes $toClient $every.Length) $every) 'every byte value 0-255 reaches the client unchanged'
        $toClient.Write($every, 0, $every.Length)
        Test-That (Test-SameBytes (Read-Bytes $toServer $every.Length) $every) 'every byte value 0-255 reaches the controller unchanged'

        $client.Close()
        $closedRead = -1
        try { $closedRead = $toServer.Read((New-Object byte[] 1), 0, 1) } catch { $closedRead = -1 }
        Test-That ($closedRead -eq 0) 'relay closes the controller side when the client closes'

        $exited = $relay.WaitForExit(10000)
        Test-That $exited 'relay exits after the client closes'
        if ($exited) { Test-That ($relay.ExitCode -eq 0) 'relay exit code is 0' }

        $lines = @()
        if (Test-Path -LiteralPath $log) { $lines = @(Get-Content -LiteralPath $log) }
        Test-That ($lines.Count -ge 2 -and $lines[0] -eq '# khi capture 1') 'log starts with the capture header'
        Test-That ($lines.Count -ge 2 -and $lines[1] -eq "# relay listen=$listenPort target=$($loopback):$targetPort") 'log names the relay ports'

        $pattern = '^([CS]) (\d+\.\d{3}) ([0-9a-f]{2}(?: [0-9a-f]{2})*)  \|.*\|$'
        $records = @($lines | Select-Object -Skip 2)
        $wellFormed = $records.Count -gt 0
        $streams = @{ C = New-Object System.Collections.Generic.List[byte]; S = New-Object System.Collections.Generic.List[byte] }
        $lastTime = -1.0
        $timesInOrder = $true
        foreach ($record in $records) {
            if ($record -notmatch $pattern) { $wellFormed = $false; continue }
            $time = [double]::Parse($Matches[2], [System.Globalization.CultureInfo]::InvariantCulture)
            if ($time -lt $lastTime) { $timesInOrder = $false }
            $lastTime = $time
            foreach ($pair in $Matches[3].Split(' ')) { $streams[$Matches[1]].Add([Convert]::ToByte($pair, 16)) }
        }
        Test-That $wellFormed 'every log record is direction, time, hex and preview'
        Test-That $timesInOrder 'log times never go backwards'
        Test-That (Test-SameBytes $streams['S'].ToArray() ([byte[]]($prompt + $every))) 'log holds every controller byte, in order'
        Test-That (Test-SameBytes $streams['C'].ToArray() ([byte[]]($typed + $every))) 'log holds every client byte, in order'

        $second = Start-Relay (Get-FreePort) $targetPort $log $work
        $refused = $second.WaitForExit(15000)
        Test-That ($refused -and $second.ExitCode -eq 2) 'relay refuses to overwrite an existing log, exit 2'
        Test-That ((@(Get-Content -LiteralPath $log)).Count -eq $lines.Count) 'the existing log is left as it was'
    }

    $deadLog = Join-Path $work 'unreachable.txt'
    $deadPort = Get-FreePort
    $deadListen = Get-FreePort
    $third = Start-Relay $deadListen $deadPort $deadLog $work
    $deadClient = Connect-WithRetry $deadListen
    Test-That ($null -ne $deadClient) 'relay accepts a connection when the target is unreachable'
    $failed = $third.WaitForExit(30000)
    Test-That ($failed -and $third.ExitCode -eq 4) 'relay exits 4 when the target is unreachable'
    Test-That (-not (Test-Path -LiteralPath $deadLog)) 'no log is left when the target is unreachable'
    if ($null -ne $deadClient) { $deadClient.Close() }
    if (-not $third.HasExited) { $third.Kill() }
} finally {
    if (-not $relay.HasExited) { $relay.Kill() }
    $target.Stop()
}

if ($script:failures -gt 0) {
    Write-Host "RED: $script:failures failure(s); relay output kept in $work"
    exit 1
}
Remove-Item -LiteralPath $work -Recurse -Force
Write-Host 'GREEN: all checks passed'
exit 0
