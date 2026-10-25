<#
.SYNOPSIS
    Emails a one-table daily report of every instance: level, stage, power, currencies, quests,
    battles, errors and watchdog restarts over the last 24 hours, each with the change since the
    previous report.

.DESCRIPTION
    Runs on PC-Guesly (MULTI_INSTANCE_SETUP.md, 6.5). Reads the MelonLoader logs of -Local here and
    of -Remote on the main PC over PowerShell remoting ($env:FIREBOT_REMOTE_HOST, credential in
    $HOME\desktop-admin.cred). Level, power and currencies come from the bot's hourly
    "[INFO] Snapshot:" line; max stage, empowers, dailies, map missions, Arena fights and guild
    donations from the profile statistics in its "[INFO] Statistics:" line; failed tasks from its
    [FAILED] lines and restarts from the watchdog's log. Each report's numbers are kept in
    %LOCALAPPDATA%\FirebotReport\<date>.json, and the next one shows the change against them.

    Mail goes through Gmail (smtp.gmail.com:587) with an app password saved in $HOME\gmail.cred, to
    $env:FIREBOT_REPORT_TO. Both env values come from the repo's .env. -NoSend only writes the HTML,
    and doesn't save the numbers, so it doesn't move the baseline.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File C:\Repos\FirestoneBot\tools\Report\daily_report.ps1
#>
param(
    [int[]]$Local = 17..31,
    [int[]]$Remote = 0..16,
    [switch]$NoSend
)

$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
Get-Content (Join-Path $repo '.env') | Where-Object { $_ -match '^\w+=' } | ForEach-Object { $k, $v = $_ -split '=', 2; Set-Item "env:$k" $v }

# Runs on each PC: one record per instance from the last 24 hours of its logs.
$collect = {
    param([int[]]$range)
    $since = (Get-Date).AddHours(-24)
    $fleetLog = @(Get-ChildItem 'C:\Users\*\AppData\Local\Temp\restart_fleet.log' -ErrorAction SilentlyContinue |
        ForEach-Object { Get-Content $_.FullName } |
        Where-Object { $_ -match '^(\d{4}-\d\d-\d\d \d\d:\d\d:\d\d) Watchdog: Steam-(\d+) .*restarting' -and [datetime]$Matches[1] -ge $since })

    function Read-Shared([string]$path) {
        $stream = [IO.File]::Open($path, 'Open', 'Read', 'ReadWrite')
        try { (New-Object IO.StreamReader($stream)).ReadToEnd() -split "`r?`n" } finally { $stream.Close() }
    }

    foreach ($n in $range) {
        $relative = "Program Files (x86)\Steam-$n\steamapps\common\Firestone\MelonLoader"
        $box = Get-ChildItem "C:\Sandbox\*\SteamB$n" -Directory -ErrorAction SilentlyContinue | Select-Object -First 1
        $dir = if ($box) { Join-Path $box.FullName "drive\C\$relative" } else { "C:\$relative" }
        $online = [bool](Get-Process Firestone -ErrorAction SilentlyContinue | Where-Object { $_.Path -like "*\Steam-$n\*" })

        # One file per game session, named after its start; the running session is only in Latest.log.
        $sessions = @(Get-ChildItem "$dir\Logs\*.log" -ErrorAction SilentlyContinue | Where-Object { $_.LastWriteTime -ge $since } | Sort-Object Name)
        $lines = foreach ($file in $sessions) {
            $start = [datetime]::ParseExact($file.BaseName, 'yyyy-MM-dd_HH-mm-ss', $null)
            $path = if ($online -and $file -eq $sessions[-1]) { "$dir\Latest.log" } else { $file.FullName }
            $day = $start.Date; $previous = $start.TimeOfDay
            foreach ($line in (Read-Shared $path)) {
                if ($line -notmatch '^\[(\d\d):(\d\d):(\d\d)\.\d+\] \[Firebot\] (\[INFO\]|\[FAILED\]|\[DEBUG\] \[[^\]]+\] \[INFO\])') { continue }
                $time = New-Object TimeSpan ([int]$Matches[1]), ([int]$Matches[2]), ([int]$Matches[3])
                if ($time -lt $previous - [TimeSpan]::FromHours(1)) { $day = $day.AddDays(1) }
                $previous = $time
                if ($day + $time -ge $since) { $line }
            }
        }

        $snapshots = @($lines | Where-Object { $_ -match 'Snapshot: level (\d+), stage (-?\d+), power (\d+), stars (\d+), gems (\d+), dust (\d+), blood (\d+)(?:, guild (\d+))?' } |
            ForEach-Object { [pscustomobject]@{ level = [int]$Matches[1]; stage = [int]$Matches[2]; power = [double]$Matches[3]; stars = [int]$Matches[4]; gems = [double]$Matches[5]; dust = [double]$Matches[6]; blood = [double]$Matches[7]; guild = if ($Matches[8]) { [double]$Matches[8] } else { $null } } })
        $last = $snapshots | Select-Object -Last 1

        # The profile's Statistics tab: "Name=text; ..." as the game shows it ("1.875"; "27,0gc" isn't a plain number).
        $stats = @{}
        $statsLine = $lines | Where-Object { $_ -match '\[INFO\] Statistics: (.*)\.$' } | Select-Object -Last 1
        if ($statsLine -and $statsLine -match '\[INFO\] Statistics: (.*)\.$') {
            foreach ($pair in $Matches[1] -split '; ') {
                $name, $text = $pair -split '=', 2
                if ($text -match '^[\d.]+$') { $stats[$name] = [double]($text -replace '\.', '') }
            }
        }

        # The formation check (FormationTask, an hour after the 08:00 UTC reset): its last "now" line and the "after" that followed it.
        $formNow = $null; $formAfter = $null
        foreach ($line in $lines) {
            if ($line -match '\[INFO\] Formation: now (.*)$') { $formNow = $Matches[1]; $formAfter = $null }
            elseif ($line -match '\[INFO\] Formation: after (.*)$') { $formAfter = $Matches[1] }
        }

        [pscustomobject]@{
            n        = $n
            formNow   = $formNow
            formAfter = $formAfter
            online   = $online
            level    = if ($last) { $last.level } else { $null }
            stage    = $stats['MaxStageReached']
            power    = if ($last) { $last.power } else { $null }
            gems     = if ($last) { $last.gems } else { $null }
            guild    = if ($last) { $last.guild } else { $null }
            dust     = if ($last) { $last.dust } else { $null }
            blood    = if ($last) { $last.blood } else { $null }
            heroPower  = $stats['TotalHeroPowerLevel']
            research   = $stats['FirestoneResearchesCompleted']
            meteorite  = $stats['MeteoriteResearchesCompleted']
            guardian   = if ($null -ne $stats['GuardianTrainingCompleted']) { $stats['GuardianTrainingCompleted'] + $stats['GuardianEnlighenmentsCompleted'] } else { $null }
            awakenings = $stats['TotalAwakenings']
            empowers   = $stats['PrestigeNumber']
            rituals    = $stats['RitualsPerformed']
            dailies    = $stats['CompletedAllDailies']
            allMissions    = $stats['CompletedAllMissions']
            allExpeditions = $stats['CompletedAllExpeditions']
            missions   = $stats['MapMissionsWon']
            arena      = $stats['AOKFightsParticipated']
            arenaWins  = $stats['DefeatedAOKPlayers']
            donated    = $stats['GuildCoinsDonated']
            failed   = @($lines | Where-Object { $_ -match '\[Firebot\] \[FAILED\]' }).Count
            restarts = @($fleetLog | Where-Object { $_ -match "Watchdog: Steam-$n " }).Count
            sessions = $sessions.Count
        }
    }
}

