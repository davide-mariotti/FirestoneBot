# Piano di test dal vivo

Nessun task è mai stato testato dentro il gioco vero - solo verificato staticamente (scansioni
UnityPy + build pulita). Questo documento guida il primo giro di test reali, un task alla volta.

**Aggiornamento dopo il primo giro (vedi `PLAN.md`, sezione "Primo giro di test dal vivo")**: trovati
e corretti diversi bug reali di navigazione (Free Pickaxes, Oracle's Gift, Character/Quests, rail di
notifica, Oracle Rituals/Experiments/Firestone Research, animazione Awakening).

**Aggiornamento dopo il secondo giro (2026-09-17)**: confermato dal vivo che `TownIrongard` era
davvero `menus/TownIrongard`, non `popups/TownIrongard` come cambiato nel giro precedente - riportato
indietro (vedi `PLAN.md`). Da lì passano moltissimi task, quindi questo sblocca la maggior parte
della lista sotto. Trovati e corretti anche due bug propri di Firestone Research (scansionava tier
bloccati invece di fermarsi al primo non sbloccato; crashava silenziosamente se restava un secondo
slot vuoto da riempire dopo aver avviato la prima ricerca) e applicato preventivamente lo stesso fix
a Meteorite Research, che ha la stessa identica struttura. Testati dal vivo e confermati funzionanti:
Guardian Training, Daily Store Offers, Firestone Research, Meteorite Research (vedi righe sotto).
**Punto aperto, non bloccante**: dopo Firestone Research il bot a volte sembra restare sulla
schermata città invece di tornare alla battaglia come gli altri task - non ancora isolato con
certezza (potrebbe essere un altro task, es. Daily Store Offers, che riapre Town subito dopo).
Da riverificare in un giro dedicato, ignorato per ora.

**Aggiornamento (2026-09-17, System Mail)**: scoperta importante confermata dall'utente - il gioco
sceglie tra **due varianti HUD parallele in base alla risoluzione/aspect ratio del client**, non è
un'ambiguità di versione. Un path hardcoded su una sola variante funziona in metà delle sessioni e
fallisce silenziosamente nell'altra metà. Corretto sistematicamente con un helper
(`UiVariantButton`, prova più path candidati) applicato al bottone mail e a **tutta** la rail di
notifica (`NotificationsLoc`, usata da quasi ogni task come fast-path opzionale + segnale di
priorità) - vedi `PLAN.md`. **Non ancora esteso** all'ambiguità Desktop/Mobile/New della barra
inferiore (Path of Glory/Inventory/Party/Hero Upgrade) - nessun test dal vivo l'ha ancora toccata.
Testato dal vivo e confermato funzionante: Mailbox (System Mail) - claim riuscito, popup "Rewards"
di conferma chiuso correttamente dal Watchdog generico (non serve un click esplicito su "OK").

**Aggiornamento (2026-09-18, Collector/apertura chest)**: causa radice trovata e risolta. Gli slot
chest in Inventario (`Common`/`Uncommon`/`Rare`/`Epic`, celle riciclate della ScrollView) hanno un
componente `Button` presente/abilitato ma con **zero listener su `onClick`** - il vero click nel
gioco reagisce direttamente agli eventi puntatore, non a `Button.onClick`. `GameButton.Click()`
(che chiama `onClick.Invoke()`) per questo non faceva nulla, mentre un click reale dell'utente
funzionava. Aggiunto `GameButton.ClickSimulated()`, che rigioca la sequenza
PointerDown/Up/Click tramite `UnityEngine.EventSystems.ExecuteEvents` direttamente sul
GameObject target - è una chiamata puramente in-process nell'EventSystem di Unity di
quell'istanza, non tocca il mouse/cursore reale del sistema operativo, quindi resta sicura su ~20
istanze del gioco in parallelo. Confermato dal vivo: la preview si apre e le chest si aprono
(Rare/Epic svuotate, Uncommon ridotta). Aggiunto anche un poll (non un timer fisso) che aspetta il
prossimo bottone (`openx1`/`openx10`) diventi cliccabile prima del click successivo, perché
l'animazione di apertura ha durata variabile e un delay fisso usciva troppo presto rischiando di
saltare l'apertura successiva. **Punto ancora aperto**: il presunto "secondo schermo" `ChestOpening`
(per incatenare più aperture dello stesso tipo senza tornare alla preview) non è mai stato
osservato esistere nella gerarchia finché non serve più di un click - probabile che con quantità
piccole tutto avvenga in un solo click sulla preview stessa; il codice lo gestisce comunque come
no-op sicuro se quello schermo non esiste. Resta da verificare con una chest che richieda più di un
ciclo (es. scorta comune grande) se quel percorso serve davvero o va rimosso.

