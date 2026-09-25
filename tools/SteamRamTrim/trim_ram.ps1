Add-Type -Namespace RamTrim -Name Native -MemberDefinition @"
[DllImport("psapi.dll")]
public static extern bool EmptyWorkingSet(IntPtr hProcess);

[DllImport("user32.dll")]
public static extern IntPtr GetForegroundWindow();

[DllImport("user32.dll")]
public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
"@

# Processi di sistema mai da toccare (nome esatto, senza .exe)
$systemExclude = @(
    "System", "Registry", "Idle", "smss", "csrss", "wininit", "winlogon",
    "services", "lsass", "dwm", "explorer", "MemCompression", "fontdrvhost",
    "audiodg", "WUDFHost", "svchost", "conhost"
)

# Processi da NON trimmare mai (nome contiene una di queste stringhe, case-insensitive)
# "steam" copre anche steamwebhelper (il componente CEF/GPU di Steam, usato per overlay e
# notifiche) - live-osservato, 2026-09-25: con 17+ istanze di Steam+Firestone attive insieme,
# svuotarne il working set ogni 5 minuti ha coinciso con crash sia di steamwebhelper.exe
# (EXCEPTION_BREAKPOINT, tipico di un allocatore che rileva uno stato di memoria inatteso) sia
# di Firestone.exe stesso (l'overlay di Steam è agganciato dentro il processo del gioco, quindi
# un problema nel componente Steam che lo coordina puo' ripercuotersi anche li').
# "unitycrashhandler" e' il watchdog di crash-report che ogni istanza di Firestone avvia
# accanto a se' - legato al gioco, non serve trimmarlo.
$neverTrimContains = @("firestone", "devenv", "code", "steam", "unitycrashhandler")

# Soglia minima: non vale la pena trimmare processi gia' piccoli
$minWorkingSetBytes = 20MB

$fgProcessId = 0
try {
    $hwnd = [RamTrim.Native]::GetForegroundWindow()
    [void][RamTrim.Native]::GetWindowThreadProcessId($hwnd, [ref]$fgProcessId)
} catch {}

Get-Process | ForEach-Object {
    $p = $_

    if ($p.Id -eq $PID) { return }
    if ($fgProcessId -ne 0 -and $p.Id -eq $fgProcessId) { return }
    if ($systemExclude -contains $p.ProcessName) { return }

    foreach ($pattern in $neverTrimContains) {
        if ($p.ProcessName -like "*$pattern*") { return }
    }

    try {
        if ($p.WorkingSet64 -ge $minWorkingSetBytes) {
            [RamTrim.Native]::EmptyWorkingSet($p.Handle) | Out-Null
        }
    } catch {}
}
