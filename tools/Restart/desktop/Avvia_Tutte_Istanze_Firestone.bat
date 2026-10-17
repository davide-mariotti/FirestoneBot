@echo off
rem Avvia (o riavvia) tutte le istanze, una alla volta, e toglie la pausa messa da Ferma_Tutte:
rem watchdog e riavvio ogni 6 ore ripartono. Lo fa l'attivita' Firestone_Riavvio (restart_fleet.ps1).
powershell -NoProfile -ExecutionPolicy Bypass -File "C:\Repos\FirestoneBot\tools\Restart\restart_fleet.ps1" -Resume
schtasks /run /tn Firestone_Riavvio
echo.
echo Le istanze ripartono una alla volta (circa un minuto e mezzo l'una).
echo Avanzamento: %TEMP%\restart_fleet.log
pause
