# Piano: gli eventi del gioco

> Aggiornato il 2026-10-10 (Primordial elements sulla flotta, carte dei prossimi mini-eventi), prima il 2026-09-30 sera. Le **sfide** di Decorated Heroes e dei mini-eventi sono fatte e girano
> su Steam-0..16 (sezione 1). Questo piano ora prepara gli **eventi che il bot non gestisce ancora**
> (sezioni 2-6): per ognuno c'è cosa fa l'evento secondo il wiki, cosa dovrà fare il bot e quando se ne
> vedrà la schermata. Niente codice prima di allora: i path si scrivono solo dopo averli visti dal vivo
> (TESTING.md, "Regole"). La sezione 9 serve alle sessioni che controllano i mini-eventi sulla flotta.

## 0. Come partire

Da incollare in una sessione nuova di Claude Code (cartella `C:\Repos\FirestoneBot`), avviata con i
permessi che le evitano di chiedere conferma a ogni comando.

**Controllo di un mini-evento** (Stardust dal 04/10, Primordial elements dal 09/10, Ethereal miners da ~14/10):

```text
Leggi EVENTS_PLAN.md e fai la sezione 9 sul mini-evento in corso, con la skill ponytail attiva:
controlla sulla flotta Steam-0..16 che il mini-evento venga aperto e che le sfide azionabili siano
fatte e reclamate. Se trovi un problema, correggi e verifica su Steam-0, poi ridistribuisci a tutte
le istanze. Aggiorna TESTING.md ed EVENTS_PLAN.md, fai commit e push.
```

**Evento nuovo** (Halloween dal 23/10, poi quelli della sezione 2):

```text
Aggiungiamo l'evento <nome> seguendo EVENTS_PLAN.md, con le regole di TESTING.md (test solo su
Steam-0, path verificati dal vivo, commit per ogni correzione verificata), skill ponytail attiva.
Parti dalla sonda (sezione 7), poi la sezione dell'evento. Per spese nuove chiedimi prima.
```

Pezzi già pronti da riusare:

- `EventTask`: apre l'evento dall'elenco (Battle → Events) per nome della carta, salta le carte
  bloccate o in arrivo, gira sul badge del bottone Events e ogni ora. Un evento nuovo è una
  sottoclasse con `EventNames`, `IsScreenVisible` e `RunEvent`, come `NewPlayerEventTask`.
- `ExchangeTab`: lo scambio di uno shop evento (moltiplicatore x10/x5, acquisti per nome finché c'è
  valuta). Priorità di oggi: Dragon blood, Meteorite, Beer. Serve solo il path del tab.
- `IconSprite.NameAt(path)`: controllo della valuta prima di ogni spesa nuova (mai `gem64`).
- Controlli silenziosi con `GameElement.FindTransform(path)` e `activeInHierarchy`, per gli stati
  normali che `IsVisible` scriverebbe come `[FAILED]`.

## 1. Sfide degli eventi: fatto (30/09)

- **Cosa fa il bot**: Decorated Heroes e `MiniEventTask` (un solo task per gli 11 mini-eventi)
  reclamano, leggono le carte, fanno quello che manca al livello in corso delle sfide azionabili,
  riaprono e reclamano di nuovo (al massimo 4 giri). Le sfide a tempo (missioni, ricerche,
  spedizioni, tempo online, nemici da uccidere) le completano i task che girano già.
