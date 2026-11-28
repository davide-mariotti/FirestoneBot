# Deploys the current Release build to one test instance on this PC (the game-state branch's Steam-27
# and Steam-28, GAMESTATE_PLAN.md), optionally clears the saved schedule of some tasks so they run at
# once, and restarts the instance at once - never left to the watchdog.
#
#   powershell -File tools\Restart\deploy_test.ps1 -Instance 27
#   powershell -File tools\Restart\deploy_test.ps1 -Instance 28 -ResetTasks hallofheroestask,beaststask -FullRead
#
# -ResetTasks: config sections whose next_run_time_internal is emptied (the task runs on the first scan).
# -FullRead: also empties hallofheroestask's hero_snapshot_time, so its daily full read runs now.
# -Set: config entries to write, "empowertask.empower_rule=growth" (string values, added when missing).
param(
    [Parameter(Mandatory)][int]$Instance,
    [string[]]$ResetTasks = @(),
    [switch]$FullRead,
    # "section.key=value" entries written while the game is stopped (a string value without its quotes).
    [string[]]$Set = @()
)
$ErrorActionPreference = 'Stop'
# Through -File a comma list arrives as one string.
$ResetTasks = @($ResetTasks | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$Set = @($Set | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })

$out ='C:\Repos\FirestoneBot\src\bin\Release\net6.0'
$real = "C:\Program Files (x86)\Steam-$Instance\steamapps\common\Firestone\Mods"
$cfg = "C:\Sandbox\$env:USERNAME\SteamB$Instance\drive\C\Program Files (x86)\Steam-$Instance\steamapps\common\Firestone\UserData\FirebotPreferences.cfg"

# The game writes its config while it runs: stop it first, so the edit isn't overwritten.
Get-Process Firestone -ErrorAction SilentlyContinue | Where-Object { $_.Path -like "*\Steam-$Instance\*" } | Stop-Process -Force
Start-Sleep 3

if ($ResetTasks.Count -gt 0 -and (Test-Path $cfg)) {
    $text = [System.IO.File]::ReadAllText($cfg)
    foreach ($section in $ResetTasks) {
        $m = [regex]::Match($text, "(?s)\[$section\]\r?\n(.*?)(?=\r?\n\[|\z)")
        if (-not $m.Success) { Write-Output "[$section] not in the config"; continue }
        $body = $m.Groups[1].Value -replace 'next_run_time_internal = "[^"]*"', 'next_run_time_internal = ""'
        if ($FullRead -and $section -eq 'hallofheroestask') { $body = $body -replace 'hero_snapshot_time = "[^"]*"', 'hero_snapshot_time = ""' }
        $text = $text.Remove($m.Groups[1].Index, $m.Groups[1].Length).Insert($m.Groups[1].Index, $body)
    }
    [System.IO.File]::WriteAllText($cfg, $text, (New-Object System.Text.UTF8Encoding($false)))
}

if ($Set.Count -gt 0 -and (Test-Path $cfg)) {
    $text = [System.IO.File]::ReadAllText($cfg)
    foreach ($entry in $Set) {
        if ($entry -notmatch '^([^.=]+)\.([^=]+)=(.*)$') { Write-Output "bad -Set '$entry'"; continue }
        $section, $key, $value = $Matches[1], $Matches[2], $Matches[3]
        $m = [regex]::Match($text, "(?s)\[$section\]\r?\n(.*?)(?=\r?\n\[|\z)")
        if (-not $m.Success) { Write-Output "[$section] not in the config"; continue }
        $line = "$key = `"$value`""
        $body = $m.Groups[1].Value
        $body = if ($body -match "(?m)^$key = .*$") { $body -replace "(?m)^$key = .*$", $line } else { "$line`r`n$body" }
        $text = $text.Remove($m.Groups[1].Index, $m.Groups[1].Length).Insert($m.Groups[1].Index, $body)
        Write-Output "[$section] $line"
    }
    [System.IO.File]::WriteAllText($cfg, $text, (New-Object System.Text.UTF8Encoding($false)))
}

$stamp = Get-Date -Format 'yyyyMMddHHmmss'
foreach ($dll in 'firebot.dll', 'Firebot.TalentEngine.dll') {
    if (Test-Path "$real\$dll") { Rename-Item "$real\$dll" "$dll.old$stamp" }
    Copy-Item "$out\$dll" $real
}
# Drop the .old copies no running game holds any more.
Get-ChildItem $real -Filter '*.old*' | ForEach-Object { try { Remove-Item $_.FullName -ErrorAction Stop } catch {} }

powershell -NoProfile -ExecutionPolicy Bypass -File C:\Repos\FirestoneBot\tools\Restart\restart_fleet.ps1 -Instance $Instance 2>&1 | Select-Object -Last 1
