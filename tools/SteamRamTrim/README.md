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

### Cosa NON viene mai toccato

- **Il processo in primo piano** (la finestra che stai usando in quel momento),
  rilevato automaticamente ad ogni esecuzione — così non c'è mai stutter
  sull'app attiva.
- **Firestone** (il gioco), **Visual Studio / VS Code** (`devenv`, `Code`) —
  esclusi sempre per nome, indipendentemente da cosa hai in primo piano.
- Processi di sistema critici (`explorer`, `dwm`, `lsass`, `csrss`, `svchost`,
  `winlogon`, `services`, `System`, `Registry`, ecc.) — mai toccati per evitare
  qualunque instabilità.
- Processi già piccoli (sotto i 20MB di working set): non vale la pena.

Modifica le liste `$systemExclude` e `$neverTrimContains` all'inizio di
`trim_ram.ps1` se vuoi aggiungere altre esclusioni.

## Uso manuale (una tantum)

Apri PowerShell (non serve amministratore) ed esegui:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "C:\Repos\FirestoneBot\tools\SteamRamTrim\trim_ram.ps1"
```

## Uso automatico (consigliato)

Per farlo girare da solo ogni 5 minuti, crea un'attività pianificata di Windows
(comando da lanciare una sola volta, non serve amministratore):

```powershell
schtasks /Create /TN "PCRamTrim" /TR "powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File `"C:\Repos\FirestoneBot\tools\SteamRamTrim\trim_ram.ps1`"" /SC MINUTE /MO 5 /F
```

Per cambiare la frequenza, modifica `/MO 5` (minuti).

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