- **Regole dell'utente**: né più né meno di quello che sblocca il claim; nessuna riserva; prima le
  quest giornaliere (colpi, giocate, forzieri e vendite partono solo dopo la quest del giorno che usa
  la stessa risorsa); mai gemme. Donazione in Gilda solo se una sfida la chiede, 1.000 (il minimo del
  gioco) anche per una sfida da 500. Upgrade esotico: il più economico, vendendo prima gli oggetti
  ammessi (Midas' Touch, Health, Damage) se le monete non bastano. "Get 50 special upgrades": nessuna
  azione, li compra Hero Upgrade. Map Missions solo `desc`, senza preferenza per tipo di missione.
- **Dove si tocca**: un testo nuovo del gioco va in `ChallengeParser` (tabella delle regex, con test in
  `ChallengeParserTests`); un'azione nuova in `EventChallengeActions`, riusando i passi dei task.
- **Come sono fatte le carte** (sonda del 30/09): in DH il numero nel testo è l'obiettivo del livello
  in corso e il conteggio parte dal reset delle 10:00; a sfida finita "Completed" o `N/N`; la carta
  dell'alchimia sotto il livello 120 ha il titolo nascosto. Nei mini-eventi un giorno bloccato ha
  `unlocked` spento. I path sono commentati in `src/Infrastructure/Paths/Events.cs`.
- **Ancora da vedere** (stato in TESTING.md, righe Events): forzieri, vendite, Tree of Life, upgrade
  esotico e donazione compaiono solo nei mini-eventi; Stardust, dal 04/10, è anche il primo
  mini-evento diverso da Sigils aperto da `MiniEventTask`.

## 2. Gli eventi del wiki e cosa copre il bot

Dal wiki (`docs/wiki/pages/Events.html` e pagine dei singoli eventi) e dall'elenco eventi visto dal vivo
il 30/09.

| Evento | Tipo | Quando | Prossimo | Cosa c'è | Bot |
|---|---|---|---|---|---|
| Decorated Heroes | ricorrente | mesi dispari, 2 settimane, livello 50 | novembre | 8 sfide al giorno, stelle, medaglia Fate | ✅ sfide, claim, scambio (sezione 6 per le medaglie) |
| Mini-eventi (11) | ricorrente | ogni 5 giorni, per 3 giorni | Stardust 04/10, Primordial elements 09/10; Ethereal miners ~14/10 e Team effort ~19/10 (carte già in elenco il 10/10) | una sfida al giorno | ✅ `MiniEventTask` |
| Halloween ("Trick or treat") | calendario | ottobre, 2 settimane, livello 10 | **23/10 verso le 10:00** (carta già in elenco) | zucche → scambio | ❌ sezione 3 |
| Winter Festival | calendario | dicembre | ~19/12 | caramelle → scambio | ❌ sezione 3 |
| Valentine ("Love is in the air") | calendario | febbraio | ~feb 2027 | cioccolatini → scambio | ❌ sezione 3 |
| Spring ("Nature's Dance") | calendario | aprile | ~apr 2027 | fiori → scambio | ❌ sezione 3 |
| Tropicana | calendario | giugno | ~giu 2027 | conchiglie → scambio | ❌ sezione 3 |
| Space ("Astral Alignment") | calendario | agosto | ~ago 2027 | capsule → scambio | ❌ sezione 3 |
| Frostfire Festival | speciale | dicembre | **03/12 verso le 10:00** (carta già in elenco) | regali da aprire, milestone | ❌ sezione 4 |
| Anniversario | speciale | aprile, 2 settimane | ~apr 2027 | check-in, milestone, scambio | ⚠️ sezione 5 |
| New Player Event | per account nuovi | una volta | - | come l'anniversario | ✅ `NewPlayerEventTask` |
| Eventi dei server nuovi, Warfront expansion | classifica | una volta | - | premi ai primi in classifica | niente da automatizzare |

Mai, in nessun evento: shop e offerte a pagamento (il Pumpkin Shop e simili, le offerte di Eve, i
pacchetti dei mini-eventi).

## 3. Eventi di calendario (il primo è Halloween, 23/10)

**Cosa fanno** (wiki, uguali per tutti e sei): un personaggio lascia cadere la valuta dell'evento
durante la battaglia, 3.000 al giorno, raccolta da sola e anche offline (massimo 24 ore). La valuta
si spende nell'edificio dell'evento in città (la capanna della strega al posto della fontana, il
negozio d'inverno...), che ha tre parti: **scambio** (forzieri e valute, ogni offerta con un limite di
acquisti), avatar (21.000 l'uno) e uno shop a pagamento. **A fine evento la valuta rimasta si perde.**

**Lo scambio di Halloween** (wiki, 2025): forziere gear (dipende dal livello: leggendario solo dal
130) 2.500, forziere jewel 2.500, forziere Oracle 2.500 (25 ciascuno); 500 monete esotiche 1.250; 1
piccone 375; 20 Strange Dust 500; 5 honor 1.500; 500 birre 1.250; **150 meteoriti 1.500 (limite
50)**; golden key, cobra key 3.000; twilight hourglass 1.820; soul ember 1.500.

**Cosa comprare**: la guida F2P (`docs/firestone_guida_F2P.md`, "Eventi e shop evento") dice
meteoriti, Dragon Blood e forzieri leggendari; da evitare birra, golden key e forzieri comuni. In
14 giorni arrivano 42.000 di valuta e i meteoriti da soli ne assorbono 75.000, quindi la priorità di
oggi di `ExchangeTab` (Dragon blood, che qui non c'è, poi Meteorite, poi Beer) mette tutto in
meteoriti, circa 4.200 a evento. **Da confermare con l'utente** prima di scrivere il task: tutto in
meteoriti, o anche i forzieri gear dove sono leggendari.

**Da fare quando parte** (ogni evento di calendario è una carta diversa nell'elenco eventi):

1. Sonda (sezione 7) sulla carta e sulla schermata che apre: nome del prefab (sotto `events/` o
   `menus/`), tab, elenco dello scambio (nome oggetto, bottone d'acquisto, moltiplicatore), come si
   chiude. Controllare anche se l'edificio in città apre la stessa schermata.
2. Se i sei eventi usano lo stesso prefab (come i mini-eventi con `MiniEvents`), un solo
   `CalendarEventTask` con i sei titoli in `EventNames`; se no, un task per evento. Si decide dal
   primo, Halloween, e si conferma al secondo (Winter Festival, dicembre).
3. `RunEvent`: tab di scambio → `ExchangeTab.BuyPriorityItems()` → chiudi. Nient'altro: gli avatar
   costano troppo e lo shop è a pagamento.
4. Verifica su Steam-0: acquisti solo delle voci attese, la valuta che scende, niente sui tab a
   pagamento; poi rollout sulla flotta e righe in TESTING.md e nel template.

## 4. Frostfire Festival (03/12)

Evento speciale, 2 settimane (nel 2025 dal 4 al 19 dicembre), livello 10; la carta c'è già
nell'elenco eventi ("Frostfire Festival", in arrivo). Secondo il wiki si aprono dei regali: ognuno dà
un forziere, delle valute (500 meteoriti, 100 Strange Dust, 1.000 monete esotiche, 100 gemme, 4
gettoni, 2 golden key, 1.000 gettoni spedizione, 5 picconi, 500 blueprints, 4 pharaoh's token) o 3
mystery box, e aprendo un regalo in 7 giorni diversi si ottiene un avatar. Dopo ogni regalo Eve fa
due offerte **a pagamento: mai**.

Da capire con la sonda: quanti regali si aprono e ogni quanto (sembra uno al giorno, gratis), se
l'apertura ha un costo (se sì, chiedere all'utente), dove sono il bottone e la milestone. Poi un task
che apre il regalo del giorno e chiude le offerte di Eve senza toccarle.

## 5. Anniversario (aprile)

Gli account che hanno già fatto un New Player Event hanno l'anniversario (2 settimane, ogni aprile):
stessa struttura (ricompensa giornaliera, milestone del tempo online, scambio delle activity coins)
e, dal nome del prefab che il gioco usa per il New Player Event (`AnniversaryShop`), molto
probabilmente la stessa schermata. Quando parte: sonda sulla carta; se apre `AnniversaryShop`, basta
aggiungere il titolo della carta a `NewPlayerEventTask.EventNames`. Da controllare anche la priorità
dello scambio: nell'evento del 6º anniversario il forziere leggendario era il miglior acquisto (guida
F2P).

## 6. Decorated Heroes di novembre: le medaglie

Il tab **Medals** non è mai stato aperto. La guida F2P ("Medaglie Fate") dice che la medaglia d'oro
(+25% all'effetto Fate) chiede circa l'85-90% delle sfide giornaliere per tutto l'evento: col bot che
ora le completa potrebbe arrivare. Al prossimo DH, con la sonda: cosa mostra il tab, se la medaglia va
reclamata a mano e con quale bottone. Se va reclamata, un passo in `DecoratedHeroesEventTask`.

## 7. La sonda

Un task temporaneo (`src/Tasks/Events/ProbeTask.cs`, gruppo Events, `NextRunTime = DateTime.MaxValue`
dopo il primo giro, `MaxRuntimeSeconds` alto) che apre la schermata e scrive nel log il sottoalbero con
testi, sprite e stato dei bottoni. **Non va committato**; dopo averlo tolto, a gioco chiuso, va
cancellata dal cfg di Steam-0 la sezione `[probetask]`. Il pezzo che scrive l'albero, usato il 30/09:

```csharp
private static void Walk(Transform t, string indent, int depth, int maxDepth, List<string> lines)
{
    for (var i = 0; i < t.childCount; i++)
    {
        var c = t.GetChild(i);
        var s = $"{indent}{c.name} [{(c.gameObject.activeInHierarchy ? "on" : "off")}]";
        var tmp = c.GetComponent<TMP_Text>();
        if (tmp != null) s += $" text='{tmp.text?.Replace("\n", "\\n")}'";
        var img = c.GetComponent<Image>();
        if (img != null && img.sprite != null) s += $" sprite={img.sprite.name}";
        var btn = c.GetComponent<Button>();
        if (btn != null) s += $" btn(interactable={btn.interactable})";
        lines.Add(s);
        if (depth < maxDepth && c.gameObject.activeInHierarchy) Walk(c, indent + "  ", depth + 1, maxDepth, lines);
    }
}
```

Si parte da `GameElement.FindTransform("<radice>")`, con `Logger.Info` del risultato. Per trovare la
schermata aperta dopo un click: `Watchdog.DumpActiveScreens()`. La carta dell'evento si apre con
`EventManager.Open` e `EventManager.OpenEvent(new[] { "<titolo>" }, t => ...)`.

## 8. Calendario

| Quando | Cosa |
|---|---|
| 01/10, dopo le 10:30 | controlli del reset (TESTING.md e memoria del reset): DH dopo le quest, Collector e Merchant coi passi condivisi |
| 02/10, 10:00 | fine di Decorated Heroes e di Sigils of Prophecy |
| 04/10, 10:00 | Stardust: primo mini-evento nuovo per `MiniEventTask`, prime azioni dei mini-eventi (sezione 1) |
| 09/10 | Primordial elements: aperto su tutte e 17, sfida del giorno reclamata dove era fatta (TESTING.md, riga Mini Event, 10/10) |
| ~14/10, ~19/10 | Ethereal miners, Team effort (mini-eventi, già tra i nomi di `MiniEventTask`) |
| 23/10, 10:00 | Halloween: sonda e task (sezione 3) |
| novembre | Decorated Heroes: medaglie (sezione 6) |
| 03/12, 10:00 | Frostfire Festival (sezione 4) |
| ~19/12 | Winter Festival: conferma del task di calendario (sezione 3) |
| aprile 2027 | Anniversario (sezione 5) |

## 9. Controllare un mini-evento sulla flotta

Un mini-evento dura 3 giorni e sblocca una sfida al giorno alle 10:00, diversa per ogni account: più
giorni passano, più sfide ci sono da guardare. Il bot gira su Steam-0..16 dal 30/09 alle 16:23, tutte
con la stessa build e lo stesso cfg (TESTING.md, "Da fare e rimandato").

**Dove sono i log.** Steam-0 è nativa, le altre sono nel sandbox di Sandboxie (il percorso reale ha
un log vecchio: leggere sempre quello nel sandbox):

- Steam-0: `C:\Program Files (x86)\Steam-0\steamapps\common\Firestone\MelonLoader\Latest.log`
- Steam-N (1..16): `C:\Sandbox\Admin\SteamB<N>\drive\C\Program Files (x86)\Steam-<N>\steamapps\common\Firestone\MelonLoader\Latest.log`

`Latest.log` copre dall'ultimo avvio; i log precedenti sono in `MelonLoader\Logs`.

**Riepilogo per istanza**, da PowerShell:

```powershell
foreach ($n in 0..16) {
  $log = if ($n -eq 0) { "C:\Program Files (x86)\Steam-0\steamapps\common\Firestone\MelonLoader\Latest.log" }
         else { "C:\Sandbox\Admin\SteamB$n\drive\C\Program Files (x86)\Steam-$n\steamapps\common\Firestone\MelonLoader\Latest.log" }
  $t = Get-Content $log
  "===== Steam-${n}: aperto " + ((@($t -match "\[EventManager '[^']+'\] attempt") -replace ".*EventManager '([^']+)'.*", '$1' | Sort-Object -Unique) -join '/') +
    " | FAILED=" + @($t -match '(EventTask|MiniEventTask|EventChallenge).*\[FAILED\]').Count + " threw/timeout=" + @($t -match 'threw:|timed out').Count
  $t -match "MiniEventTask.*(Event '|Not open)|Chests: opened|Merchant: sold|Sell grid|Tree of Life: |Meteorite research '|Exotic upgrade|Guild donation|Enlightenment|waits for today|MiniEvents\] ClaimAllChallenges: claimed" |
    ForEach-Object { '  ' + ($_ -replace '^\[([0-9:]+)\.\d+\].*\[(INFO|DEBUG)\] ', '$1 ') } | Select-Object -Unique
}
```

**Cosa deve comparire**:

1. **Apertura**: `[EventManager '<nome>'] attempt 1/3 ... done=True` su ogni account a cui l'evento
   è aperto; `Not open to this account` dove la carta è bloccata per livello (su Sigils: Steam-5 e
   -16). `[FAILED] '<nome>' didn't open` vuol dire che la carta non apre `events/MiniEvents`: sonda
   (sezione 7) sulla carta e sulla schermata che apre.
2. **Sfide**: per ogni giorno sbloccato e non reclamato, `Event '<nome>': '<testo>' x/y -> <tipo>, N
   missing` oppure `challenge not handled: '<testo>'`. Un testo azionabile che finisce in "not
   handled" è un testo nuovo del gioco: si aggiunge a `ChallengeParser` (con un test in
   `ChallengeParserTests`).
3. **Azioni**, ognuna con al massimo i passi mancanti, poi `N step(s) done in round R - reopening to
   claim` e `[MiniEvents] ClaimAllChallenges: claimed k/3` con k > 0:

   | Sfida | Riga attesa | Da controllare |
   |---|---|---|
   | Open N chests | `Chests: opened n/N` e `[ChestOpening] '/Wooden': done, opened a/b` | dal forziere più economico |
   | Sell N items | `Sell grid: ...`, `Merchant: sold n/N` | solo Midas' Touch, Scroll of Health, Scroll of Damage |
   | Tree of Life | `Tree of Life: <nodo> a -> b` | un acquisto per ogni passo mancante |
   | Meteorite research | `Meteorite research '<nodo>' for C: balance X -> Y` | vista dal vivo il 30/09 (Steam-1) |
   | Enlighten guardians | `Enlightenment 1/1: ...` o `the button isn't clickable` | 20 Strange Dust l'una |
   | Exotic upgrade | `Exotic upgrade: cheapest '<nome>' costs C, coins X -> Y after sales.` poi `Exotic upgrade '<nome>': coins X -> Y.` | il più economico; vendite solo degli oggetti ammessi |
   | Donate guild coins | `Guild donation for N missing: guild coins X -> Y.` | 1.000 (o "Max" sotto 1.000 se basta) |

   Le righe di forzieri, vendite, Tree of Life e meteoriti escono anche dai task normali (Collector,
   Merchant, Tree of Life, Meteorite Research): per la sfida contano solo quelle subito dopo una riga
   `Event '<nome>': ... -> <tipo>` con lo stesso orario. Per non fermare il riepilogo a metà, non
   passarne l'uscita a `Select-Object -First`.

   `waits for today's <Task> to finish first` non è un errore: la quest giornaliera con la stessa
   risorsa non è ancora fatta, e il mini-evento riprova al giro dopo (ogni ora).
4. **Criteri**: nessuna spesa oltre i passi mancanti, niente venduto fuori dai tre oggetti ammessi,
   mai gemme, `FAILED=0` e `threw/timeout=0` per gli eventi. Il dettaglio di un'istanza:
   `Select-String -Path $log -Pattern "Event '" -Context 0,12`.

**Se serve una correzione**: si segue TESTING.md, sezione "Per l'agente" (comandi per fermare,
compilare, installare e avviare Steam-0; regole). Si corregge e si verifica **solo su Steam-0**, poi
si ridistribuisce a tutta la flotta, sempre da PowerShell (Git Bash rovina gli argomenti `/box:`):

1. Fermare tutto: Steam-0 con `Get-Process | Where-Object { $_.Path -like 'C:\Program Files (x86)\Steam-0\*' } | Stop-Process -Force`,
   le altre con `& 'C:\Program Files\Sandboxie-Plus\Start.exe' /box:SteamB<N> /terminate`; controllare
   che non resti nessun `Firestone` né `steam`.
2. Copiare `firebot.dll` e `Firebot.TalentEngine.dll` (da `src\bin\Release\net6.0`) nei `Mods` di
   Steam-0 e nei **due** `Mods` di ogni Steam-1..16, quello reale e quello nel sandbox: 33 cartelle.
3. Riavviare in sequenza, 20 s l'una: Steam-0 con `steam.exe -silent -applaunch 1013320`, le altre con
   `Start.exe /box:SteamB<N> "C:\Program Files (x86)\Steam-<N>\steam.exe" -silent -applaunch 1013320`.
4. Aspettare `Started. Enabled tasks: 38 of 40 loaded.` in ogni log e ripetere il riepilogo qui sopra.

**Alla fine**: aggiornare in TESTING.md la riga Mini Event e quella di Decorated Heroes (sfide viste
dal vivo, date, istanze), in questo file "Ancora da vedere" (sezione 1) e il calendario (sezione 8);
poi commit e push.
