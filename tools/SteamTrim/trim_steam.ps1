# Libera RAM dai steamwebhelper.exe (l'interfaccia CEF di Steam, una copia per istanza), ma solo
# quando la RAM fisica occupata arriva a -MinRamPercent: e' il trim che si lanciava a mano con la RAM
# al 99%, messo in un'attivita' pianificata ogni 10 minuti che il resto del tempo non tocca niente.
#
# Volutamente poco aggressivo: mai steam.exe, Firestone.exe o altri processi; salta il processo GPU
# di steamwebhelper, il piu' delicato, e quelli sotto i 150 MB; non chiude niente (EmptyWorkingSet
# sposta solo le pagine inutilizzate, il processo le ricarica se gli servono); pausa breve tra un
# processo e l'altro. Ogni trim fatto aggiunge una riga a %TEMP%\trim_steam.log.
# L'attivita' gira dentro una console senza finestra (conhost --headless), quindi non si vede niente;
# -WindowStyle Hidden resta come riserva, se una build di Windows non supportasse --headless.
#
# Installazione, una volta per PC, senza Amministratore (gira solo con l'utente collegato):
#   powershell -NoProfile -ExecutionPolicy Bypass -File C:\Repos\FirestoneBot\tools\SteamTrim\trim_steam.ps1 -Install
# Soglia diversa: aggiungi -MinRamPercent 85. Rimozione:
#   schtasks /Delete /TN FirebotSteamTrim /F

param(
    [int]$MinRamPercent = 90,
    [switch]$Install
)

if ($Install) {
    $action = New-ScheduledTaskAction -Execute 'conhost.exe' -Argument ("--headless powershell.exe -NoProfile " +
        "-WindowStyle Hidden -ExecutionPolicy Bypass -File `"$PSCommandPath`" -MinRamPercent $MinRamPercent")
    $trigger = New-ScheduledTaskTrigger -Once -At (Get-Date) -RepetitionInterval (New-TimeSpan -Minutes 10)
    Register-ScheduledTask -TaskName 'FirebotSteamTrim' -Action $action -Trigger $trigger -Force | Out-Null
    "Attivita' FirebotSteamTrim registrata: ogni 10 minuti, trim sopra il $MinRamPercent% di RAM."
    exit
}

$os = Get-CimInstance Win32_OperatingSystem
$usedPercent = [int](100 - 100 * $os.FreePhysicalMemory / $os.TotalVisibleMemorySize)
if ($usedPercent -lt $MinRamPercent) {
    "RAM al $usedPercent%, sotto la soglia del $MinRamPercent%: niente da fare."
    exit
}

Add-Type -Namespace SteamTrim -Name Native -MemberDefinition @"
[DllImport("psapi.dll")]
public static extern bool EmptyWorkingSet(IntPtr hProcess);
"@

$minWorkingSetBytes = 150MB

$targets = Get-CimInstance Win32_Process -Filter "Name = 'steamwebhelper.exe'" |
    Where-Object { $_.CommandLine -notmatch '--type=gpu-process' }

$before = 0; $after = 0; $trimmed = 0
foreach ($t in $targets) {
    $p = Get-Process -Id $t.ProcessId -ErrorAction SilentlyContinue
    if (-not $p -or $p.WorkingSet64 -lt $minWorkingSetBytes) { continue }
    $before += $p.WorkingSet64
    try { [void][SteamTrim.Native]::EmptyWorkingSet($p.Handle); $trimmed++ } catch { continue }
    Start-Sleep -Milliseconds 200
    $p.Refresh(); $after += $p.WorkingSet64
}

$summary = "{0:yyyy-MM-dd HH:mm} RAM {1}% | steamwebhelper trimmati: {2} | liberati: {3:N0} MB" -f `
    (Get-Date), $usedPercent, $trimmed, (($before - $after) / 1MB)
$summary
Add-Content -Path (Join-Path $env:TEMP 'trim_steam.log') -Value $summary
