@echo off
rem Avvia (o riavvia) tutte le istanze, 5 alla volta, e toglie la pausa messa da Ferma_Tutte:
rem watchdog e riavvio ogni 6 ore ripartono. Lo fa l'attivita' Firestone_Riavvio (restart_fleet.ps1).
powershell -NoProfile -ExecutionPolicy Bypass -File "C:\Repos\FirestoneBot\tools\Restart\restart_fleet.ps1" -Resume
schtasks /run /tn Firestone_Riavvio
echo.
echo Le istanze ripartono 5 alla volta (circa un minuto e mezzo a gruppo).
echo Avanzamento: %TEMP%\restart_fleet.log
pause
