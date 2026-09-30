# Test dal vivo

Runbook per una sessione di test **autonoma** di Claude Code sul PC della flotta, sull'istanza
Steam-0: prova tutti i task, trova i bug dai log, li corregge, ricompila, ridistribuisce e riprova
da solo. In fondo: stato di ogni task, problemi noti, lavoro rimandato e come tornare alla versione
precedente. Aggiornato al 2026-09-30, dopo la prima sessione di test dal vivo su Steam-0 (dopo il
riallineamento alla guida F2P, `docs/firestone_guida_F2P.md`, e la pulizia del codice del 28/09) e
il rollout di Hall of Heroes su Steam-0..16.

## Prompt di avvio

Da incollare in Claude Code (VS Code, cartella `C:\Repos\FirestoneBot`). Per lavorare davvero da
solo, Claude Code va avviato con i permessi che gli evitano di chiedere conferma a ogni comando.

```text
Leggi TESTING.md e seguilo dall'inizio alla fine in autonomia, con la skill ponytail attiva
(se non parte da sola: /ponytail). Lavora solo sull'istanza Steam-0. Fai un commit per ogni
correzione verificata dal vivo; a fine sessione fai push e dammi il riepilogo richiesto in
"Fase 3".
```

---

## Per l'agente

Il giro completo (Fase 1) è stato fatto il 29/09: lo stato di ogni task è in "Stato per task" e non
va rifatto per lavorare su un task solo. In quel caso bastano la Preparazione, il task isolato come
nel punto 3 della Fase 2 (acceso solo lui) e la Fase 3; si ricontrollano solo i task toccati.

### Contesto

