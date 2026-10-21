<#
.SYNOPSIS
    Restarts the instances Steam-<From>..<To> a few at a time, lowest number first, leaving the highest
    ones off when memory runs out. With -Watch, restarts only the instances that crashed or hung.

.DESCRIPTION
    Run by two scheduled tasks on each PC (MULTI_INSTANCE_SETUP.md, 6.4): Firestone_Riavvio every 6
    hours, and Firestone_Watchdog every 5 minutes with -Watch.

    Restart: since Steam's October client update, every Steam start downloaded and extracted it,
    failed to replace steam.exe inside the sandbox and rolled back; on 16/10, with 18 Steams launched
    20 s apart, 14 hung without ever starting the game. A steam.cfg with BootStrapperInhibitAll
    (written before each start) skips that: 5 Steams launched together all had their game in 48-87 s
    (17/10). The instances are stopped and started -Batch at a time, so each one is down for ~2
    minutes. Before a group starts, Steam is trimmed and the available RAM is read: only the
    instances that keep it over -MinAvailableGB, at -GameGB a game, are started, the others with the
    next group; under -MinAvailableGB the rest stay off until the next restart. The highest started is saved
    for the watchdog.

    Watch: an instance up to that highest one is restarted when Firestone.exe isn't running, or its
    MelonLoader log hasn't changed for 15 minutes (the bot never goes 8 minutes without a line). It
    does nothing while a restart is running.

    Stop: stops every instance that is running and pauses the restart and the watchdog
    (%TEMP%\restart_fleet.paused) until a restart is started by hand. The three bats on the desktop
    (tools\Restart\desktop) wrap -Stop, -Instance and the Firestone_Riavvio task.

    An instance without a Sandboxie box folder (Steam-0 on the main PC) is run outside the sandbox.
    Every step is logged to %TEMP%\restart_fleet.log.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File C:\Repos\FirestoneBot\tools\Restart\restart_fleet.ps1 -From 17 -To 31
    powershell -NoProfile -ExecutionPolicy Bypass -File C:\Repos\FirestoneBot\tools\Restart\restart_fleet.ps1 -From 17 -To 31 -Watch
#>
param(
    [int]$From = 17,
    [int]$To = 31,
    [double]$MinAvailableGB = 1.5,
    # Instances a restart starts together, and the RAM each new game is counted for.
    [int]$Batch = 5,
    [double]$GameGB = 1.5,
    [switch]$Watch,
    # Restarts just this instance, leaving the watchdog's range as it is.
    [int]$Instance = -1,
    # Stops every running instance and pauses the scheduled restart and the watchdog.
    [switch]$Stop,
    # Lifts the pause set by -Stop (used by the start-all bat before the restart).
    [switch]$Resume
)

$sandboxie = 'C:\Program Files\Sandboxie-Plus\Start.exe'
$trim = Join-Path $PSScriptRoot '..\SteamTrim\trim_steam.ps1'
$log = Join-Path $env:TEMP 'restart_fleet.log'
$topFile = Join-Path $env:TEMP 'restart_fleet.top'
$pauseFile = Join-Path $env:TEMP 'restart_fleet.paused'
if ($Resume) { Remove-Item $pauseFile -ErrorAction SilentlyContinue; return }
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

function Stop-Instance([int[]]$ns) {
    # The games first, then a pause so Steam sees them exit and closes its Steam Cloud session: killing
    # both at once left Steam-2 asking about "pending cloud sessions" at every launch (16/10, 10 hours).
    $games = Get-Process Firestone -ErrorAction SilentlyContinue | Where-Object { $p = $_.Path; $ns | Where-Object { $p -like "*\Steam-$_\*" } }
    if ($games) { $games | Stop-Process -Force -ErrorAction SilentlyContinue; Start-Sleep 15 }

    foreach ($n in $ns) { Stop-Steam $n }
    Start-Sleep 5
}

