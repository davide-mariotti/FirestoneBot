# Aggiunge il nome istanza (es. "Steam-5") al titolo della finestra di ogni box Sandboxie
# usato da questo progetto (SteamB1..SteamB16 -> Steam-1..Steam-16), sfruttando le due
# impostazioni interne di Sandboxie-Plus che governano il titolo delle finestre sandboxate:
#   BoxNameTitle=y      - abilita la decorazione del titolo per quel box
#   BoxAlias=<nome>      - il nome mostrato al posto del nome interno del box (es. "SteamB5")
# Trovate nella stringhe di SbieDll.dll (non documentate esplicitamente altrove), non ancora
# presenti in nessuna sezione del file - questo script le aggiunge solo dove mancano, senza
# toccare nient'altro (idempotente: si può rilanciare senza duplicare righe).
#
# DEVE essere eseguito come Amministratore (Sandboxie.ini è scrivibile solo da
# Administrators/SYSTEM - una sessione utente normale ha solo lettura).
#
# Dopo l'esecuzione, riavvia le istanze Steam-1..Steam-16 (Ferma+Avvia) per vedere il nuovo
# titolo - Sandboxie applica il titolo alla creazione della finestra, non retroattivamente.

$ErrorActionPreference = "Stop"

$path = "C:\Windows\Sandboxie.ini"
$backup = "C:\Windows\Sandboxie.ini.bak-$(Get-Date -Format yyyyMMdd-HHmmss)"

Copy-Item $path $backup
Write-Host "Backup creato: $backup"

$lines = [System.IO.File]::ReadAllLines($path, [System.Text.Encoding]::Unicode)
$out = New-Object System.Collections.Generic.List[string]
$changed = 0

for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    $out.Add($line)

    if ($line -match '^\[SteamB(\d+)\]$') {
        $n = [int]$matches[1]
        if ($n -ge 1 -and $n -le 16) {
            # Scansiona il resto della sezione per vedere se le chiavi esistono già
            $j = $i + 1
            $hasAlias = $false
            $hasTitle = $false
            while ($j -lt $lines.Count -and $lines[$j] -notmatch '^\[.*\]$') {
                if ($lines[$j] -match '^BoxAlias=') { $hasAlias = $true }
                if ($lines[$j] -match '^BoxNameTitle=') { $hasTitle = $true }
                $j++
            }

            if (-not $hasTitle) { $out.Add("BoxNameTitle=y"); $changed++ }
            if (-not $hasAlias) { $out.Add("BoxAlias=Steam-$n"); $changed++ }
        }
    }
}

if ($changed -eq 0) {
    Write-Host "Nessuna modifica necessaria - le chiavi erano già presenti ovunque."
} else {
    [System.IO.File]::WriteAllLines($path, $out, [System.Text.Encoding]::Unicode)
    Write-Host "Fatto: $changed riga/e aggiunte. Riavvia le istanze Steam-1..Steam-16 per vedere il nuovo titolo."
}
