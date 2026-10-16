<#
.SYNOPSIS
    Restarts the sandboxed instances Steam-<From>..<To> one at a time, lowest number first, and leaves
    the highest ones off when memory runs out.

.DESCRIPTION
    Run by the Firestone_Riavvio task on PC-Guesly (MULTI_INSTANCE_SETUP.md, 6.4) in place of
    Avvia_Tutte_Istanze_Firestone.bat. Since Steam's October client update, every Steam start
    downloads and extracts it, fails to replace steam.exe inside the sandbox and rolls back; on
    16/10, with 18 Steams launched 20 s apart, 14 hung without ever starting the game. Launched alone,
    waiting for Firestone.exe, each one came up in ~70 s.

    Each instance is stopped and started again in turn, so it is down for only ~1-2 minutes. Before
    starting one, Steam is trimmed and the available RAM is read: under -MinAvailableGB, that
    instance and every higher one are stopped and stay off until the next run. A Steam that hasn't
    started the game after 150 s gets its box restarted once more. Every step is logged to
    %TEMP%\restart_fleet.log.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File C:\Repos\FirestoneBot\tools\Restart\restart_fleet.ps1 -From 17 -To 34
#>
param(
    [int]$From = 17,
    [int]$To = 34,
    [double]$MinAvailableGB = 1.5
)

$sandboxie = 'C:\Program Files\Sandboxie-Plus\Start.exe'
$trim = Join-Path $PSScriptRoot '..\SteamTrim\trim_steam.ps1'
$log = Join-Path $env:TEMP 'restart_fleet.log'

function Log([string]$text) {
    $line = '{0:yyyy-MM-dd HH:mm:ss} {1}' -f (Get-Date), $text
    Add-Content $log $line
    $line
}
function Test-Game([int]$n) { [bool](Get-Process Firestone -ErrorAction SilentlyContinue | Where-Object { $_.Path -like "*\Steam-$n\*" }) }
function Get-AvailableGB { (Get-CimInstance Win32_PerfFormattedData_PerfOS_Memory).AvailableBytes / 1GB }

Log "Restart of Steam-$From..$To"
foreach ($n in $From..$To) {
    & $sandboxie "/box:SteamB$n" /terminate
    Start-Sleep 5
    & powershell -NoProfile -ExecutionPolicy Bypass -File $trim -MinRamPercent 0 *> $null
    $available = Get-AvailableGB
    if ($available -lt $MinAvailableGB) {
        $n..$To | ForEach-Object { & $sandboxie "/box:SteamB$_" /terminate }
        Log ("Steam-$n..$To left off: {0:N1} GB available, under $MinAvailableGB" -f $available)
        break
    }

    foreach ($try in 1, 2) {
        if ($try -eq 2) { & $sandboxie "/box:SteamB$n" /terminate; Start-Sleep 5 }
        & $sandboxie "/box:SteamB$n" "C:\Program Files (x86)\Steam-$n\steam.exe" -silent -applaunch 1013320
        $start = Get-Date
        while (-not (Test-Game $n) -and ((Get-Date) - $start).TotalSeconds -lt 150) { Start-Sleep 10 }
        if (Test-Game $n) { break }
    }

    # A new game's memory grows for about a minute: measure the next one only after that.
    Start-Sleep 60
    Log ("Steam-{0}: {1}, {2:N1} GB available" -f $n, $(if (Test-Game $n) { 'up' } else { 'FAILED, game not started' }), (Get-AvailableGB))
}
Log 'Done'
