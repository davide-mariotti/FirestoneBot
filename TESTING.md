# Test dal vivo

Runbook per una sessione di test **autonoma** di Claude Code sul PC della flotta, sull'istanza
Steam-0: prova tutti i task, trova i bug dai log, li corregge, ricompila, ridistribuisce e riprova
da solo. In fondo: stato di ogni task, problemi noti, lavoro rimandato e come tornare alla versione
precedente. Aggiornato al 2026-10-03 (log di Steam-0..16 dalle 18:03 del 2/10 alle 11 del 3/10, col
reset delle 10:00; secondo PC: reset del 2/10, Empower di un giorno, rollout di 094b4f6), prima al 2026-10-02 (log di Steam-0..16 dalle 22:00 dell'1/10 alle 16:00 del 2/10,
con il reset delle 10:00), dopo il giro del reset dell'1/10 su Steam-0..16 e l'aggiornamento del
secondo PC (Steam-17..34, account a livello 30-59), e dopo la prima sessione di test dal vivo su Steam-0 (dopo il
riallineamento alla guida F2P, `docs/firestone_guida_F2P.md`, e la pulizia del codice del 28/09),
il rollout di Hall of Heroes su Steam-0..16 e, il 30/09 sera, quello delle sfide degli eventi
(`EVENTS_PLAN.md`) su Steam-0..16.

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
   `start_bot_delay = 60.0`. Nelle sezioni dei task: `enabled` come in
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

## Configurazione di riferimento (Steam-0, 30/09)

