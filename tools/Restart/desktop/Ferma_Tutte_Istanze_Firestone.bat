@echo off
rem Ferma tutte le istanze accese e mette in pausa watchdog e riavvio ogni 6 ore,
rem finche' non si lancia Avvia_Tutte_Istanze_Firestone.bat (senza pausa il watchdog le riaccenderebbe).
powershell -NoProfile -ExecutionPolicy Bypass -File "C:\Repos\FirestoneBot\tools\Restart\restart_fleet.ps1" -Stop
echo.
echo Istanze ferme. Per riaccenderle: Avvia_Tutte_Istanze_Firestone.bat
pause
