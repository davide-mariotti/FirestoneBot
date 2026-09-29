# Test dal vivo

Runbook per una sessione di test **autonoma** di Claude Code sul PC della flotta, sull'istanza
Steam-0: prova tutti i task, trova i bug dai log, li corregge, ricompila, ridistribuisce e riprova
da solo. In fondo: stato di ogni task, problemi noti, lavoro rimandato e come tornare alla versione
precedente. Aggiornato al 2026-09-28, dopo il riallineamento alla guida F2P
(`docs/firestone_guida_F2P.md`) e la pulizia del codice.

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
   `start_bot_delay = 10.0`. Nelle sezioni dei task: `enabled` come in
   `tools/ConfigTemplate/FirebotPreferences.template.cfg` (tutto acceso tranne `hallofheroestask` e
   `warmachinestask`), e `next_run_time_internal = ""` ovunque, così ogni task è subito dovuto.
4. Build, deploy, avvio.

### Fase 1: giro completo

Lascia girare il bot finché ogni task acceso ha avuto il suo turno: uno alla volta, può volerci
un'ora. Man mano, per ogni task, annota l'esito: finito senza errori, `[FAILED]`, mai partito
(livello), oppure comportamento diverso da quello scritto nella colonna "Da verificare" delle
tabelle sotto. Controlla anche i punti di "Da riverificare dopo la pulizia".

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

## Stato per task

Legenda: ✅ verificato dal vivo · ⚠️ verificato in parte · ❌ mai girato dal vivo ·
🔄 logica cambiata il 2026-09-28, da riverificare.

"Liv." è il livello personaggio sotto il quale il task non parte mai. "Default" è `enabled` in un
file di configurazione nuovo.

### Quests

| Task | Sezione | Liv. | Default | Stato | Da verificare / note |
|---|---|:-:|:-:|:-:|---|
| Quests | `[queststask]` | - | on | ✅ | Claim di giornaliere e settimanali. Clicca anche i claim disabilitati: nei log del 26-29/09 ci sono 288 `Click ignored` sui `claimButton`, 1 s ciascuno (vedi "Problemi noti"). |
| Beer Exchange | `[beerexchangetask]` | 15 | on | ✅ | Usa solo l'offerta pagata in birra, mai le due in gemme. 29/09: senza badge non apriva mai il mercato (lo `stormyButton` della Taverna non lo apre); ora entra da `actionButtons/shop` ("Market"), verificato dal vivo. |
| Collector | `[collectorquesttask]` | - | on | 🔄 | Apre solo chest Wooden/Iron/Common e al massimo 4 al giorno; Uncommon e superiori non si toccano. Jewel e celestial sempre tutte. Da confermare: che Wooden e Iron valgano davvero meno di Common (se no cambia solo quale chest economica usa). |
| Gamer | `[gamerquesttask]` | 15 | on | ✅ | 10 giocate in taverna, senza scendere sotto `min_token_reserve`. |
| Merchant | `[merchantquesttask]` | 30 | on | 🔄 | Vende solo le posizioni 3, 2, 1 della griglia (Midas, Health, Damage), mai Speed né i consumabili di gold e meteoriti. Controlla la riga `Sell grid:` del log: deve elencare quei nomi in quelle posizioni. |
| Miner | `[minerquesttask]` | 50 | on | ✅ | 5 colpi al Cristallo Arcano. La scorciatoia x5 non è verificata; se manca, fa 5 colpi singoli. |

### Town

