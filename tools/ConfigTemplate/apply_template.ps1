<#
.SYNOPSIS
    Aligns the FirebotPreferences.cfg of Steam-<From>..<To> to FirebotPreferences.template.cfg, which is
    Steam-0's configuration (TESTING.md, "Configurazione di riferimento").

.DESCRIPTION
    Copies every setting that isn't auto-managed from the template. Keeps each account's own state
    (next runs, daily counters, known_maxed_nodes, hero_snapshot, campaign defeats) and its talentstask.guide_start_index, and sets
    window_grid_first_instance to -FirstInstance. A sandboxed instance gets both copies: the real path
    and the one inside C:\Sandbox\<user>\SteamB<N>. Files are written as UTF-8 without BOM.

    Run it with the games closed - they rewrite the file when they exit. A key the file doesn't have
    yet is only reported: start that instance once with the current DLL (it adds the missing
    sections), close it, and run this again.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\ConfigTemplate\apply_template.ps1 -From 17 -To 34 -Check
    powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\ConfigTemplate\apply_template.ps1 -From 17 -To 34
#>
param(
    [int]$From = 0,
    [int]$To = 16,
    # The instance in the grid's top-left cell: the lowest number on this PC.
    [int]$FirstInstance = $From,
    # Explicit files instead of the Steam-<From>..<To> range.
    [string[]]$File,
    # Only report the differences.
    [switch]$Check
)

$ErrorActionPreference = 'Stop'

$autoManaged = 'next_run_time_internal', 'last_done_date', 'gear_chests_opened_today', 'gear_chests_date',
    'reserve_override_used_date', 'plays_done_today', 'plays_done_date', 'known_maxed_nodes', 'hero_snapshot',
    'hero_snapshot_time', 'defeats'
$perAccount = @('talentstask.guide_start_index')

# "section.key" -> value, for every line of a cfg that is a setting to copy.
function Get-Settings([string[]]$lines) {
    $settings = @{}
    $section = $null
    foreach ($line in $lines) {
        if ($line -match '^\[(.+)\]$') { $section = $Matches[1]; continue }
        if ($section -and $line -match '^([a-z_]+) = (.*)$' -and $autoManaged -notcontains $Matches[1]) {
            $key = "$section.$($Matches[1])"
            if ($perAccount -notcontains $key) { $settings[$key] = $Matches[2] }
        }
    }
    $settings
}

$reference = Get-Settings ([IO.File]::ReadAllLines((Join-Path $PSScriptRoot 'FirebotPreferences.template.cfg')))
$reference['firebot_settings.window_grid_first_instance'] = "$FirstInstance"

if (-not $File) {
    $running = Get-Process Firestone -ErrorAction SilentlyContinue |
        Where-Object { $_.Path -match '\\Steam-(\d+)\\' -and [int]$Matches[1] -ge $From -and [int]$Matches[1] -le $To }
    if ($running -and -not $Check) { throw "Chiudi prima il gioco di Steam-$From..$To (il gioco riscrive il cfg quando esce)." }

    # A sandboxed game only writes the copy inside its box: the real one is just kept in step, so its
    # missing keys aren't worth reporting.
    $fallbacks = @()
    $File = foreach ($n in $From..$To) {
        $relative = "Program Files (x86)\Steam-$n\steamapps\common\Firestone\UserData\FirebotPreferences.cfg"
        $real = "C:\$relative"
        $boxed = "C:\Sandbox\$env:USERNAME\SteamB$n\drive\C\$relative"
        if (Test-Path $boxed) {
            $boxed
            if (Test-Path $real) { $real; $fallbacks += $real }
        }
        elseif (Test-Path $real) { $real }
        else { Write-Host "Steam-${n}: nessun cfg (avvia l'istanza una volta con la DLL nuova)." }
    }
}

$utf8NoBom = New-Object System.Text.UTF8Encoding $false
$total = 0
foreach ($path in $File) {
    $bytes = [IO.File]::ReadAllBytes($path)
    $text = [IO.File]::ReadAllText($path)
    $newline = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
    $lines = $text -split "`r?`n"

    $changes = @()
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { $changes += '  BOM tolto' }

    $seen = @{}
    $section = $null
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^\[(.+)\]$') { $section = $Matches[1]; continue }
        if ($section -and $lines[$i] -match '^([a-z_]+) = (.*)$') {
            $key = "$section.$($Matches[1])"
            $seen[$key] = $true
            if ($reference.ContainsKey($key) -and $Matches[2] -ne $reference[$key]) {
                $changes += "  ${key}: $($Matches[2]) -> $($reference[$key])"
                $lines[$i] = "$($Matches[1]) = $($reference[$key])"
            }
        }
    }
    $missing = @($reference.Keys | Where-Object { -not $seen.ContainsKey($_) -and $fallbacks -notcontains $path } | Sort-Object)

    if ($changes -or $missing) {
        Write-Host "${path}: $($changes.Count) da cambiare, $($missing.Count) chiavi mancanti"
        $changes | ForEach-Object { Write-Host $_ }
        if ($missing) { Write-Host "  mancano: $($missing -join ', ')" }
    }
    if ($changes -and -not $Check) { [IO.File]::WriteAllText($path, ($lines -join $newline), $utf8NoBom) }
    $total += $changes.Count
}

Write-Host ("{0} file, {1} valori {2}." -f @($File).Count, $total, $(if ($Check) { 'da cambiare' } else { 'cambiati' }))