Per velocizzare i cicli di test in questa sessione, `auto_start` è stato temporaneamente messo a
`true` (con `start_bot_delay` al minimo consentito di 10s) invece di `false` come raccomandato
sotto - il bot parte da solo ad ogni avvio del gioco senza bisogno di premere F7. Ricordarsi di
rimetterlo a `false` a fine sessione di test se si torna alla procedura "un task alla volta" con
osservazione manuale.

## Perché un task alla volta

Attivare tutto insieme renderebbe impossibile capire quale azione ha causato quale effetto nel
gioco. La procedura è: abilita **un solo** task nel file di configurazione, avvia il gioco, osserva
se fa quello che dovrebbe, poi disabilitalo prima di passare al successivo.

`auto_start` deve restare `false` per tutta la fase di test: si avvia il bot a mano con `F7` solo
quando si vuole osservare quel task specifico, poi si preme di nuovo `F7` per fermarlo prima di
richiudere il gioco e modificare il file.

## Prima di iniziare

1. **Chiudi completamente il gioco** (il file `firebot.dll` è bloccato mentre Firestone è aperto).
2. Il file di configurazione reale è in `Firestone/UserData/FirebotPreferences.cfg` (diverso dal
   vecchio `config/FirebotPreferences.cfg` che era nel repository - quello era solo un riferimento
   statico, mai caricato dal gioco, ed è stato rimosso nella pulizia).
3. **I nomi delle sezioni sono cambiati** rispetto a prima: ora corrispondono al nome della classe
   C# del task (es. `[alchemist]` è diventato `[experimentstask]`, `[oracle]` è diventato
   `[oracleritualstask]`). Le vecchie sezioni con `enabled = true` restano nel file ma non
   corrispondono più a nessun task reale - il bot le ignora, non c'è rischio che qualcosa parta da
   solo al primo avvio. Ogni sezione elencata sotto va cercata/creata con il nome esatto indicato.
4. Verifica che `debug_mode = true` sotto `[firebot_settings]`, per avere log dettagliati in
   console durante i test.
5. Log da tenere d'occhio: `Firestone/MelonLoader/Latest.log` - ogni task stampa quando parte,
   cosa trova, e quando pianifica il prossimo controllo.

## Procedura per ogni task

1. Chiudi il gioco (se aperto).
2. Apri `FirebotPreferences.cfg`, trova la sezione del task (o creala se non esiste ancora - basta
   avviare il bot una volta con tutto spento perché tutte le sezioni vengano generate), imposta
   `enabled = true` sotto quella sezione soltanto.
3. Salva, avvia il gioco, aspetta il caricamento completo, premi `F7`.
4. Osserva il comportamento atteso (vedi la tabella sotto) e il log per eventuali errori.
5. Premi di nuovo `F7` per fermare il bot, chiudi il gioco.
6. Rimetti `enabled = false` per quel task prima di passare al successivo.

Alcuni task richiedono uno stato specifico nel gioco per avere qualcosa da fare (es. missioni già
completate, valuta accumulata) - se non hanno nulla da fare la prima volta, è normale: riprova dopo
aver raggiunto quella condizione, o confronta comunque il comportamento "niente da fare" con quanto
descritto (dovrebbe restare inerte senza errori, non bloccarsi).

---

## Comportamenti sempre attivi (non sono nello scheduler dei task)

Questi due non hanno una sezione "un task alla volta" nello stesso senso - partono e si fermano con
`F7` insieme al resto, ma girano in continuo invece che a schedulazione.