| Task | Sezione | Liv. | Default | Stato | Da verificare / note |
|---|---|:-:|:-:|:-:|---|
| Daily Store Offers | `[dailystoreofferstask]` | - | on | ✅ | Check-in giornaliero e le due mystery box gratuite. Entra solo dai badge CheckIn/MysteryBox: il `storeButton` dell'HUD non apre niente. |
| Engineer | `[engineertask]` | 50 | on | ✅ | Strumenti ogni 6 ore. Verificato dal badge; la strada dall'edificio (popup GarageSelection) è corretta ma non riverificata. |
| War Machines | `[warmachinestask]` | 50 | **off** | ⚠️ | Navigazione e stop sul popup CurrencyMissing verificati, un livellamento completo no. Spento: consuma gli stessi gettoni spedizione del Personal Tree, che la guida mette prima. |
| Guardian Training | `[guardiantrainingtask]` | - | on | ✅ | Allena `guardian_index` se sbloccato, altrimenti Vermilion. |
| Experiments | `[experimentstask]` | 120 | on | ⚠️ | Claim verificato. Default `resource_type = "0"` (solo Dragon blood). |
| Oracle Rituals | `[oracleritualstask]` | 200 | on | ❌ | Nessun account a 200. Vedi "Da fare e rimandato". |
| Oracle's Gift | `[oraclesgifttask]` | 200 | on | ❌ | Nessun account a 200. |
| Firestone Research | `[firestoneresearchtask]` | - | on | 🔄 | Priorità in ordine: Raining Gold, Firestone Finder, Firestone Effect, Trainer Skills, Expeditioner; poi il primo nodo mai toccato. Controlla nel log `Selected research #N`: con nodi a livello 0 disponibili deve comparire `fresh`. Se non compare mai, il testo del livello non è nel formato atteso (vedi "Problemi noti"). |
| Meteorite Research | `[meteoriteresearchtask]` | - | on | ⚠️ | Priorità: Raining Gold, Firestone Finder, Firestone Effect. Mai sotto `min_meteorite_reserve` (3000). 29/09: il saldo si leggeva da un contatore nascosto nel tab Meteoriti, sempre 0, quindi non ha mai ricercato (69 giri su 69 nei log). Ora la riga `[INFO] Meteorite balance N below ...` mostra il saldo vero (2380 su Steam-0, sotto la riserva). La scelta per priorità resta da vedere quando il saldo supera 3000. |
| Empower | `[empowertask]` | - | on | ⚠️ | Solo a rapporto `min_reset_ratio = 1.0`, cioè il "+100%" della guida; i limiti di tempo sono spenti. Lettura dei Firestone corretta il 29/09: oltre T il gioco scrive due lettere minuscole (aa = 1e15, poi ×1000 per lettera). Su Steam-0 i testi erano `'1,79bl'` e `'65,67bl'`, letti come 0 (416 letture su 416 nei log del 26-29/09); ora escono 1.79E+126 e 6.567E+127, rapporto 0,03. Un reset vero non è ancora stato visto: da confermare che il valore mostrato dal gioco al momento del reset sia davvero +100%. Steam-0 ha ancora il cfg vecchio (`min_reset_ratio = 2`, limiti a 60/120 minuti), quindi lì empowera a 2 h. |
| Arena of Kings | `[arenaofkingstask]` | 80 | on | ✅ | Scelta dell'avversario più debole e lettura dei gettoni verificate; un ciclo completo di 5 gettoni no. Può durare minuti. |
| Pirate's Prize | `[piratesprizetask]` | 10 | on | ✅ | Solo la traccia gratuita. 29/09: il gioco scarica il menu PirateShip quando non serve e lo ricostruisce in più di 1 s; prima circa metà dei giri scriveva `Tier list root not found`. Ora il task aspetta la lista (verificato al primo giro dopo l'avvio, il caso che falliva). |
| System Mail | `[systemmailtask]` | - | on | ✅ | Il percorso del badge della posta è ipotizzato; senza, gira comunque ogni 6 ore. |

### Guild

| Task | Sezione | Liv. | Default | Stato | Da verificare / note |
|---|---|:-:|:-:|:-:|---|
| Expedition | `[expeditiontask]` | 10 | on | ✅ | Parte sempre la prima spedizione in lista. |
| Tree of Life | `[treeoflifetask]` | 10 | on | 🔄 | Personal Tree. Priorità: Raining Gold, Firestone Finder, Firestone Effect, Battle Cry, Miner (nuova). |
| Free Pickaxes | `[freepickaxestask]` | 50 | on | 🔄 | Reclama da `pickaxe_claim_threshold` (5) in su. **Dal 29/09 gira solo col badge** (`NextRunTime = MaxValue`, come Talents); la strada Gilda → Guild Shop è stata tolta. Motivo: nei log del 26-29/09 quella strada non ha mai raggiunto il timer in 1.015 giri su 1.038, ognuno con un nuovo tentativo dopo 2 minuti (102 minuti in 3 giorni, il 22% del tempo del bot). Col badge invece ha funzionato 9 volte su 9. Da verificare: che quando compare il badge il task reclami, e che non giri quando il badge non c'è. |
| Awakening | `[awakeningtask]` | 50 | on | ⚠️ | L'attesa dell'animazione è stimata. Il badge resta acceso: nei log del 26-29/09 il task ha girato 325 volte, una ogni ~50 s, per 33 minuti. Dal 29/09 `BadgeCooldown` limita i giri a uno ogni 30 minuti (vedi "Problemi noti"). |
| Chaos Rift | `[chaosrifttask]` | 100 | on | ⚠️ | Solo Tomes of Power, mai Eclipse Stones. Il badge resta acceso: nei log del 26-29/09 il task ha girato 430 volte, una ogni ~40 s, per 73 minuti. Dal 29/09 `BadgeCooldown` limita i giri a uno ogni 30 minuti. In 3 sessioni su 9 `menus/ChaosRift` non esisteva e `hitButton` era nascosto: da verificare che colpisca davvero. Se non colpisce, forse è per questo che il badge non si spegne. |
| Forbidden Knowledge | `[forbiddenknowledgetask]` | 100 | on | ✅ | Schermata verificata; il bottone dell'edificio in Gilda è ipotizzato. |
| Guardian Holy Upgrade | `[guardianholyupgradetask]` | 100 | on | ✅ | Chaos Rift → Upgrades → Magic Quarters. 29/09: lasciava aperti LockedGuardian, Chaos Rift e la Gilda (li chiudeva il Watchdog); ora li chiude il task. |