$rows = @(& $collect $Local)
if ($Remote) {
    $rows += Invoke-Command -ComputerName $env:FIREBOT_REMOTE_HOST -Credential (Import-Clixml "$HOME\desktop-admin.cred") -ScriptBlock $collect -ArgumentList (, $Remote) |
        Select-Object * -ExcludeProperty PSComputerName, RunspaceId, PSShowComputerName
}
$rows = @($rows | Sort-Object n)

$store = Join-Path $env:LOCALAPPDATA 'FirebotReport'
New-Item -ItemType Directory -Force $store | Out-Null
$today = Get-Date
# Each run is compared with the last one before it, same day included (a manual test moves the baseline).
$previousFile = Get-ChildItem "$store\*.json" | Sort-Object Name | Select-Object -Last 1
$before = @{}
$previousTime = $null
if ($previousFile) {
    (Get-Content $previousFile.FullName -Raw | ConvertFrom-Json) | ForEach-Object { $before[[int]$_.n] = $_ }
    $previousTime = [datetime]::ParseExact($previousFile.BaseName, 'yyyy-MM-dd_HHmm', $null)
}
$stamp = $today.ToString('yyyy-MM-dd_HHmm')

# --- HTML ---
$it = [Globalization.CultureInfo]::GetCultureInfo('it-IT')
function Format-Number($value) { if ($null -eq $value) { '&ndash;' } else { ([double]$value).ToString('N0', $it) } }
function Cell($row, [string]$key, [switch]$Abbreviate) {
    $value = $row.$key
    $text = Format-Number $value
    if ($Abbreviate -and $null -ne $value -and [double]$value -ge 1e6) { $text = ([double]$value / 1e6).ToString('0.0', $it) + ' M' }
    $old = $before[[int]$row.n]
    if ($null -ne $value -and $old -and $null -ne $old.$key) {
        $delta = [double]$value - [double]$old.$key
        if ($delta -ne 0) {
            $color = if ($delta -gt 0) { '#1a7f37' } else { '#cf222e' }
            $sign = if ($delta -gt 0) { '+' } else { '' }
            $d = if ($Abbreviate -and [math]::Abs($delta) -ge 1e6) { ($delta / 1e6).ToString('0.0', $it) + ' M' } else { $delta.ToString('N0', $it) }
            $text += " <span style=`"color:$color;font-size:11px`">$sign$d</span>"
        }
    }
    $text
}

$td = 'padding:4px 6px;border-bottom:1px solid #eaeef2;text-align:right;white-space:nowrap'
$th = 'padding:5px 6px;border-bottom:2px solid #d0d7de;text-align:right;font-weight:600;color:#57606a;white-space:nowrap'
$headers = 'Instance', 'Status', 'Level', 'Max stage', 'Hero power', 'WM power', 'Research', 'Meteorite', 'Guardian', 'Awakenings', 'Empowers', 'Rituals', 'Gems', 'Guild coins', 'Dust', 'Blood', 'All dailies', 'All missions', 'All expeditions', 'Map missions', 'Arena fights', 'Arena wins', 'Guild donated', 'Errors', 'Restarts'
$body = foreach ($r in $rows) {
    $state = if ($r.online) { '<span style="color:#1a7f37">&#9679; online</span>' } else { '<span style="color:#cf222e">&#9679; offline</span>' }
    $restartCell = if ($r.restarts) { "<b style=`"color:#cf222e`">$($r.restarts)</b>" } else { '0' }
    $cells = "Steam-$($r.n)", $state, (Cell $r level), (Cell $r stage), (Cell $r heroPower -Abbreviate), (Cell $r power -Abbreviate),
        (Cell $r research), (Cell $r meteorite), (Cell $r guardian), (Cell $r awakenings), (Cell $r empowers), (Cell $r rituals),
        (Cell $r gems), (Cell $r guild), (Cell $r dust), (Cell $r blood), (Cell $r dailies), (Cell $r allMissions), (Cell $r allExpeditions),
        (Cell $r missions), (Cell $r arena), (Cell $r arenaWins), (Cell $r donated -Abbreviate), $r.failed, $restartCell
    '<tr>' + (($cells | ForEach-Object { "<td style=`"$td`">$_</td>" }) -join '') + '</tr>'
}

# --- Formation ---
# "Boris, Talia, ... (leader Solaine, score 63.54); best ... (leader Boris, score 63.61, x1.17); target ... (leader Talia); leader ratio 0.07; changing."
function Team([string]$names, [string]$leader) {
    (($names -split ', ') | ForEach-Object { if ($_ -eq $leader) { "<b>$_</b>&#9733;" } else { $_ } }) -join ', '
}
$formation = foreach ($r in $rows) {
    $now = if ($r.formNow -match '^(.+?) \(leader (\w+), score [\d.]+\); best (.+?) \(leader (\w+), score [\d.]+, x([\d.]+)\); target (.+?) \(leader (\w+)\)') { $Matches } else { $null }
    $cells = if (-not $now) { 'nessun controllo nelle ultime 24 ore', '', '' }
    else {
        $after = if ($r.formAfter -match '^(.+?) \(leader (\w+), score [\d.]+, x([\d.]+) on before\); (.+)\.$') { $Matches } else { $null }
        if ($after -and $after[4] -eq 'as planned') {
            (Team $after[1] $after[2]), ("<span style=`"color:#1a7f37`">cambiata, x$($after[3])</span> &middot; prima: " + (Team $now[1] $now[2]))
        }
        elseif ($after) { (Team $now[1] $now[2]), '<span style="color:#cf222e">cambio non riuscito</span>' }
        else { (Team $now[1] $now[2]), "<span style=`"color:#57606a`">tenuta (la migliore rendeva x$($now[5]))</span>" }
        Team $now[6] $now[7]
    }
    $cells = @($cells)
    "<tr><td style=`"$td`">Steam-$($r.n)</td>" + (($cells | ForEach-Object { "<td style=`"$td;text-align:left`">$_</td>" }) -join '') + '</tr>'
}

$online = @($rows | Where-Object online).Count
function Problem([string]$label, $matching, [scriptblock]$detail = { "$($_.n)" }) {
    $list = @($matching | ForEach-Object $detail)
    if ($list) { "<b>${label}:</b> " + ($list -join ', ') }
}
$problems = @(
    Problem 'Ferme' ($rows | Where-Object { -not $_.online })
    Problem 'Riavviate dal watchdog' ($rows | Where-Object { $_.restarts }) { "$($_.n) (x$($_.restarts))" }
    Problem 'Livello fermo dal report precedente' ($rows | Where-Object { $old = $before[[int]$_.n]; $previousTime -and ($today - $previousTime).TotalHours -ge 20 -and $old -and $null -ne $_.level -and $_.level -eq $old.level })
    Problem 'Senza snapshot nelle ultime 24 ore' ($rows | Where-Object { $_.online -and $null -eq $_.level })
    Problem 'Cambio di formazione non riuscito' ($rows | Where-Object { $_.formAfter -and $_.formAfter -notmatch 'as planned\.$' })
)
$levels = ($rows | Where-Object { $old = $before[[int]$_.n]; $old -and $null -ne $_.level -and $null -ne $old.level } | ForEach-Object { $_.level - $old.level } | Measure-Object -Sum).Sum
$compared = if ($previousFile) { "Differenze rispetto al report del $($previousTime.ToString('dd/MM')) alle $($previousTime.ToString('HH:mm'))." } else { 'Primo report: le differenze compariranno da domani.' }

$html = @"
<div style="font-family:Segoe UI,Helvetica,Arial,sans-serif;font-size:13px;color:#1f2328">
<p style="font-size:15px;margin:0 0 4px"><b>$online/$($rows.Count) istanze attive</b>$(if ($previousFile) { " &middot; $levels $(if ($levels -eq 1) { 'livello guadagnato' } else { 'livelli guadagnati' }) in tutto" })</p>
<p style="margin:0 0 12px;color:#57606a">Ultime 24 ore, fino alle $($today.ToString('HH:mm')). $compared</p>
$(if ($problems) { '<p style="margin:0 0 12px;color:#cf222e">' + ($problems -join '<br>') + '</p>' } else { '<p style="margin:0 0 12px;color:#1a7f37">Nessun problema.</p>' })
<table style="border-collapse:collapse;font-size:12px">
<tr>$(($headers | ForEach-Object { "<th style=`"$th`">$_</th>" }) -join '')</tr>
$($body -join "`n")
</table>
<p style="margin:12px 0 0;color:#8c959f;font-size:11px">Valori all'ultimo snapshot del bot (ogni ora); il numero piccolo &egrave; la differenza dal report precedente. WM power = potenza della squadra di war machine. Dal profilo (Statistics), come totali: Max stage = Max Stage Reached; Hero power = potenza totale degli eroi; Research e Meteorite = ricerche Firestone e Meteorite completate; Guardian = addestramenti + illuminazioni del guardiano; Awakenings; Empowers = empower al Temple; Rituals = rituali dell'Oracolo; All dailies / All missions / All expeditions = giorni con tutte le giornaliere / missioni della mappa / spedizioni completate (+1 al giorno = tutto fatto); Map missions = missioni vinte; Arena fights / Arena wins = combattimenti / vittorie nell'Arena of Kings; Guild donated = Guild Coin donate. Errors = task falliti nelle ultime 24 ore. Restarts = interventi del watchdog nelle ultime 24 ore.</p>
<p style="font-size:15px;margin:20px 0 4px"><b>Formazione</b></p>
<p style="margin:0 0 8px;color:#57606a">Dal controllo giornaliero un'ora dopo il reset: 11:02 con l'ora legale, 10:02 con quella solare (FORMATION_PLAN.md). &#9733; = leader. La squadra cambia solo se rende almeno il 10% in pi&ugrave;; x = danno di squadra dopo / prima. Obiettivo = la squadra che i bonus dell'account (artefatti, beast, alberi) favoriscono a parit&agrave; di investimento.</p>
<table style="border-collapse:collapse;font-size:12px">
<tr>$((('Instance', 'Squadra in battaglia', 'Ultimo controllo', 'Obiettivo') | ForEach-Object { "<th style=`"$th;text-align:left`">$_</th>" }) -join '')</tr>
$($formation -join "`n")
</table>
</div>
"@
$html | Set-Content "$store\$stamp.html" -Encoding UTF8

if ($NoSend) { "Report scritto in $store\$($today.ToString('yyyy-MM-dd')).html"; return }
Send-MailMessage -SmtpServer smtp.gmail.com -Port 587 -UseSsl -Credential (Import-Clixml "$HOME\gmail.cred") `
    -From $env:FIREBOT_REPORT_TO -To $env:FIREBOT_REPORT_TO -Subject "Firestone BOT $($today.ToString('dd/MM/yyyy'))" `
    -Body $html -BodyAsHtml -Encoding UTF8 -WarningAction SilentlyContinue
# Saved only once the mail is out: the next report compares with what was actually sent.
$rows | ConvertTo-Json | Set-Content "$store\$stamp.json" -Encoding UTF8
"Report inviato a $env:FIREBOT_REPORT_TO"