| # | Nome | Sezione cfg | Comportamento atteso |
|---|------|--------------|----------------------|
| - | **Hero Upgrade** | `[hero_upgrade]` | Durante una battaglia, il leader e ogni slot eroe vengono potenziati automaticamente (bottone upgrade tenuto premuto a intervalli). Verifica che il gold cali e i livelli salgano, senza click visibili su eroi non ancora sbloccati. |
| - | **AutoRetreat** | `[auto_retreat]` | Se lo stage in battaglia non avanza per `stall_minutes` (default 3 min), il bot dovrebbe cliccare la freccia "torna indietro" nello stage in battaglia `retreat_stages` volte (default 5). Per testarlo in tempi ragionevoli, abbassa temporaneamente `stall_minutes` a 1 e fermati apposta su uno stage duro. |

---

## Quests (6 task che gestiscono le missioni giornaliere)

| # | Nome | Sezione cfg | Livello min. | Comportamento atteso |
|---|------|--------------|:---:|----------------------|
| 1 | ✅ Quests (claim giornaliere/settimanali) | `[queststask]` | - | Apre Character → Missioni, clicca claim su ogni missione già completata (giornaliere e settimanali), lascia stare quelle non ancora fatte. **Testato 2026-09-17: funziona, switcha correttamente tra daily e weekly.** |
| 2 | ⚠️ Collector | `[collectorquesttask]` | - | Apre l'Inventario, apre le chest (gear/jewel/celestial) tenendo da parte `min_common_chest_reserve` (default 10) chest comuni - il numero di chest comuni in inventario non dovrebbe scendere sotto quella soglia. **Testato 2026-09-18: risolto il bug di fondo (click sullo slot chest non arrivava a destinazione, vedi nota sopra) - confermato dal vivo che le chest si aprono davvero (Rare/Epic svuotate, Uncommon ridotta). Funziona ma non è ancora perfetto: i tempi tra un'apertura e l'altra vanno ottimizzati ulteriormente (il poll attuale aiuta ma non è la soluzione definitiva). Percorso "apri più lotti di fila senza richiudere" non ancora esercitato da un caso reale. Da rivedere in un giro dedicato.** |
| 3 | ✅ Gamer | `[gamerquesttask]` | 15 | In Taverna, gioca fino a 10 partite con i Game Token, lasciandone almeno `min_token_reserve` di scorta. **Testato 2026-09-18: 3 bug reali trovati e corretti.** (1) Il bottone edificio "tavern" nel Town Hub apre in realtà un popup intermedio "TavernSelection" con due carte ("tavern"/"scarabGame") - serve un secondo click sulla carta giusta, non entra direttamente nella schermata (vedi `Town.OpenTavern`/`Town.OpenScarabGame`). (2) Il contatore token letto (`counterInteraction/quantity`) era quello sbagliato, sempre vuoto/inattivo - quello vero è `currencyInteraction (GameToken)/quantity`, il che faceva fallire silenziosamente ogni tentativo di gioco (leggeva 0 token). (3) Cliccare "Play" da solo non basta: apre 6 carte coperte intercambiabili, serve un click su una di esse per far partire l'animazione e completare davvero il turno (altrimenti i token non scendono). Aggiunta anche l'ottimizzazione richiesta: se conviene ed è affrontabile rispettando la riserva, usa il moltiplicatore x10 (un solo turno da 10 invece di 10 turni da 1) - costo lineare confermato (Play 1 = 1 token, Play 10 = 10). **Confermato dall'utente: funziona tutto.** |
| 4 | ✅ BeerExchange | `[beerexchangetask]` | 15 | Taverna → Mercato: compra ripetutamente il pacchetto da 5 token finché conviene. **Testato 2026-09-18: trovati e corretti 2 bug reali prima di lasciar cliccare nulla.** (1) Root del popup sbagliato (`menus/TavernMarket` invece di `popups/TavernMarket`, stesso errore già visto per BattlePass). (2) La struttura reale NON è "un item con due bottoni birra/gemme" come da wiki: sono **tre item separati** (`gameTokenWithBeer` x5/birra con bottone proprio `purchaseButtonOffer`, `gameTokenBulk` x20/gemme, `gameToken` x5/gemme) - il path assunto in origine (`gameToken/purchaseButton`) avrebbe cliccato l'offerta a **gemme**, mai scattato dal vivo solo per il bug (1) che bloccava tutto prima. Corretto con il path reale, **confermato dall'utente: acquisto completo riuscito, spesa birra corretta, nessun impatto sulle gemme**. Il loop clicca ripetutamente finché conviene (non solo una volta). **Punto minore non bloccante**: il pulsante "tavern" nel Town Hub (`townBg/parent/tavern`) risulta ancora non verificato - la navigazione reale finora è sempre passata dalla notifica rapida, mai dal percorso "garantito" Town→Tavern. |
| 5 | ⚠️ Merchant | `[merchantquesttask]` | 30 | Exotic Merchant: usa tutti gli oggetti oro in inventario, vende esattamente 10 oggetti diversi (uno per tipo, mai lo stesso due volte), fa un solo upgrade, poi torna a reclamare la quest "Merchant". **Testato 2026-09-18: 3 bug reali trovati e corretti.** (1) Consumare un item oro compattava la griglia, facendo scivolare un item diverso (es. un totem) nella stessa posizione che il codice continuava a cliccare - ora rilegge il nome reale prima di ogni click. (2) Mancava l'attesa di popolamento della lista item dopo il cambio tab (stessa causa delle chest) - aggiunta. (3) La vendita riselezionava sempre lo stesso tipo (slot non ancora vuoto) vendendo 10 copie della stessa cosa invece di 1 per tipo - ora tiene traccia dei nomi già venduti. Esclusi esplicitamente oro e Midas' Touch dalla vendita. **Confermato dall'utente: uso oggetti oro e vendita ora corretti.** Verifica completa (incluso il claim finale della quest) bloccata oggi dall'esaurimento delle risorse test sull'account - da riconfermare dopo il reset di domani mattina. |
| 6 | ✅ Miner | `[minerquesttask]` | 50 | Gilda → Cristallo Arcano: colpisce il cristallo 5 volte (click singoli). **Testato 2026-09-17: funziona perfettamente.** Aggiunta ottimizzazione richiesta: se il moltiplicatore quantità può essere impostato a x5, un solo click sostituisce i 5 colpi singoli (vedi `ArcaneCrystal.TrySetQuantityTo5`). |