### Map e Warfront

| Task | Sezione | Liv. | Default | Stato | Da verificare / note |
|---|---|:-:|:-:|:-:|---|
| Map Missions | `[mapmissionstask]` | - | on | 🔄 | Ora le più lunghe per prime (`mission_time_order = "desc"`). |
| Warfront Campaign Loot | `[warfrontcampaignloottask]` | 50 | on | ❌ | Mai verificato esplicitamente. |
| Warfront Daily Missions | `[warfrontdailymissionstask]` | 50 | on | ✅ | Battaglie reali verificate. Da ricontrollare: il popup "Here are your rewards!" visto il 18/09 dopo una battaglia. |

### Character

| Task | Sezione | Liv. | Default | Stato | Da verificare / note |
|---|---|:-:|:-:|:-:|---|
| Talents | `[talentstask]` | - | on | ✅ | Parte solo sul badge TalentAvailable. |
| Path of Glory | `[pathofglorytask]` | - | on | ✅ | Traccia gratuita e Golden (se posseduta); non compra mai il pass. |
| Hall of Heroes | `[hallofheroestask]` | - | **off** | ❌ | Non accenderlo in questa sessione: prima serve il riordino degli slot (vedi "Da fare e rimandato"). |

### Scarab Game

| Task | Sezione | Liv. | Default | Stato | Da verificare / note |
|---|---|:-:|:-:|:-:|---|
| Pharaoh's Vault | `[pharaohsvaulttask]` | 60 | on | ✅ | Spin con i Noble Token gratuiti, vault, milestone. 29/09: la schermata non ha un ingresso alle milestone (dump dal vivo: `actionButtons` ha solo pharaohVault, beasts, lostInscriptions, shop; la barra del livello apre `ScarabGameLevelBonuses`). Le milestone si reclamano solo quando le apre il badge `ScarabGameMilestones`, come nei log del 26/09; il click sul path inesistente è stato tolto. |
| Scarab Game Free Token | `[scarabgamefreetokentask]` | 60 | on | ✅ | Omaggio giornaliero del tab Saldi. |

### Events

| Task | Sezione | Liv. | Default | Stato | Da verificare / note |
|---|---|:-:|:-:|:-:|---|
| Decorated Heroes | `[decoratedheroeseventtask]` | - | on | 🔄 | Acquisti nell'ordine Dragon blood → Meteorite → Beer. |
| New Player Event | `[newplayereventtask]` | - | on | 🔄 | Stesso ordine: compra Meteorite, poi spende il resto in Beer. Gli account vecchi non hanno l'evento (il task rallenta da solo). |
| Mass Production | `[massproductioneventtask]` | - | on | ✅ | Solo il tab Challenges. |
| Sigils of Prophecy | `[sigilsofprophecyeventtask]` | - | on | ✅ | Aggiunto il 29/09. Stessa schermata di Mass Production (`events/MiniEvents`), cambia solo il riquadro nell'elenco eventi. Verificato dal vivo: il riquadro apre MiniEvents, 1 claim su 3 giorni (gli altri ancora bloccati), schermata chiusa. Da ricontrollare nei giorni successivi: che reclami ogni giorno sbloccato. |

Un evento non in corso per l'account scrive `'<evento>' isn't in this account's event list`: non è
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

## Da riverificare dopo la pulizia del 2026-09-28

Il codice è stato ripulito senza cambiare comportamento (verificato con un confronto token per
token e rilettura di ogni logica toccata), ma alcuni pezzi condivisi sono stati riscritti. Il giro
completo li copre; questi sono i task in cui guardare con più attenzione:

- Attese e retry dei click (`Poll`): Map Missions, Warfront Daily Missions, Collector, Gamer,
  Pharaoh's Vault, Arena of Kings, Chaos Rift.
