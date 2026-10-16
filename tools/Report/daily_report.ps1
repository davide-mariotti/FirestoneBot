<#
.SYNOPSIS
    Emails a one-table daily report of every instance: level, stage, power, currencies, quests,
    battles, errors and watchdog restarts over the last 24 hours, each with the change since the
    previous report.

.DESCRIPTION
    Runs on PC-Guesly (MULTI_INSTANCE_SETUP.md, 6.5). Reads the MelonLoader logs of -Local here and
    of -Remote on the main PC over PowerShell remoting ($env:FIREBOT_REMOTE_HOST, credential in
    $HOME\desktop-admin.cred). Level, stage, power and currencies come from the bot's hourly
    "[INFO] Snapshot:" line; the rest from its other [INFO] lines. Each report's numbers are kept in
    %LOCALAPPDATA%\FirebotReport\<date>.json, and the next one shows the change against them.

    Mail goes through Gmail (smtp.gmail.com:587) with an app password saved in $HOME\gmail.cred, to
    $env:FIREBOT_REPORT_TO. Both env values come from the repo's .env. -NoSend only writes the HTML
    next to the json.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File C:\Repos\FirestoneBot\tools\Report\daily_report.ps1
#>
param(
    [int[]]$Local = 17..30,
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
        $daily = 0; $weekly = 0
        $lines | Where-Object { $_ -match 'Quests: claimed (\d+) daily, (\d+) weekly' } | ForEach-Object { $daily += [int]$Matches[1]; $weekly += [int]$Matches[2] }

        [pscustomobject]@{
            n        = $n
            online   = $online
            level    = if ($last) { $last.level } else { $null }
            stage    = ($snapshots | Where-Object { $_.stage -ge 0 } | Measure-Object stage -Maximum).Maximum
            power    = if ($last) { $last.power } else { $null }
            stars    = if ($last) { $last.stars } else { $null }
            gems     = if ($last) { $last.gems } else { $null }
            dust     = if ($last) { $last.dust } else { $null }
            blood    = if ($last) { $last.blood } else { $null }
            guild    = if ($last) { $last.guild } else { $null }
            daily    = $daily
            weekly   = $weekly
            arena    = @($lines | Where-Object { $_ -match 'Arena: .*-> slot' }).Count
            won      = @($lines | Where-Object { $_ -match 'Campaign mission .*-> won' }).Count
            lost     = @($lines | Where-Object { $_ -match 'Campaign mission .*-> lost' }).Count
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
$previousFile = Get-ChildItem "$store\*.json" | Where-Object { $_.BaseName -lt $today.ToString('yyyy-MM-dd') } | Sort-Object Name | Select-Object -Last 1
$before = @{}
if ($previousFile) { (Get-Content $previousFile.FullName -Raw | ConvertFrom-Json) | ForEach-Object { $before[[int]$_.n] = $_ } }
$rows | ConvertTo-Json | Set-Content "$store\$($today.ToString('yyyy-MM-dd')).json" -Encoding UTF8

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

$td = 'padding:5px 8px;border-bottom:1px solid #eaeef2;text-align:right;white-space:nowrap'
$th = 'padding:6px 8px;border-bottom:2px solid #d0d7de;text-align:right;font-weight:600;color:#57606a;white-space:nowrap'
$headers = 'Istanza', 'Stato', 'Livello', 'Stage max', 'Potenza', 'Stelle', 'Gemme', 'Guild coin', 'Polvere', 'Sangue', 'Quest g/s', 'Arena', 'Campagna v/p', 'Errori', 'Riavvii'
$body = foreach ($r in $rows) {
    $state = if ($r.online) { '<span style="color:#1a7f37">&#9679; attiva</span>' } else { '<span style="color:#cf222e">&#9679; ferma</span>' }
    $restartCell = if ($r.restarts) { "<b style=`"color:#cf222e`">$($r.restarts)</b>" } else { '0' }
    $cells = "Steam-$($r.n)", $state, (Cell $r level), (Cell $r stage), (Cell $r power -Abbreviate), (Cell $r stars), (Cell $r gems), (Cell $r guild), (Cell $r dust), (Cell $r blood),
        "$($r.daily)/$($r.weekly)", $r.arena, "$($r.won)/$($r.lost)", $r.failed, $restartCell
    '<tr>' + (($cells | ForEach-Object { "<td style=`"$td`">$_</td>" }) -join '') + '</tr>'
}

$online = @($rows | Where-Object online).Count
function Problem([string]$label, $matching, [scriptblock]$detail = { "$($_.n)" }) {
    $list = @($matching | ForEach-Object $detail)
    if ($list) { "<b>${label}:</b> " + ($list -join ', ') }
}
$problems = @(
    Problem 'Ferme' ($rows | Where-Object { -not $_.online })
    Problem 'Riavviate dal watchdog' ($rows | Where-Object { $_.restarts }) { "$($_.n) (x$($_.restarts))" }
    Problem 'Livello fermo da ieri' ($rows | Where-Object { $old = $before[[int]$_.n]; $old -and $null -ne $_.level -and $_.level -eq $old.level })
    Problem 'Senza snapshot nelle ultime 24 ore' ($rows | Where-Object { $_.online -and $null -eq $_.level })
)
$levels = ($rows | Where-Object { $old = $before[[int]$_.n]; $old -and $null -ne $_.level -and $null -ne $old.level } | ForEach-Object { $_.level - $old.level } | Measure-Object -Sum).Sum
$compared = if ($previousFile) { "Differenze rispetto al report del $([datetime]::ParseExact($previousFile.BaseName, 'yyyy-MM-dd', $null).ToString('dd/MM'))." } else { 'Primo report: le differenze compariranno da domani.' }

$html = @"
<div style="font-family:Segoe UI,Helvetica,Arial,sans-serif;font-size:13px;color:#1f2328">
<p style="font-size:15px;margin:0 0 4px"><b>$online/$($rows.Count) istanze attive</b>$(if ($previousFile) { " &middot; $levels livelli guadagnati in tutto" })</p>
<p style="margin:0 0 12px;color:#57606a">Ultime 24 ore, fino alle $($today.ToString('HH:mm')). $compared</p>
$(if ($problems) { '<p style="margin:0 0 12px;color:#cf222e">' + ($problems -join '<br>') + '</p>' } else { '<p style="margin:0 0 12px;color:#1a7f37">Nessun problema.</p>' })
<table style="border-collapse:collapse;font-size:12px">
<tr>$(($headers | ForEach-Object { "<th style=`"$th`">$_</th>" }) -join '')</tr>
$($body -join "`n")
</table>
<p style="margin:12px 0 0;color:#8c959f;font-size:11px">Livello, stage, potenza (war machine) e valute dall'ultimo snapshot del bot (ogni ora); stage max = il pi&ugrave; alto visto negli snapshot. Quest g/s = giornaliere/settimanali riscattate. Arena = combattimenti. Campagna = battaglie vinte/perse. Errori = task falliti. Riavvii = interventi del watchdog.</p>
</div>
"@
$html | Set-Content "$store\$($today.ToString('yyyy-MM-dd')).html" -Encoding UTF8

if ($NoSend) { "Report scritto in $store\$($today.ToString('yyyy-MM-dd')).html"; return }
Send-MailMessage -SmtpServer smtp.gmail.com -Port 587 -UseSsl -Credential (Import-Clixml "$HOME\gmail.cred") `
    -From $env:FIREBOT_REPORT_TO -To $env:FIREBOT_REPORT_TO -Subject "Firestone BOT $($today.ToString('dd/MM/yyyy'))" `
    -Body $html -BodyAsHtml -Encoding UTF8 -WarningAction SilentlyContinue
"Report inviato a $env:FIREBOT_REPORT_TO"