function Stop-Steam([int]$n) {
    if (Test-Boxed $n) { & $sandboxie "/box:SteamB$n" /terminate; return }

    # Outside the sandbox. Found through WMI: Get-Process can't read some processes' path, and an
    # orphaned UnityCrashHandler64 it missed made Steam-0 fail every launch as "already running"
    # (AppError_16, 16/10 18:00 to the reboot).
    $folder = "C:\Program Files (x86)\Steam-$n\*"
    foreach ($try in 1..3) {
        $left = @(Get-CimInstance Win32_Process | Where-Object { $_.ExecutablePath -like $folder })
        if (-not $left) { break }
        $left | ForEach-Object { taskkill /F /T /PID $_.ProcessId 2>&1 | Out-Null }
        Start-Sleep 3
    }
    $left = @(Get-CimInstance Win32_Process | Where-Object { $_.ExecutablePath -like $folder })
    if ($left) { Log ("Steam-${n}: still running after stop: " + (($left | ForEach-Object { "$($_.Name) ($($_.ProcessId))" }) -join ', ')) }
}

# Steam Cloud off for Firestone, whose progress lives on the game's servers: a Steam stopped mid-upload
# left a pending cloud operation, and every later launch waited on "Cloud out of date" for a click on
# "Play anyway" (Steam-17, 18/10 00:00-12:32). The switch is apps\1013320\cloudenabled in the
# account's sharedconfig.vdf (in localconfig.vdf Steam ignores it); with it the launch logs "AC Launch,
# Sync Disabled" and Steam uploads the file to the account. Written while Steam is off. A boxed
# instance reads the real file until Steam writes its own copy in the box, so both are edited.
function Disable-Cloud([int]$n) {
    $userdata = "Program Files (x86)\Steam-$n\userdata"
    $roots = @("C:\$userdata") + @(if (Test-Boxed $n) { Join-Path (Get-Box $n) "drive\C\$userdata" })
    foreach ($file in Get-ChildItem ($roots | ForEach-Object { "$_\*\7\remote\sharedconfig.vdf" }) -ErrorAction SilentlyContinue) {
        $text = [IO.File]::ReadAllText($file.FullName)
        if ($text -match '"1013320"\s*\{[^{}]*"cloudenabled"') { continue }
        if ($text -notmatch '"1013320"\s*\{') {
            if ($text -notmatch '"apps"\s*\{') { $text = ([regex]'"Steam"\s*\{').Replace($text, "`$0`n`"apps`"`n{`n}", 1) }
            $text = ([regex]'"apps"\s*\{').Replace($text, "`$0`n`"1013320`"`n{`n}", 1)
        }
        $text = ([regex]'"1013320"\s*\{').Replace($text, "`$0`n`"cloudenabled`"`t`"0`"", 1)
        [IO.File]::WriteAllText($file.FullName, $text, (New-Object Text.UTF8Encoding $false))
    }
}

# steam.cfg stops the client update every start tried and rolled back (see above): without it, Steams
# launched together hung; with it each one starts ~15 s sooner and 5 at once all came up (17/10).
function Send-Start([int]$n) {
    $folder = "C:\Program Files (x86)\Steam-$n"
    if (-not (Test-Path "$folder\steam.cfg")) { Set-Content "$folder\steam.cfg" 'BootStrapperInhibitAll=enable' -ErrorAction SilentlyContinue }
    Disable-Cloud $n
    if (Test-Boxed $n) { & $sandboxie "/box:SteamB$n" "$folder\steam.exe" -silent -applaunch 1013320 }
    else { Start-Process "$folder\steam.exe" -ArgumentList '-silent', '-applaunch', '1013320' }
}

function Wait-Game([int[]]$ns) {
    $start = Get-Date
    while (@($ns | Where-Object { -not (Test-Game $_) }).Count -and ((Get-Date) - $start).TotalSeconds -lt 150) { Start-Sleep 2 }
}

# Starts stopped instances together; a Steam that hung gets the single-instance start, from scratch.
function Start-Group([int[]]$ns, [string]$label) {
    $ns | ForEach-Object { Send-Start $_ }
    Wait-Game $ns
    foreach ($n in $ns) {
        $up = Test-Game $n
        if (-not $up) { Stop-Instance $n; $up = Start-Instance $n }
        Log ("{0}-{1}: {2}" -f $label, $n, $(if ($up) { 'up' } else { 'FAILED, game not started' }))
    }
}

# Starts the instance and waits for the game; a Steam that hangs gets one more try from scratch.
function Start-Instance([int]$n) {
    foreach ($try in 1, 2) {
        if ($try -eq 2) { Stop-Instance $n }
        Send-Start $n
        Wait-Game $n
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
    if ($Stop) {
        Set-Content $pauseFile (Get-Date)
        $running = @(Get-CimInstance Win32_Process | ForEach-Object { if ($_.ExecutablePath -match '\\Steam-(\d+)\\') { [int]$Matches[1] } } | Sort-Object -Unique)
        Log ("Stop of all instances ({0}); restart and watchdog paused" -f ($running -join ','))
        if ($running) { Stop-Instance $running }
        Log 'Stopped'
        return
    }
    if ($Instance -ge 0) {
        Log "Restart of Steam-$Instance alone"
        Stop-Instance $Instance
        Log ("Steam-{0}: {1}" -f $Instance, $(if (Start-Instance $Instance) { 'up' } else { 'FAILED, game not started' }))
        return
    }
    if (Test-Path $pauseFile) {
        if (-not $Watch) { Log "Restart skipped: paused by the stop-all bat since $(Get-Content $pauseFile)" }
        return
    }
    if ($Watch) {
        $top = if (Test-Path $topFile) { [int](Get-Content $topFile) } else { $To }
        $dead = @(foreach ($n in $From..[Math]::Min($top, $To)) {
            $file = Get-GameLog $n
            $reason = if (-not (Test-Game $n)) { 'game not running' }
                      elseif ((Test-Path $file) -and ((Get-Date) - (Get-Item $file).LastWriteTime).TotalMinutes -gt $staleMinutes) { "log unchanged for over $staleMinutes min" }
            if (-not $reason) { continue }
            Log "Watchdog: Steam-$n $reason, restarting it" | Out-Host
            $n
        })
        # After a PC reboot that's every instance: a group at a time, like the restart.
        for ($i = 0; $i -lt $dead.Count; $i += $Batch) {
            $group = @($dead[$i..([Math]::Min($i + $Batch, $dead.Count) - 1)])
            Stop-Instance $group
            Start-Group $group 'Watchdog: Steam'
        }
        return
    }

    Log "Restart of Steam-$From..$To, $Batch at a time"
    $top = $To
    $n = $From
    while ($n -le $To) {
        $group = @($n..[Math]::Min($n + $Batch - 1, $To))
        Stop-Instance $group
        & powershell -NoProfile -ExecutionPolicy Bypass -File $trim -MinRamPercent 0 *> $null
        # Each new game takes ~1.3 GB in its first minute (17/10): only the ones that fit start now, the
        # rest go to the next group, measured once these have grown.
        $available = Get-AvailableGB
        $fit = [int][Math]::Min($group.Count, [Math]::Floor(($available - $MinAvailableGB) / $GameGB) + 1)
        if ($fit -le 0) {
            Stop-Instance ($n..$To)
            $top = $n - 1
            Log ("Steam-$n..$To left off: {0:N1} GB available, under $MinAvailableGB" -f $available)
            break
        }
        $group = @($group | Select-Object -First $fit)
        $n += $fit
        Start-Group $group 'Steam'
        # The games' memory grows for about a minute: the next group is measured after that, but only
        # when the RAM is close enough to the limit for it to matter.
        if ($n -le $To -and (Get-AvailableGB) -lt $MinAvailableGB + $GameGB * $Batch) { Start-Sleep 60 }
        Log ("{0:N1} GB available" -f (Get-AvailableGB))
    }
    Set-Content $topFile $top
    Log "Done, watchdog covers Steam-$From..$top"
}
finally {
    $mutex.ReleaseMutex()
}
