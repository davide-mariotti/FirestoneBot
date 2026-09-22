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
$neverTrimContains = @("firestone", "devenv", "code")

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
