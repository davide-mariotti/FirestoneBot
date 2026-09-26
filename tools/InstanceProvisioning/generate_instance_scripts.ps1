# Genera Avvia_Steam-N.bat / Ferma_Steam-N.bat per un intervallo di istanze, con lo stesso
# identico formato usato per le istanze 0-16 su questo PC (vedi docs/MULTI_INSTANCE_SETUP.md).
#
# Uso:
#   .\generate_instance_scripts.ps1 -Start 17 -End 34 -OutDir "C:\Users\Admin\Desktop\Firestone Bots"
#
# Per default genera SOLO istanze sandboxate (Sandboxie box "SteamB<N>"), cioè lo stesso schema
# usato per Steam-1..Steam-16. Se vuoi un'istanza NATIVA (senza sandbox, come Steam-0 su questo
# PC), passa il suo numero in -NativeInstances (es. -NativeInstances 17) e verrà generata con lo
# schema "nativo" (Get-Process/Stop-Process per fermarla, avvio diretto di steam.exe).
#
# Non serve Amministratore - scrive solo file .bat, non tocca Sandboxie.ini (per quello vedi
# add_sandbox_boxes.ps1 nella stessa cartella).

param(
    [Parameter(Mandatory=$true)][int]$Start,
    [Parameter(Mandatory=$true)][int]$End,
    [string]$OutDir = "C:\Users\Admin\Desktop\Firestone Bots",
    [int[]]$NativeInstances = @(),
    [string]$AppId = "1013320"
)

if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir -Force | Out-Null }

$sandboxieExe = 'C:\Program Files\Sandboxie-Plus\Start.exe'

foreach ($n in $Start..$End) {
    $isNative = $NativeInstances -contains $n
    $steamPath = "C:\Program Files (x86)\Steam-$n"

    if ($isNative) {
        $avvia = @"
@echo off
setlocal

set APPID=$AppId

echo ============================================
echo   Avvio istanza Firestone - Steam-$n (nativa)
echo ============================================
echo.

start "" "$steamPath\steam.exe" -silent -applaunch %APPID%

echo.
echo Istanza avviata.
echo.
pause
"@
        $ferma = @"
@echo off
setlocal

echo ============================================
echo   Arresto istanza Firestone - Steam-$n (nativa)
echo ============================================
echo.

powershell -NoProfile -Command "Get-Process | Where-Object { `$_.Path -like '$steamPath\*' } | Stop-Process -Force -ErrorAction SilentlyContinue"

echo.
echo Istanza fermata.
echo.
pause
"@
    } else {
        $box = "SteamB$n"
        $avvia = @"
@echo off
setlocal

set APPID=$AppId
set SANDBOXIE="$sandboxieExe"

echo ============================================
echo   Avvio istanza Firestone - Steam-$n (sandbox $box)
echo ============================================
echo.

%SANDBOXIE% /box:$box "$steamPath\steam.exe" -silent -applaunch %APPID%

echo.
echo Istanza avviata.
echo.
pause
"@
        $ferma = @"
@echo off
setlocal

set SANDBOXIE="$sandboxieExe"

echo ============================================
echo   Arresto istanza Firestone - Steam-$n (sandbox $box)
echo ============================================
echo.

%SANDBOXIE% /box:$box /terminate

echo.
echo Istanza fermata.
echo.
pause
"@
    }

    Set-Content -Path (Join-Path $OutDir "Avvia_Steam-$n.bat") -Value $avvia -Encoding ASCII
    Set-Content -Path (Join-Path $OutDir "Ferma_Steam-$n.bat") -Value $ferma -Encoding ASCII
    Write-Host "Generati Avvia_Steam-$n.bat / Ferma_Steam-$n.bat ($(if ($isNative) {'nativa'} else {"sandbox $box"}))"
}

Write-Host ""
Write-Host "Fatto: $($End - $Start + 1) istanze generate in '$OutDir'."