- Selettori di quantità (`QuantityToggle`): Miner (x5), Chaos Rift (colpi multipli), Gamer, uno
  shop evento.
- Base comune degli eventi (`EventTask`): uno qualsiasi dei tre. Un evento il cui riquadro c'è ma
  non si apre ora scrive `[FAILED] '<evento>' didn't open`.
- Tempi letti dal gioco (`TimeParser`, riscritto): Empower (minuti di avventura) e ogni `Next:` nel
  log, che deve corrispondere al timer mostrato dal gioco.
- Pirate's Prize e le milestone del New Player Event (ricerca delle voci per indice).
- Watchdog: qualunque task che lasci aperto un popup.

Modifiche del 29/09, fatte dopo l'analisi dei log del 26-29/09 (questa volta il comportamento cambia):

- **`BadgeCooldown`**: nel log, Chaos Rift e Awakening non devono più ripartire a distanza di
  secondi. Tra due giri dello stesso task partiti dal badge devono passare almeno 30 minuti. Un task
  il cui badge si riaccende per lavoro nuovo (Quests, Map Missions) riparte comunque al suo timer.
- **Free Pickaxes solo da badge**: nella tabella di stato deve avere `Next` = 12/31/9999. Quando
  compare il badge deve girare e reclamare.
- **Empower**: la riga `[INFO] Adventure time: ...` riporta i testi grezzi tra apici. Su Steam-0 il
  29/09 erano `'1,79bl'` e `'65,67bl'`; la lettura è stata corretta lo stesso giorno (vedi la tabella).

## Problemi noti

- I popup di avvio ("offline progress", "what's new") sopravvivono a `Watchdog.ForceClearAll`
  (24/09) e non sono mappati. La diagnostica temporanea che li elencava all'avvio è stata tolta da
  `Main.cs`; è recuperabile dal commit `a27ecf0` (`DumpActiveScreensOverTime`).
- Il `storeButton` dell'HUD non apre niente (zero listener): Daily Store Offers dipende dai suoi badge.
- Percorsi ipotizzati, mai visti dal vivo: badge della posta, bottone Forbidden Knowledge in Gilda,
  bottone Party.
- **Badge che restano accesi = task in loop (corretto il 29/09, da verificare dal vivo).** Prima un
  badge acceso rendeva il task sempre pronto, anche se aveva appena girato: Chaos Rift e Awakening
  hanno girato così per 106 minuti in 3 giorni (log del 26-29/09). Ora in `BotTask.IsReady` il badge
  conta solo se il task non ha girato negli ultimi 30 minuti (`BadgeCooldown`, calcolato su
  `LastRunTime`). I timer non cambiano: `NextRunTime` e il nuovo tentativo dopo 2 minuti di un giro
  fallito funzionano come prima. MinerQuestTask ha in più il suo override, che esclude i giri fino
  al giorno dopo.
- **Ogni click a vuoto aspetta comunque `InteractionDelay`.** `GameButton.Click` aspetta anche
  quando il bottone è nascosto o disabilitato, e non clicca. Succede per esempio con i passi Town →
  edificio dopo che il badge ha già aperto la schermata, o con i claim disabilitati di Quests.
  Correzione proposta: aspettare solo dopo un click vero. Serve una prova su Steam-0, perché qualche
  attesa a vuoto potrebbe dare per caso il tempo a una schermata lenta.

### Analisi dei log del 26-29/09 (Steam-0, bot prima della pulizia del 28/09)

Circa 69 ore di log. Il bot ha lavorato per 466 minuti in tutto: 1.038 giri di Free Pickaxes, 430 di
Chaos Rift, 416 di Empower, 325 di Awakening. Il metodo è sommare, task per task, le righe `[Task] …
finished in Ns | Next: …`. Il ritardo programmato si calcola rispetto alla riga `Task Table - <data>`
che segue ogni task.

| Dove va il tempo | Minuti in 3 giorni | Causa |
|---|--:|---|
| Free Pickaxes | 105 | timer mai letto senza badge → nuovo tentativo ogni 2 minuti |
| Chaos Rift | 74 | badge sempre acceso |
| Map Missions | 67 | in buona parte legittimo (missioni che finiscono a orari diversi); 19 nuovi tentativi da 2 minuti |
| Empower | 36 | controllo ogni 5 minuti, con Firestone letti sempre a 0 |
| Awakening | 33 | badge sempre acceso |
| Meteorite Research | 6 | 6 s a giro, ma non ricercava mai: saldo letto 0 (corretto il 29/09) |

