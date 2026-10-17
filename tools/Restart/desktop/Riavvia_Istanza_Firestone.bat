@echo off
rem Riavvia una sola istanza: chiude il gioco, poi Steam, e la riavvia aspettando che il gioco parta.
set /p N=Numero dell'istanza da riavviare (es. 14):
powershell -NoProfile -ExecutionPolicy Bypass -File "C:\Repos\FirestoneBot\tools\Restart\restart_fleet.ps1" -Instance %N%
pause