## Town (edifici cittadini)

| # | Nome | Sezione cfg | Livello min. | Comportamento atteso |
|---|------|--------------|:---:|----------------------|
| 7 | ✅ Daily Store Offers | `[dailystoreofferstask]` | - | Reclama la ricompensa giornaliera di accesso e la mystery box gratuita giornaliera; non tocca i bundle a pagamento accanto. **Testato 2026-09-17: mystery box confermata, funziona.** |
| 8 | ✅ Engineer | `[engineertask]` | 50 | Reclama gli strumenti pronti dall'Ingegnere quando disponibili. **Testato 2026-09-17: funziona, letto timer reale (6h) dal quick-access della notifica. ⚠️ 2026-09-18: durante il fix di War Machines scoperto che il percorso di fallback via building click (`Town.OpenEngineer`, usato solo se la notifica non è attiva) era rotto per lo stesso motivo (popup "GarageSelection" mai gestito) - corretto insieme al fix di War Machines, ma non ancora ri-verificato dal vivo passando per quel percorso specifico (il test del 17/09 aveva sempre la notifica attiva).** |
| 9 | ✅ War Machines | `[warmachinestask]` | 50 | Town → Engineer (popup "GarageSelection", card "garage") → War Machines → tab Workshop: livella ogni war machine posseduta finché il bottone di livellamento resta cliccabile o finché mancano gli Expedition Token (popup "CurrencyMissing" gestito, si ferma subito invece di girare a vuoto). **Testato 2026-09-18: bug di navigazione risolto (il bottone "Engineer/warMachinesButton" non esisteva - il building "Engineer" apre in realtà un popup di scelta con 3 card: engineer/garage/trainingBase, War Machines sta dietro "garage", non dentro la schermata Engineer). Stesso fix applicato a `Town.OpenEngineer` (bug latente mai emerso perché `[engineertask]` era sempre stato testato solo via notifica quick-access). Confermato dall'utente via screenshot: naviga correttamente e si ferma pulito sul popup "Get more Expedition Token". Lasciato `enabled = true` su richiesta dell'utente. Verifica end-to-end completa (con Expedition Token sufficienti per un livellamento reale) ancora da fare.** |
| 10 | ✅ Guardian Training | `[guardiantrainingtask]` | - | Magic Quarters: avvia l'allenamento sul guardiano configurato (`guardian_index`). **Testato 2026-09-17: funziona, torna correttamente alla schermata di battaglia.** |
| 11 | Experiments (Alchemist) | `[experimentstask]` | 120 | Avvia/reclama esperimenti in Alchemist; se `resource_type` è vuoto non fa nulla (comportamento voluto, di norma da configurare esplicitamente). |
| 12 | Oracle Rituals | `[oracleritualstask]` | 200 | Reclama rituali completati e ne avvia uno nuovo. |
| 13 | Oracle's Gift | `[oraclesgifttask]` | 200 | Reclama il regalo giornaliero dell'Oracolo. |
| 14 | ✅ Firestone Research | `[firestoneresearchtask]` | - | Library → tab Firestone Research: avvia/reclama ricerca, con "Raining Gold" sempre priorità se disponibile. **Testato 2026-09-17 (3 giri, 2 bug trovati e corretti - vedi nota in alto): ora riempie correttamente più slot vuoti in un solo run, senza scansionare tier bloccati.** |
| 15 | ✅ Meteorite Research | `[meteoriteresearchtask]` | - | Library → tab Meteorite Research: stesso principio, sui 5 alberi di meteorite. **Testato 2026-09-17 (con lo stesso fix applicato preventivamente): funziona.** |
| 16 | ✅ Temple of Eternals (Empower) | `[empowertask]` | - | Fa il reset/prestige solo quando il rapporto Firestone trovate/possedute e i minuti di avventura configurati sono soddisfatti - non dovrebbe mai fare empower "a caso". **Testato 2026-09-18: l'avventura era ferma da 12h24m (oltre la soglia massima di 2h) - il task ha correttamente forzato l'empower a prescindere dal rapporto (ratio=0 ma tempo massimo superato). Nessun errore, popup aperto e richiuso correttamente. Confermato dall'utente: funziona.** |
| 17 | ✅ Free Pickaxes | `[freepickaxestask]` | 50 | Reclama piccozze gratuite solo una volta raggiunta la soglia `pickaxe_claim_threshold`. **Testato 2026-09-18 (soglia temporaneamente a 1 per il test): funziona, notifica quick-access aperta correttamente, timer di rigenerazione letto bene. Confermato dall'utente.** |
| 18 | ✅ Scarab's Game (omaggio) | `[scarabgamefreetokentask]` | 60 | Taverna → Scarab's Game → shop: reclama l'omaggio giornaliero gratuito nel tab Saldi. **Testato 2026-09-18**: oltre al fix di navigazione (Town → edificio tavern → carta "scarabGame", vedi Gamer Quest), trovato un altro bug dello stesso tipo BattlePass/TavernMarket: `ScarabGameShopLoc.Root` puntava a `menus/ScarabGameShop`, in realtà è `popups/ScarabGameShop`. **Confermato dall'utente: funziona.** Flusso completo confermato: shop (token gratis) → spin → vault. **Punto aperto, non bloccante**: lo shop ha anche una tab "Monthly pass" con un secondo omaggio gratis ("Pharaoh's token x1") non ancora reclamato da questo task - solo la tab "Saldi" è gestita oggi. Da aggiungere in un giro dedicato. |
| 19 | ✅ Pharaoh's Vault + spin | `[pharaohsvaulttask]` | 60 | Gira la slot con i Noble Token gratuiti, apre il Pharaoh's Vault quando ci sono abbastanza Ancient Coin. **Testato 2026-09-18**: oltre al fix di navigazione, trovati e corretti: (1) `PharaohsVaultLoc.Root` puntava a `popups/PharaohsVault`, in realtà è `menus/PharaohsVault` (stesso pattern di bug, ma invertito). (2) Nessuna attesa "smart" tra uno spin/apertura e l'altro - aggiunta la stessa attesa a poll già usata per le chest (`ScarabGame.WaitUntilClickable`). Lo spin manuale (bottone "Play") ha funzionato bene, non serve la modalità "Auto". **Confermato dall'utente: funziona, vault aperto correttamente.** |
| 20 | ✅ Mailbox | `[systemmailtask]` | - | Reclama ogni ricompensa in posta (Arcane Crystal, traguardi livello, rank Arena, Battle Pass) - non deve mai toccare il tasto elimina. **Testato 2026-09-17 (fix HUD a doppia variante, vedi nota in alto): funziona.** |
| 21 | Hall of Heroes | `[hallofheroestask]` | - | Per ogni eroe: sblocca tier gear T2/T3 se possibile, incanta gear T2/T3 (tutti gli eroi) + T1 (solo eroi nella formazione attiva) + tutti i jewel. **Punto critico da osservare**: verifica che il T1 venga incantato sugli eroi giusti (quelli davvero in formazione) - è l'assunzione meno sicura di tutto il codice, vedi `PLAN.md` Task 31. |
| 22 | ✅ Arena of Kings | `[arenaofkingstask]` | 80 | Town → Battles (popup "WFMenuSelection", card "arena") → Arena of Kings. Sceglie l'avversario più debole tra i 3 mostrati, rerollando ogni 5s (poll sul bottone, non timer fisso); dopo 3 min accetta fino a +5% di potenza, poi +10%, poi +20%, oltre i 9 min combatte comunque il migliore. Può girare a lungo (fino a 5 token/giorno) - non è un bug se impiega minuti. **Testato 2026-09-18: risolti 3 bug distinti - (1) "WFMenuSelection/bg/arena" dava "path broken" solo perché la notifica quick-access aveva già aperto la schermata prima, popup e card sono reali; (2) `MyPower` leggeva sempre 0 per due motivi sovrapposti: il nome reale è "totalPower" non "arenaPower", e il suo testo include l'etichetta ("Arena power: 18.336") invece del solo numero come per gli avversari - aggiunto lo strip dell'etichetta in `GameText.GetParsedDoubleAbbreviated`; (3) `TokensAvailable` leggeva sempre 0 per lo stesso bug "N/Total" già visto nei Talenti ("5/5"). Aggiunto anche un fix generale in `GetParsedDoubleAbbreviated` per numeri tipo "17.242" (letti erroneamente come 17,242 in stile invariant culture). Confermato dall'utente: sceglie correttamente gli avversari più deboli invece di fare sempre refresh, combatte, e si ferma pulito (6s, nessun errore) quando i token finiscono. Verifica completa di un ciclo da 5 token rimandata a domani (token di oggi già esauriti durante i test).** |
| 23 | ✅ Pirate's Prize | `[piratesprizetask]` | - | Town → Pirate Ship (edificio "ship") → tab Pirate's Prize: reclama solo i premi della traccia gratuita del reward track a livello personaggio (sblocca da livello 10); non tocca mai la traccia a pagamento né le altre tab (Mercenaries, Captain's Deal, Skins). **Feature nuova, mai automatizzata prima - Testato 2026-09-18: trovato e corretto un bug architetturale mai visto finora nel codice.** I ~20 elementi della lista premi (`ppTierInteraction(Clone)`) NON sono pool/virtualizzati come inizialmente ipotizzato (esistono tutti in gerarchia simultaneamente, lo ScrollRect si limita a scorrerli in vista) - il vero problema è che condividono tutti lo STESSO nome senza indice per-istanza (a differenza di ogni altra lista pool nel codice, che usa `[pool N]` o `(N)`), e `GameElement`/`GameButton` risolvono sempre per stringa di percorso ad ogni accesso: `Transform.Find` colpiva quindi sempre lo stesso (primo) fratello a prescindere da quale tier si pensava di controllare, facendo ricontrollare 20 volte lo stesso elemento invece di scorrerli davvero. Risolto bypassando `GameElement`/`GameButton` per la scansione: risolve il contenitore una sola volta via `Transform` grezzo, poi itera `GetChild(i)` direttamente (riferimenti Transform live, univoci) e clicca il bottone claim di ogni tier con un click simulato manuale (`ExecuteEvents`) sulla Transform già nota. **Confermato dall'utente: funziona.** Abilitato su -0. |

## Guild

| # | Nome | Sezione cfg | Livello min. | Comportamento atteso |
|---|------|--------------|:---:|----------------------|
| 24 | ✅ Expedition | `[expeditiontask]` | 10 | Reclama la spedizione attiva completata e ne avvia una nuova. **Testato 2026-09-18: funziona, nessun errore, nuova spedizione avviata e prossimo controllo schedulato correttamente.** |
| 25 | ✅ Tree of Life (Personal) | `[treeoflifetask]` | 10 | Gilda → Albero della Vita → vista Personale: compra upgrade con Expedition Token, priorità a Raining Gold/Firestone Finder/Firestone Effect. **Testato 2026-09-18: 2 bug reali trovati e corretti.** (1) Cliccare un nodo apre un popup di anteprima ("Magic spells / Level 0/5 / Buy upgrade 600") - il codice non lo sapeva e non premeva mai il bottone verde, restando bloccato fino al timeout di 120s del framework (task forzatamente interrotto). Aggiunto il click sul vero bottone (`bg/normal/buyUpgradeButton`) + chiusura popup. (2) Quando i token finiscono, il gioco mostra un popup bloccante "CurrencyMissing" ("You need N more Expedition token") che il codice non riconosceva, continuando a girare a vuoto su tutti i 20 nodi - ora lo rileva e si ferma subito (task da 70s a 9s). **Confermato dall'utente: funziona perfettamente.** |
| 26 | Awakening | `[awakeningtask]` | 50 | Spende Arcane Crystal per risvegliare eroi, usando sempre il moltiplicatore più alto disponibile. **Testalo dopo Mailbox**, altrimenti probabilmente non ci sono cristalli da spendere. |

## Map & Warfront

| # | Nome | Sezione cfg | Livello min. | Comportamento atteso |
|---|------|--------------|:---:|----------------------|
| 27 | ✅ Map Missions | `[mapmissionstask]` | - | Reclama missioni completate, ne avvia di nuove nell'ordine configurato (`mission_time_order`, default `asc`). **Testato 2026-09-18: funziona, più missioni attive scansionate correttamente (tempi parsati bene), nuova missione avviata senza errori.** |
| 28 | Warfront Campaign Loot | `[warfrontcampaignloottask]` | 50 | Reclama i rotoli di ricompensa disponibili della campagna Warfront. |
| 29 | ⚠️ Warfront Daily Missions (Liberator) | `[warfrontdailymissionstask]` | 50 | Combatte le missioni di liberazione una a una, aspettando l'esito reale della battaglia prima di passare alla successiva. **Testato 2026-09-18**: trovato e corretto un ritardo di popolamento mancante nella lista missioni (stessa causa delle chest - tutte le missioni risultavano "non cliccabili" al primo check). Battaglie reali confermate funzionanti (round vinti, ricompense assegnate). **Punto ancora aperto**: dopo la battaglia compare un popup "Here are your rewards!" con bottone "OK" che il codice non riconosce (né `WFBattleWonLoc` né `WFBattleDefeatLoc` corrispondono) - il task resta bloccato fino al timeout (ridotto da 5 minuti a 40s su richiesta dell'utente, dato che le battaglie reali durano pochi secondi). Da trovare il path reale del popup e aggiungere il click, come già fatto per l'Albero della Vita. |