I due loop (Free Pickaxes e i badge accesi) valgono il 45% del tempo del bot. Il loop di New Player
Event ogni 2 minuti compare solo nei primi tre log ed è stato corretto il 26/09 alle 17:23
(`314aa75`).
- Firestone Research, livello del nodo: `Preview.CurrentLevel` legge tutte le cifre del testo
  insieme. Se il gioco mostra "0/50", il livello letto è 50 e i nodi mai toccati non vengono
  riconosciuti: la scelta ripiega sul primo nodo disponibile. Il controllo è quello su `fresh`
  indicato sopra.
- `HoldButton` (Hero Upgrade) manda il pointer down ma mai il pointer up: il "rilascio" avviene
  quando il bottone smette di essere interattivo. Funziona da mesi; da tenere presente se Hero
  Upgrade si comporta in modo strano.
- I selettori di quantità si fermano sulla prima opzione x10/x5 che incontrano nel ciclo, non sulla
  più grande. Il costo è proporzionale: cambia solo il numero di click.
- Collector: i tempi tra un'apertura di chest e l'altra non sono ottimali (18/09).

## Da fare e rimandato

- **Distribuire la configurazione sul fleet**: i default della Fase 0 (Personal Tree al posto delle
  War Machines, Empower solo a +100%, task Oracle accesi, missioni `desc`, Experiments solo Dragon
  blood) valgono solo per i file nuovi. Sulle 34 istanze esistenti vanno applicati a mano, a gioco
  chiuso, e per le sandboxate anche nella copia dentro il box. Il riferimento è
  `tools/ConfigTemplate/FirebotPreferences.template.cfg`.
- **Hall of Heroes, prima di accenderlo**: `AlwaysEnchantSlots = { 3, 4, 5, 6, 7 }` spende i Void
  Crystal su Wrist/Shoulder/Belt prima del Ring, che per la guida è il pezzo più importante. Il
  riordino sarebbe `{ 6, 3, 7, 4, 5 }`, ma la corrispondenza indice → slot è dedotta: prima va
  dumpato l'elenco reale degli 8 figli di `GearGrid`. Da verificare anche che Party e Hall of
  Heroes elenchino gli eroi nello stesso ordine.
- **Rituali Oracle**: ce ne sono quattro (Obedience: forzieri solar; Harmony: comet; Concentration:
  oracle's gift ed emblemi; Serenity: lunar), da 40 minuti, uno alla volta, reset ogni 6 ore. Oggi
  parte il primo in ordine di griglia. A cadenza h24 dovrebbero comunque partire tutti dentro la
  finestra: quando un account arriva a 200, verificare prima di tutto che il task ne riavvii uno a
  ogni completamento. Solo se non lo fa serve la scelta per nome, che richiede un dump UnityPy della
  griglia (oggi nessun testo col nome del rituale è mappato).
- Rimandati perché a cadenza h24 rendono poco: modalità "Next Milestone" di Hero Upgrade, upgrade
  globali/speciali (`upgradesButtonUI`, mai mappato), spedizione con più punti, missione del drago
  prioritaria (`MissionPin` non ha il tipo di missione), Wrist un livello sopra Relic.
- Non ancora gestiti: claim gratuito del Monthly pass nello shop di Scarab's Game, missioni Dungeon
  del Warfront, moltiplicatore bulk delle War Machines, Soul stones (Hall of Heroes, livello 200).
- Scartato: la routine di push (pergamene e pouch allo stage ottimale). Fragile e rischia di
  sprecare risorse; i consumabili di gold si tengono per l'uso a mano.

## Da segnalare subito se succede

- Qualunque spesa di gemme o click su un acquisto a pagamento.
- Eclipse Stones comprate (Chaos Rift deve comprare solo Tomes of Power).
- Venduti o usati barili, gold istantaneo o meteoriti istantanee (5/10/30 min, 1 h).
- Un task che non chiude la schermata che ha aperto.
- Hall of Heroes che incanta il tier 1 di un eroe fuori dalla formazione.

## Tornare indietro

Il tag `pre-cleanup-2026-09-28` segna il codice prima della pulizia (commit `a27ecf0`, già con il
riallineamento alla guida F2P). Il commit precedente al riallineamento è `10eebca`.

```powershell
cd C:\Repos\FirestoneBot
git switch --detach pre-cleanup-2026-09-28     # oppure 10eebca
# build e deploy come sopra, sull'istanza che serve (a gioco chiuso)
git switch main                                # per tornare all'ultima versione
```

Per annullare una sola parte della pulizia senza tornare indietro su tutto: `git revert <commit>`,
dove i commit sono quelli della serie successiva al tag (`git log --oneline pre-cleanup-2026-09-28..main`).
