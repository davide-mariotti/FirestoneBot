# Crea le sezioni box di Sandboxie ("[SteamB<N>]") per un intervallo di istanze, clonando le
# impostazioni di un box esistente già funzionante come template (di default SteamB1) e
# assegnando un BorderColor diverso a ciascuna + BoxNameTitle/BoxAlias (titolo finestra con il
# nome istanza, vedi set_box_titles.ps1). Idempotente: salta i box che esistono già.
#
# DEVE essere eseguito come Amministratore (Sandboxie.ini è scrivibile solo da
# Administrators/SYSTEM). Fa un backup automatico prima di modificare.
#
# Uso:
#   .\add_sandbox_boxes.ps1 -Start 17 -End 34
#
# Dopo l'esecuzione, riavvia Sandboxie (o semplicemente avvia la prima istanza nuova - Sandboxie
# rilegge Sandboxie.ini automaticamente) e verifica con SandMan.exe che i box SteamB17..SteamB34
# siano visibili e abilitati.

param(
    [Parameter(Mandatory=$true)][int]$Start,
    [Parameter(Mandatory=$true)][int]$End,
    [string]$TemplateBox = "SteamB1",
    [string]$IniPath = "C:\Windows\Sandboxie.ini"
)

$ErrorActionPreference = "Stop"

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) { Write-Error "Rilancia PowerShell come Amministratore."; exit 1 }

$backup = "$IniPath.bak-$(Get-Date -Format yyyyMMdd-HHmmss)"
Copy-Item $IniPath $backup
Write-Host "Backup creato: $backup"

$lines = [System.IO.File]::ReadAllLines($IniPath, [System.Text.Encoding]::Unicode)

# Estrae il corpo del box template (tutte le righe tra "[SteamB1]" e la sezione successiva),
# escludendo BorderColor/BoxAlias/BoxNameTitle che vengono rigenerati per ogni nuovo box.
$templateHeaderIdx = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -eq "[$TemplateBox]") { $templateHeaderIdx = $i; break }
}
if ($templateHeaderIdx -eq -1) { Write-Error "Box template '$TemplateBox' non trovato in $IniPath."; exit 1 }

$templateBody = New-Object System.Collections.Generic.List[string]
$j = $templateHeaderIdx + 1
while ($j -lt $lines.Count -and $lines[$j] -notmatch '^\[.*\]$') {
    if ($lines[$j] -notmatch '^(BorderColor|BoxAlias|BoxNameTitle)=') {
        $templateBody.Add($lines[$j])
    }
    $j++
}

Write-Host "Template preso da [$TemplateBox] ($($templateBody.Count) righe di impostazioni)."

# Colori ben distinguibili in sequenza, ciclici se servono più di quanti listati qui.
$palette = @(
    "#00FFFF", "#00FF00", "#FFFF00", "#FF8000", "#FF00FF", "#FF0000",
    "#FF8080", "#80FF80", "#8080FF", "#FFFF80", "#80FFFF", "#FF80FF",
    "#C0C0FF", "#FFC080", "#FFD9B3", "#B3FFD9", "#D9B3FF", "#B3D9FF"
)

$existingBoxes = $lines | Where-Object { $_ -match '^\[SteamB(\d+)\]$' } | ForEach-Object { [int]($_ -replace '[^\d]','') }

$out = New-Object System.Collections.Generic.List[string]
$out.AddRange($lines)
$added = 0

foreach ($n in $Start..$End) {
    if ($existingBoxes -contains $n) {
        Write-Host "SteamB$n esiste già - salto."
        continue
    }

    $color = $palette[($n - 1) % $palette.Count]
    $out.Add("")
    $out.Add("[SteamB$n]")
    $out.Add("BorderColor=$color,ttl,6,192")
    $out.Add("BoxNameTitle=y")
    $out.Add("BoxAlias=Steam-$n")
    $out.AddRange($templateBody)

    Write-Host "Aggiunto [SteamB$n] (BorderColor=$color, BoxAlias=Steam-$n)."
    $added++
}

if ($added -eq 0) {
    Write-Host "Nessun box nuovo da aggiungere - tutti già presenti."
} else {
    # Aggiorna anche BoxGrouping in [UserSettings_*] se presente, così i nuovi box compaiono
    # subito nell'albero di SandMan invece di restare "non raggruppati".
    for ($i = 0; $i -lt $out.Count; $i++) {
        if ($out[$i] -match '^BoxGrouping=') {
            $newNames = ($Start..$End | Where-Object { $existingBoxes -notcontains $_ } | ForEach-Object { "SteamB$_" }) -join ","
            $out[$i] = $out[$i].TrimEnd() + "," + $newNames
            break
        }
    }

    [System.IO.File]::WriteAllLines($IniPath, $out, [System.Text.Encoding]::Unicode)
    Write-Host ""
    Write-Host "Fatto: $added box aggiunti. Apri SandMan.exe per confermare che siano visibili,"
    Write-Host "poi genera gli script Avvia/Ferma corrispondenti con generate_instance_scripts.ps1."
}