## Character

| # | Nome | Sezione cfg | Livello min. | Comportamento atteso |
|---|------|--------------|:---:|----------------------|
| 30 | ⚠️ Talents | `[talentstask]` | - | Character → tab Talenti: investe punti seguendo la sequenza guidata, salva dopo ogni nodo. **2026-09-18: 2 bug tecnici trovati e corretti** (path `menus/Character` → `popups/Character`; parsing "1/96" che tornava sempre 0). **Bug di design flaggato lo stesso giorno**: il bot seguiva rigidamente l'ordine della guida dall'inizio, "rattoppando" qualsiasi talento indietro rispetto al target anche se l'account (già avanzato) l'aveva volutamente saltato - task lasciato `enabled = false`. **Risolto 2026-09-20**: aggiunta calibrazione una tantum (`guide_start_index`, override manuale in cfg) - al primo run il bot trova il prefisso più lungo della guida già soddisfatto dai rank reali (senza buchi dall'inizio) e investe solo da lì in avanti, mai indietro. Trovato e corretto anche un secondo bug, indipendente dalla guida: `CharacterScreen.Open` non apriva sempre la schermata Character al primo click quando il task girava presto dopo l'avvio del gioco (click "silenzioso", nessun errore, stesso pattern del bottone Store) - ora ritenta fino a 5 volte con 2s di attesa; se comunque non si apre, il task esce senza toccare la calibrazione invece di calibrare su dati inesistenti (bug osservato e corretto durante il test). **Testato 2026-09-20: la calibrazione gira su dati reali (struttura Character confermata: `bg/submenuButtons/{character,talents,achievements,statistics,quests}`), nessun errore nel loop di investimento, run pulito in ~8s.** L'utente non si fida ancora della calibrazione automatica - task lasciato `enabled = false` su tutte le istanze finché non viene rivalutata. |
| 31 | ✅ Path of Glory (Battle Pass) | `[pathofglorytask]` | - | Reclama le ricompense disponibili sia sulla traccia gratuita che su quella Golden (se posseduta) - non deve mai comprare il pass premium. **Testato 2026-09-17: path del bottone d'ingresso corretto (Mobile/Desktop, vedi nota in alto), ma nessun reward era disponibile per verificare la logica di claim vera e propria. Testato di nuovo 2026-09-18 con reward reali disponibili: notifica rilevata correttamente, schermata apert; ogni tier scorre `FreeClaimBtn`/`GoldenClaimBtn` e clicca solo quelli cliccabili (i log mostrano "hidden or inactive" solo per i tier già reclamati/non ancora sbloccati, un click riuscito non produce log per design). Confermato dall'utente: funziona perfettamente. Abilitato.** |

---

## Cose da segnalare se succedono (non dovrebbero, ma sono i punti più a rischio)

- Qualsiasi spesa di **gemme** non prevista (in particolare durante BeerExchange).
- Un task che resta bloccato/non chiude mai la schermata che ha aperto (il Watchdog dovrebbe
  ripulire comunque entro `max_task_runtime`, ma se capita è un bug da segnalare).
- Hall of Heroes che potenzia il gear T1 su un eroe che *non* è nella formazione attiva.
- Qualunque click su un bottone di acquisto reale (a pagamento) invece che su un claim gratuito.

## Bonus volanti: implementati 2026-09-20

`[flying_bonus_hunter]` - il vecchio file di configurazione pre-rewrite aveva questa sezione
("Taps the flying dragon-with-beer and meteorite-hunter bonuses when they cross the screen") assente
dal codice fino ad oggi. Localizzati dal vivo tramite una ricerca ricorsiva per nome su tutta la
scena: sono figli diretti di `battleRoot/battleMain/battleCanvas` (parte del Canvas UI, non oggetti
3D fuori dal Canvas come sembrava dallo scan statico originale). Trovata anche una quarta variante
mai documentata, `CoworkerMeteoriteHunter`, oltre alle 3 note (`DragonWithBeer`,
`FemaleDragonWithBeer`, `MeteoriteHunter`). Ognuno dei 4 container è esso stesso un bottone cliccabile
reale - click diretto, nessun trucco necessario. Implementato come `BotAction` indipendente (non uno
scheduled task) con poll ogni 2s di default, dato che questi bonus sono visibili solo per pochi
secondi. **Testato su Steam-0: avvio pulito, nessun errore sui path una volta caricata la scena
battaglia** (solo "path broken" nei primissimi secondi, prima che la scena finisse di caricare - atteso).
Un controllo diagnostico temporaneo ha causato un crash dell'intero bot (~12 minuti offline) durante
lo sviluppo - rimosso, vedi `PLAN.md` per i dettagli. **Da confermare dall'utente**: che il click
sui bonus rilevati corrisponda davvero alla ricompensa reclamata in gioco (nessun modo di verificarlo
dai log, dato che i click riusciti non vengono loggati). Abilitato e testato solo su Steam-0, non
ancora esteso alla flotta.
