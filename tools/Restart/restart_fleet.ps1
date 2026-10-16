<#
.SYNOPSIS
    Restarts the instances Steam-<From>..<To> one at a time, lowest number first, leaving the highest
    ones off when memory runs out. With -Watch, restarts only the instances that crashed or hung.

.DESCRIPTION
    Run by two scheduled tasks on each PC (MULTI_INSTANCE_SETUP.md, 6.4): Firestone_Riavvio every 6
    hours, and Firestone_Watchdog every 5 minutes with -Watch.

    Restart: since Steam's October client update, every Steam start downloads and extracts it, fails
    to replace steam.exe inside the sandbox and rolls back; on 16/10, with 18 Steams launched 20 s
    apart, 14 hung without ever starting the game. Launched alone, waiting for Firestone.exe, each
    one came up in ~70 s. Each instance is stopped and started again in turn, so it is down for only
    ~1-2 minutes. Before starting one, Steam is trimmed and the available RAM is read: under
    -MinAvailableGB, that instance and every higher one are stopped and stay off until the next
    restart. The highest instance started is saved for the watchdog.

    Watch: an instance up to that highest one is restarted when Firestone.exe isn't running, or its
    MelonLoader log hasn't changed for 15 minutes (the bot never goes 8 minutes without a line). It
    does nothing while a restart is running.

    An instance without a Sandboxie box folder (Steam-0 on the main PC) is run outside the sandbox.
    Every step is logged to %TEMP%\restart_fleet.log.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File C:\Repos\FirestoneBot\tools\Restart\restart_fleet.ps1 -From 17 -To 34
    powershell -NoProfile -ExecutionPolicy Bypass -File C:\Repos\FirestoneBot\tools\Restart\restart_fleet.ps1 -From 17 -To 34 -Watch
#>
param(
    [int]$From = 17,
    [int]$To = 34,
    [double]$MinAvailableGB = 1.5,
    [switch]$Watch
)

$sandboxie = 'C:\Program Files\Sandboxie-Plus\Start.exe'
$trim = Join-Path $PSScriptRoot '..\SteamTrim\trim_steam.ps1'
$log = Join-Path $env:TEMP 'restart_fleet.log'
$topFile = Join-Path $env:TEMP 'restart_fleet.top'
$staleMinutes = 15

function Log([string]$text) {
    $line = '{0:yyyy-MM-dd HH:mm:ss} {1}' -f (Get-Date), $text
    Add-Content $log $line
    $line
}
function Get-Box([int]$n) { "C:\Sandbox\$env:USERNAME\SteamB$n" }
function Test-Boxed([int]$n) { Test-Path (Get-Box $n) }
function Test-Game([int]$n) { [bool](Get-Process Firestone -ErrorAction SilentlyContinue | Where-Object { $_.Path -like "*\Steam-$n\*" }) }
function Get-AvailableGB { (Get-CimInstance Win32_PerfFormattedData_PerfOS_Memory).AvailableBytes / 1GB }

function Get-GameLog([int]$n) {
    $relative = "Program Files (x86)\Steam-$n\steamapps\common\Firestone\MelonLoader\Latest.log"
    if (Test-Boxed $n) { Join-Path (Get-Box $n) "drive\C\$relative" } else { "C:\$relative" }
}

function Stop-Instance([int]$n) {
    if (Test-Boxed $n) { & $sandboxie "/box:SteamB$n" /terminate }
    else { Get-Process | Where-Object { $_.Path -like "C:\Program Files (x86)\Steam-$n\*" } | Stop-Process -Force -ErrorAction SilentlyContinue }
    Start-Sleep 5
}

# Starts the instance and waits for the game; a Steam that hangs gets one more try from scratch.
function Start-Instance([int]$n) {
    foreach ($try in 1, 2) {
        if ($try -eq 2) { Stop-Instance $n }
        $steam = "C:\Program Files (x86)\Steam-$n\steam.exe"
        if (Test-Boxed $n) { & $sandboxie "/box:SteamB$n" $steam -silent -applaunch 1013320 }
        else { Start-Process $steam -ArgumentList '-silent', '-applaunch', '1013320' }
        $start = Get-Date
        while (-not (Test-Game $n) -and ((Get-Date) - $start).TotalSeconds -lt 150) { Start-Sleep 10 }
        if (Test-Game $n) { return $true }
    }
    $false
}

# One run at a time: a restart waits for a watchdog pass to end, a watchdog pass skips a restart.
$mutex = New-Object System.Threading.Mutex($false, 'Global\FirebotRestartFleet')
try { $owned = $mutex.WaitOne($(if ($Watch) { 0 } else { -1 })) }
catch [System.Threading.AbandonedMutexException] { $owned = $true }   # the last run was killed mid-way
if (-not $owned) { return }

try {
    if ($Watch) {
        $top = if (Test-Path $topFile) { [int](Get-Content $topFile) } else { $To }
        foreach ($n in $From..[Math]::Min($top, $To)) {
            $file = Get-GameLog $n
            $reason = if (-not (Test-Game $n)) { 'game not running' }
                      elseif ((Test-Path $file) -and ((Get-Date) - (Get-Item $file).LastWriteTime).TotalMinutes -gt $staleMinutes) { "log unchanged for over $staleMinutes min" }
            if (-not $reason) { continue }
            Log "Watchdog: Steam-$n $reason, restarting it"
            Stop-Instance $n
            Log ("Watchdog: Steam-{0} {1}" -f $n, $(if (Start-Instance $n) { 'up' } else { 'FAILED, game not started' }))
        }
        return
    }

    Log "Restart of Steam-$From..$To"
    $top = $To
    foreach ($n in $From..$To) {
        Stop-Instance $n
        & powershell -NoProfile -ExecutionPolicy Bypass -File $trim -MinRamPercent 0 *> $null
        $available = Get-AvailableGB
        if ($available -lt $MinAvailableGB) {
            $n..$To | ForEach-Object { Stop-Instance $_ }
            $top = $n - 1
            Log ("Steam-$n..$To left off: {0:N1} GB available, under $MinAvailableGB" -f $available)
            break
        }
        $up = Start-Instance $n
        # A new game's memory grows for about a minute: measure the next one only after that.
        Start-Sleep 60
        Log ("Steam-{0}: {1}, {2:N1} GB available" -f $n, $(if ($up) { 'up' } else { 'FAILED, game not started' }), (Get-AvailableGB))
    }
    Set-Content $topFile $top
    Log "Done, watchdog covers Steam-$From..$top"
}
finally {
    $mutex.ReleaseMutex()
}
