Add-Type -Namespace RamTrim -Name Native -MemberDefinition @"
[DllImport("psapi.dll")]
public static extern bool EmptyWorkingSet(IntPtr hProcess);
"@

# Approccio semplificato (2026-09-26): la versione precedente trimmava "tutto tranne una
# blacklist" (Steam/Firestone esclusi, il resto del sistema si', incluso qualunque processo di
# terze parti), ed e' rimasta comunque associata a crash frequenti delle istanze anche dopo aver
# escluso Steam/Firestone esplicitamente - probabilmente per pressione di memoria indiretta sul
# resto del sistema con 17+ istanze attive. Ora si trimma SOLO steamwebhelper.exe (il componente
# CEF/GPU di Steam per overlay e notifiche: ne gira una copia per ogni istanza Steam, e' pesante,
# ed e' l'unico pezzo di Steam non direttamente coinvolto nell'hosting del gioco). steam.exe,
# Firestone.exe e unitycrashhandler restano intoccati, e nessun altro processo di sistema viene
# toccato.
$trimOnlyContains = @("steamwebhelper")

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