Firebot è una mod MelonLoader per Firestone Idle RPG: automatizza il gioco cliccando la sua UI
Unity da dentro il processo. Codice in `src/` (C#, net6.0), task in `src/Tasks`, path della UI in
`src/Infrastructure/Paths` (un file per schermata). Il bot gira su 34 istanze in produzione; qui si
testa solo **Steam-0**, un'istanza nativa (senza Sandboxie):

| Cosa | Dove |
|---|---|
| Repo | `C:\Repos\FirestoneBot` |
| Gioco | `C:\Program Files (x86)\Steam-0\steamapps\common\Firestone` |
| Mod | `<gioco>\Mods\firebot.dll` e `<gioco>\Mods\Firebot.TalentEngine.dll` |
| Configurazione | `<gioco>\UserData\FirebotPreferences.cfg` |
| Log | `<gioco>\MelonLoader\Latest.log` (riscritto a ogni avvio; i precedenti in `MelonLoader\Logs`) |

Controlla con `Test-Path` che questi percorsi esistano prima di iniziare. Se Steam-0 non c'è, fermati
e chiedi. Non toccare **mai** le altre istanze (Steam-1..34): né i loro processi né i loro file.

### Regole

- **Mai spendere gemme o soldi veri**, mai cliccare offerte a pagamento, mai comprare Eclipse
  Stones, mai vendere o usare barili, gold istantaneo o meteoriti istantanee (5/10/30 min, 1 h). Se
  una correzione richiede di toccare un bottone del genere, fermati e chiedi.
- La configurazione si modifica **solo a gioco chiuso**: il gioco riscrive il file quando esce.
  Salvala in UTF-8 senza BOM, con gli strumenti di modifica file: `Set-Content -Encoding UTF8` di
  PowerShell 5.1 aggiunge il BOM.
- **Parti dai log, non dalle ipotesi.** Prima di cambiare codice, trova la riga `[FAILED]` che
  nomina il path rotto, o le righe `[DEBUG]` del task che mostrano dove si ferma.
- **Path reali, non dedotti.** Un path si corregge solo dopo averlo visto nel gioco (dump dal vivo,
  vedi sotto) o nel dump degli asset. Ordine di fiducia: path già confermati dal vivo in questo
  codice > dump dal vivo > dump UnityPy degli asset > `docs/screens`. Prima di concludere che un
  bottone o un ingresso "non esiste", guarda l'elenco **completo** dei figli del contenitore, non
  solo quelli già mappati.
- Controlla dove viene usata una costante prima di cambiarla: alcuni path sono suffissi relativi
  (usati come `new GameButton(path, elementoPadre)`) e non vanno resi assoluti.
- Navigazione esplicita: ogni task clicca l'intera catena fino alla sua schermata e i tab che gli
  servono. Il badge di notifica è solo una scorciatoia cliccata prima.
- Convenzione dei path: sono considerati verificati dal vivo salvo un commento che dica il
  contrario. Quando confermi un path segnato come ipotizzato, togli quel commento; quando lo
  correggi, correggi anche i commenti e i documenti che lo descrivevano.
- Correzioni minime, sulla causa. Niente astrazioni nuove, niente refactoring non richiesti dal bug.
- Se la stessa correzione fallisce 3 volte, documenta cosa hai visto in "Problemi noti", rimetti il
  codice com'era e passa al task successivo.

### Comandi

**Fermare Steam-0** (chiude anche il client Steam di quell'istanza):

```powershell
$root = 'C:\Program Files (x86)\Steam-0'
Get-Process | Where-Object { $_.Path -like "$root\*" } | Stop-Process -Force
# Prima di toccare cfg o Mods, controlla che non sia rimasto nessun processo sotto $root.
```

**Build e deploy** (la build si può fare anche a gioco aperto; la copia no, i DLL sono bloccati):

```powershell
$env:COMMON_DIR = 'C:\Program Files (x86)\Steam-0\steamapps\common'
dotnet build C:\Repos\FirestoneBot\src\firebot.csproj -c Release
$out = 'C:\Repos\FirestoneBot\src\bin\Release\net6.0'
$mods = 'C:\Program Files (x86)\Steam-0\steamapps\common\Firestone\Mods'
Copy-Item "$out\firebot.dll", "$out\Firebot.TalentEngine.dll" $mods -Force
```

La build deve dare 0 errori e 0 warning; `dotnet test tests\Firebot.TalentEngine.Tests` deve
passare.

**Avviare Steam-0:**

```powershell
Start-Process 'C:\Program Files (x86)\Steam-0\steam.exe' -ArgumentList '-silent', '-applaunch', '1013320'
```

Con `auto_start = true` il bot parte da solo: è pronto quando `Latest.log` contiene
`Started. Enabled tasks: N of M loaded.` (di solito entro qualche minuto). Aspetta controllando il
log a intervalli, con un'attesa in background, non con pause lunghe in primo piano.

**Leggere il log.** `debug_mode = true` in `[firebot_settings]` è l'unico interruttore che serve:
accende le righe `[DEBUG]` del logger, che è anche dove finiscono i `[FAILED]` dei path. Le righe
da cercare:

| Riga | Significato |
|---|---|
| `Started. Enabled tasks: N of M loaded.` | Bot avviato |
| `[Task] <Gruppo - Nome> finished in Xs \| Next: ...` | Un task ha finito e ha pianificato il prossimo giro |
| `[FAILED] Task <Gruppo - Nome> timed out after Xs.` / `threw: ...` | Il task si è bloccato o è andato in eccezione |
| `[FAILED] Root '...' missing`, `Root Object not found`, `Path broken: ...` | Un path non risolve: rotto |
| `[FAILED] Element is hidden or inactive`, `Click ignored: Button disabled` | Il path esiste, ma l'elemento non è visibile o cliccabile in quel momento |
| `[Bot Status] Task Table` | Tabella di stato, stampata dopo ogni task |

Nel log ogni task si chiama `Gruppo - Nome`, come nella colonna Task della tabella di stato: per
esempio `Quests - Collector Quest`, `Town - Firestone Research`, `Warfront - Daily Missions`. Un
task `Waiting` con `Next Run` già passato è sotto il suo livello di sblocco: non è un errore.

```powershell
$log = 'C:\Program Files (x86)\Steam-0\steamapps\common\Firestone\MelonLoader\Latest.log'
Select-String -Path $log -Pattern '\[FAILED\]|threw:|timed out' | Select-Object -Last 40
Select-String -Path $log -Pattern '\[Task\] ' | Select-Object -Last 40
```

**Vedere lo schermo.** Uno screenshot dello schermo principale, da aprire con il tool di lettura
file; Steam-0 è la finestra in alto a sinistra della griglia:

```powershell
Add-Type -AssemblyName System.Windows.Forms, System.Drawing
$b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
$bmp = New-Object System.Drawing.Bitmap $b.Width, $b.Height
[System.Drawing.Graphics]::FromImage($bmp).CopyFromScreen($b.Location, [System.Drawing.Point]::Empty, $b.Size)
$bmp.Save("$env:TEMP\steam0.png")
```

**Vedere la gerarchia reale di una schermata:**

- **Dal vivo** (la fonte migliore): aggiungi temporaneamente, nel punto del task in cui la schermata
  è aperta, `Logger.Info(Watchdog.DumpChildrenRecursive("<path della radice>", 4));` oppure
  `Logger.Info(Watchdog.DumpActiveScreens());`. Build, deploy, avvio, leggi il log, poi **togli la
  riga** prima del commit.
- **Dagli asset**, senza avviare il gioco: `python tools\unity_ui_mapper.py <NomeRadice>`, con
  `FIRESTONE_DATA` impostata su `<gioco>\Firestone_Data` (serve `python -m pip install UnityPy`). Mostra
  la struttura del prefab, non lo stato a runtime (rami inattivi, cloni).

### Preparazione

1. `git pull`, poi annota `git log -1 --oneline`.
2. Ferma Steam-0. Copia `FirebotPreferences.cfg` e i due DLL di `Mods` in
   `C:\Repos\FirestoneBot-test-backup\` (fuori da `Mods`: MelonLoader carica ogni `.dll` che trova
   lì).
3. Nel cfg, in `[firebot_settings]`: `auto_start = true`, `debug_mode = true`,
   `start_bot_delay = 30.0`. Nelle sezioni dei task: `enabled` come in
   `tools/ConfigTemplate/FirebotPreferences.template.cfg` (tutto acceso tranne i due task Oracle), e `next_run_time_internal = ""` ovunque, così ogni task è subito dovuto.
4. Build, deploy, avvio.

### Fase 1: giro completo

Lascia girare il bot finché ogni task acceso ha avuto il suo turno: uno alla volta, può volerci
un'ora. Man mano, per ogni task, annota l'esito: finito senza errori, `[FAILED]`, mai partito
(livello), oppure comportamento diverso da quello scritto nella colonna "Da verificare" delle
tabelle sotto.

### Fase 2: correzioni

Per ogni problema trovato:

1. Individua dal log il passo esatto che fallisce.
2. Se è un path, trova quello reale (dump dal vivo o degli asset) e correggilo in
   `src/Infrastructure/Paths`.
3. Isola il task: ferma Steam-0, metti `enabled = false` a tutti gli altri task, per questo
   `enabled = true` e `next_run_time_internal = ""`. Build, deploy, avvio, osserva. Ripeti finché
   passa.
4. Riaccendi gli altri task. Aggiorna la riga del task qui sotto (stato ✅ e data) e ogni documento
   che descriveva il path o il comportamento vecchio.
5. Commit, con nel messaggio la prova vista dal vivo (quale riga di log o quale schermata).

### Fase 3: chiusura

1. Ferma Steam-0. Nel cfg rimetti i valori del backup in `[firebot_settings]` e negli `enabled`;
   lascia i valori auto-gestiti che il bot ha scritto.
2. Deploy dell'ultima build buona, riavvia Steam-0 e controlla che parta (`Started.`) senza
   `[FAILED]`.
3. Aggiorna questo file (stati, data, problemi nuovi), commit, e push se il prompt di avvio lo
   chiede.
4. Riepilogo per l'utente: cosa è passato, cosa ha fallito e perché, cosa hai corretto (con i
   commit), cosa resta aperto e cosa richiede una sua decisione.

---

## Configurazione di riferimento (Steam-0, 29/09)

Tutte le istanze, su entrambi i PC, devono avere questi valori in `FirebotPreferences.cfg`. Il
29/09 sera Steam-1..16 sono state allineate a Steam-0 e verificate con un confronto chiave per
chiave. I valori auto-gestiti (`next_run_time_internal`, `last_done_date`, contatori del giorno,
`known_maxed_nodes`) non si copiano: il bot li scrive da solo.

Il template `tools/ConfigTemplate/FirebotPreferences.template.cfg` è questa stessa configurazione, e
`tools/ConfigTemplate/apply_template.ps1` la applica a un intervallo di istanze: a gioco chiuso (il
gioco riscrive il file quando esce), in UTF-8 senza BOM, e per le istanze sandboxate sia nel percorso
classico sia in quello del sandbox. Tiene i valori auto-gestiti e le due eccezioni qui sotto, e con
`-Check` mostra solo le differenze. Una chiave che il file non ha ancora viene solo segnalata: si
avvia l'istanza una volta con la DLL nuova (crea le sezioni mancanti), si chiude e si rilancia lo
script.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\ConfigTemplate\apply_template.ps1 -From 0 -To 16 -Check  # questo PC
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\ConfigTemplate\apply_template.ps1 -From 17 -To 34        # secondo PC
```

Due eccezioni, che dipendono dalla macchina o dall'account e non vanno copiate da Steam-0:

- `window_grid_first_instance`: il numero più basso delle istanze del PC (0 qui, 17 sul PC con
  Steam-17..34).
- `talentstask.guide_start_index`: la calibrazione del singolo account. Lasciare quello che c'è; su
  un account nuovo `-1`, che si calibra da solo al primo giro.

`[firebot_settings]`: `auto_start = true`, `start_bot_delay = 30.0`, `scan_interval = 5.0`,
`interaction_delay = 1.0`, `max_task_runtime = 120.0`, `debug_mode = true`, `shortcut_key = "F7"`,
`free_speedup_seconds = 170.0`, `low_resource_mode = true`, `target_frame_rate = 15`,
`render_quality_level = 0`, `window_width = 504`, `window_height = 316`,
`window_grid_enabled = true`, `window_grid_columns = 5`.

Task spenti (`enabled = false`): `oracleritualstask`, `oraclesgifttask`. Tutti gli altri accesi,
compresi `warmachinestask`, `hallofheroestask` (dal 30/09), `massproductioneventtask`,
`sigilsofprophecyeventtask` e le azioni di background `hero_upgrade`, `auto_retreat`,
`flying_bonus_hunter`.

| Sezione | Impostazioni |
|---|---|
| `collectorquesttask` | `min_common_chest_reserve = 10` (solo per le 6 chest extra) |
| `empowertask` | `min_reset_ratio = 2.0`, `min_adventure_minutes = 60`, `max_adventure_minutes = 120` |
| `experimentstask` | `resource_type = "0"` |
| `guardiantrainingtask` | `guardian_index = 0`, `use_strange_dust = false` |
| `meteoriteresearchtask` | `recheck_interval_minutes = 60`, `min_meteorite_reserve = 3000` |
| `freepickaxestask` | `pickaxe_claim_threshold = 5` |
| `mapmissionstask` | `mission_time_order = "asc"` |
| `pathofglorytask` | `recheck_interval_minutes = 60` |
| `talentstask` | `priority_overrides = ""` (e `guide_start_index`: vedi sopra) |
| `hero_upgrade` | `sweep_interval_seconds = 5.0`, `upgrade_target_slots = ""` |
| `auto_retreat` | `stall_minutes = 3.0`, `retreat_stages = 5` |
| `flying_bonus_hunter` | `poll_seconds = 2.0` |

Un file nuovo nasce invece con i default del codice, cioè i default F2P della Fase 0, diversi in
quattro punti: Empower `1.0` / `0` / `0` (solo il +100%), Map Missions `desc`, War Machines spento,
task Oracle accesi. Per questo su un'istanza nuova lo script va passato dopo il primo avvio. Passare
il fleet ai default F2P resta la decisione aperta in "Da fare e rimandato".

---

## Stato per task

Legenda: ✅ verificato dal vivo · ⚠️ verificato in parte · ❌ mai girato dal vivo ·
🔄 logica cambiata di recente (28-29/09), da riverificare.

"Liv." è il livello personaggio sotto il quale il task non parte mai. "Default" è `enabled` in un
file di configurazione nuovo.

### Quests

| Task | Sezione | Liv. | Default | Stato | Da verificare / note |
|---|---|:-:|:-:|:-:|---|
| Quests | `[queststask]` | - | on | ✅ | Claim di giornaliere e settimanali, solo i claim pronti (8 s a giro senza niente da reclamare). Le quest di Collector, Gamer, Merchant e Miner le reclamano già i loro task. Dal 30/09 la riga `[INFO] Quests: claimed N daily, M weekly.` dice quanti ne ha presi: il 30/09 alle 11 Steam-1 e -14 hanno reclamato 1 giornaliera a testa. |
| Beer Exchange | `[beerexchangetask]` | 15 | on | ✅ | Usa solo l'offerta pagata in birra, mai le due in gemme. 29/09: senza badge non apriva mai il mercato (lo `stormyButton` della Taverna non lo apre); ora entra da `actionButtons/shop` ("Market"), verificato dal vivo. |
| Collector | `[collectorquesttask]` | - | on | ✅ | Legge il progresso vero della quest ("Open 4 Chests") e apre le chest mancanti dalla più economica (Wooden, Iron, Common, poi Uncommon fino a Legendary se non c'è altro: la quest ha la priorità). Nello stesso giro apre 6 chest extra, solo Wooden/Iron/Common e mai Common sotto `min_common_chest_reserve`, per dare oggetti da vendere al Merchant. Jewel e celestial sempre tutte. 30/09, al reset: 17 quest su 17 da 0/4 a 4/4, ma ogni rarità apriva una sola chest (la schermata dei risultati cercata in `popups/` invece che in `menus/ChestOpening`), quindi la quest è stata fatta con 1 Wooden, 1 Iron, 1 Common e 1 Uncommon (più una Rare su Steam-11 e -16) e gli extra si sono fermati a 3/6. Corretto e verificato su Steam-2: 6 Wooden di fila in 20 s. Il primo giro corretto della parte quest si vede al reset dell'1/10. |
| Gamer | `[gamerquesttask]` | 15 | on | ✅ | Legge il progresso vero ("Play 10 times") e gioca a x1 una carta per ogni giocata mancante, finché ci sono gettoni (li spende solo questa quest). 30/09, al reset: Steam-0 da 0/10 a 10/10 con 20 gettoni (39 s), e così sulle istanze con gettoni; Steam-6 con 5 gettoni si è fermato a 5/10, Steam-11 con 0 gettoni a 0/10, e riprovano ogni ora. |
| Merchant | `[merchantquesttask]` | 30 | on | ✅ | Legge il progresso vero ("Sell 10 items") e vende quanti oggetti mancano, scelti per nome visibile e in quest'ordine: Midas' Touch, Scroll of Health, Scroll of Damage. Mai Scroll of Speed né altro (oro e meteoriti istantanei, barili). La riga `[INFO] Sell grid:` elenca nomi e quantità veri. 30/09, al reset: 12 istanze a 10/10; le altre 5 si sono fermate a 6-9/10 perché avevano finito proprio quei tre oggetti (es. Steam-1: 5 Damage, 3 Health, 1 Midas = 9), mai venduto altro. Più oggetti arriveranno dalle 6 chest extra di Collector, ora corrette. |
| Miner | `[minerquesttask]` | 50 | on | ✅ | Legge il progresso vero e colpisce una volta per colpo mancante, a 1 piccone per colpo (`hitButton/costText`; su alcuni account il selettore x1/x5 è nascosto). 30/09, al reset: 17 su 17 a 5/5, ma su 8 la rilettura subito dopo i colpi diceva 0-4/5 (la schermata delle quest resta indietro di qualche secondo): al giro successivo tutte a 5/5 senza altri colpi, quindi costa solo un controllo di 4 s un'ora dopo. Il 29/09 invece Steam-16 aveva perso davvero 2 colpi su 5 (35 picconi, poi 32). Dal 30/09 ogni colpo è confermato dal contatore dei picconi (sceso di 1, altrimenti riclicca, al massimo 3 volte) con una riga `[ArcaneCrystal] click n/3: pickaxes X -> Y`: da vedere al reset dell'1/10. |

### Town

| Task | Sezione | Liv. | Default | Stato | Da verificare / note |
|---|---|:-:|:-:|:-:|---|
| Daily Store Offers | `[dailystoreofferstask]` | - | on | ✅ | Check-in giornaliero e le due mystery box gratuite. Entra solo dai badge CheckIn/MysteryBox: il `storeButton` dell'HUD non apre niente. Una volta reclamato il check-in del giorno (`last_done_date`) aspetta il reset delle 10:00. 30/09: dopo il reset ha reclamato su 17 istanze su 17 (`Next` alle 10:00 del giorno dopo, badge spenti). |
| Engineer | `[engineertask]` | 50 | on | ✅ | Strumenti ogni 6 ore. Verificato dal badge (anche il 29/09); la strada dall'edificio (popup GarageSelection) è corretta ma non riverificata: col badge la schermata è già aperta e il `townButton` è nascosto. |
| War Machines | `[warmachinestask]` | 50 | **off** | ⚠️ | Navigazione e stop sul popup CurrencyMissing verificati, un livellamento completo no. Spento: consuma gli stessi gettoni spedizione del Personal Tree, che la guida mette prima. |
| Guardian Training | `[guardiantrainingtask]` | - | on | ✅ | Allena `guardian_index` se sbloccato, altrimenti Vermilion. |
| Experiments | `[experimentstask]` | 120 | on | ⚠️ | Claim verificato. Default `resource_type = "0"` (solo Dragon blood). |
| Oracle Rituals | `[oracleritualstask]` | 200 | on | ❌ | Nessun account a 200. Vedi "Da fare e rimandato". |
| Oracle's Gift | `[oraclesgifttask]` | 200 | on | ❌ | Nessun account a 200. |
| Firestone Research | `[firestoneresearchtask]` | - | on | ✅ | Priorità in ordine: Raining Gold, Firestone Finder, Firestone Effect, Trainer Skills, Expeditioner; poi il primo nodo mai toccato. Il livello si legge dal numero prima della barra (`'Level 33/50'` → 33). 30/09: `fresh` visto su Steam-5 e -6 (`Selected research #1 ... (priorityRank=none, fresh)`), e le priorità scelte sulle altre (rank 1, 3, 4). |
| Meteorite Research | `[meteoriteresearchtask]` | - | on | ✅ | Priorità: Raining Gold, Firestone Finder, Firestone Effect; poi il primo nodo sbloccato con un costo. Un livello a giro (costi 200-1.000 meteoriti, niente timer). Mai sotto `min_meteorite_reserve` (3000). Ogni giro scrive `[INFO] Meteorite balance N ('testo'), reserve 3000.` 30/09: col saldo sopra 3.000 (fino a 14.114 dopo gli sblocchi di Hall of Heroes) sceglie Firestone Effect (`priorityRank=2`) su Steam-12..14 e il primo nodo disponibile altrove, dove i nodi prioritari (terzo strato) non sono ancora sbloccati. |
| Empower | `[empowertask]` | - | on | ⚠️ | Solo a rapporto `min_reset_ratio = 1.0`, cioè il "+100%" della guida; i limiti di tempo sono spenti. Lettura dei Firestone corretta il 29/09: oltre T il gioco scrive due lettere minuscole (aa = 1e15, poi ×1000 per lettera). Su Steam-0 i testi erano `'1,79bl'` e `'65,67bl'`, letti come 0 (416 letture su 416 nei log del 26-29/09); ora escono 1.79E+126 e 6.567E+127, rapporto 0,03. Reset vero visto il 29/09 alle 17:53 (per il limite di 2 h del cfg vecchio): i Firestone del Temple sono passati da `65,67bl` a `98,33bl`, cioè esattamente i `32,62bl` trovati (+50%, rapporto letto 0,5). Da confermare che il "+X%" mostrato dal gioco al reset coincida col rapporto letto. Steam-0 ha ancora il cfg vecchio (`min_reset_ratio = 2`, limiti a 60/120 minuti), quindi lì empowera a 2 h. Dal 30/09 una lettura vuota (Temple non aperto, di solito per i popup d'avvio) riprova dopo 5 minuti invece di aspettare `min_adventure_minutes` da un tempo letto 0: prima costava un'ora (Steam-12, -13, -15 dopo un riavvio). Verificato su Steam-4, -8, -10, -14; Steam-4 (rapporto 14,4) e -8 (oltre 2 h) hanno poi fatto l'empower. |
| Arena of Kings | `[arenaofkingstask]` | 80 | on | ✅ | Scelta dell'avversario più debole e lettura dei gettoni verificate. 30/09 dopo il reset, sui 7 account a livello 80: giri da 81 a 467 s chiusi normalmente (6 s a gettoni finiti). Dal 30/09 una riga `[INFO] Arena: N token(s), look L: my power ..., opponents ... -> slot S.` per combattimento: il ciclo di 5 gettoni si legge al reset dell'1/10. |
| Pirate's Prize | `[piratesprizetask]` | 10 | on | ✅ | Solo la traccia gratuita. 29/09: il gioco scarica il menu PirateShip quando non serve e lo ricostruisce in più di 1 s; prima circa metà dei giri scriveva `Tier list root not found`. Ora il task aspetta la lista (verificato al primo giro dopo l'avvio, il caso che falliva). |
| System Mail | `[systemmailtask]` | - | on | ✅ | Il percorso del badge della posta è ipotizzato; senza, gira comunque ogni 6 ore. |

### Guild

| Task | Sezione | Liv. | Default | Stato | Da verificare / note |
|---|---|:-:|:-:|:-:|---|
| Expedition | `[expeditiontask]` | 10 | on | ✅ | Parte sempre la prima spedizione in lista. |
| Tree of Life | `[treeoflifetask]` | 10 | on | ⚠️ | Personal Tree. Priorità: Raining Gold, Firestone Finder, Firestone Effect, Battle Cry, Miner (nuova). Il 29/09 ha comprato per 49 s senza errori, ma gli acquisti non vengono loggati: la priorità non si può controllare dal log. |
| Free Pickaxes | `[freepickaxestask]` | 50 | on | ✅ | Reclama da `pickaxe_claim_threshold` (5) in su. Dal 29/09 gira solo col badge (`NextRunTime = MaxValue`, come Talents); la strada Gilda → Guild Shop è stata tolta perché nei log del 26-29/09 non ha mai raggiunto il timer in 1.015 giri su 1.038. Verificato il 29/09: `Next` = 12/31/9999; alle 17:23 il badge ha aperto lo shop, il task ha reclamato e il badge si è spento; senza badge non è più ripartito. Da controllare: il formato del testo della quantità (`Quantity` concatena tutte le cifre, come faceva il livello di Firestone Research; con un testo tipo "3/30" leggerebbe 330). |
| Awakening | `[awakeningtask]` | 50 | on | ⚠️ | L'attesa dell'animazione è stimata. Il badge resta acceso: nei log del 26-29/09 il task ha girato 325 volte, una ogni ~50 s, per 33 minuti. Dal 29/09 `BadgeCooldown` limita i giri a uno ogni 30 minuti: verificato sugli eventi, non su Awakening (il 29/09 il suo badge non si è acceso). |
| Chaos Rift | `[chaosrifttask]` | 100 | on | ⚠️ | Solo Tomes of Power, mai Eclipse Stones. Gira solo su Steam-0 (l'unico account a livello 100). 30/09 al reset ha colpito con le 10 Moon Stone ricaricate (poi a 0), a x5, ma il click su Market subito dopo i colpi è stato ignorato e non ha comprato niente; ora il click si ripete finché il negozio non si apre (verificato su un secondo giro, senza colpi: aperto al primo tentativo). Tolto anche il click sull'auto-hit (bottone senza stato leggibile, spento su Steam-0). Da vedere al reset dell'1/10: il negozio aperto subito dopo i colpi. |
| Forbidden Knowledge | `[forbiddenknowledgetask]` | 100 | on | ✅ | Schermata verificata; il bottone dell'edificio in Gilda è ipotizzato. |
| Guardian Holy Upgrade | `[guardianholyupgradetask]` | 100 | on | ✅ | Chaos Rift → Upgrades → Magic Quarters. 29/09: lasciava aperti LockedGuardian, Chaos Rift e la Gilda (li chiudeva il Watchdog); ora li chiude il task. |

### Map e Warfront

| Task | Sezione | Liv. | Default | Stato | Da verificare / note |
|---|---|:-:|:-:|:-:|---|
| Map Missions | `[mapmissionstask]` | - | on | 🔄 | Ora le più lunghe per prime (`mission_time_order = "desc"`). Il 29/09 gira senza errori (30-35 s), ma Steam-0 ha ancora `asc` nel cfg: `desc` non verificato. |
| Warfront Campaign Loot | `[warfrontcampaignloottask]` | 50 | on | ⚠️ | Il 29/09 gira senza errori (3 s); non si è visto niente da reclamare. |
| Warfront Daily Missions | `[warfrontdailymissionstask]` | 50 | on | ✅ | Battaglie reali. 30/09 al reset: 2 missioni su 10 su 16 account e 4 su Steam-0, in 26-49 s: le altre non hanno il bottone di combattimento (non disponibili per l'account), quindi il limite è del gioco. Nessun popup "Here are your rewards!" rimasto aperto in 34 battaglie. |

### Character

| Task | Sezione | Liv. | Default | Stato | Da verificare / note |
|---|---|:-:|:-:|:-:|---|
| Talents | `[talentstask]` | - | on | ✅ | Parte solo sul badge TalentAvailable. |
| Path of Glory | `[pathofglorytask]` | - | on | ✅ | Traccia gratuita e Golden (se posseduta); non compra mai il pass. |
| Hall of Heroes | `[hallofheroestask]` | - | on | ✅ | Riscritto il 29/09: legge la formazione dal Party, ogni 24 h rilegge tutti gli eroi (`hero_snapshot` nel cfg) e con `EnchantPlanner` decide prima cosa comprano Void Crystal ed Ethereal Shards; prima di ogni click rilegge livello e costo dallo schermo. Gli eroi sotto la gear power del tier bloccato successivo (1300 per il tier 2, 6600 per il 3) ricevono i Void Crystal per primi, tier 1 compreso anche fuori formazione; alla soglia il bot sblocca il tier confermando il popup `GearTierUnlock` (480 meteoriti per il tier 2, 720 per il 3), solo se l'icona del costo è quella dei meteoriti. Verificato su Steam-0 il 29/09 (lettura completa, incantesimi, snapshot vecchio corretto senza click, secondo giro in 8 s) e su Steam-0..16 il 30/09: primo giro su 16 account da 6-8 eroi in 86-266 s, da 4 a 120 incantesimi per account, classe 0 vista al lavoro (es. Steam-4, Cirilo da 500 a 1.800 di power); dopo la correzione del popup, 67 tier sbloccati su 13 account, nessuno rimasto bloccato, nessun timeout né eccezione. Un tier appena sbloccato ha gli slot vuoti (`M`) finché non arrivano i pezzi dai forzieri: la lettura completa del giorno dopo li vede. |

### Scarab Game

| Task | Sezione | Liv. | Default | Stato | Da verificare / note |
|---|---|:-:|:-:|:-:|---|
| Pharaoh's Vault | `[pharaohsvaulttask]` | 60 | on | ✅ | Spin con i Noble Token gratuiti, vault, milestone. 29/09: la schermata non ha un ingresso alle milestone (dump dal vivo: `actionButtons` ha solo pharaohVault, beasts, lostInscriptions, shop; la barra del livello apre `ScarabGameLevelBonuses`). Le milestone si reclamano solo quando le apre il badge `ScarabGameMilestones`, come nei log del 26/09; il click sul path inesistente è stato tolto. |
| Scarab Game Free Token | `[scarabgamefreetokentask]` | 60 | on | ✅ | Omaggio giornaliero del tab Saldi. |

### Events

| Task | Sezione | Liv. | Default | Stato | Da verificare / note |
|---|---|:-:|:-:|:-:|---|
| Decorated Heroes | `[decoratedheroeseventtask]` | - | on | ✅ | Acquisti nell'ordine Dragon blood → Meteorite → Beer (29/09: ordine confermato nel log; Meteorite non c'è nello shop di questo account, valuta insufficiente per il resto). 29/09: 3 sfide reclamate al giro partito dal badge. |
| New Player Event | `[newplayereventtask]` | - | on | ✅ | Stesso ordine: compra Meteorite, poi spende il resto in Beer; check-in giornaliero e milestone. Gli account vecchi non hanno l'evento (il task rallenta da solo), come Steam-0. 30/09 dopo il reset, su Steam-8..16: check-in reclamato, 1 milestone su 14, 1 Meteorite comprato. |
| Mass Production | `[massproductioneventtask]` | - | on | ✅ | Solo il tab Challenges. |
| Sigils of Prophecy | `[sigilsofprophecyeventtask]` | - | on | ✅ | Aggiunto il 29/09. Stessa schermata di Mass Production (`events/MiniEvents`), cambia solo il riquadro nell'elenco eventi. Verificato dal vivo: il riquadro apre MiniEvents, 1 claim su 3 giorni (gli altri ancora bloccati), schermata chiusa. 30/09 (secondo giorno): ha reclamato la sfida del giorno su 9 istanze; sulle altre nessuna sfida era ancora completa, e reclama solo quelle. 30/09: su Steam-15 e -16 la carta c'è ma è bloccata per l'account, e il task la ritentava ogni 2 minuti (65 giri in una mattina); ora una carta bloccata aspetta un'ora come un evento assente. Verificato su entrambe. |

Un evento non in corso per l'account, o bloccato per lui, scrive `'<evento>' isn't open to this account (not listed or locked)`: non è
un errore.

Dal 29/09 gli eventi partono anche sul badge rosso del bottone Events
(`rightSideUI/menuButtons/eventsButton/notification`, visto dal vivo), oltre al controllo orario. Il
badge è uno solo per tutti gli eventi, quindi quando si accende girano tutti, al massimo uno ogni 30
minuti (`BadgeCooldown`). Verificato su Steam-0: Decorated Heroes è partito alle 17:19 invece che
alle 17:36 e ha reclamato 3 sfide; dopo il giro dei quattro eventi il badge si è spento. Costo: circa
7 s ciascuno per gli eventi non in corso.

### Azioni di background (girano in continuo, non nello scheduler)

| Azione | Sezione | Default | Stato | Da verificare / note |
|---|---|:-:|:-:|---|
| Hero Upgrade | `[hero_upgrade]` | on | ✅ | |
| AutoRetreat | `[auto_retreat]` | on | ✅ | Per provarlo in fretta: `stall_minutes = 1` su uno stage duro. |
| Flying Bonus Hunter | `[flying_bonus_hunter]` | on | ⚠️ | Bersagli trovati dal vivo su Steam-0. 29/09: i 4 bottoni sono sempre attivi e cliccabili, e il bot li cliccava tutti a ogni giro (~4 s di click ogni ~6 s), con un'eccezione del gioco sugli hunter fermi. In volo è attivo solo il loro figlio (`hunter`, `dragon`, `femaleDragon`): ora clicca solo allora, e ogni click scrive `[FlyingBonusHunter] Clicking ...`. Dopo il click non compare nessun popup. Da confermare: che il click dia davvero la ricompensa (il bonus resta spesso in volo e viene cliccato 2 volte). |

Globali: `low_resource_mode` ✅ (18/09) e griglia delle finestre ✅ (26/09, 18 istanze su
2560x1440).

## Problemi noti

- **Popup di avvio.** Il Watchdog li chiude (`popups/OfflineProgress/bg/collectButton` e
  `events/DecoratedHeroesPromotion/bg/closeButton`), ma compaiono da 10 a ~50 s dopo `Started.`. Con
  `start_bot_delay = 10` i primi task partivano mentre erano aperti e fallivano in silenzio: Quests
  (15 s di tentativi sull'avatar), Empower (testi vuoti: col cfg vecchio di Steam-0 il controllo
  slittava di un'ora), Meteorite Research (saldo 0). Con 60 sono stati chiusi prima del primo task in
  tutti gli otto avvii di test. Dal 29/09 il default è 30 (scelta dell'utente), applicato anche al
  fleet: nel primo avvio a 30 i popup erano già chiusi prima del primo task. In un avvio del 29/09
  però sono comparsi ~45 s dopo `Started.`, e sul fleet (29/09, 21:47) su Steam-7 dopo ~50 s: il
  primo task (Collector) non ha aperto la scheda Personaggio e ha riprovato un'ora dopo. Il 30/09,
  nei riavvii delle 10:19 e delle 10:32, il primo task (Empower) non ha aperto il Temple su 4-5
  istanze su 17: Empower ora riprova dopo 5 minuti, ma lo stesso può capitare a qualunque primo
  task. Se dà fastidio, portarlo a 60. Il vecchio "what's new" non si è visto.
- Il `storeButton` dell'HUD non apre niente (zero listener): Daily Store Offers dipende dai suoi badge.
- Percorsi ipotizzati, mai visti dal vivo: badge della posta, bottone Forbidden Knowledge in Gilda.
- `[FAILED] Path broken: ... popups/CurrencyMissing/bg/closeButton` compare una volta per sessione al
  primo controllo del popup valuta: il gioco crea il popup solo la prima volta che lo mostra. Non è
  un errore.
- **Party**: gli eroi della formazione si caricano uno alla volta, per 2-4 s dopo l'apertura (anche
  i tick del deck e il "Deployed: N/5"). Subito dopo l'avvio, con i popup iniziali ancora aperti, il
  bottone Party può non esserci: Hall of Heroes tiene allora la formazione dello snapshot.
- **Badge che restano accesi = task in loop (corretto e verificato il 29/09).** Prima un
  badge acceso rendeva il task sempre pronto, anche se aveva appena girato: Chaos Rift e Awakening
  hanno girato così per 106 minuti in 3 giorni (log del 26-29/09). Ora in `BotTask.IsReady` il badge
  conta solo se il task non ha girato negli ultimi 30 minuti (`BadgeCooldown`, calcolato su
  `LastRunTime`). I timer non cambiano: `NextRunTime` e il nuovo tentativo dopo 2 minuti di un giro
  fallito funzionano come prima. MinerQuestTask ha in più il suo override, che esclude i giri fino
  al giorno dopo.
- **Ogni click a vuoto aspetta comunque `InteractionDelay`.** `GameButton.Click` aspetta anche
  quando il bottone è nascosto o disabilitato, e non clicca. Succede per esempio con i passi Town →
  edificio dopo che il badge ha già aperto la schermata, o con i claim disabilitati di Quests.
  Correzione proposta: aspettare solo dopo un click vero. Non applicata il 29/09: il gioco scarica e
  ricostruisce alcuni menu quando servono (PirateShip, WorldMap) e ci mette più di 1 s, quindi le
  attese a vuoto della navigazione oggi coprono davvero schermate lente. Tolto invece il caso che
  costava di più: i claim di Quests e Merchant (`CharacterScreen.QuestClaimButtons`) ora elencano
  solo i bottoni cliccabili (Quests da 37 s a 8 s).
- **Empower controlla ogni 5 minuti** finché il rapporto non basta (`RetryDelay`). Il 29/09 su
  Steam-0: rapporto da 0,03 a 0,39 in un'ora. Con i default F2P del codice (`min_reset_ratio = 1.0`,
  nessun limite di tempo) sono 5 s ogni 5 minuti per ore; un ritardo più lungo sotto una certa soglia
  di rapporto dimezzerebbe il costo.
- **Chaos Rift a volte non apre la schermata** dal bottone della Gilda (29/09, 16:31), mentre due
  minuti dopo Guardian Holy Upgrade l'ha aperta al primo click. Non riprodotto nel giro isolato.

- `HoldButton` (Hero Upgrade) manda il pointer down ma mai il pointer up: il "rilascio" avviene
  quando il bottone smette di essere interattivo. Funziona da mesi; da tenere presente se Hero
  Upgrade si comporta in modo strano.
- I selettori di quantità si fermano sulla prima opzione x10/x5 che incontrano nel ciclo, non sulla
  più grande. Il costo è proporzionale: cambia solo il numero di click.
- `QuantityToggle`, riscritto nella pulizia del 28/09, non è ancora stato visto al lavoro: Chaos
  Rift a colpi multipli (serve Moon Stone) e gli shop degli eventi.
- Collector: i tempi tra un'apertura di chest e l'altra non sono ottimali (18/09).

## Da fare e rimandato

- **DLL del 30/09 sul fleet**: la mattina del 30/09 (Hall of Heroes) è stata distribuita su
  Steam-0..16 (questo PC, percorso classico e sandbox), e i loro cfg sono stati allineati al template
  (vedi "Configurazione di riferimento"; `apply_template.ps1 -Check`: 33 file, 0 differenze). Un
  secondo PC (istanze 17-34, `MULTI_INSTANCE_SETUP.md`) va aggiornato a parte. Le sezioni e le chiavi
  nuove (`[sigilsofprophecyeventtask]`, `last_done_date` di Daily Store Offers, `hero_snapshot`) si
  creano da sole.
- **Default F2P sul fleet (da decidere)**: i default della Fase 0 nel codice (Personal Tree al posto
  delle War Machines, Empower solo a +100%, task Oracle accesi, missioni `desc`) valgono solo per i
  file nuovi. Il 29/09 il template e il fleet di questo PC sono stati allineati a Steam-0 (vedi
  "Configurazione di riferimento"), che ha ancora i valori vecchi per quei quattro punti. Se si
  passa ai default F2P: si cambiano Steam-0, il template e quella sezione, poi si ripassa
  `apply_template.ps1` su tutte le istanze.
- **Hall of Heroes sul secondo PC (istanze 17-34)**: acceso su Steam-0..16 la mattina del 30/09
  (template, `apply_template.ps1` e DLL nuova). Per replicarlo basta la procedura "Aggiornare il bot
  su istanze già in funzione" di `MULTI_INSTANCE_SETUP.md`: `apply_template.ps1` mostrerà
  `hallofheroestask.enabled: false -> true`. Al primo giro ogni account legge tutti gli eroi e spende
  i Void Crystal, gli Ethereal Shards e i meteoriti accumulati (da 1,5 a 4,5 minuti per istanza), e
  i controlli sono nella stessa procedura.
- **Rituali Oracle**: ce ne sono quattro (Obedience: forzieri solar; Harmony: comet; Concentration:
  oracle's gift ed emblemi; Serenity: lunar), da 40 minuti, uno alla volta, reset ogni 6 ore. Oggi
  parte il primo in ordine di griglia. A cadenza h24 dovrebbero comunque partire tutti dentro la
  finestra: quando un account arriva a 200, verificare prima di tutto che il task ne riavvii uno a
  ogni completamento. Solo se non lo fa serve la scelta per nome, che richiede un dump UnityPy della
  griglia (oggi nessun testo col nome del rituale è mappato).
- Rimandati perché a cadenza h24 rendono poco: modalità "Next Milestone" di Hero Upgrade, upgrade
  globali/speciali (`upgradesButtonUI`, mai mappato), spedizione con più punti, missione del drago
  prioritaria (`MissionPin` non ha il tipo di missione).
- Non ancora gestiti: claim gratuito del Monthly pass nello shop di Scarab's Game, missioni Dungeon
  del Warfront, moltiplicatore bulk delle War Machines, Soul stones (Hall of Heroes, livello 200).
- Scartato: la routine di push (pergamene e pouch allo stage ottimale). Fragile e rischia di
  sprecare risorse; i consumabili di gold si tengono per l'uso a mano.

## Da segnalare subito se succede

- Qualunque spesa di gemme o click su un acquisto a pagamento.
- Eclipse Stones comprate (Chaos Rift deve comprare solo Tomes of Power).
- Venduti o usati barili, gold istantaneo o meteoriti istantanee (5/10/30 min, 1 h).
- Un task che non chiude la schermata che ha aperto.
- Hall of Heroes che incanta il tier 1 di un eroe fuori dalla formazione, tranne gli eroi in sblocco
  tier (sotto la gear power del tier bloccato successivo).

## Tornare indietro

Il tag `pre-hall-of-heroes-2026-09-29` segna il codice prima della riscrittura di Hall of Heroes
(commit `6a75e0a`). Il tag `pre-cleanup-2026-09-28` segna il codice prima della pulizia (commit `a27ecf0`, già con il
riallineamento alla guida F2P). Il commit precedente al riallineamento è `10eebca`.

```powershell
cd C:\Repos\FirestoneBot
git switch --detach pre-cleanup-2026-09-28     # oppure 10eebca
# build e deploy come sopra, sull'istanza che serve (a gioco chiuso)
git switch main                                # per tornare all'ultima versione
```

Per annullare una sola parte della pulizia senza tornare indietro su tutto: `git revert <commit>`,
dove i commit sono quelli della serie successiva al tag (`git log --oneline pre-cleanup-2026-09-28..main`).