Tutte le istanze, su entrambi i PC, devono avere questi valori in `FirebotPreferences.cfg`. Il
29/09 sera Steam-1..16 sono state allineate a Steam-0 e verificate con un confronto chiave per
chiave; il 30/09 alle 16:10, al riavvio completo, di nuovo tutte e 17 (`start_bot_delay = 60`
ovunque, Map Missions `desc`). I valori auto-gestiti (`next_run_time_internal`, `last_done_date`, contatori del giorno,
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

`[firebot_settings]`: `auto_start = true`, `start_bot_delay = 60.0`, `scan_interval = 5.0`,
`interaction_delay = 1.0`, `max_task_runtime = 120.0`, `debug_mode = true`, `shortcut_key = "F7"`,
`free_speedup_seconds = 170.0`, `low_resource_mode = true`, `target_frame_rate = 10` (dall'1/10; prima 15),
`render_quality_level = 0`, `window_width = 504`, `window_height = 316`,
`window_grid_enabled = true`, `window_grid_columns = 5`.

Task spenti (`enabled = false`): `oracleritualstask`, `oraclesgifttask`. Tutti gli altri accesi,
compresi `warmachinestask`, `hallofheroestask` (dal 30/09), `minieventtask` (dal 30/09, al
posto di `massproductioneventtask` e `sigilsofprophecyeventtask`), `emblemmarkettask` (dall'1/10) e le azioni di background `hero_upgrade`, `auto_retreat`,
`flying_bonus_hunter`.

| Sezione | Impostazioni |
|---|---|
| `collectorquesttask` | `min_common_chest_reserve = 10` (solo per le 6 chest extra) |
| `empowertask` | `min_reset_ratio = 1.0`, `min_adventure_minutes = 60`, `max_adventure_minutes = 360` (dall'1/10; prima `2.0` / `60` / `120`) |
| `experimentstask` | `resource_type = "0"` |
| `guardiantrainingtask` | `guardian_index = 0`, `use_strange_dust = false` |
| `meteoriteresearchtask` | `recheck_interval_minutes = 60`, `min_meteorite_reserve = 3000` |
| `freepickaxestask` | `pickaxe_claim_threshold = 5` |
| `mapmissionstask` | `mission_time_order = "desc"` (dal 30/09; prima `asc`) |
| `pathofglorytask` | `recheck_interval_minutes = 60` |
| `talentstask` | `priority_overrides = ""` (e `guide_start_index`: vedi sopra) |
| `hero_upgrade` | `sweep_interval_seconds = 15.0` (dall'1/10; prima `5.0`), `upgrade_target_slots = ""` |
| `auto_retreat` | `stall_minutes = 3.0`, `retreat_stages = 5` |
| `flying_bonus_hunter` | `poll_seconds = 2.0` |

Un file nuovo nasce invece con i default del codice, cioè i default F2P della Fase 0, diversi in
tre punti: Empower `1.0` / `0` / `0` (il template ha in più il primo controllo a 60 minuti e il tetto
a 6 h), War Machines spento, task Oracle accesi (Map Missions `desc` è ormai anche nel template). Per questo su un'istanza nuova lo script va passato dopo il primo avvio. Passare
il fleet ai default F2P resta la decisione aperta in "Da fare e rimandato".

---

## Stato per task

Legenda: ✅ verificato dal vivo · ⚠️ verificato in parte · ❌ mai girato dal vivo ·
🔄 logica cambiata di recente (28-30/09), da riverificare.

"Liv." è il livello personaggio sotto il quale il task non parte mai. "Default" è `enabled` in un
file di configurazione nuovo.

### Quests

| Task | Sezione | Liv. | Default | Stato | Da verificare / note |
|---|---|:-:|:-:|:-:|---|
| Quests | `[queststask]` | - | on | ✅ | Claim di giornaliere e settimanali, solo i claim pronti (8 s a giro senza niente da reclamare). Le quest di Collector, Gamer, Merchant e Miner le reclamano già i loro task. Dal 30/09 la riga `[INFO] Quests: claimed N daily, M weekly.` dice quanti ne ha presi: il 30/09 alle 11 Steam-1 e -14 hanno reclamato 1 giornaliera a testa. |
| Beer Exchange | `[beerexchangetask]` | 15 | on | ✅ | Usa solo l'offerta pagata in birra, mai le due in gemme. 29/09: senza badge non apriva mai il mercato (lo `stormyButton` della Taverna non lo apre); ora entra da `actionButtons/shop` ("Market"), verificato dal vivo. |
| Collector | `[collectorquesttask]` | - | on | ✅ | Legge il progresso vero della quest ("Open 4 Chests") e apre le chest mancanti dalla più economica (Wooden, Iron, Common, poi Uncommon fino a Legendary se non c'è altro: la quest ha la priorità). Nello stesso giro apre 6 chest extra, solo Wooden/Iron/Common e mai Common sotto `min_common_chest_reserve`, pensate per dare oggetti da vendere al Merchant (l'1/10 si è visto che non ne danno: vedi Merchant). Jewel e celestial sempre tutte. 30/09, al reset: 17 quest su 17 da 0/4 a 4/4, ma ogni rarità apriva una sola chest (la schermata dei risultati cercata in `popups/` invece che in `menus/ChestOpening`), quindi la quest è stata fatta con 1 Wooden, 1 Iron, 1 Common e 1 Uncommon (più una Rare su Steam-11 e -16) e gli extra si sono fermati a 3/6. Corretto e verificato su Steam-2: 6 Wooden di fila in 20 s. **1/10, al reset:** catena corretta su tutte le 8 istanze che hanno fatto la quest (Steam-1: `'/Wooden': done, opened 4/4`, poi 6 extra; `Collector: 4/4 chest(s) for the quest, 6/6 extra`, 50-55 s). Sulle altre 9 il task ha letto le quest di ieri (vedi "Problemi noti", corretto); su Steam-0, con la correzione e la quest rimessa in coda, alle 11:08 `0/4 -> 4/4` con 4 Wooden e 6 extra. **2/10, al reset:** 17 su 17 `0/4 -> 4/4` entro le 10:06; la rilettura è scattata su Steam-12 e -16 (10:00:06 `4/4 right after the reset - read again later`, alle 10:06 `0/4 -> 4/4`), nessun `N/N -> N/N`. Il popup "New items" si chiude dentro l'apertura (1-3 volte per istanza) e ogni rarità arriva al conteggio pieno. **3/10, al reset:** 17 su 17 `0/4 -> 4/4` entro le 10:15; rilettura su 9 istanze (Steam-0, 2, 3, 5, 9, 11..14), ogni volta seguita da `0/4 -> 4/4`. |
| Gamer | `[gamerquesttask]` | 15 | on | ✅ | Legge il progresso vero ("Play 10 times") e gioca a x1 una carta per ogni giocata mancante, finché ci sono gettoni (li spende solo questa quest). 30/09, al reset: Steam-0 da 0/10 a 10/10 con 20 gettoni (39 s), e così sulle istanze con gettoni; Steam-6 con 5 gettoni si è fermato a 5/10, Steam-11 con 0 gettoni a 0/10, e riprovano ogni ora. 1/10, al reset: 14 su 15 a 10/10, Steam-11 a 3/10 con 3 gettoni. Dal 30/09 le giocate sono in `GamerQuestTask.Play`, usato anche dalle sfide degli eventi (riga `Tavern: N draw(s) to play, T token(s)`). 3/10, al reset: 17 su 17 `0/10 -> 10/10` entro le 10:04. |
| Merchant | `[merchantquesttask]` | 30 | on | ✅ | Legge il progresso vero ("Sell 10 items") e vende quanti oggetti mancano, scelti per nome visibile e in quest'ordine: Midas' Touch, Scroll of Health, Scroll of Damage. Mai Scroll of Speed né altro (oro e meteoriti istantanei, barili). La riga `[INFO] Sell grid:` elenca nomi e quantità veri. 30/09, al reset: 12 istanze a 10/10; le altre 5 si sono fermate a 6-9/10 perché avevano finito proprio quei tre oggetti (es. Steam-1: 5 Damage, 3 Health, 1 Midas = 9), mai venduto altro. Più oggetti arriveranno dalle 6 chest extra di Collector, ora corrette. Dal 30/09 l'Exotic Upgrade del giro si compra solo con l'icona `exoticCoin64` (vista dal vivo su tutti i 12 upgrade) ed è confermato dal contatore delle monete (`Exotic upgrade '<nome>': coins X -> Y`); **1/10, al reset:** upgrade comprato su tutte le 15 istanze che hanno fatto la quest, sempre confermato dal contatore (es. Steam-1 `coins 1736 -> 488`, Steam-3 `941 -> 29`). Quest a 10/10 su 8 istanze; 7 si sono fermate a 3-9/10, ancora per mancanza dei tre oggetti (Steam-3: 2 Damage e 1 Health), anche dopo le 6 chest extra di Collector. I forzieri non li riforniscono: i 90 Wooden/Iron aperti quella mattina su 9 istanze hanno dato un solo Midas' Touch, e gli Scroll vengono dalle missioni avventura (wiki). **Dall'1/10 (scelta dell'utente)**, finiti i tre, la quest si completa con i potenziamenti da battaglia che il bot non usa mai, in quest'ordine: Totem of Annihilation, Totem of Agony, Guardian's Rune, Dragon Armor (decine su ogni account). Mai Drums of War (aumenta il gold dei consumabili tenuti per l'uso a mano), mai Speed, gold istantaneo o barili. Le sfide degli eventi vendono ancora solo i tre. Verificato su Steam-3 alle 12:04: `sold 0/5` dai tre, poi `sold 5/5` dalla riserva, quest `5/10 -> 10/10`, upgrade esotico comprato. **Secondo PC, 1/10 alle 14:53-14:55** (account a livello 30-59, quasi senza i tre oggetti): 12 quest ferme a 2-9/10 tutte a 10/10, 4-7 vendite dalla riserva ciascuna (Steam-27: Midas x1, poi `sold 7/7`, `2/10 -> 10/10`). Le griglie prima della vendita sono in `2026-10-01-fleet\sell-grids-1455.txt`: al giro dopo Scroll of Speed, oro, barili e Drums of War devono avere le stesse quantità. **2/10, al reset:** 17 quest su 17 a 10/10 (Steam-8, -10, -12, -15 con 2-5 vendite dalla riserva), upgrade esotico comprato su tutte e 17, sempre confermato dal contatore. **3/10, al reset:** 17 su 17 a 10/10 (Steam-9, -11, -13 con 4 vendite dalla riserva, Steam-10 con 2); upgrade esotico su 16, confermato dal contatore; su Steam-0 nessun upgrade pagabile (in quel caso il task non scrive niente). **Secondo PC, verificato al reset del 2/10:** sulle 11 istanze con la griglia dell'1/10, nessuna quantità scesa per Scroll of Speed, Pouch/Bucket/Crate/Barrel of gold e Drums of war (Steam-19: Speed da 7 a 13, il resto uguale; vendute 5 Damage e 3 Health, poi 2 dalla riserva). Merchant 10/10 su tutte e 18. |
| Miner | `[minerquesttask]` | 50 | on | ✅ | Legge il progresso vero e colpisce una volta per colpo mancante, a 1 piccone per colpo (`hitButton/costText`; su alcuni account il selettore x1/x5 è nascosto). 30/09, al reset: 17 su 17 a 5/5, ma su 8 la rilettura subito dopo i colpi diceva 0-4/5 (la schermata delle quest resta indietro di qualche secondo): al giro successivo tutte a 5/5 senza altri colpi, quindi costa solo un controllo di 4 s un'ora dopo. Il 29/09 invece Steam-16 aveva perso davvero 2 colpi su 5 (35 picconi, poi 32). Dal 30/09 ogni colpo è confermato dal contatore dei picconi (sceso di 1, altrimenti riclicca, al massimo 3 volte) con una riga `[ArcaneCrystal] click n/3: pickaxes X -> Y`: vista dal vivo il 30/09 sui 10 colpi della sfida di Decorated Heroes (tutti al primo click, 31 → 21), che usa gli stessi passi (`MinerQuestTask.HitCrystal`, riga `Arcane Crystal: N hit(s) to do, P pickaxe(s)`). **1/10, al reset:** 15 quest su 15 da 0/5 a 5/5 con 5 picconi esatti, e la rilettura subito dopo dava già 5/5 (Steam-13 e -16 vedi Collector); il riclick è servito 4 volte (Steam-0, 2, 4, 7: `click 1/3: pickaxes 18 -> 18`, poi `click 2/3: 18 -> 17`). **2/10, al reset:** 15 su 17 a 5/5, ogni colpo al primo click. Steam-0 aveva un solo piccone (`0/5 -> 1/5`, poi `0 pickaxe(s)` ogni ora: vedi Free Pickaxes; alle 16:06 la quest era `5/5` senza colpi del bot, e il badge dei picconi si era spento alle 15:53 senza un suo giro); su Steam-12, con 92 picconi, nessun click è andato a segno (vedi "Problemi noti"). **3/10, al reset:** 17 su 17 a 5/5, 13 subito e 4 al giro delle 11:02. Chi ha colpito tra le 10:01:36 e le 10:01:55 (le 9 istanze con la rilettura di Collector, dove Miner parte prima) ha visto i colpi in ritardo: Steam-12 e -13 3 click a vuoto (`82 -> 82`), Steam-14 3 colpi e poi 3 a vuoto, Steam-0 colpi contati a scatti (`3 -> 0`), e alle 10:01:53 il contatore è sparito su Steam-2, -5, -9, -11 (`101 -> 0`). Alle 11:02 i click "a vuoto" risultavano presi: Steam-12 e -13 picconi scesi di 3 e quest a 3/5 (2 colpi, `3/5 -> 5/5`), Steam-14 già 5/5 (6 colpi in tutto, un piccone in più). Chi ha colpito dalle 10:02:48 (8 istanze) 5/5 al primo click. |

### Town

| Task | Sezione | Liv. | Default | Stato | Da verificare / note |
|---|---|:-:|:-:|:-:|---|
| Daily Store Offers | `[dailystoreofferstask]` | - | on | ✅ | Check-in giornaliero e le due mystery box gratuite. Entra solo dai badge CheckIn/MysteryBox: il `storeButton` dell'HUD non apre niente. Una volta reclamato il check-in del giorno (`last_done_date`) aspetta il reset delle 10:00. 30/09: dopo il reset ha reclamato su 17 istanze su 17 (`Next` alle 10:00 del giorno dopo, badge spenti). |
| Engineer | `[engineertask]` | 50 | on | ✅ | Strumenti ogni 6 ore. Verificato dal badge (anche il 29/09); la strada dall'edificio (popup GarageSelection) è corretta ma non riverificata: col badge la schermata è già aperta e il `townButton` è nascosto. Non si può provare dal vivo: il timer del task scade quando gli strumenti sono pronti, cioè quando il badge è già acceso (1/10: 34 giri nella flotta, tutti dal badge). |
| War Machines | `[warmachinestask]` | 50 | **off** | ⚠️ | Navigazione e stop sul popup CurrencyMissing verificati, un livellamento completo no. Spento: consuma gli stessi gettoni spedizione del Personal Tree, che la guida mette prima. |
| War Machine Rarity | `[warmachineraritytask]` | 50 | on | ✅ | Badge `WarMachinesRarity`, altrimenti ogni 12 h. Macchina per macchina in ordine di griglia, alza la rarità solo se il costo è in Tools (`toolsIcon64`); conferma dal contatore Tools che scende. Il contatore (`counters/currencyInteraction (Tools)`) esiste solo col tab rarità aperto: letto prima dava -1 (corretto il 30/09). 30/09 dopo il rollout: Steam-2 e Steam-3 hanno alzato `warMachineSquare (1)` per 10.000 Tools (10468 → 468, 10271 → 271); gli altri 13 account non arrivavano al costo. |
| Guardian Training | `[guardiantrainingtask]` | - | on | ✅ | Allena `guardian_index` se sbloccato, altrimenti Vermilion. `GuardianTrainingTask.Enlighten` illumina Vermilion per le sfide degli eventi (vedi Decorated Heroes); `use_strange_dust` resta spento. |
| Guardian Evolution | `[guardianevolutiontask]` | - | on | ✅ | Badge `GuardianEvolution`, altrimenti ogni 12 h. Evolve ogni guardiano col bottone cliccabile e il costo in Strange Dust (icona `strangeDust64`); conferma dal bottone che cambia o sparisce. 30/09 dopo il rollout: Steam-13 (13:29) e Steam-14 (13:59, col badge acceso dopo il primo giro) hanno evoluto Vermilion per 300 Strange Dust; il badge apre Magic Quarters già sul guardiano e dopo il click il bottone sparisce. Gli altri 15 account non avevano evoluzioni pronte (Steam-0: serve livello 10, costo 600). |
| Experiments | `[experimentstask]` | 120 | on | ⚠️ | Claim verificato. Default `resource_type = "0"` (solo Dragon blood). |
| Oracle Rituals | `[oracleritualstask]` | 200 | on | ❌ | Nessun account a 200. Vedi "Da fare e rimandato". |
| Oracle's Gift | `[oraclesgifttask]` | 200 | on | ❌ | Nessun account a 200. |
| Firestone Research | `[firestoneresearchtask]` | - | on | ✅ | Priorità in ordine: Raining Gold, Firestone Finder, Firestone Effect, Trainer Skills, Expeditioner; poi il primo nodo mai toccato. Il livello si legge dal numero prima della barra (`'Level 33/50'` → 33). 30/09: `fresh` visto su Steam-5 e -6 (`Selected research #1 ... (priorityRank=none, fresh)`), e le priorità scelte sulle altre (rank 1, 3, 4). 1/10: eccezione dopo l'avvio di una ricerca, corretta (vedi "Problemi noti"). |
| Meteorite Research | `[meteoriteresearchtask]` | - | on | ✅ | Priorità: Raining Gold, Firestone Finder, Firestone Effect; poi il primo nodo sbloccato con un costo. Un livello a giro (costi 200-1.000 meteoriti, niente timer). Mai sotto `min_meteorite_reserve` (3000). Ogni giro scrive `[INFO] Meteorite balance N ('testo'), reserve 3000.` 30/09: col saldo sopra 3.000 (fino a 14.114 dopo gli sblocchi di Hall of Heroes) sceglie Firestone Effect (`priorityRank=2`) su Steam-12..14 e il primo nodo disponibile altrove, dove i nodi prioritari (terzo strato) non sono ancora sbloccati. **Bug corretto l'1/10 (d38ba2d):** il carosello si apre sull'ultimo albero visto e la scansione andava solo avanti, quindi gli alberi precedenti non venivano mai guardati: trovava un nodo solo il primo giro dopo l'avvio del gioco (Steam-0: acquisti in 2 giri su 12; Steam-14 solo al primo giro di ognuna delle sue 4 sessioni) e i meteoriti si accumulavano (9.000-14.000 su Steam-12..16). Ora la scansione torna prima a `tree1`. In più, senza nodi prioritari, sceglieva il primo nodo con un costo e non il più economico (Steam-0: Skip wave a 800 con 450 disponibili, accanto a nodi da 500 e 600). Visto dal vivo su Steam-14: da `Tree II` a `tree1` (tutto al massimo), poi `tree2`, un nodo scelto in 4 giri di fila (prima solo al primo). Un dump dell'1/10 mostra cosa c'è: su Steam-0 e -14 `tree1` è tutto `Level N - Max`, in `tree2` sono sbloccati 4-8 nodi da 500-800 e gli altri chiedono un livello (`Requires: Level 4`). **Secondo bug, corretto l'1/10 (96ef59a):** dopo un acquisto il bot ricliccava il nodo per leggere il costo seguente e chiudeva anteprima e Biblioteca entro 1-3 s; da lì il gioco non apriva più nessuna anteprima fino al riavvio (tutti i click dei giri successivi senza anteprima: era il vero motivo per cui comprava solo il primo giro di ogni sessione). Riprodotto su Steam-16 con un solo livello comprato. Il gioco riabilita le ricerche 5 s dopo un acquisto (`MeteoriteResearchHandler`, `reenableCompleteMeteoriteResearchTime = 5`). Ora i livelli si comprano dall'anteprima aperta una volta, dopo l'ultimo acquisto il bot non tocca niente per 6 s, e un giro fa un nodo solo: se ha comprato, il successivo parte dopo 1 minuto con la Biblioteca riaperta. Visto su Steam-8: acquisto alle 20:32, giri delle 20:34 e 20:37 con tutte le anteprime lette. Per le prove Steam-16 e Steam-8 hanno comprato sotto la riserva (riserva abbassata a mano: 3.200 e 800 meteoriti, nodi scelti dal bot). **Correzione confermata il 2/10:** in 18 h 68 acquisti su 16 istanze (fino a 8 per account), e nessun giro dopo un acquisto senza anteprime; saldi alle 16:00 tra 2.977 e 3.611, Steam-8 e -16 tornati sopra la riserva da soli. **Secondo PC, 3/10 dalle 02:05** (prima volta con le due correzioni): saldi accumulati fino a 14.300; 6 livelli per giro dall'anteprima aperta (`'Expose Weakness' for 250: balance 12151 -> 11901`, poi 11651 ... ogni 1,2 s), un giro ogni ~3 minuti, 12-36 livelli per istanza in 45 minuti, fino a Raining gold. Alle 02:50 tutte e 18 ferme tra 3.043 e 3.986, nessun acquisto sotto 3.000; ogni acquisto confermato dal saldo (4 su ~500 scesi di 1 meteorite in meno: il guadagno durante il click). |
| Empower | `[empowertask]` | - | on | ✅ | Solo a rapporto `min_reset_ratio = 1.0`, cioè il "+100%" della guida; i limiti di tempo sono spenti. Lettura dei Firestone corretta il 29/09: oltre T il gioco scrive due lettere minuscole (aa = 1e15, poi ×1000 per lettera). Su Steam-0 i testi erano `'1,79bl'` e `'65,67bl'`, letti come 0 (416 letture su 416 nei log del 26-29/09); ora escono 1.79E+126 e 6.567E+127, rapporto 0,03. Reset vero visto il 29/09 alle 17:53 (per il limite di 2 h del cfg vecchio): i Firestone del Temple sono passati da `65,67bl` a `98,33bl`, cioè esattamente i `32,62bl` trovati (+50%, rapporto letto 0,5). **Confermato il 3/10 che il rapporto letto è il guadagno vero:** sui 72 empower di Steam-0..16 dalle 18:03 del 2/10, i Firestone del Temple letti dopo sono quelli di prima più i trovati, entro lo 0,3% (l'arrotondamento del testo), con rapporti da 0,55 (empower forzato a 6 h) a 1,58. **1/10, dai log del giorno:** tutta la flotta aveva il cfg di Steam-0 (`min_reset_ratio = 2`, limiti a 60/120 minuti), quindi il +200% non arrivava mai e ogni empower era forzato a 2 h, con rapporti tra 0,21 e 1,84 (in calo nel corso della giornata, es. Steam-1 da 0,88 a 0,37). Dall'1/10 (scelta dell'utente) la flotta è a `1.0` / `60` / `360`: empower a +100% come dice la guida, con un tetto di sicurezza a 6 h. Attenzione: il rapporto rallenta molto al muro degli stage; al ritmo di fine avventura, ad arrivare a +100% servirebbero da 0,4 h in più (Steam-3, -7) a 6-8 h (Steam-5, -10, -16), quindi su quegli account l'empower arriverà al tetto delle 6 h. Primo empower col cfg nuovo su Steam-7 alle 14:31: `Adventure time: 01:55:11 ... Ratio: 1.08 (need 1 after 01:00:00, or force empower after 06:00:00)`, Temple da `16,02ar` a `33,33ar` (= 16,02 + 17,31 trovati). Da rileggere dopo un giorno: a che rapporto e dopo quanto avviene ogni empower (`Adventure time: ...` subito prima di un'avventura che riparte da 0). **Riletto sul secondo PC** (account a livello 30-59, 1/10 14:47 → 2/10 23:29, 18 istanze): 170 empower, 145 a rapporto 1,00-1,18 (mediana 163 minuti di avventura), 25 forzati dal tetto delle 6 h con rapporto 0,41-0,98 (Steam-29: 3 su 5; Steam-18 tre di fila con rapporto in calo 0,96 → 0,73 → 0,53, poi di nuovo +100% in 106 minuti). Rapporti sopra 1,2 solo quando il +100% arriva prima dei 60 minuti minimi (Steam-21: 2,06 e 2,8). Nessun empower sotto il rapporto prima delle 6 h. Dal 30/09 una lettura vuota (Temple non aperto, di solito per i popup d'avvio) riprova dopo 5 minuti invece di aspettare `min_adventure_minutes` da un tempo letto 0: prima costava un'ora (Steam-12, -13, -15 dopo un riavvio). Verificato su Steam-4, -8, -10, -14; Steam-4 (rapporto 14,4) e -8 (oltre 2 h) hanno poi fatto l'empower. 30/09: il Tempio a volte compare qualche secondo dopo il click, e letti subito i tre testi erano vuoti (~20 giri su ~630 nella flotta, `[FAILED] Temple's Firestones read as 0 from ''`); ora aspetta fino a 5 s il testo, altrimenti riprova fra 5 min. Verificato su Steam-10 il giro normale (5 s). **Cadenza col cfg `1.0` / `60` / `360`, dalle 22:00 dell'1/10 alle 16:00 del 2/10** (il primo empower di Steam-2..11 e -14, tra le 00:43 e le 02:51, è al tetto per un'avventura iniziata prima del riavvio): Steam-0 10 empower, uno ogni ~1 h 45 a rapporto 1,1-3,0; Steam-1 14, ogni ~1 h 15, rapporto 1,0-9,2; Steam-2, -3, -7 10-11, ogni 1 h 05-1 h 40, rapporto 1,0-5,0; Steam-4 e -5 8, ogni 1 h 15-2 h, rapporto 1,0-1,8. Gli account al muro degli stage: Steam-11, -13, -15, -16 ogni 1 h 45-5 h 45, sempre a 1,0-1,05 (mai al tetto); Steam-6, -8, -12 un po' a 1,0 (3-5 h) e un po' al tetto a 0,67-0,94; Steam-9, -10, -14 quasi sempre al tetto delle 6 h, a 0,63-0,94. Dove il +100% arriva prima dei 60 minuti (Steam-1: 9,2 a 1 h 15) è `min_adventure_minutes` a decidere. |
| Arena of Kings | `[arenaofkingstask]` | 80 | on | ✅ | Scelta dell'avversario più debole e lettura dei gettoni verificate. **2/10, al reset:** 5 gettoni su 5 su tutti gli 8 account a livello 80 (Steam-0..7), giri da 228 a 588 s, nessuna uscita per griglia nascosta. 30/09 dopo il reset, sui 7 account a livello 80: giri da 81 a 467 s chiusi normalmente (6 s a gettoni finiti). 1/10, al reset: una riga `[INFO] Arena: N token(s), look L: my power ..., opponents ... -> slot S.` per combattimento, 5 su 5 gettoni su Steam-0, 1, 4, 5, 6 (7-10 minuti); sugli account deboli ogni gettone usa tutti e 12 gli sguardi e prende il più debole. **Bug corretto l'1/10:** dopo un reroll o una battaglia la griglia degli avversari può restare nascosta e le potenze si leggono 0, cioè "più deboli": il task sceglieva lo slot 0 senza bottone, aspettava 5 minuti la battaglia e ricominciava. Steam-3 e -7 ci hanno perso 5 minuti; su Steam-2 la griglia non è più ricomparsa e il bot è rimasto fermo in Arena per un'ora (10:02-11:05, fino a `MaxRuntimeSeconds`), senza fare nessun altro task. Ora aspetta fino a 20 s che le potenze compaiano, altrimenti chiude l'arena con `opponents not shown ... - left for the next run`. **Visto dal vivo l'1/10 su Steam-2**, due volte con l'ultimo gettone: alle 11:18 dopo due battaglie (giro di 244 s) e alle 12:10 appena aperta l'arena (27 s). Il badge `ArenaTokens` si spegne appena l'arena è stata aperta (dopo le 11:18 non è più ripartito per 50 minuti; si riaccende al riavvio del gioco), quindi il gettone rimasto aspetta il controllo successivo, 6 h dopo (17:18, 18:10): ancora prima del reset delle 10:00, quindi non si perde. **3/10, al reset:** su 6 degli 8 account (Steam-1, -2, -3, -4, -6, -7), tra le 10:07 e le 10:10, durante l'Arena il gioco ha ricaricato la scena (`Stopped.`, 15-30 s dopo `Started.`, senza riavvio del processo): mai visto prima nei log dal 29/09. Dopo il ricaricamento la "my power" letta è salita (Steam-7 da 6.680 a 17.485, Steam-1 da 21.850 a 26.277), quindi la sessione aveva una potenza vecchia. Il bot è ripartito da solo e l'Arena ha ripreso: 5 gettoni su 5 su 7 account; su Steam-6 il giro dopo il ricaricamento (10:11) non ha trovato l'ingresso dell'Arena e i gettoni rimasti aspettano le 16:11, prima del reset. Player.log vuoto: la causa è lato gioco e non si vede da qui. |
| Pirate's Prize | `[piratesprizetask]` | 10 | on | ✅ | Solo la traccia gratuita. 29/09: il gioco scarica il menu PirateShip quando non serve e lo ricostruisce in più di 1 s; prima circa metà dei giri scriveva `Tier list root not found`. Ora il task aspetta la lista (verificato al primo giro dopo l'avvio, il caso che falliva). |
| Emblem Market | `[emblemmarkettask]` | 65 | on | ✅ | Exotic Merchant → Emblem market, solo gear (Emblem of Courage) e jewels (Emblem of Valor): tutti i lotti che gli emblemi pagano del forziere più raro mostrato, mai uno più economico al suo posto, mai celestials, nessuna riserva (scelte dell'utente, 01/10); poi apre esattamente i forzieri comprati. Ogni 6 h. La rarità è il codice che il gioco tiene sulla cella (`chestType` di `ExoticEmblemMarketGearChestInteraction` / `ExoticEmblemMarketJewelChestInteraction`: Common 0 … Epic 3, Wooden 0 … Golden 2, Diamond 3), non il nome; un forziere non ancora sbloccato è una cella nascosta (il Diamond su Steam-0). Ogni click solo con l'icona `emblemOfCourage64` / `emblemOfValor64` e il contatore della categoria (`counters/currencyInteraction (EmblemOfCourage)` / `(EmblemOfValor)`, visibile solo con la categoria aperta) che copre il costo, e conta solo se il contatore scende esattamente del costo. Il click compra subito, senza popup di conferma. Nell'inventario i forzieri jewel hanno uno slot per rarità col nome del codice (`/Wooden`, `/Iron`, `/Golden`; uno slot `jewelChest` non esiste, quindi quella voce di Collector non trova mai niente), e Collector non apre mai i Golden. **1/10 su Steam-0** (livello 166): `gear: 'Epic chest' (rarity 3 Epic), 5000 each, emblems 8878 -> 3878, 1 lot(s)`, `jewels: 'Golden chest' (rarity 2 Golden), 5000 each, emblems 10100 -> 100, 2 lot(s)`, `opened 3/3 '/Epic'`, `opened 10/10 '/Golden'` (36 s, nient'altro toccato); dopo un riavvio i contatori erano 3.878 e 100 e il giro non ha comprato niente (8 s). **Flotta, 1/10 21:42-21:48:** su Steam-1..15 sempre Epic e Golden, ogni lotto confermato dal contatore (Steam-1 Courage `7795 -> 2795`, Valor `37567 -> 2567`, 7 lotti), nessun `[FAILED]`; Epic comprati su 7 account (Courage 5.743-8.435), Golden su tutti e 15 (2-7 lotti, Valor fino a 37.567). Steam-16 sotto il livello 65: il task non parte (non ha mai fatto nemmeno Beasts, livello 60). L'apertura si fermava sul popup "New items": vedi "Problemi noti". **2/10:** tre acquisti nella notte (Steam-2, -8, -10, un lotto Golden ciascuno, es. Steam-10 Valor `5379 -> 379`), aperti `5/5 '/Golden'` con il popup chiuso 2-3 volte durante l'apertura (37-39 s); negli altri giri ogni 6 h nessun lotto pagabile, nessun `[FAILED] Emblem market`. **Dalle 18:03 del 2/10 alle 10:10 del 3/10:** 7 acquisti, sempre del forziere più raro e confermati dal contatore (Steam-0 un lotto Epic `5231 -> 231`, aperti `3/3 '/Epic'`; Steam-1, -4, -5, -6, -7, -11 un lotto Golden ciascuno, aperti `5/5`), nessun `[FAILED]`. Da verificare: un'apertura di più di 10 forzieri con l'attesa di 15 s, e il Diamond (cella visibile con 100 stelle della campagna, slot `/Diamond`), mai visto su nessun account. |
| System Mail | `[systemmailtask]` | - | on | ✅ | Il percorso del badge della posta è ipotizzato; senza, gira comunque ogni 6 ore. 1/10, secondo PC: `leftSideUINew/mail/notification` esiste (letto "hidden or inactive", non "Path broken", su tutte e 18), la variante `bottomLeftSideUI` no; mai visto acceso, nessuna posta con ricompense in 54 giri. |

### Guild

| Task | Sezione | Liv. | Default | Stato | Da verificare / note |
|---|---|:-:|:-:|:-:|---|
| Expedition | `[expeditiontask]` | 10 | on | ✅ | Parte sempre la prima spedizione in lista. |
| Tree of Life | `[treeoflifetask]` | 10 | on | ✅ | Personal Tree. Priorità: Raining Gold, Firestone Finder, Firestone Effect, Battle Cry, Miner (nuova); poi il nodo al livello più basso. Dal 30/09 logga ogni acquisto (`Tree of Life: Mana Heroes 4 -> 5.`) e il nodo che non si può pagare (`... at 3 costs more tokens than are left.`): alle 14:14-14:20 acquisti su Steam-3, 4, 7, gli altri senza gettoni. Il costo dipende solo dal livello, quindi al primo nodo troppo caro prova solo quelli a livello più basso: prima li provava uno per uno, 4 s l'uno, 40-50 s a giro senza niente da comprare; su Steam-5 ora 9 s. Dal 30/09 gli acquisti sono in `TreeOfLifeTask.Buy`, usato anche dalle sfide dei mini-eventi (un acquisto per quanto chiede la sfida); il giro normale è riverificato su Steam-0 (15:28, due nodi troppo cari, 13 s) e sulla flotta dopo il rollout (Steam-1: 7 acquisti, `Rage Heroes 3 -> 4` e altri, poi stop al primo nodo troppo caro). |
| Free Pickaxes | `[freepickaxestask]` | 50 | on | ✅ | Reclama da `pickaxe_claim_threshold` (5) in su. Dal 29/09 gira solo col badge (`NextRunTime = MaxValue`, come Talents); la strada Gilda → Guild Shop è stata tolta perché nei log del 26-29/09 non ha mai raggiunto il timer in 1.015 giri su 1.038. Verificato il 29/09: `Next` = 12/31/9999; alle 17:23 il badge ha aperto lo shop, il task ha reclamato e il badge si è spento; senza badge non è più ripartito. Dal 30/09 il giro scrive `[INFO] Free pickaxes: N ('testo'), threshold 5.`: vista l'1/10 su Steam-6, -7 e -16, `Free pickaxes: 5 ('x5')`, quindi il testo è "xN" e la lettura è giusta. Anche il claim funziona: su tutte e 17 le istanze una lettura ogni 8 ore esatte, sempre a `x5` (senza il claim il badge resterebbe acceso e il numero salirebbe). **Bug trovato il 2/10:** dal passaggio a 10 fps (1/10, 16:41) la maggior parte dei giri legge `0 ('')` (prima, dal 30/09, 32 letture e nessuna vuota): 244 letture vuote su 262 nella flotta in 18 h, Steam-0 e -13 mai un claim (29 e 33 giri), Steam-0 rimasto con 1 piccone e la quest Miner ferma a 1/5. Ogni giro vuoto lasciava lo shop aperto al Watchdog (letture vuote = chiusure del Watchdog, istanza per istanza). Catturata la finestra di Steam-13 alle 16:16: il task ha letto alle 02.360, lo shop è comparso dopo, alle 02.758, con `x15`, "Next free 55:01" e Claim. Il task leggeva 2 s dopo il click sul badge: a 10 fps lo shop ci mette di più, e anche il click di chiusura arrivava durante l'apertura. Ora aspetta fino a 5 s che la quantità compaia. **Verificato su Steam-13 alle 17:12** (riavviato con la build nuova): alle 51.792 la quantità era ancora nascosta, alle 52.295 `Free pickaxes: 16 ('x16')`, claim (la riga mostra "Claimed" e il prossimo tra 01:34:18), shop chiuso dal task, badge spento. **Flotta dalle 18:03-18:08 del 2/10:** nei primi 10 minuti Steam-1, -2, -3, -9, -10, -11 hanno letto e reclamato da `x6` a `x13`, nessuna lettura vuota e nessuno shop chiuso dal Watchdog. **Correzione confermata il 3/10:** dalle 18:03 del 2/10 alle 10:25 del 3/10, 2-3 letture per istanza su tutte e 17 (`x5` ogni 8 h esatte, più le `x6`-`x16` al riavvio), nessuna `0 ('')` e nessun `GuildShop` chiuso dal Watchdog. **Secondo PC, 3/10:** con 094b4f6 (10 fps, senza la correzione) dalle 02:04 alle 13:28, 51 letture vuote e 51 `GuildShop` chiusi dal Watchdog su 14 istanze; Steam-26 e -27 a 0 picconi con la Miner ferma a `0/5` tutto il giorno. Con la build nuova (13:29), alle 13:35 Steam-26 e -27 `Free pickaxes: 30 ('x30')`, claim e Miner `0/5 -> 5/5`; Steam-19 `x5`; nessuna lettura vuota. |
| Awakening | `[awakeningtask]` | 50 | on | ⚠️ | L'attesa dell'animazione è stimata. Il badge resta acceso: nei log del 26-29/09 il task ha girato 325 volte, una ogni ~50 s, per 33 minuti. Dal 29/09 `BadgeCooldown` limita i giri a uno ogni 30 minuti: verificato sugli eventi e, l'1/10, anche su Awakening: dalle 10:30 il badge resta acceso e il task gira ogni 30 minuti (Steam-0: 10:39, 11:09, 11:40), 6-11 s a giro. 3/10: stesso schema di Chaos Rift prima della correzione, un risveglio per giro (Steam-16: 6 giri da 8,7 s ogni 30 minuti dalle 04:34 alle 07:05, poi badge spento), probabilmente perché dopo `Awaken` e i 2,5 s stimati il bottone non è ancora cliccabile e il ciclo si ferma (non visto dal vivo). I cristalli si spendono comunque tutti; se serve, la stessa attesa del bottone di Chaos Rift (`Poll.Until`). |
| Chaos Rift | `[chaosrifttask]` | 100 | on | ✅ | Solo Tomes of Power, mai Eclipse Stones. Gira solo su Steam-0 (l'unico account a livello 100). 30/09 al reset ha colpito con le 10 Moon Stone ricaricate (poi a 0), a x5, ma il click su Market subito dopo i colpi è stato ignorato e non ha comprato niente; ora il click si ripete finché il negozio non si apre (verificato su un secondo giro, senza colpi: aperto al primo tentativo). Tolto anche il click sull'auto-hit (bottone senza stato leggibile, spento su Steam-0). 1/10 alle 11:02 (24 h dopo il giro precedente): negozio aperto al primo tentativo subito dopo i colpi. Il numero di colpi non era nel log: dall'1/10 c'è la riga `[INFO] Chaos Rift: N hit(s).` Dall'1/10 gira anche su Steam-1 e -4, arrivati a 100: primo giro alle 11:14 e 11:17, 1 e 0 colpi (selettore x1/x5 nascosto), negozio aperto al primo tentativo. **2/10:** a 100 ora anche Steam-2, -3, -5, -7. Ogni giro fa un colpo solo: subito dopo il click il bottone non è cliccabile (l'animazione del colpo) e il ciclo si ferma. Il badge resta acceso finché ci sono Moon Stone, quindi il task riparte ogni 30 minuti (`BadgeCooldown`) e le pietre si spendono comunque tutte: Steam-0 (selettore bulk) alle 11:00 e 11:30 un colpo, alle 12:00 zero, poi badge spento; sugli account senza selettore (x1) 10-18 giri al giorno da 14 s, tutti con un colpo e il negozio aperto al primo tentativo. Nessun popup rimasto aperto. **Corretto il 3/10:** prima di ogni colpo il task aspetta fino a 6 s che il bottone torni cliccabile, come `ArcaneCrystal.Hit`. Le Moon Stone si ricaricano alle 11:00. Su Steam-0 (build nuova dalle 10:25) alle 11:00 `Chaos Rift: 2 hit(s)` in un giro da 31,6 s (10 pietre a x5, prima 14 s), negozio aperto al primo tentativo; alle 11:30 il badge del boss era spento e il giro, partito dal badge `ChaosRiftSupplies`, ha fatto `0 hit(s)`. Nello stesso momento Steam-1..5 e -7, con la build vecchia, un colpo alle 11:00 e uno alle 11:30. Le altre istanze a livello 100 prendono la correzione al prossimo aggiornamento. |
| Forbidden Knowledge | `[forbiddenknowledgetask]` | 100 | on | ✅ | Schermata verificata; il bottone dell'edificio in Gilda è ipotizzato (parte sempre dal badge, che apre già la schermata). L'1/10 gira anche su Steam-1 e -4: 7 giri nella flotta, sempre 71-73 s, perché apre l'anteprima di ogni nodo delle tre tavole anche se è già al massimo. Nessun errore; non scrive cosa compra. |
| Guardian Holy Upgrade | `[guardianholyupgradetask]` | 100 | on | ✅ | Chaos Rift → Upgrades → Magic Quarters. 29/09: lasciava aperti LockedGuardian, Chaos Rift e la Gilda (li chiudeva il Watchdog); ora li chiude il task. |

### Map e Warfront

| Task | Sezione | Liv. | Default | Stato | Da verificare / note |
|---|---|:-:|:-:|:-:|---|
| Map Missions | `[mapmissionstask]` | - | on | ✅ | Le più lunghe per prime (`mission_time_order = "desc"`), dal 30/09 16:10 su tutta la flotta (prima `asc`); nessuna preferenza per tipo di missione (decisione dell'utente: il pin non dice il tipo e servirebbe una mappatura nuova). 30/09 dopo il rollout: giri da 17 a 26 s, nessun `[FAILED]`. 1/10: dal commit 7106092 il giro scrive l'ordine con le durate lette dai pin; su Steam-0 `Map missions: started 0 of 14, in order ProtectTheFishermen 01:42, TrainElfArchers 01:32, ... EnemyBorder 00:17` (squadre tutte occupate): l'ordine è quello giusto e la prima squadra libera prende la più lunga. |
| Warfront Campaign Loot | `[warfrontcampaignloottask]` | 50 | on | ✅ | Il 29/09 gira senza errori (3 s); non si è visto niente da reclamare. **3/10:** dalle 18:03 del 2/10, su tutte e 17 parte dal badge `WarfrontCampaign` ogni 6 h esatte (Steam-5: 20:17, 02:17, 08:17), clicca il claim senza `Click ignored` e legge il timer successivo a 6 h; il badge non lo rilancia dopo 30 minuti, quindi si spegne col claim. Su alcune istanze (Steam-15) il timer letto subito dopo il claim dà il giro successivo dopo 2 minuti: quello trova il claim disabilitato e legge le 6 h (5 s in più). Al reset il badge si accende anche per le missioni giornaliere e il task gira a vuoto (claim disabilitato), come previsto. Il task non scrive cosa riceve. |
| Warfront Daily Missions | `[warfrontdailymissionstask]` | 50 | on | ✅ | Battaglie reali. 30/09 al reset: 2 missioni su 10 su 16 account e 4 su Steam-0, in 26-49 s: le altre non hanno il bottone di combattimento (non disponibili per l'account), quindi il limite è del gioco. Nessun popup "Here are your rewards!" rimasto aperto in 34 battaglie. La formazione è la squadra attiva, la stessa della campagna e dell'Arena (sonda del 3/10). |
| Warfront Campaign | `[warfrontcampaigntask]` | 50 | on | ✅ | Dal 3/10 (`CAMPAIGN_PLAN.md`). Ogni 6 h legge missioni, stelle e battle power dai dati del gioco (nessuna schermata) e combatte, dalla potenza richiesta più bassa, ogni missione/difficoltà sbloccata che la battle power raggiunge, fino alla prima sconfitta (massimo 10 battaglie): pin → popup → `fightButton` → `WFBattle` → `WFBattleWon`/`WFBattleDefeat`. Una sconfitta (cfg `defeats`, `missione:difficoltà@potenza@ora`) si ritenta con il 5% di potenza in più o dopo 24 h, e intanto non si tenta niente con un margine (potenza / richiesta) non più alto (decisione dell'utente). La soglia del gioco non basta per vincere: Easy 32 persa e poi vinta a 1,88×, Easy 33 persa due volte a 1,66×. Prima delle battaglie confronta la squadra attiva con la più forte (tank nel posto 0, healer in fondo, per potenza senza crew) e conta gli eroi fuori dalle crew: solo se manca qualcosa apre la squadra (popup di una missione → "Battle formation"), toglie tutte le macchine dal mazzo e le rimette nell'ordine giusto, dà gli eroi liberi uno alla volta alla macchina con la crew più piccola e salva. Togliere una macchina le toglie la crew, quindi a ogni cambio di macchine le crew si ridistribuiscono. Se la battle power scende, `[FAILED] Campaign squad: battle power dropped`. L'Arena usa la stessa squadra. **Visto su Steam-0 il 3/10, task isolato:** giro 1, `Campaign mission 32 Easy: required 422800, power 796762, ratio 1.88 -> won, stars 42 -> 43`, poi 33 Easy `-> lost`, stop, 84 s; dopo la vittoria nessun popup rimasto aperto. Giro 2, a vuoto: `1 defeat(s) waiting (margin up to 1.66), 0 to try` (Normal 12 a 1,04× scartata), 0,1 s. Giro 3, con la sconfitta retrodatata di 25 h: 33 Easy ritentata (persa, 42,7 s) e la sconfitta riscritta. Il ramo del +5% non è stato provato a parte (è la stessa condizione). Giro con tutti i task accesi (15:18): `heroes in crews 10 of 10, crew slots 20` (gli eroi posseduti sono quelli con `id` diverso da 0 in `heroesDict`, che li elenca tutti e 42), 0,16 s. **Squadra, Steam-8 il 3/10 alle 15:34** (task isolato): `Campaign squad: Talos Damage 2138; strongest: Goliath Tank 2449, Thunderclap Damage 2253, Talos Damage 2138, FireCracker Damage 1848; heroes in crews 4 of 8`, dopo i click `0:Goliath[0], 1:Thunderclap[0], 2:Talos[0], 3:FireCracker[0]`, poi 2 eroi a testa, `battle power 51583 -> 99430`, heroes in crews 4 -> 8, nessun popup rimasto aperto (confermato anche dall'utente a schermo). Subito dopo Easy 21 vinta a 2,49× ed Easy 22 persa a 2,19×. **Flotta, primo giro (aaa5445, 15:26-16:00):** 143 stelle vinte su Steam-1..16, nessun `[FAILED]`; 12 account fermati dal tetto di 10 battaglie ancora vincendo, quindi un giro che finisce sul tetto riparte dopo 30 minuti invece che dopo 6 h. Squadre incomplete: una sola macchina su Steam-5, -8, -9, -11, due su -10 e -14, tre su -12, quattro su cinque su -7; tank non davanti su -2 e -6; un eroe fuori dalle crew su -3 e -12. |

### Character

| Task | Sezione | Liv. | Default | Stato | Da verificare / note |
|---|---|:-:|:-:|:-:|---|
| Talents | `[talentstask]` | - | on | ✅ | Parte solo sul badge TalentAvailable. |
| Path of Glory | `[pathofglorytask]` | - | on | ✅ | Traccia gratuita e Golden (se posseduta); non compra mai il pass. |
| Hall of Heroes | `[hallofheroestask]` | - | on | ✅ | Riscritto il 29/09: legge la formazione dal Party, ogni 24 h rilegge tutti gli eroi (`hero_snapshot` nel cfg) e con `EnchantPlanner` decide prima cosa comprano Void Crystal ed Ethereal Shards; prima di ogni click rilegge livello e costo dallo schermo. Gli eroi sotto la gear power del tier bloccato successivo (1300 per il tier 2, 6600 per il 3) ricevono i Void Crystal per primi, tier 1 compreso anche fuori formazione; alla soglia il bot sblocca il tier confermando il popup `GearTierUnlock` (480 meteoriti per il tier 2, 720 per il 3), solo se l'icona del costo è quella dei meteoriti. Verificato su Steam-0 il 29/09 (lettura completa, incantesimi, snapshot vecchio corretto senza click, secondo giro in 8 s) e su Steam-0..16 il 30/09: primo giro su 16 account da 6-8 eroi in 86-266 s, da 4 a 120 incantesimi per account, classe 0 vista al lavoro (es. Steam-4, Cirilo da 500 a 1.800 di power); dopo la correzione del popup, 67 tier sbloccati su 13 account, nessuno rimasto bloccato, nessun timeout né eccezione. Un tier appena sbloccato ha gli slot vuoti (`M`) finché non arrivano i pezzi dai forzieri: la lettura completa del giorno dopo li vede. **Rarità degli eroi (dal 3/10):** a ogni lettura completa (una al giorno), per ultimo perché il tab `heroRarity` nasconde i contatori di Void Crystal ed Ethereal Shards, il task apre la rarità di ogni eroe, prima quelli in formazione e poi gli altri (scelta dell'utente: Cirilo, in formazione, prima di Boris), e compra ogni passaggio che il gioco offre finché bastano i contratti. Clicca solo con il bottone cliccabile, l'icona `contracts64` e il contatore `currencyInteraction (Contract)` che copre il costo; conta l'acquisto solo se il contatore scende esattamente del costo, poi aspetta che il titolo cambi prima del passaggio dopo. Costi della wiki: Uncommon 30, Rare 70, Epic 150 (×5 agli attributi dell'eroe, ×3 a quelli di tutti e al Raining Gold già a Uncommon). Il requisito si legge nel testo: verde `#0FE607` soddisfatto, grigio `#BDBEBD` no, e col requisito mancante il bottone non è cliccabile. **Visto dal vivo su Steam-0 il 3/10:** Cirilo `Common -> Uncommon` (contratti 375 → 345) e poi Boris, fuori formazione, `Common -> Uncommon` (345 → 315); Solaine, Talia, Leo, Muriel già Uncommon e fermi a Rare per i gioielli; Burt, Benedictus, Blaze, Luana Common senza tutto l'equip uncommon: nessun acquisto. Dopo il passaggio il gioco apre per qualche secondo `menus/Achievements`, che nasconde il contatore e poi si chiude da solo: per questo l'attesa del contatore è di 15 s. Il pannello resta indietro di un attimo: subito dopo l'acquisto di Boris mostrava ancora 'Common' col bottone cliccabile e un secondo click non ha speso niente (315 → 315); ora il passaggio dopo si valuta solo a titolo cambiato (nella build della terza prova, 13:05, dove nessun eroe era acquistabile: da vedere al prossimo acquisto). Giro della rarità su 10 eroi: 22 s. **Secondo PC, 3/10 alle 13:34-13:42** (lettura completa forzata su tutte e 18): 6 righe per istanza, sempre `'Common - <eroe>', next 'Uncommon - <eroe>' for '30' (contracts64), contracts 138-221, 'Requires to be Uncommon rarity: <color=#BDBEBD>Gear Items</color>' - nothing bought`: requisito letto come mancante, nessun click, nessun `[FAILED] Hero rarity`. L'acquisto qui non si può provare finché gli eroi non hanno l'equip uncommon. |

### Scarab Game

| Task | Sezione | Liv. | Default | Stato | Da verificare / note |
|---|---|:-:|:-:|:-:|---|
| Pharaoh's Vault | `[pharaohsvaulttask]` | 60 | on | ✅ | Spin con i Noble Token gratuiti, vault, milestone. 29/09: la schermata non ha un ingresso alle milestone (dump dal vivo: `actionButtons` ha solo pharaohVault, beasts, lostInscriptions, shop; la barra del livello apre `ScarabGameLevelBonuses`). Le milestone si reclamano solo quando le apre il badge `ScarabGameMilestones`, come nei log del 26/09; il click sul path inesistente è stato tolto. |
| Scarab Game Free Token | `[scarabgamefreetokentask]` | 60 | on | ✅ | Omaggio giornaliero del tab Saldi. |
| Beasts | `[beaststask]` | 60 | on | ✅ | Badge `ScarabGameBeastRelease`, altrimenti ogni 12 h. Rilascia la bestia se il `releaseButton` della tavoletta è attivo (sei sigilli), poi spende le Soul Embers in livelli, uno per bestia a giro, solo con icona `soulEmber64` e conferma dal contatore; mai la rarità (Cobra Keys). La lista ha tutte e 30 le bestie e si riordina dopo un livello (la bestia appena rilasciata passa dall'indice 29 allo 0), quindi ogni giro cerca di nuovo quelle possedute (bottone Upgrade visibile); a fine giro logga anche il contatore del gioco (`beastNum`, "1/30"). 30/09: livelli a 10 Soul Embers l'uno su Steam-0, 1, 2, 3, 4, 5, 6; rilascio visto su Steam-3 e Steam-6 (nessun popup da chiudere: `releaseTheBeastPopup/closeButton`, dal dump, resta nascosto). Corretti lo stesso giorno: il primo giro selezionava anche le 29 non possedute a ogni livello (439 s su Steam-0, ora 20-50 s), e gli indici salvati all'inizio puntavano altrove dopo il riordino (Steam-3: un solo livello con 24 Soul Embers, poi altri 2 col fix). |

### Events

| Task | Sezione | Liv. | Default | Stato | Da verificare / note |
|---|---|:-:|:-:|:-:|---|
| Decorated Heroes | `[decoratedheroeseventtask]` | - | on | ✅ | Acquisti nell'ordine Dragon blood → Meteorite → Beer (29/09: ordine confermato nel log; Meteorite non c'è nello shop di questo account, valuta insufficiente per il resto). 29/09: 3 sfide reclamate al giro partito dal badge. Dal 30/09 dopo i claim legge le 8 carte e scrive una riga per carta (`Event 'Decorated heroes': 'Hit the arcane crystal 10 times.' 5/10 -> CrystalHits, 5 missing.`, oppure `challenge not handled: ...` per quelle che completano gli altri task); verificato su Steam-0. Poi fa quello che manca al livello in corso delle sfide azionabili, né più né meno, riapre, reclama e ripete (al massimo 4 giri, uno per livello); colpi al cristallo e giocate in taverna solo dopo che la quest Miner o Gamer di oggi è fatta, e contano per entrambe. 30/09 su Steam-0: colpi 5/10 → 5 colpi, claim, "15 times" 10/15 → 5 colpi, claim, 15/15; giocate 10/12 → 2, claim, 12/12; 3 claim in 66 s. Una lettura rimasta indietro rispetto ai passi appena fatti non viene rifatta (`shows N, M expected - left for the next run`). Illuminazione dei guardian (Vermilion, 20 Strange Dust l'una, solo con l'icona `strangeDust64`; approvata dall'utente il 30/09): 30/09 su Steam-0 "Enlighten guardians 1 times." 0/1 → 1, claim, 1/2 → 1, claim, 2/3 → 1, claim, 3/3 (60 dust, 79 s); il bottone non cambia testo né costo dopo il click, la conferma è la carta riletta. **Flotta, 30/09 16:13-16:20**: su Steam-1..16 lo stesso giro (3 illuminazioni, 10 colpi, 2 giocate, 5-8 claim, 94-108 s), nessun `[FAILED]` né timeout; Steam-11 ha fatto colpi e illuminazioni ma le giocate aspettano la sua quest Gamer (`waits for today's GamerQuestTask`); su Steam-15 la terza illuminazione ha trovato il bottone non cliccabile ed è riuscita al giro dopo (ora il log scrive `the button isn't clickable`). Sotto il livello 120 la carta dell'alchimia ha il titolo nascosto: saltata con un controllo silenzioso (prima 2 `[FAILED]` a sessione su 16 istanze), si leggono 7 carte. Le stesse azioni valgono per i mini-eventi, con in più forzieri (`CollectorQuestTask.OpenChests`, dal più economico, dopo la quest Collector), vendite (`MerchantQuestTask.Sell`, solo i tre oggetti ammessi, dopo la quest Merchant), Tree of Life (`TreeOfLifeTask.Buy`) e ricerche di meteoriti (`MeteoriteResearchTask.Research`, senza riserva; **vista dal vivo** su Steam-1 il 30/09: "Complete 1 meteorite researches." 0/1 → 'All main attributes' per 600, saldo 2.734 → 2.134, claim), un upgrade dell'Exotic Merchant (`MerchantQuestTask.Upgrade`: il più economico con icona `exoticCoin64`, ignorando la priorità, e se le monete non bastano vende prima gli oggetti ammessi fino a coprirlo, al massimo 200; dopo la quest Merchant) e la donazione in Gilda (Gilda → `bank` → `popups/GuildBank`, solo quando una sfida la chiede: 1.000 guild coin, il minimo del gioco, anche per una sfida da 500, scelta dell'utente; sotto 1.000 monete "Max" solo se copre quello che manca): forzieri, vendite, Tree of Life, upgrade esotico e donazione **mai visti dal vivo**, compaiono solo nei mini-eventi (Stardust dal 04/10). "Get 50 special upgrades" non ha azione: li compra Hero Upgrade. Vedi EVENTS_PLAN.md. **1/10, primo giro dopo il reset:** su 16 istanze (Steam-2 era ferma in Arena) le stesse sfide del 30/09, ogni livello esatto (es. Steam-0: colpi 6/10 → 4, giocate 10/12 → 2, 3 illuminazioni; Steam-13 e -16 da 0: 15 colpi, 12 giocate, 3 illuminazioni, 167 s); su Steam-11 le giocate aspettano la quest Gamer ferma a 3/10 (`waits for today's GamerQuestTask`). Ogni tanto il tab dello shop non si apre dopo i claim e i 16 oggetti si leggono vuoti (`16 item(s) scanned: , , ,`: Steam-1 e -5 un giro ciascuno, Steam-0 alle 11:00): nessun acquisto in quel giro, si riprova un'ora dopo. **Secondo PC, 1/10:** la carta è bloccata sotto il livello 50 (16 istanze su 18: `Match 'Decorated heroes' ... is locked for this account`, poi `Not open to this account`, nessun errore). Steam-18, appena arrivato a 50, ha carte più piccole: "Hit the arcane crystal 5 times", "Play 4/8/12 times", "Enlighten guardians 1/2/3 times"; 3 giri, 3 illuminazioni, 12 giocate e 6 claim in 2 minuti, mentre i colpi aspettano la quest Miner (`0/5 -> 0/5` con 0 picconi). Anche i mini-eventi sono bloccati per livello su tutte e 18. **Dal 2/10 alle 10:00 il tab Challenges non c'è più** (`submenuButtons/decoratedHeroesChallenges` nascosto su tutte e 17; prima del reset le sfide erano tutte complete): è finita la fase delle sfide dell'evento. Il task reclama 0, non legge carte e passa allo shop, senza errori. |
| New Player Event | `[newplayereventtask]` | - | on | ✅ | Stesso ordine: compra Meteorite, poi spende il resto in Beer; check-in giornaliero e milestone. Gli account vecchi non hanno l'evento (il task rallenta da solo), come Steam-0. 30/09 dopo il reset, su Steam-8..16: check-in reclamato, 1 milestone su 14, 1 Meteorite comprato. 1/10, secondo PC: l'evento è aperto su tutte e 18 le istanze; nella giornata un check-in a testa, 4-6 milestone e 36 acquisti di Meteorite in tutto, nessun `[FAILED]`. Non scrive niente a livello INFO: si vede solo dalle righe `[DEBUG] [AnniversaryShop]`. |
| Mini Event | `[minieventtask]` | - | on | ⚠️ | 30/09 sulla flotta: aperto Sigils su 15 istanze (bloccato per livello su Steam-5 e -16, `Not open to this account`), sfide lette: su Steam-1 "Complete 1 meteorite researches." fatta e reclamata, su Steam-11 "Kill 100 enemies with the hero: Leo." (nessuna azione, per scelta). Dal 30/09 un solo task per gli 11 mini-eventi (sostituisce Mass Production e Sigils of Prophecy): apre la prima carta non bloccata con uno degli 11 nomi del wiki (tutti aprono `events/MiniEvents`), reclama ogni giorno sbloccato nel tab Challenges e scrive una riga per ogni sfida sbloccata non ancora reclamata. Solo il tab Challenges. Verificato su Steam-0 con Sigils of Prophecy (claim, lettura, chiusura, 5 s). **Da verificare:** il primo mini-evento diverso da Sigils, Stardust dal 04/10 alle 10:00 (la carta c'è già tra gli "Upcoming events", col lucchetto). Storia di Sigils: 29/09 1 claim su 3 giorni; 30/09 claim del secondo giorno su 9 istanze; su Steam-15 e -16 la carta è bloccata per livello e ora aspetta un'ora come un evento assente. |

Accelerazioni (Experiments, Firestone Research, Map Missions): il bot preme `speedUpButton` solo nella
finestra gratuita degli ultimi minuti (`free_speedup_seconds`). Visto dal vivo il 30/09 su 7 click: `finishDesc`
nascosto, `costText 'Free'`, icona delle gemme (`gem64`) nascosta, e ogni click scrive una riga `speed-up shows ...`.
Dal 30/09 il click richiede anche l'icona delle gemme nascosta (`SpeedUpButton.IsFree`), che non dipende dalla lingua.

Un evento non in corso per l'account, bloccato per lui o ancora in arrivo scrive `Not open to this account (not listed,
locked or upcoming)`: non è un errore.

Dal 29/09 gli eventi partono anche sul badge rosso del bottone Events
(`rightSideUI/menuButtons/eventsButton/notification`, visto dal vivo), oltre al controllo orario. Il
badge è uno solo per tutti gli eventi, quindi quando si accende girano tutti, al massimo uno ogni 30
minuti (`BadgeCooldown`). Verificato su Steam-0: Decorated Heroes è partito alle 17:19 invece che
alle 17:36 e ha reclamato 3 sfide; dopo il giro dei quattro eventi il badge si è spento. Costo: circa
7 s ciascuno per gli eventi non in corso.

### Azioni di background (girano in continuo, non nello scheduler)

| Azione | Sezione | Default | Stato | Da verificare / note |
|---|---|:-:|:-:|---|
| Hero Upgrade | `[hero_upgrade]` | on | ✅ | Tiene premuto 0,5 s ogni bottone di livello pagabile, in modalità x100/MAX. Dall'1/10 un passaggio ogni 15 s invece di 5 (default e template): in MAX una pressione compra già tutto quello che si può, quindi un terzo delle pressioni. |
| AutoRetreat | `[auto_retreat]` | on | ✅ | Per provarlo in fretta: `stall_minutes = 1` su uno stage duro. |
| Flying Bonus Hunter | `[flying_bonus_hunter]` | on | ✅ | Bersagli trovati dal vivo su Steam-0. 29/09: i 4 bottoni sono sempre attivi e cliccabili, e il bot li cliccava tutti a ogni giro (~4 s di click ogni ~6 s), con un'eccezione del gioco sugli hunter fermi. In volo è attivo solo il loro figlio (`hunter`, `dragon`, `femaleDragon`): ora clicca solo allora, e ogni click scrive `[FlyingBonusHunter] Clicking ...`. Dopo il click non compare nessun popup. 1/10: il bonus restava in volo e veniva cliccato 1, 2 o 3 volte (1.242 / 497 / 1.038 voli su Steam-0..16, ~315 click per istanza in 12,5 h). Un dump dal vivo su Steam-0 prima e dopo il click mostra che il primo click sgancia il carico: `dragon/beerParent/beerDrop` e `hunter/bagParent/bagDrop` spariscono dal portatore, che poi attraversa lo schermo vuoto. Ora un solo click per volo (commit 1df960b). |

Globali: `low_resource_mode` ✅ (18/09) e griglia delle finestre ✅ (26/09, 18 istanze su
2560x1440). CPU, misurata l'1/10: ogni istanza usa il 12-16% di un core, quasi tutto per disegnare (i task girano circa l'8% del tempo); in tutto il 30% della macchina (8 core logici). Steam-0 a `target_frame_rate = 10` è sceso dal 12,5% al 7,9% di un core (le altre, a 15, al 13,5% nello stesso intervallo); in 35 minuti a 10 fps nessun errore, task con le durate di prima, rapporto di Empower a 2 h in linea con le avventure precedenti (0,33 contro 0,35 e 0,32), stesso muro degli stage (626). Dalle 16:41-16:47 dell'1/10 tutta la flotta è a 10 (build 1df960b, con anche a7c65ec del secondo PC). RAM, misurata l'1/10 alle 17:40: 32 GB fisici all'84%, commit 54 GB su 88 (il pagefile da 56 GB lascia 34 GB di margine). Per istanza: Firestone 1,5 GB di commit (texture ~400 MB, Unity ~300, .NET/IL2CPP ~160, il resto codice, driver e MelonLoader), Steam 0,9 GB (steam.exe e 6 steamwebhelper, quasi tutto nei 2 renderer). Provato senza risultato: `Resources.UnloadUnusedAssets` (-14 MB), `globalTextureMipmapLimit = 2` (0: sprite 2D senza mipmap), le opzioni di Steam `-nofriendsui -nochatui -cef-disable-gpu` (883 MB contro 889 del controllo, stessi 7 processi), e chiudere Steam a gioco avviato (Firestone si chiude con lui: Steam-16, 18:03). NoSteamWebHelper era già stato escluso il 30/09. Resta FirebotSteamTrim (attività pianificata, `tools/SteamTrim`), che sopra il 90% di RAM svuota il working set dei steamwebhelper: il 1/10 alle 16:51, dopo il riavvio della flotta, RAM al 99% e 9,4 GB recuperati. Oltre questo serve altra RAM fisica. **3/10, dopo 16 h di sessione:** commit 75 GB su 88, Firestone da 1,5 GB appena avviato a 2,6-3,0 GB per istanza; in 48 minuti (10:20-11:08) le 16 istanze non riavviate sono cambiate di -50 MB in tutto, quindi la crescita si ferma lì e il margine (13-14 GB) regge.

## Problemi noti

- **Forzieri Iron mai aperti sugli account a livello 30-59 (secondo PC, corretto il 3/10, da
  verificare al reset del 4/10).** Dal 29/09 su tutte e 18 le istanze `'/Iron': neither button
  available` tre volte, poi `giving up ... opened 0/N`: ~60 s persi a ogni tentativo, due per giro
  (quest ed extra). Al reset del 3/10 Collector è andato in timeout (120 s) su 15 istanze su 18, pur
  chiudendo la quest a 4/4 con Wooden e Common, e gli extra restavano a 0/6. Il prefab di
  `ChestOpenPreview` ha `bg/chestRequirementBg/text`, inattivo di default: si presume che mostri un
  requisito al posto dei bottoni (sul PC principale gli Iron si aprono). Ora, se è visibile e x1 no,
  l'apertura scrive `[INFO] [ChestOpening] '/Iron': can't be opened yet ('<testo>')` e passa allo
  slot dopo. Se la riga non compare e il `giving up` resta, l'ipotesi è sbagliata.
- **Apertura dei forzieri ferma sul popup "New items" (corretto l'1/10).** Un forziere che dà un
  oggetto mai avuto (un gioiello, dai Golden) apre `popups/NewItem` sopra la schermata dei risultati,
  che nasconde i suoi bottoni finché il popup resta aperto. `ChestOpening.OpenDownTo` aspettava i
  bottoni 7,5 s, poi si arrendeva, e il Watchdog chiudeva il popup a task finito. Al primo giro
  dell'Emblem market (21:42-21:48) 9 aperture su 15 si sono fermate a 10-23 di 15-35 Golden (Steam-1
  16/35, Steam-8 10/15). Ora il popup si chiude dentro l'apertura (`closing the New items popup`) e
  si aspettano fino a 15 s, perché un x10 di Golden ci ha messo ~8 s. In più, a fine apertura si
  aspetta che la schermata dei risultati si possa chiudere: prima restava aperta e la chiudeva il
  Watchdog (anche dopo Collector). I Golden comprati e rimasti chiusi sono stati aperti a mano su
  Steam-1..11 con una build temporanea, il popup chiuso da 1 a 6 volte per istanza (Steam-2 18/18,
  Steam-1 19/19). Su Steam-3 restano 2 Golden comprati e non aperti (10/12: il x10 è durato più dei
  7,5 s di allora). **Verificato il 2/10:** 3 aperture di 5 Golden dell'Emblem market e tutte le
  aperture di Collector al reset, col popup chiuso da 1 a 3 volte, arrivate al conteggio pieno.
  Da vedere al prossimo x10: che l'attesa di 15 s basti. Dopo un'apertura il gioco
  apre a volte `menus/UnlockWarMachine`, anche dopo Collector: lo chiude il Watchdog.
- **Steam-12: il cristallo arcano non accetta i colpi (2/10).** Dal reset nessun colpo è andato a
  segno: 6 giri, 16 click, picconi fermi a 92 (poi 97) e quest Miner a 0/5. La finestra catturata
  alle 17:05 mostra il cristallo a livello 2 (19.887/23.175 HP): il bottone Hit diventa grigio dopo
  il click e torna verde, ma né i picconi né gli HP cambiano, mentre un altro membro della gilda
  colpisce (2.683 → 2.774 di danno). Il bot se ne accorge dal contatore e non spreca niente. Le
  altre 15 istanze con picconi hanno colpito al primo click; su Steam-12 andava il 30/09. Sembra lo
  stato della sessione di gioco (avviata l'1/10 alle 22:07). Dopo il riavvio delle 18:07 la quest
  risultava già `5/5` e Quests l'ha reclamata: il server aveva probabilmente contato i colpi che il
  gioco non mostrava. Se ricapita, un riavvio dell'istanza basta. **3/10:** stesso effetto per un
  minuto subito dopo il reset (10:01:40-10:01:55) su 4 istanze: i click non cambiano il contatore,
  ma il server li prende e un'ora dopo la quest li mostra (vedi Miner). Il bot riclicca al massimo 3
  volte per colpo, quindi lo spreco è di qualche piccone (uno su Steam-14). Senza riavvii.
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
  task. Dal 30/09 il default e il template sono a 60 (scelta dell'utente); il fleet lo prende al
  prossimo riavvio completo, con `apply_template.ps1` a giochi chiusi (vedi "Da fare e rimandato").
  Anche a 60 s, nel riavvio dell'1/10 alle 11:10 su 2 istanze su 16 (Steam-11 e -13) l'avatar era
  ancora coperto al primo task (Collector, `not on the Daily tab`, riprova dopo un'ora).
  Il vecchio "what's new" non si è visto.
- **Quest di ieri subito dopo il reset (corretto l'1/10).** Per un minuto o più dopo le 10:00 la
  schermata delle quest può mostrare ancora le quest di ieri, già complete. L'1/10 Collector, il
  primo a partire (10:00:03-10:00:42), ha letto `4/4 -> 4/4` su 9 istanze (Steam-0, 2, 4, 7, 10, 11,
  13, 15, 16) e si è riprogrammato al giorno dopo senza aprire niente; su Steam-13 e -16 è successo a
  tutte e quattro le quest, fino alle 10:01:09 (le hanno poi coperte in parte i colpi e le giocate di
  Decorated Heroes). Steam-1 alle 10:00:13 leggeva già 0/4: il ritardo cambia da istanza a istanza.
  Daily Store Offers e Warfront Daily Missions non ne soffrono, riprovano da soli. Ora una quest già
  completa alla prima lettura, nei primi 15 minuti dopo le 10:00, si rilegge dopo 5 minuti
  (`right after the reset - read again later`). **Verificato al reset del 2/10:** è scattata su
  Steam-12 e -16 (Collector alle 10:00:06), rilette alle 10:06 con `0/4 -> 4/4`; nessuna quest
  saltata sulle 17 istanze. Sul secondo PC, ancora
  con la versione vecchia al reset dell'1/10, Collector ha letto `4/4 -> 4/4` alle 10:00 su 15
  istanze su 18 e Gamer `10/10 -> 10/10` su Steam-22 (non solo Collector, quindi): rimesse in coda
  alle 14:44 con lo script di `MULTI_INSTANCE_SETUP.md`, alle 14:53-14:55 Collector `0/4 -> 4/4` su
  tutte e 15 e Gamer `0/10 -> 10/10` su Steam-22. **Verificato al reset del 2/10 sul secondo PC**
  (build 5c0bbab): su Steam-17..25 Collector alle 10:00 leggeva ancora `4/4` (e Gamer `10/10` su
  Steam-19..25), `right after the reset - read again later`, poi alle 10:05-10:07 `0/4 -> 4/4` e
  `0/10 -> 10/10` su tutte e 9; su Steam-26..34 la prima lettura delle 10:01 era già quella giusta.
  Nessuna quest saltata su 18 istanze.
- **Countdown vuoto dopo l'avvio di una ricerca (corretto l'1/10).** Sul secondo PC, dove le
  ricerche durano pochi minuti e Firestone Research riparte ogni 2-30 minuti, il task è andato in
  eccezione 3 volte nella giornata su 54 ricerche avviate (Steam-30, -33, -34: speed-up gratuito,
  `Selected research #3`, 2 s dopo `ArgumentOutOfRangeException - ... un-representable DateTime`):
  lo slot appena avviato non mostrava ancora il countdown, il testo vuoto dava `DateTime.MinValue` e
  togliere la finestra gratuita da quel valore lancia l'eccezione. Costava un nuovo tentativo dopo
  2 minuti. Lo stesso calcolo c'era in Experiments e Map Missions: ora passano tutti da
  `SpeedUpButton.FreeFrom`, e uno slot senza countdown viene saltato. Su Steam-30 alle 15:08 la stessa
  sequenza (speed-up gratuito, `Selected research #3`) è finita normalmente con `Next` letto
  dall'altro slot; con una frequenza di 1 su 18 non è ancora una prova. Dal 1/10 15:00 al 2/10 23:29:
  Steam-30 (con la correzione) 41 ricerche, nessuna eccezione; le altre 17 (senza) ~720 ricerche,
  un'eccezione (Steam-22 alle 19:30). Dal 3/10 alle 02:11 tutte e 18 hanno la correzione.
- Hall of Heroes, sugli account a livello 30-59: `Ethereal Shards balance '' isn't a number` due volte
  al giorno su tutte le istanze del secondo PC. Il contatore è nascosto (Jewels non ancora sbloccati),
  il task salta gli incantesimi Jewels come deve; è solo una riga `[FAILED]` di troppo.
- Steam-16 si è chiuso alle 10:48 dell'1/10 senza errori nel log del bot né eventi di crash in
  Windows (ultima riga un click di Flying Bonus Hunter), ed è ripartito alle 10:53.
- Il `storeButton` dell'HUD non apre niente (zero listener): Daily Store Offers dipende dai suoi badge.
- Percorsi ipotizzati, mai visti dal vivo: badge della posta (l'oggetto esiste, mai visto acceso),
  bottone Forbidden Knowledge in Gilda.
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

- **Riavvio completo di Steam-0..16: fatto il 30/09 alle 16:10.** Tutte e 17 girano con la
  stessa build (d19bb02: sfide degli eventi, Mini Event, e le correzioni del 30/09 che prima erano
  solo su alcune istanze) e lo stesso cfg (`apply_template.ps1 -From 0 -To 16`: 63 valori, cioè
  `start_bot_delay` 30 → 60 su Steam-2..16 e `mission_time_order` asc → desc su tutte). I cfg di
  prima sono in `C:\Repos\FirestoneBot-test-backup\2026-09-30-fleet`. Le sezioni
  `[massproductioneventtask]` e `[sigilsofprophecyeventtask]` restano nei cfg di Steam-1..16 senza
  un task: non fanno niente. Non passare l'uscita di `apply_template.ps1` a `Select-Object -First`: in
  PowerShell 5.1 ferma lo script dopo quelle righe e i cfg restanti non vengono scritti (30/09).
- **1/10 alle 11:10: build 205de34 su Steam-0..16** (quest rilette dopo il reset, uscita da Arena
  con gli avversari nascosti, colpi di Chaos Rift nel log), con lo stesso cfg (`-Check`: 0 valori
  da cambiare). Prima del riavvio sono stati svuotati `last_done_date` e `next_run_time_internal`
  delle quest saltate la mattina (Collector su Steam-2, 4, 7, 10, 11, 15; tutte e quattro su
  Steam-13 e -16; Collector su Steam-0 già alle 11:05). I cfg di prima sono in
  `C:\Repos\FirestoneBot-test-backup\2026-10-01-fleet`. Avvio 11:11-11:17, 16 su 16 `Started.
  Enabled tasks: 38 of 40`, nessun timeout né eccezione. Quest recuperate: Collector `0/4 -> 4/4`
  su Steam-2, 4, 7, 10, 15, 16 (e Steam-0), Merchant 10/10 su Steam-16 e 7/10 su Steam-13 (oggetti
  finiti); Gamer e Miner su 13 e 16 erano già completi (colpi e giocate di Decorated Heroes). Su
  Steam-11 e -13 Collector era il primo task dopo l'avvio e ha letto `not on the Daily tab` (popup
  d'avvio, vedi "Problemi noti"): riprova da solo un'ora dopo. Steam-2, con l'arena riaperta, ha
  letto di nuovo gli avversari (`24425 / 25876 / 23751`) e ha ripreso coi 3 gettoni rimasti. Il
  secondo PC va aggiornato con la stessa procedura: senza, al reset del 2/10 le stesse quest saltano
  anche lì.
- **1/10 alle 12:06: build 14244ce su Steam-0..16** (riserva di vendita del Merchant). Provata
  prima su Steam-3 (12:04), poi distribuita sulle altre con `next_run_time_internal` di Merchant
  svuotato dove la quest era incompleta. 16 su 16 `Started.`, nessun timeout né eccezione; Merchant
  a 10/10 su Steam-1, 8, 9, 13, 15 (`sold 0/N` dai tre, poi `sold N/N` dalla riserva). Steam-4
  l'aveva già chiusa alle 12:01 con 2 Scroll of Damage arrivati dalle missioni. Oggi tutte e 17 le
  quest Merchant sono complete. Alle 12:33 Steam-0 è passata a 7106092, che aggiunge solo la riga
  di log dell'ordine di Map Missions; Steam-1..16 restano su 14244ce fino al prossimo riavvio.
- **2/10: build 9b43494 (Free Pickaxes) su Steam-0..16.** Steam-0 alle 16:17, Steam-13 alle 17:10,
  le altre 15 avviate in sequenza alle 18:03-18:08: 17 su 17 `Started. Enabled tasks: 39 of 41`,
  nessun timeout né eccezione, `apply_template.ps1 -Check` a 0 valori da cambiare. Backup di cfg e
  DLL di prima in `C:\Repos\FirestoneBot-test-backup\2026-10-02-freepickaxes`. Su Steam-12 è stato
  svuotato `next_run_time_internal` del Miner per rileggerlo subito: dopo il riavvio la quest era
  già `5/5` (alle 17:05 `0/5`, nessun colpo del bot da allora) e Quests l'ha reclamata, quindi
  probabilmente il server contava i colpi che quella sessione non mostrava. Il secondo PC è stato
  aggiornato col template ancora a 15 fps (1/10, 14:44; i 10 fps sono di 5000fcf, 16:54): finché
  resta a 15 non dovrebbe avere il problema, ma la correzione va portata prima di passarlo a 10.
  **Il 3/10 alle 02:04 il secondo PC è passato a 10 fps con 094b4f6, che non ha la correzione:** va
  aggiornato a una build da 9b43494 in su (pushata il 3/10). Riguarda solo gli account a livello 50
  o più; da controllare lì `Free pickaxes: 0 ('')` e `Watchdog] Closing popup: .*GuildShop`.
- **3/10: Steam-0 sulla build con la correzione di Chaos Rift (6810f6b) e la rarità degli eroi**
  (Steam-0 riavviata più volte tra le 10:25 e le 13:01 per le prove; backup di cfg e DLL di prima in
  `C:\Repos\FirestoneBot-test-backup\2026-10-03-chaosrift`). Steam-1..16 e il secondo PC restano
  sulle build di prima fino al prossimo aggiornamento (procedura di `MULTI_INSTANCE_SETUP.md`, nessuna
  chiave nuova nei cfg).
- **Secondo PC (istanze 17-34)**: basta la procedura "Aggiornare il bot su istanze già in
  funzione" di `MULTI_INSTANCE_SETUP.md`, aggiornata l'1/10. Porta tutto quello del 30/09 (Hall of
  Heroes, i tre task nuovi, avvio a 60 s, sfide degli eventi) e dell'1/10 (quest rilette dopo il
  reset, uscita da Arena, riserva del Merchant). Nessuna chiave nuova nei cfg; c'è lo script per
  rimettere in coda le quest saltate se si aggiorna dopo le 10:00, e i controlli nel punto 11.
  **Fatto l'1/10 alle 14:44** (build 5c0bbab, prima 30/09 23:39): `apply_template.ps1 -From 17 -To
  34` ha cambiato solo i due valori di Empower (72 su 36 file), quest rimesse in coda, avvio in
  sequenza 14:45-14:52, 18 su 18 `Started. Enabled tasks: 38 of 40`, `-Check` a 0, nessun timeout
  né eccezione. I cfg e i log di prima sono in `C:\Repos\FirestoneBot-test-backup\2026-10-01-fleet`
  su quel PC. Sandbox in `C:\Sandbox\dmari` e nessun `Mods` dentro i sandbox: le DLL vanno solo nel
  percorso reale. Alle 15:00 Steam-30 è passata alla build con la correzione di Firestone Research
  (vedi "Problemi noti"); le altre 17 la prendono al prossimo aggiornamento. **3/10 alle 02:04:
  build 094b4f6 su Steam-17..34** (Emblem market, popup "New items", Meteorite Research, un click
  per volo, 10 fps). `apply_template.ps1`: 72 valori (`target_frame_rate` 15 → 10 e
  `sweep_interval_seconds` 5 → 15, rimasti indietro dall'1/10), quest non rimesse in coda (la build
  vecchia aveva già la rilettura e mancavano 8 ore al reset). Avvio 02:04-02:11, 18 su 18
  `Started. Enabled tasks: 39 of 41` e `targetFrameRate=10`, `-Check` a 0, nessun timeout né
  eccezione fino alle 02:51. Emblem market non parte (nessun account a 65). Flying Bonus Hunter: un
  click per volo (le coppie a pochi secondi sono due portatori diversi, drago e hunter). CPU alle
  02:50: 12,9% di un core per istanza (7-15), non confrontabile con il PC principale (32 thread
  logici, nessuna misura a 15 fps su questo PC). RAM all'87%, commit 67 GB su 88. Cfg di prima in
  `C:\Repos\FirestoneBot-test-backup\2026-10-03-fleet`. **3/10 alle 13:29: build a13bdfd più la
  correzione dei forzieri Iron su Steam-17..34** (Free Pickaxes, Chaos Rift, rarità degli eroi).
  `-Check` a 0 prima e dopo, quest non rimesse in coda (094b4f6 ha già la rilettura), lettura
  completa di Hall of Heroes rimessa in coda per vedere la rarità. Avvio 13:29-13:37, 18 su 18
  `Started. Enabled tasks: 39 of 41`, nessun timeout né eccezione fino alle 13:45. Cfg, log e DLL
  di prima in `C:\Repos\FirestoneBot-test-backup\2026-10-03-heroRarity`.
- **Default F2P sul fleet (da decidere)**: i default della Fase 0 nel codice (Personal Tree al posto
  delle War Machines, Empower solo a +100%, task Oracle accesi) valgono solo per i file nuovi. Il
  template e il fleet di questo PC sono allineati a Steam-0 (vedi "Configurazione di
  riferimento"); le missioni `desc` sono passate al fleet il 30/09 ed Empower a +100% l'1/10
  (scelta dell'utente, con tetto a 6 h). Restano War Machines e task Oracle. Se si
  passa ai default F2P: si cambiano Steam-0, il template e quella sezione, poi si ripassa
  `apply_template.ps1` su tutte le istanze.
- **Rituali Oracle**: ce ne sono quattro (Obedience: forzieri solar; Harmony: comet; Concentration:
  oracle's gift ed emblemi; Serenity: lunar), da 40 minuti, uno alla volta, reset ogni 6 ore. Oggi
  parte il primo in ordine di griglia. A cadenza h24 dovrebbero comunque partire tutti dentro la
  finestra: quando un account arriva a 200, verificare prima di tutto che il task ne riavvii uno a
  ogni completamento. Solo se non lo fa serve la scelta per nome, che richiede un dump UnityPy della
  griglia (oggi nessun testo col nome del rituale è mappato).
- Rimandati perché a cadenza h24 rendono poco: modalità "Next Milestone" di Hero Upgrade, upgrade
  globali/speciali (`upgradesButtonUI`, mai mappato), spedizione con più punti, missione del drago
  prioritaria (`MissionPin` non ha il tipo di missione).
- **Eventi non ancora gestiti** (piano in `EVENTS_PLAN.md`, con date e procedura): eventi di calendario
  (Halloween dal 23/10, poi Winter Festival), Frostfire Festival (03/12), anniversario (aprile), tab
  Medals di Decorated Heroes (novembre). Si fanno quando se ne vede la schermata dal vivo.
- **Campagna del Warfront, crew**: le crew si toccano solo per gli eroi liberi o a un cambio di
  macchine (allora si ridistribuiscono, uno alla volta alla crew più piccola). Crew bilanciate o
  piene davanti: domanda dell'utente del 3/10, da decidere coi numeri del log (sezione 7 di
  `CAMPAIGN_PLAN.md` per la crew calcolata dai bonus degli eroi).
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
- Emblem market: un forziere comprato diverso dal più raro mostrato nella sua categoria (mai uno più
  economico al suo posto, regola dell'utente), un acquisto non pagato in emblemi, o una riga
  `[FAILED] Emblem market` (click senza il calo esatto del contatore).

## Tornare indietro

Il tag `pre-campaign-2026-10-03` segna il codice prima del task della campagna del Warfront
(`CAMPAIGN_PLAN.md`), con già la rarità degli eroi e le correzioni di Chaos Rift e Free Pickaxes
(commit `a13bdfd`; cfg e DLL di Steam-0 di quel momento in
`C:\Repos\FirestoneBot-test-backup\2026-10-03-pre-campaign`). Il tag `pre-hall-of-heroes-2026-09-29`
segna il codice prima della riscrittura di Hall of Heroes (commit `6a75e0a`). Il tag `pre-cleanup-2026-09-28` segna il codice prima della pulizia (commit `a27ecf0`, già con il
riallineamento alla guida F2P). Il commit precedente al riallineamento è `10eebca`.

```powershell
cd C:\Repos\FirestoneBot
git switch --detach pre-cleanup-2026-09-28     # oppure 10eebca
# build e deploy come sopra, sull'istanza che serve (a gioco chiuso)
git switch main                                # per tornare all'ultima versione
```

Per annullare una sola parte della pulizia senza tornare indietro su tutto: `git revert <commit>`,
dove i commit sono quelli della serie successiva al tag (`git log --oneline pre-cleanup-2026-09-28..main`).
