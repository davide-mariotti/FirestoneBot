# RAM Trim

Script che libera la RAM inutilizzata occupata dai processi in background del PC,
senza chiudere nulla e senza interrompere quello che stai usando attivamente.

Non serve per far girare il bot: è solo un'utility a parte, tenuta qui per comodità.

## Come funziona

Chiama l'API di Windows `EmptyWorkingSet` su (quasi) tutti i processi in
esecuzione. Questo forza Windows a scaricare su pagefile le pagine di memoria che
quei processi non stanno usando in quel momento. I processi continuano a
funzionare normalmente: se tornano a usare quei dati, Windows li ricarica (con un
piccolo overhead di I/O, trascurabile).

Non è un cap/limite di memoria: è uno "svuotamento" periodico. Un processo che
torna ad essere usato intensamente riprenderà a occupare RAM finché non viene
rieseguito il trim.

### Cosa viene toccato (2026-09-26: approccio semplificato)

La prima versione trimmava "tutto tranne una blacklist" (Steam/Firestone
esclusi, il resto del sistema si'). Anche dopo aver escluso esplicitamente
Steam e Firestone, le istanze continuavano a crashare spesso con 17+ istanze
attive insieme - probabilmente per pressione di memoria indiretta sul resto
del sistema. Ora la lista si è capovolta: invece di una blacklist ampia, c'è
una whitelist molto stretta.

**Viene trimmato solo `steamwebhelper.exe`** — il componente CEF/GPU di Steam
per overlay e notifiche. Ne gira una copia per ogni istanza Steam attiva, è
pesante (è essenzialmente un mini-Chromium), ed è l'unico componente di Steam
non direttamente coinvolto nell'hosting del gioco.

Tutto il resto — `steam.exe`, `Firestone.exe`, `UnityCrashHandler`, qualunque
altro processo di sistema o applicazione — non viene mai toccato.

Modifica la lista `$trimOnlyContains` all'inizio di `trim_ram.ps1` se vuoi
includere altri processi nel trim.

## Uso manuale (una tantum)

Apri PowerShell (non serve amministratore) ed esegui:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "C:\Repos\FirestoneBot\tools\SteamRamTrim\trim_ram.ps1"
```

## Uso automatico (consigliato)

Per farlo girare da solo ogni 20 minuti, crea un'attività pianificata di
Windows (comando da lanciare una sola volta, non serve amministratore):

```powershell
schtasks /Create /TN "PCRamTrim" /TR "powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File `"C:\Repos\FirestoneBot\tools\SteamRamTrim\trim_ram.ps1`"" /SC MINUTE /MO 20 /F
```

Per cambiare la frequenza, modifica `/MO 20` (minuti). 5 minuti (il valore
usato in precedenza) è più aggressivo del necessario con molte istanze
Steam+Firestone attive insieme - un intervallo più lungo riduce quanto spesso
Windows deve ripaginare in memoria i processi appena svuotati.

### Rimuovere l'attività

```powershell
schtasks /Delete /TN "PCRamTrim" /F
```

### Verificare che sia attiva

```powershell
schtasks /Query /TN "PCRamTrim" /V /FO LIST
```

## Note

- Funziona solo sui processi dell'utente corrente (non serve né richiede
  privilegi di amministratore). Alcuni processi protetti (es. Windows Defender
  `MsMpEng`) rifiutano l'accesso: vengono ignorati automaticamente, nessun
  errore visibile.
- Se sposti la cartella del repo, ricorda di aggiornare il percorso nel comando
  `schtasks` (o ricreare l'attività).
- L'attività persiste dopo un riavvio del PC e riparte da sola al login.
