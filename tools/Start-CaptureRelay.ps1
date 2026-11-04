<#
  File:        Start-CaptureRelay.ps1
  Project:     KawasakiVSCode (dev-time tooling)
  Description: Logging relay for captures (Article IV; quickstart P0 and P1). KRterm connects to the
               listen port on this PC, and every byte is forwarded unchanged to the target and back.
               Each chunk read is logged in the capture format of specs/001-khi-terminal-load-save/
               contracts/capture-format.md. The relay serves one connection, then exits: 0 when the
               session closes, 2 when the log already exists (it never overwrites one), 4 when the
               target is unreachable, as khi's exit codes do.
  Author:      RCH Automation LLC
  Created:     2026-11-03
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][ValidateRange(1, 65535)][int]$ListenPort,
    [Parameter(Mandatory = $true)][ValidateNotNullOrEmpty()][string]$TargetHost,
    [Parameter(Mandatory = $true)][ValidateRange(1, 65535)][int]$TargetPort,
    [Parameter(Mandatory = $true)][ValidateNotNullOrEmpty()][string]$OutFile
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$outPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutFile)
if (Test-Path -LiteralPath $outPath) {
    [Console]::Error.WriteLine("khi relay: $outPath already exists; a capture is never overwritten")
    exit 2   # refused before connecting, as khi's exit code 2
}
[void][System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($outPath))

$latin1 = [System.Text.Encoding]::GetEncoding(28591)
$writer = New-Object System.IO.StreamWriter($outPath, $false, (New-Object System.Text.ASCIIEncoding))
$writer.NewLine = "`n"

function Write-Record([string]$direction, [double]$seconds, [byte[]]$bytes, [int]$count) {
    $hex = [System.BitConverter]::ToString($bytes, 0, $count).Replace('-', ' ').ToLowerInvariant()
    $preview = [regex]::Replace($latin1.GetString($bytes, 0, $count), '[^\x20-\x7E]', '.')
    $time = $seconds.ToString('0.000', [System.Globalization.CultureInfo]::InvariantCulture)
    $writer.WriteLine("$direction $time $hex  |$preview|")
    $writer.Flush()
}

$listener = $null
$clientSocket = $null
$targetClient = $null
$records = 0
$byteCounts = @{ C = 0L; S = 0L }
$closedBy = 'nobody'
try {
    $writer.WriteLine('# khi capture 1')
    $writer.WriteLine("# relay listen=$ListenPort target=${TargetHost}:$TargetPort")
    $writer.Flush()

    $listener = New-Object System.Net.Sockets.TcpListener([System.Net.IPAddress]::Loopback, $ListenPort)
    $listener.Start()
    Write-Host "khi relay: listening on port $ListenPort on this PC; forwarding to ${TargetHost}:$TargetPort"
    $clientSocket = $listener.AcceptSocket()
    $listener.Stop()
    $listener = $null
    $clientSocket.NoDelay = $true
    Write-Host 'khi relay: client connected'

    $targetClient = New-Object System.Net.Sockets.TcpClient
    try {
        $targetClient.Connect($TargetHost, $TargetPort)
    } catch {
        $writer.Dispose(); $writer = $null
        Remove-Item -LiteralPath $outPath -Force
        [Console]::Error.WriteLine("khi relay: cannot connect to ${TargetHost}:${TargetPort}: $($_.Exception.Message)")
        exit 4   # connection failed, as khi's exit code 4
    }
    $targetSocket = $targetClient.Client
    $targetSocket.NoDelay = $true
    Write-Host "khi relay: connected to ${TargetHost}:$TargetPort; logging to $outPath"

    $buffer = New-Object byte[] 65536
    $clock = [System.Diagnostics.Stopwatch]::StartNew()
    $zero = $null   # time zero is the receipt of the first byte (capture-format.md)
    $routes = @(
        @{ From = $clientSocket; To = $targetSocket; Direction = 'C' },
        @{ From = $targetSocket; To = $clientSocket; Direction = 'S' })
    $open = $true
    while ($open) {
        $moved = $false
        foreach ($route in $routes) {
            if (-not $route.From.Poll(0, [System.Net.Sockets.SelectMode]::SelectRead)) { continue }
            $count = 0
            try { $count = $route.From.Receive($buffer) } catch [System.Net.Sockets.SocketException] { $count = 0 }
            if ($count -le 0) { $open = $false; $closedBy = $route.Direction; break }
            $now = $clock.Elapsed.TotalSeconds   # stamped on receipt, before forwarding
            if ($null -eq $zero) { $zero = $now }
            $sent = 0
            try {
                while ($sent -lt $count) {
                    $sent += $route.To.Send($buffer, $sent, $count - $sent, [System.Net.Sockets.SocketFlags]::None)
                }
            } catch [System.Net.Sockets.SocketException] { $open = $false; $closedBy = $route.Direction }
            Write-Record $route.Direction ($now - $zero) $buffer $count
            $records++
            $byteCounts[$route.Direction] += $count
            $moved = $true
            if (-not $open) { break }
        }
        if ($open -and -not $moved) { Start-Sleep -Milliseconds 1 }
    }
} finally {
    foreach ($socket in @($clientSocket, $(if ($null -ne $targetClient) { $targetClient.Client } else { $null }))) {
        if ($null -ne $socket) {
            try { $socket.Shutdown([System.Net.Sockets.SocketShutdown]::Both) } catch { }
            $socket.Close()
        }
    }
    if ($null -ne $listener) { $listener.Stop() }
    if ($null -ne $writer) { $writer.Dispose() }
}

$side = switch ($closedBy) { 'C' { 'the client' } 'S' { 'the controller' } default { 'nobody' } }
Write-Host "khi relay: closed by $side; $records records, $($byteCounts['C']) bytes toward the controller, $($byteCounts['S']) bytes from it"
exit 0
