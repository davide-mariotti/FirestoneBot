# Trim manuale, da lanciare a mano quando serve (non e' un'attivita' pianificata):
#   powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\SteamRamTrim\trim_steam_now.ps1
#
# Volutamente poco aggressivo:
# - tocca SOLO steamwebhelper.exe (l'interfaccia CEF di Steam, una copia per istanza), mai
#   steam.exe, Firestone.exe o altri processi di sistema;
# - salta il processo GPU di steamwebhelper (--type=gpu-process), il piu' delicato da toccare;
# - salta i processi gia' sotto i 150 MB, dove non c'e' niente di utile da recuperare;
# - non chiude niente: EmptyWorkingSet sposta solo le pagine inutilizzate fuori dalla RAM, il
#   processo le ricarica da solo se gli servono di nuovo;
# - una pausa breve tra un processo e l'altro per non creare un picco di I/O su disco.

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

"steamwebhelper trimmati: {0} | RAM prima: {1:N0} MB | dopo: {2:N0} MB | liberati: {3:N0} MB" -f `
    $trimmed, ($before / 1MB), ($after / 1MB), (($before - $after) / 1MB)
