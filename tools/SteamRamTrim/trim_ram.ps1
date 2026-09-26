Add-Type -Namespace RamTrim -Name Native -MemberDefinition @"
[DllImport("psapi.dll")]
public static extern bool EmptyWorkingSet(IntPtr hProcess);
"@

# Approccio semplificato (2026-09-26): la versione precedente trimmava "tutto tranne una
# blacklist" (Steam/Firestone esclusi, il resto del sistema si', incluso qualunque processo di
# terze parti), ed e' rimasta comunque associata a crash frequenti delle istanze anche dopo aver
# escluso Steam/Firestone esplicitamente - probabilmente per pressione di memoria indiretta sul
# resto del sistema con 17+ istanze attive. Poi si e' provato a trimmare SOLO steamwebhelper.exe:
# ATTENZIONE - questo e' esattamente cio' che l'incidente del 2026-09-25 aveva gia' indicato come
# causa dei crash di steamwebhelper.exe e Firestone.exe (l'overlay Steam e' agganciato dentro il
# processo del gioco). Ora lo scope e' stato allargato di nuovo a "steam" (copre steam.exe,
# steamwebhelper.exe, steamservice.exe, steamerrorreporter.exe) per test - riprovare in scenari
# reali se i crash si ripresentano; potrebbe non essere il trim la causa principale, dato che lo
# stesso giorno e' stato osservato anche un crash di SbieSvc.exe (STACK_OVERFLOW) indipendente da
# questo script. Firestone.exe e unitycrashhandler restano sempre esclusi, e nessun processo di
# sistema o di Sandboxie (SbieSvc/SandMan/Start.exe, nessuno contiene "steam" nel nome) viene mai
# toccato.
$trimOnlyContains = @("steam")

# Soglia minima: non vale la pena trimmare processi gia' piccoli
$minWorkingSetBytes = 20MB

Get-Process | ForEach-Object {
    $p = $_

    $matches = $false
    foreach ($pattern in $trimOnlyContains) {
        if ($p.ProcessName -like "*$pattern*") { $matches = $true; break }
    }
    if (-not $matches) { return }

    try {
        if ($p.WorkingSet64 -ge $minWorkingSetBytes) {
            [RamTrim.Native]::EmptyWorkingSet($p.Handle) | Out-Null
        }
    } catch {}
}
