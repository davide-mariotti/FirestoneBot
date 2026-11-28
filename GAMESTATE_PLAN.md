# Piano: il bot legge e naviga dal codice del gioco, non dallo schermo

## LEGGI PRIMA: prompt per una nuova chat (aggiornato il 28/11 mattina)

Se l'utente scrive "LEGGI GAMESTATE_PLAN.md", parti da qui. Scrivi all'utente **sempre in italiano**.

**Il lavoro.** Repo `C:\Repos\FirestoneBot`, branch `game-state` (non è stato fatto il merge su main). Il
bot decide dai dati del gioco (handler IL2CPP da `GameInitialize.HandlerLoader` / `MenuLoader`, in
codice `GameState.H`) invece di leggere lo schermo. Fai commit e push dopo ogni passo verificato, e
aggiorna questo file con quello che fai e coi numeri visti nei log.

**Regole che non cambiano:**
- solo letture di dati e le stesse chiamate della UI del gioco (`OpenMenu`, `CloseLastOnMenuStack`,
  l'`onClick` dei suoi pulsanti, getter puri); mai scrivere campi del gioco, mai funzioni di rete o
  `*Callback` / `*AskServer`;
- ogni lettura passa da `GameState.Get` / `GetRef`, con lo schermo come riserva;
- un dato nuovo prima si confronta con lo schermo e lo si usa solo quando coincide; se un errore costa
  una spesa, il task si fida dei dati solo dopo una visita in cui coincidevano;
- una spesa conta solo quando la valuta (o la quantità) scende, e un'icona di prezzo diversa ferma il
  click;
- non toccare: i doni di gilda (`ClaimMemberRewardsFromGuildItems`, l'utente deve ancora rispondere),
  Frostfire;
- scelte di gioco dell'utente (28/11): **Midas Touch** è scarso e prezioso (il Merchant non lo vende più,
  si usa solo dove serve davvero); **Strange Dust** solo per guardiani ed eroi, **niente Alchimista**;
  Meteorite Research resta com'è; l'Empower segue la regola della crescita.

**Dove gira.** Dal 28/11 alle 03:53-04:11 il branch gira su **tutte le istanze Steam-17..31 di
PC-Guesly** (questo PC, box Sandboxie `C:\Sandbox\dmari\SteamB<N>`), su richiesta dell'utente.
DESKTOP-ADMIN e main non sono stati toccati: un deploy di main deve saltare 17-31. DLL di main per
tornare indietro: `C:\Repos\FirestoneBot-test-backup\2026-11-28-fleet-main`.
- Build: `$env:COMMON_DIR="C:\Program Files (x86)\Steam-17\steamapps\common"; dotnet build C:\Repos\FirestoneBot\src\firebot.csproj -c Release -p:Il2CppAssembliesDir="C:\Sandbox\dmari\SteamB27\drive\C\Program Files (x86)\Steam-27\steamapps\common\Firestone\MelonLoader\Il2CppAssemblies"`;
  test: `dotnet test tests\Firebot.TalentEngine.Tests` (97 al 28/11).
- Deploy di un'istanza, che la riavvia da solo (controlla sempre che dica "up"):
  `powershell -ExecutionPolicy Bypass -File tools\Restart\deploy_test.ps1 -Instance N`, con
  `-ResetTasks a,b` per far ripartire subito dei task e `-Set "sezione.chiave=valore"` per scrivere voci
  del cfg a gioco fermo (aggiunge sezione e voce se mancano). Per la flotta: un'istanza alla volta.
- Log: `C:\Sandbox\dmari\SteamB<N>\drive\C\Program Files (x86)\Steam-<N>\steamapps\common\Firestone\MelonLoader\Latest.log`
  (e `Logs\`), da leggere in condivisione (`[IO.File]::Open(p,'Open','Read','ReadWrite')`). Da
  guardare: `[Task] X finished in`, `[FAILED] Task`, `[GameState] mismatch` / `fallback` (noti e
  innocui: `research state` KeyNotFound '0', `weekly quests renew`, i sigilli di Hall of Heroes al primo
  giro di un'istanza), le due eccezioni di MelonLoader a ogni avvio ("Unable to get process modules",
  "SetAsLastSibling").

**Cosa è nuovo dal 28/11 notte e va verificato** (dettaglio più sotto in "Fatto il 28/11"):
1. **Empower, regola della crescita** (default `[empowertask] empower_rule = "growth"`): al muro, col
   rapporto raggiunto, aspetta finché la crescita del banco degli ultimi 5 minuti batte la media della
   run; al massimo 90 minuti dopo il muro. Primi Empower veri: -22 alle 04:19, -20 alle 04:25, -28 alle
   04:32 ("0.40%/min vs 0.41% - would empower"). Da misurare: durata delle run e crescita del banco
   per ora, rispetto alla regola vecchia.
2. **Stage di farm al muro** (AutoRetreat): misura l'oro della run al minuto
   (`SpecialGameStatistics.save.totalGoldThisRun`) 2 minuti per stage dal muro in giù e farma il
   migliore. -27 alle 04:25: 1037 10^547,71, 1036 10^547,99, **1035 10^548,17**, 1034 10^548,16. Righe
   `[AutoRetreat] Farm sample` / `Farming stage`.
3. **Prova della squadra su -28** (`[formationtask] scoring = "abilities"` solo lì, dalle 02:55): Muriel
   leader con Boris, Talia, Solaine e Astrid al posto di Cirilo, Luana, Benedictus e Burt. Muri di -28
   prima: 999, 1005, 1006, 1006, 1007, 1011; con la squadra nuova entrata a metà run: 1012. Il
   confronto vero sono le run intere dopo le 04:32. Se il muro sale chiaramente più del solito (+0-6 a
   run), applicarla alla flotta (`-Set formationtask.scoring=abilities`, poi `-ResetTasks formationtask`)
   e far investire Hall of Heroes sul carry, non sui buffer.
4. **Push giornaliero** (`DailyPushTask`, spento di default, acceso solo su -27): stato in "Push
   giornaliero: stato" qui sotto.
5. **Merchant:** non vende più Midas' Touch.

**Push giornaliero: stato** (28/11 04:40): vedi la sezione "Fatto il 28/11 alle 04:40" più sotto.

---

> Scritto il 2026-11-26 da un'analisi con REA della build del 06/11 (metadata IL2CPP v31), rivisto lo
> stesso giorno su richiesta dell'utente:
> - tutte le chiamate migliorabili, non solo i timer: lo scopo è un bot leggerissimo e velocissimo;
> - si possono usare anche le funzioni del gioco che agiscono (vedi "Regole"): ACTk non le vede;
> - si lavora su un branch, si prova **solo su Steam-27** per 12 ore, poi tutta la flotta e merge su
>   main.
>
> I nomi e i tipi dei campi e dei metodi sono **osservati** nei metadata. Il significato (unità dei
> timer, quando si aggiornano, quale menu apre un numero di sottomenu) è **dedotto dai nomi**: la
> fase 0 lo verifica prima di scrivere il resto.

## Per una nuova chat: come continuare

Questa sezione basta per riprendere il lavoro senza la conversazione che l'ha prodotto. Il resto del
file è la storia e il dettaglio.

**Cosa si sta facendo.** Il branch `game-state` porta il bot a decidere dai dati del gioco (gli handler
IL2CPP raggiungibili da `GameInitialize.HandlerLoader` e `GameInitialize.MenuLoader`) invece che
leggendo testi e pulsanti dallo schermo. Lo schermo resta solo dove è indispensabile: il click
dell'azione e, come riserva, quando un dato non si legge. La richiesta dell'utente: "quello che si può
fare da codice facciamolo".

**Regole che non cambiano** (dettaglio in "Regole" e "Cosa non fare"):
- solo letture di dati e le stesse chiamate della UI del gioco: `NavigationControl.OpenMenu`,
  `CloseLastOnMenuStack`, l'`onClick` dei suoi pulsanti, getter puri;
- mai scrivere campi del gioco, mai chiamare funzioni di rete (`SocketAPI`, `ServerCalls`, `*AskServer`,
  `*Callback`);
- ogni lettura passa da `GameState.Get` / `GetRef`, che in caso di errore scrivono
  `[GameState] fallback ...` una volta per sessione; il chiamante allora torna allo schermo;
- un dato nuovo si confronta prima con lo schermo (`GameState.Compare` / `CompareTimes`, righe
  `[GameState] mismatch ...`) e lo si usa solo quando coincide. Sigilli di Hall of Heroes e rarità degli
  eroi lo fanno da soli, con una chiave in config (`seals_seen`, `rarity_checked`);
- la spesa di una valuta si conta solo quando la valuta scende davvero (dato del gioco), e un'icona di
  prezzo diversa da quella attesa ferma il click.

**Dove gira.** PC-Guesly, istanze nelle box Sandboxie `C:\Sandbox\dmari\SteamB<N>`.
- Il branch gira su **Steam-27** (dal 26/11) e su **Steam-28** (dal 27/11 12:01). **Steam-26 resta su
  main** come confronto. Le DLL di main per tornare indietro sono in
  `C:\Repos\FirestoneBot-test-backup\2026-11-26-steam27` e `...\2026-11-27-steam28`.
- Finché il test non è chiuso, un deploy di main sulla flotta salta Steam-27 e Steam-28.
- L'altro PC (DESKTOP-ADMIN) è tutto su main.

**Comandi.**
- Build contro gli assembly della box (il gioco di -27 e -28 è la stessa build, GameAssembly.dll del
  06/11 11:27):
  `$env:COMMON_DIR="C:\Program Files (x86)\Steam-17\steamapps\common"; dotnet build C:\Repos\FirestoneBot\src\firebot.csproj -c Release -p:Il2CppAssembliesDir="C:\Sandbox\dmari\SteamB27\drive\C\Program Files (x86)\Steam-27\steamapps\common\Firestone\MelonLoader\Il2CppAssemblies"`
- Test dei pianificatori: `dotnet test tests\Firebot.TalentEngine.Tests` (96 al 27/11).
- Deploy su un'istanza di test, con riavvio immediato (l'utente vuole sempre il riavvio fatto da noi,
  mai lasciato al watchdog):
  `powershell -ExecutionPolicy Bypass -File tools\Restart\deploy_test.ps1 -Instance 27` (senza
  `-ExecutionPolicy Bypass` Windows blocca lo script). Con
  `-ResetTasks treeoflifetask,beaststask` i task elencati partono subito; con `-FullRead` anche la
  lettura completa di Hall of Heroes.
- Log: `C:\Sandbox\dmari\SteamB<N>\drive\C\Program Files (x86)\Steam-<N>\steamapps\common\Firestone\MelonLoader\Latest.log`,
  e `Logs\` per le sessioni precedenti. Il gioco li tiene aperti: vanno letti in condivisione.
  - `[Task] X finished in Ns` dà la durata di ogni giro;
  - `From the game: ... - not opening` è un giro deciso dai dati;
  - `[GameState] mismatch` / `fallback` sono da guardare;
  - lo stato salvato dei task è in `...\UserData\FirebotPreferences.cfg`, nella box.
- Dump dell'API del gioco: `tests\GameApi\CoreASM.dll.api.txt` (e `SharedASM`), gitignored e non da
  committare (il repo è pubblico). Ci sono nomi e tipi di campi e metodi, non i tipi delle proprietà:
  per quelli si compila una riga di prova e si legge l'errore del compilatore. Un membro statico va
  chiamato col nome del tipo, `Il2Cpp.X.membro` (il compilatore lo dice con CS0176).

**Il test e il merge.** Si chiude con 12 ore senza problemi dall'ultimo deploy. I criteri sono in
"Criteri per estendere alla flotta". Se passa, il punto 7 del piano: togliere `GameStateProbe`, merge
`--no-ff` su main, deploy alla flotta su entrambi i PC, azzerare lo stato dei task cambiati (memoria
`reset-task-state-on-rollout`: i `next_run_time_internal`; per `hallofheroestask` anche
`seals_seen = "-"` e `rarity_checked = ""`), aggiornare README e TESTING.md. I controlli orari e la
valutazione erano cron della sessione: una chat nuova li deve rifare, o controllare a mano.

**Fatto e verificato dal vivo** (dettaglio e numeri in "Stato"):
- badge, timer e navigazione dal gioco;
- decidono dai dati: ricerche, esperimenti con l'accelerazione gratuita, spedizioni, missioni, Empower,
  Oracle's Gift, mail, Path of Glory, eventi, Quests, Personal Tree, Forbidden Knowledge, Bestie
  (livelli), guardiani (evoluzione, Holy, addestramento), Pharaoh's Vault, Awakening, War Machine
  Rarity, Daily Missions;
- Hall of Heroes: sigilli e lettura completa dai dati, 13 eroi su 13 identici allo schermo su -27 e
  -28;
- i saldi letti da `CurrencyHandler` invece che dai contatori.

**Nuove funzioni del 27/11, già viste dal vivo su -27 e -28:**
- **soul stone** (Hall of Heroes, `SpendSoulstones`): il prezzo è in Soul Shards (icona `soulShard64`),
  30 al livello 1, tetto 2 × la rarità come per il gear. Al primo giro 40 incantesimi per istanza, il
  massimo per giro: Soul Shards da 2620 a 1270 su -27 e da 2590 a 1270 su -28;
- **rarità delle Bestie** con le Cobra Keys (`BeastsTask.RaiseRarities`; l'icona si chiama
  `snakeKey64`): GoldenMonkey su -27, chiavi da 34 a 19;
- **upgrade degli artefatti** (`AncientArtifactTask.Raise`): la rarità con le Golden Keys, i livelli con
  le Twilight Hourglass, decisi da `CanIncreaseRarity` / `HaveTheKeysToRarityUp` / `CanIncreaseLevel` /
  `HaveTheTwilightHourglassToLevelUp`. La schermata si apre solo dal pulsante `actionButtons/artifacts`
  della Tavern: la navigazione del gioco non la apre, e chiamata dalla Tavern chiude la Tavern. Su -27
  1 rarità e 14 livelli, su -28 2 rarità e 25 livelli (le valute finiscono quasi a zero: si spende
  tutto quello che il gioco permette).

**Fatto il 27/11, da vedere al prossimo giro vero:**
- rarità degli eroi saltata per gli eroi a cui i dati dicono che manca un requisito (`rarity_checked`):
  al giro delle 12:43 i requisiti dei dati coincidevano con le schede;
- quest giornaliere (Collector, Gamer, Merchant, Miner) dai dati (`CharacterScreen.DailyQuestFromGame`):
  il primo giro vero è al reset del 28/11; la riscossione resta a QuestsTask;
- addestramento dei guardiani: decide dai dati se aprire (alle 12:44 non ha aperto su tutte e due,
  addestramento in corso e Strange Dust alla riserva).

**Fatto il 27/11 alle 13:28** (deploy su -27 e -28):
- **Map Missions:** un giro raccoglie tutte le missioni le cui finestre gratuite si sovrappongono. Prima
  due missioni a 2 minuti l'una dall'altra costavano due giri da 7 s. Senza squadre libere e senza
  missioni nella finestra non apre la mappa (alle 13:25, 0,1 s);
- **Collector Quest:** conta i forzieri dai dati (`GearChestHandler`, `JewelChestHandler`,
  `CelestialChestHandler`, più Mystery Box e Oracle's Gift come valute). A quest fatta e senza niente
  oltre la riserva non apre l'inventario;
- **Arena of Kings:** i token da `ArenaOfKingsMech.battleTokensCurrent` (la mia potenza resta letta dal
  testo: non ho trovato un dato pulito);
- **Talenti:** non apre se `TalentsHandler.availablePoints` è 0. Il livello del personaggio, che ogni
  task controlla a ogni giro, viene da `HandlerLoader.CharacterLevel.level`;
- **Chaos Rift:** non apre se il dio non si può colpire (`ChaosRiftHandler.CanAttackGod` e
  `ChaosRiftMech.CanAttack(1)`) e se le Dark Runes non bastano per un tomo. Il prezzo vero del tomo
  (17839, cresce con ogni tomo comprato) lo dà `ChaosRiftShopSuppliesInteraction.GetTomeExchangeCostMultiple()`,
  che però vale 0 finché il negozio non è stato aperto: il bot ricorda quello visto all'ultima visita,
  quindi dopo ogni riavvio c'è una visita (13 s) per impararlo.

**Fatto il 27/11 alle 15:00** (deploy su -27 e -28, tutto con lo schermo come riserva):
- **Oracle Blessings** non apre se nessuna benedizione di `OracleHandler.blessings` ha `CanLevelUp` /
  `hasAvailableUpgrade`;
- **Pirate's Prize** non apre se `PirateShipHandler.GetPiratePrizeUnclaimedRewards()` è 0;
- **Beer Exchange** non apre senza il badge BeerExchange letto dai dati, cioè il controllo del gioco
  stesso (`NotificationHandler.BeerExchangeCheck`);
- **Scarab Game, token gratuito**: `ScarabGameHandler.DailyFreePharaohTokenAvailable`;
- **Daily Store Offers** non apre senza i badge CheckIn o MysteryBox letti dai dati;
- **Free Pickaxes**: il conteggio da `GuildShopSuppliesSubmenu.freePickaxeState` (generati meno
  riscossi) si confronta con il negozio a ogni visita; sotto la soglia salta l'apertura dopo una visita
  che ha confermato la coincidenza;
- **War Machines** (ferme fino all'albero a 50): non apre con meno di 500 Expedition Tokens;
- **Arena of Kings**: anche la mia potenza dai dati (`WarMachineHandler.myFormationArenaPower`);
- **Merchant Quest**: nome e quantità degli oggetti in vendita da `ExoticSellItemInteraction`
  (`itemName`, `quantityOwned`), non dai testi; il click di vendita resta;
- **Collector Quest**: le quantità per casella dai dati (`ChestOpening.CountFromGame`);
- **stage dell'AutoRetreat**: `MainBattle.CurrentStageNumber`;
- **sfide degli eventi**: i codici del gioco (`eventChallengesProgress` con
  `CalendarEventsDataHandler.challengeRequirements`, `MiniEventsHandler.activeChallenges`) tradotti
  nei tipi del bot (`GameState.CalendarChallenges` / `MiniChallenges`). A ogni visita si confrontano con
  le carte; quando coincidono, senza sfide da riscuotere o da fare e senza valuta dell'evento in più
  (Decorated Heroes, Trick or Treat), l'evento non si apre;
- **Formation** apre Party solo per cambiare eroi (prima ogni giorno, anche in dry run), e **Hall of
  Heroes** prende la formazione da `Heroes.Read()` invece di aprire Party a ogni giro;
- **bottino della campagna Warfront**: si riscuote ogni 6 ore dall'ultima riscossione
  (`WFCampaignMissionHandler.lastLootClaimTimestamp` + 6 h coincideva al secondo con il conto alla
  rovescia dello schermo, su -27 e -28): prima di quell'ora non apre.

**Fatto il 27/11 alle 15:47** (deploy su -27 e -28 alle 15:35-15:47, misurato sui log 11:28-15:20):
- **Spedizioni:** la fine viene dal gioco (`expeditionData.activeExpeditionStartTime` + `activeExpedition.timeReq`),
  non dal conto alla rovescia dello schermo, che finiva qualche secondo prima: su -27 alle 12:45 e alle
  13:32 il bot ha aperto con il claim ancora spento ed è tornato 2 minuti dopo (13:32:14, fine vera
  13:32:15). Con una spedizione in corso non apre prima della fine (+2 s). Senza spedizioni il
  rinnovo della lista resta quello dello schermo (17:13:02, il dato non c'è). Al rinnovo: su -27 alle
  17:13 ExploreTheCatacombs, fine del gioco 18:06:07 contro 18:06:08 dello schermo; su -28 alle 17:14
  TrainTheRecruits, 18:14:11 tutti e due. Il claim a fine spedizione, visto dal vivo: -27 alle 18:06:12
  ha riscosso ExploreTheCatacombs e avviato CraftABow (fine 18:53:14 sia dal gioco che dallo schermo),
  -28 alle 18:14:16 TrainTheRecruits e avviato ExploreTheCatacombs (19:07:18 dal gioco, 19:07:19 dallo
  schermo). 3,1 s ognuno, nessun "Click ignored", nessun giro a vuoto dopo.
- **Badge GuardianTraining "non visto":** è il flag `guardianTrainingNotificationSeen` del gioco, in
  memoria, che ogni riavvio rimette a falso. Da quando Guardian Training decide dai dati (12:44) il
  badge restava acceso fino alla fine dell'addestramento: su -27 e -28 per tutta la sessione 13:29-14:56.
  Ora, se il badge è acceso solo per quello, il task usa la scorciatoia del badge e chiude (come Seen
  Badges, senza cliccare niente dentro): 1,6 s, badge spento, alle 15:39, 15:47 su -27 e 15:48 su -28.
  Il giro completo di prima costava 10,8 s.
- **BadgeCooldown:** i 30 minuti valgono solo se il badge del gioco era ancora acceso alla fine del
  giro; un badge che si accende dopo aspetta 2 minuti. Su -27 Quests aveva deciso "niente da
  riscuotere" alle 13:33:55, una quest si è completata subito dopo e il badge è rimasto acceso fino alle
  14:04. Per i badge che il gioco non sa dire (posta, Path of Glory, eventi) resta tutto come prima.
- **Map Missions, mappa vuota:** non apre se nessuna missione in corso è finita o nella finestra
  gratuita e le squadre libere non hanno missioni da avviare (`missionsOnMap` con `squadsReq` ≤ libere;
  il conteggio coincideva con lo schermo su 5 giri, 12/10/8/6/1). Torna al primo tra fine missione e
  rinnovo (`TimeLeftForNewMissions`: 17:13:07 contro 17:13:03 dello schermo). Su -27 alle 15:48: 6
  squadre libere, mappa vuota, 0,1 s invece di 2,6 s.
- **Map Missions, attesa dopo il click su una missione:** il secondo fisso è diventato l'attesa
  dell'anteprima di quella missione (`PreviewMissionPopup.mapMission`, menu `PreviewMission`) più 0,2 s.
  La sonda: 0,89 s la prima volta (caricamento), 0,10 s le successive. Su -28 alle 15:49: 1,19 s e
  0,40 s; giro con uno speed-up e un avvio in 6,7 s, contro 7,3 s alle 15:43. Al rinnovo delle 17:13
  su -27: 4 missioni avviate (squadre 6 → 0) in 7,2 s, attese 1,19 / 0,40 / 0,40 / 0,40 s.

Minuti di task per ora dalle 15:48 alle 17:06 (78 minuti, senza deploy in mezzo): **-27 0,74, -28 1,73
(8 giri di Map Missions, 56 s), -26 su main 6,16** (Meteorite 220 s, System Mail 74 s, Empower 73 s).
Badge in quelle ore: su -27 solo TemplePrestige (16:17-16:25, la regola dell'Empower); su -28 i badge
del riavvio spenti entro il minuto e TemplePrestige 15:48-15:58. Nessun task ripartito a ripetizione
con il nuovo `BadgeCooldown`.

Gli eventi non attivi (Mini, Trick or Treat, New Player su -27) e Decorated Heroes finito fanno una
visita a sessione e poi una ogni 6 ore (`_lastVisit` in memoria), 7-8 s l'una: le visite delle 16:04
venivano dai deploy delle 15:35-15:46, quelle delle 17:08 dal riavvio programmato delle 17:06, e alle
17:04 su -27 il giro ha detto "not running - not opening" (0,1 s). Con un riavvio ogni 6 ore sono ~3,5
s/h per account: lasciato com'è, è la verifica che il dato "non attivo" sia giusto.

Dopo ogni riavvio Experiments fa un giro da 3,3 s senza niente da fare: il badge è il "non visto" degli
slot fermi (`strangeDustExperimentSeen`, `exoticCoinExperimentSeen`; il bot usa solo il Dragon Blood),
e aprire l'Alchemist è il modo di spegnerlo. Costa quanto una visita di Seen Badges: lasciato com'è.

Un evento del calendario finito (Decorated Heroes alle 10:00 del 27/11) tiene la carta aperta per il
suo negozio: i dati dicono "non attivo" e non ha più sfide, quindi lo si visita ogni 6 ore per spendere
la valuta rimasta. Il confronto delle sfide si fa solo a evento in corso, e quello di "attivo" segnala
solo il caso sbagliato davvero (dati "attivo" e carta assente). Per le sfide del calendario la regola
dei livelli (`c` livelli riscossi su `challengeRequirements`) è ancora da confermare con il prossimo
evento in corso: finché carte e dati non coincidono, le sfide non decidono niente.

**Fatto il 27/11 alle 20:43** (deploy su -27 alle 20:42 e -28 alle 20:43, richieste dell'utente:
Empower, Talents, Enlighten, Ancient Artifact, ordine delle missioni, addestramento, il resto dei testi):
- **Talents:** punti disponibili e spesi da `TalentsHandler` (`availablePoints`, `usedTalentPoints`),
  il contatore solo come riserva e come confronto al primo giro (`talent points`). Il probe di
  `usedTalentPoints` coincideva con il "speso" del contatore su tutti i giri (-27 225/226, -28 223/224).
- **Guardian Training:**
  - Train solo senza addestramento in corso (`TrainingEnd`): prima un click a vuoto a ogni giro che
    apriva per l'Enlighten;
  - l'Enlighten in blocco prova solo i passi che la dust paga sopra la riserva (`enlightenmentDustReq` ×
    passo) e che il gioco offre (`enlightQuantitiesAvailable`): su -28 alle 20:26 il giro non ha
    illuminato niente e ha premuto il pulsante delle quantità per ~8 s;
  - il prezzo mostrato viene dal gioco (`enlightenmentDustReq` × `currentEnlightQuantity`) dopo un
    confronto col testo che coincide, nella stessa sessione (un prezzo sbagliato spende sotto la
    riserva); fino ad allora il testo.
- **Ancient Artifact:** se dopo un livello il popup AAModify resta aperto sullo stesso artefatto e
  modo, il livello seguente è un altro click del suo pulsante (prima ~2,6 s a livello: seleziona,
  pulsante, popup, conferma, chiudi). Se si chiude, tutto come prima. Il log dice quanti livelli sono
  passati dal popup aperto e quanto è durato il giro.
- **Map Missions:** l'ordine delle missioni da avviare viene dalla durata del gioco
  (`MapMissionInteraction.mapMission.realTimeReq`), il testo del pin solo come riserva, confrontati
  missione per missione (`map mission duration`). Lo stato di ogni pin (libera, in corso, finita) dai
  dati (`missionState` e la fine) è per ora solo confrontato con le icone (`map mission state ...`):
  sbagliarlo lascerebbe una squadra ferma.
- **Oracle's Gift:** il numero di doni dalla valuta `OraclesGift` (come Collector Quest), il contatore
  come riserva e confronto.
- **Miner Quest:** la quantità per colpo del cristallo (`ArcaneCrystalMech.currentHitQuantity`) è solo
  confrontata col testo, perché sbagliarla spende piccozze.
- **Empower:** nessun cambio di regola. Il log `[Empower] curve` dice anche cosa farebbe la regola
  "della crescita" (vedi sotto, "Empower: cosa dicono le curve").
- **Probe `skips`** (una riga quando cambia): gli upgrade Skip dell'Exotic Merchant partono dallo
  stage 1200 (Tree2), sopra il muro di -27 e -28 (~1031), quindi oggi non servono. Le ricerche:
  Firestone Skip Wave 25/25 (25%) e Skip Stage 20/20 (6%), al massimo; Meteorite Skip Wave livello 10
  (3%) e Skip Stage livello 10 (2%), 800 meteoriti a livello, ~0,2-0,3% a livello.

**Empower: cosa dicono le curve** (`[Empower] curve` e `[Probe] stage` di -27 e -28, 27/11):
- Una run è ~137 minuti di salita a ~7,5 stage al minuto dallo stage 1 al muro (1024 → 1027 → 1031 su
  -27), poi il muro. Il ritmo è lo stesso dall'inizio alla fine: lo limita il gioco, non il danno.
- I Firestones trovati vengono dall'oro della run (`Firestones.firestoneTotalGoldEarnings`,
  `goldForTheFirstFirestone`). Durante la salita crescono di ~0,4 ordini di grandezza al minuto; al muro
  l'oro continua ad arrivare e crescono ancora del 7-11% al minuto (-27: 10^219,38 → 10^219,52 dalle
  18:43 alle 18:48, 10^218,70 → 10^218,93 dalle 16:22 alle 16:27).
- Il bot fa l'Empower 3-5 minuti dopo il muro. In quel momento il "banco" cresce del 5-6% al minuto
  (crescita dei trovati × trovati/(posseduti+trovati)), mentre la media della run è 1,1-3%: aspettare
  al muro rende più che ripartire. Una stima con l'oro lineare al muro dà l'ottimo dopo ~30 minuti di
  muro, con il banco che cresce ~25% più in fretta per ora; da verificare dal vivo.
- Visto dal vivo con la riga nuova, -27 alle 21:15 (muro a 1032, stallo alle 21:11:54): trovati da
  10^219,75 a 10^219,98 in 5 minuti, "last 5 min 6.82%/min vs run average 0.83%/min - would wait". Il
  bot ha fatto l'Empower lo stesso, con rapporto 2,27, 3 minuti dopo lo stallo. Dopo il ritiro il gioco
  è in modalità farm (`MainBattle.FarmingWave` True allo stage 1028).
- Il rapporto all'Empower scende run dopo run (-27: 70 → 15 → 3,7 → 2,3; -28: 24 → 11,7), perché il
  muro sale poco (1024 → 1027 → 1031 → 1032). Sotto 1 la regola di oggi aspetta al muro che l'oro della
  farm porti i trovati ai posseduti: allunga da sola il muro, ma solo quando il rapporto è basso.
- In metà delle run i trovati partono già dal ~15-19% dei posseduti e restano fermi fino al minuto
  ~105 (es. -27 18:53-20:24, 10^218,77); nelle altre partono da 10^164-169 e salgono subito. Al muro
  le due curve si equivalgono: per la regola non cambia niente.
- La regola "della crescita": al muro, Empower quando la crescita del banco nell'ultimo controllo
  scende sotto la media della run (`ln(1 + trovati/posseduti) / minuti`). Si regola da sola e non ha
  bisogno che il rapporto arrivi a 1. Il badge TemplePrestige resterebbe acceso più a lungo. Da
  decidere con l'utente; finché non decide, la riga del log dice solo "would empower" / "would wait".

**Fatto il 27/11 alle 21:36** (deploy su -27 alle 21:35 e -28 alle 21:36):
- **Map Missions:** lo stato dei pin (libera, in corso, finita) viene dal gioco, le icone come riserva e
  confronto. Coincidevano su tutti i pin dei 5 giri delle 21:06-21:33 (4 su -27, 1 su -28), e così le
  durate: l'ordine 41 → 17 minuti su -27, 50 → 17 su -28.
- **Empower:** la regola "della crescita" c'è, spenta: `[empowertask] empower_rule = "ratio"` (oggi) o
  `"growth"`. Con `growth`, al muro e col rapporto raggiunto, il bot aspetta (un controllo ogni 5
  minuti) finché la crescita dell'ultimo controllo batte la media della run; dopo un riavvio al muro
  aspetta un controllo per avere il confronto. La riga `[Empower] curve` la valuta comunque.
- **Probe:** `empower` ha anche l'oro della run (`runGold`, da cui vengono i trovati); `abilities` le
  abilità degli eroi schierati (nome, risorsa, durata, ricarica, descrizione del gioco), una riga
  quando cambiano.

**Visto nella notte 27-28/11** (dal deploy delle 21:36):
- Enlighten in blocco (23:07 su -27 e -28): 4 illuminazioni in 5,5 s contro 12,8 s, prezzo del gioco
  uguale al testo; i giri di solo Train delle 01:13 / 01:24 3,3-3,5 s.
- Talents (-28 alle 00:54): 1 punto investito, punti del gioco uguali al contatore.
- Empower: -27 alle 23:32 e 01:53 (muro 1033 tutte e due), -28 alle 01:23 (muro 1011); ogni volta
  "would wait" (al muro 2,3-5,8%/min contro 0,54-0,59% di media). Il rapporto all'Empower di -27 è
  sceso a 1,05 e 1,29.
- Nessun task fallito; solo il fallback noto `research state`.

**Fatto il 28/11 alle 02:53** (deploy su -27 alle 02:52 e -28 alle 02:53, richieste dell'utente):
- **Empower:** la regola della crescita è quella di default (`empower_rule = "growth"`, scelta
  dall'utente; su -27 e -28 scritta nel cfg con `deploy_test.ps1 -Set`). Aspetta anche mentre AutoRetreat
  misura gli stage di farm, e al massimo 90 minuti dopo il muro.
- **Muro, stage di farm:** al posto dei 5 stage fissi AutoRetreat misura l'oro al minuto (l'oro della
  run, `SpecialGameStatistics.save.totalGoldThisRun`) per 2 minuti al muro, poi uno stage sotto alla
  volta finché rende di più (al massimo `retreat_stages`), e torna al migliore. È quello che l'utente
  fa a mano col tooltip del Barrel of Gold: il gioco ne calcola il valore con
  `OfflineCalculations.CalculateInventoryItemGold` (oro dell'ondata / tempo per passarla), ma solo
  per lo stage attuale e non è una lettura pura, quindi si misura.
- **Formazione, prova su -28:** `[formationtask] scoring = "abilities"` aggiunge al danno quello che le
  abilità di ogni eroe moltiplicano il danno della squadra (buff di squadra, debuff sul boss: tabella in
  `FormationPlanner.AbilitySupport`), cercando tra tutte le squadre da 5. Il default resta `damage`.
  Alle 02:55 su -28: Cirilo, Luana, Benedictus, Burt → Boris, Talia, Solaine, Astrid, leader Muriel,
  "as planned", ×3,55 su quel punteggio. I muri di -28 prima: 999, 1005, 1006, 1006, 1007, 1011 (+0-6
  a run); quelli di -27: 1027, 1031, 1032, 1033, 1033. Il cambio è a metà run (minuto 92): gli eroi
  nuovi partono con meno livelli d'oro.
- `deploy_test.ps1 -Set "sezione.chiave=valore"` scrive voci del cfg a gioco fermo.

**Formazione: cosa dicono i dati** (`Formation heroes` e `[Probe] abilities`, 27/11):
- Il danno è quasi tutto di un eroe. -27: Talia 10^184,49 (leader, +0,13), Boris 10^183,38 (~5%), gli
  altri tre ~10^181,3, 0,05% l'uno. -28: Muriel leader con fattore 10^2,81, gli altri quattro insieme
  ~0,1%.
- Quindi negli slot che non sono il "carry" conta solo quello che l'eroe fa alla squadra o al boss,
  non il suo danno. Il planner sceglie i 5 col danno più alto, le abilità non sono nel modello
  (FORMATION_PLAN.md, "Stato").
- Le abilità dal gioco: Boris "Anger" +50% al danno della squadra per 25 s, ricarica 25 s; Talia
  "Annihilation" +60% danni subiti dal bersaglio per 8 s ogni 15 s; Astrid "Devastating Shot" +60% per
  7 s, ricarica 1,84 s; Muriel "Rally" +15% velocità d'attacco della squadra. -27 ha già Boris, Talia e
  Astrid. -28 ha Muriel, Cirilo, Luana e Benedictus (healer) e Burt, con Boris, Talia e Astrid in
  panchina: con i loro buff il danno sul boss sarebbe circa ×3 (stima, uptime e cumulo da verificare).
- Proposta per l'utente: tenere il carry (e il leader) per danno e scegliere gli altri slot per i
  moltiplicatori che danno, dalle abilità del gioco (durata/ricarica). È una scelta di gioco.

Già dai dati senza bisogno di modifiche: **Warfront Campaign** (potenza, missioni e requisiti dal
gioco, apre solo per una missione da tentare o per cambiare l'equipaggio) e **Blueprints**
(`BlueprintPlanner` dai dati, non apre senza niente da comprare).

**Guardati e lasciati come sono:**
- Seen Badges: i badge "visto" li riaccende ogni riavvio, perché il gioco tiene i flag solo in memoria, e
  scriverli è vietato; costa 3 s al riavvio;
- Expeditions, Experiments, Research, Map Missions, Oracle Rituals, Quests: decidono già dai dati;
  quando aprono è per l'azione, che resta un click.

**Prossimi candidati** (dati già trovati):
- il ritiro degli oggetti distribuiti dalla gilda (`ClaimMemberRewardsFromGuildItems`, Banca di Gilda →
  Tesoreria): l'utente deve ancora dire se la sua gilda li distribuisce, non toccarlo prima;
- la regola dei livelli delle sfide del calendario (`CalendarEventChallengeData.c`), da confermare col
  prossimo evento in corso: oggi le carte e i dati non sono mai stati confrontati a evento attivo;
- Frostfire, routine di push con le pergamene e vendita delle pergamene: in pausa per scelta
  dell'utente.

## Come cercare altre ottimizzazioni

Il metodo usato il 26-27/11, passo per passo.

1. **Dove va il tempo.** Dai log delle istanze di test (Steam-27, Steam-28) e di Steam-26 su main, la
   durata di ogni `[Task] X finished in Ns`, raggruppata per task: numero di giri, totale, media. I giri
   da 0,1 s sono già decisi dai dati. Contano quelli che aprono una schermata e poi non fanno niente, o
   che leggono molti testi.
2. **Cosa legge dallo schermo.** Nel codice del task e del suo modello (`src/GameModel/Features/...`):
   `new GameText(...)` (testi), `IsVisible()` / `IsClickable()` usati per decidere, scansioni di liste
   sullo schermo. `grep -rn "new GameText(" src` le trova tutte (73 al 27/11, molte ora solo riserva).
3. **Il dato del gioco.** In `tests\GameApi\CoreASM.dll.api.txt` (e `SharedASM` per gli enum):
   - `grep -n "^type NomeTipo$"` dà campi e metodi;
   - `HandlerLoader` (`GameInitialize.HandlerLoader`, in codice `GameState.H`) e `MenuLoader`
     (`GameInitialize.MenuLoader`) espongono gli handler e i menu come `prop`;
   - le valute sono `CurrencyCode` (`GameState.Currency(code)`);
   - i badge sono `NotificationType` (`GameState.HasBadge`).

   Il dump non ha i tipi delle proprietà: si compila una riga di prova (`int x = H.Qualcosa.prop;`) e
   l'errore del compilatore dice il tipo, poi si toglie la riga. Un membro statico va scritto
   `Il2Cpp.Tipo.membro` (errore CS0176 altrimenti).
4. **REA, se il nome non basta.** REA (npm `rea-agents`, skill `reverse-engineer-anything`, MCP `rea`)
   rifà il dump dopo un aggiornamento del gioco:
   `rea inspect-il2cpp-metadata <box>\Firestone_Data\il2cpp_data\Metadata\global-metadata.dat --binary <box>\GameAssembly.dll`,
   poi `python tests\GameApi\dump_api.py` (istruzioni in testa al file). Va lanciato da PowerShell,
   perché da Git Bash rovina i percorsi. Per capire cosa fa un metodo (una formula di costo, quando un
   flag cambia) si può decompilarne il corpo con REA e Ghidra. Più rapido di solito: scrivere il valore
   nel log accanto a quello dello schermo e guardarli.
5. **Scrivere la modifica.**
   - Ogni lettura in `GameState.Get` / `GetRef`; se il dato non si legge, si torna allo schermo.
   - Il dato nuovo prima si confronta con lo schermo (`GameState.Compare`, o una riga di log con
     tutti e due).
   - Lo si usa solo quando coincide. Se può sbagliare in modo costoso (una spesa saltata o doppia),
     il task si fida dei dati solo dopo una visita in cui coincidevano. Esempi: `seals_seen` e
     `rarity_checked` in Hall of Heroes, `_trusted` in Free Pickaxes, `_challengesTrusted` negli
     eventi.
   - Una spesa si conta solo quando la valuta scende, e il click si ferma se l'icona del prezzo non è
     quella attesa.
   - Le righe di log hanno la forma `From the game: ... - not opening.` quando si salta l'apertura.
6. **Provare.**
   - Build contro gli assembly della box, poi `tools\Restart\deploy_test.ps1` con `-ResetTasks`
     (e `-FullRead` per Hall of Heroes), così i task toccati partono subito.
   - Verifica nei `Latest.log`: `From the game`, `mismatch`, `fallback`, `[FAILED] Task` e le righe di
     spesa.
   - Ogni deploy fa ripartire la finestra di 12 ore: va scritto in "Stato".

7. **Il censimento dei badge.** Dopo ogni deploy, per ogni sessione, i badge rimasti accesi a lungo
   nelle righe `[Rail] Lit` (la memoria `rail-must-be-empty`: un badge acceso a lungo è un bug). Un task
   che decide dai dati e non apre lascia acceso un badge "non visto" (i flag `*Seen` di
   `NotificationHandler`, rimessi a falso da ogni riavvio): è così che è saltato fuori GuardianTraining
   acceso per ore. Anche un giro che non apre fa partire il `BadgeCooldown`, quindi va guardato quanto
   aspetta un badge che si accende subito dopo.

Guardati il 27/11 pomeriggio (dai log 11:28-15:20 di -27 e -28):
- il tempo dei task è ~1-1,5 min/h su -27 e -28; quello che resta è quasi tutto azione: Map Missions
  (speed-up e avvii, ~2 s a missione), soul stone e artefatti (spese vere), Experiments e ricerche;
- Chaos Rift ed eventi non fanno una visita a ogni riavvio, come diceva questo file: il `NextRunTime`
  è salvato nel cfg (Chaos Rift 2 giri e gli eventi 3-4 su 9 riavvii dalle 11:28). Non c'è da guadagnare;
- Oracle Rituals: parte il primo rituale della griglia. Sceglierlo dai dati (premi) è una scelta di gioco,
  da chiedere all'utente;
- Map Missions: l'ordine delle missioni viene ancora dai testi dei pin (durata), ma la lettura è
  istantanea; i dati (`missionsOnMap`: `realTimeReq`, `squadsReq`, `missionInteraction`) basterebbero,
  il guadagno sarebbe solo di robustezza;
- testi ancora letti per decidere, senza costo di navigazione: i punti dei Talents (il contatore, c'è
  `TalentsHandler.availablePoints`/`usedTalentPoints`), il costo dell'Enlighten
  (`MagicQuartersInfoSubmenu.enlightenmentDustReq` × `currentEnlightQuantity`), i nomi degli oggetti nei
  negozi degli eventi;
- Ancient Artifact: ogni livello costa 2,6 s (seleziona, apre AAModify, conferma, chiude); se il popup
  restasse aperto dopo un livello, i livelli seguenti costerebbero un click. Da provare con una sonda,
  le Twilight Hourglass arrivano di rado;
- GuardianHolyUpgrade ha anche lui un flag "non visto" (`guardianHolyUpgradeNotificationSeen`) e il task
  decide dai dati senza aprire. Su -27 e -28 il badge non si accende mai; su -26 (main) si spegne quando
  si apre Magic Quarters. Al rollout va guardato nel censimento;
- Guardian Training: quando apre per l'Enlighten (dust sopra la riserva) clicca Train anche con un
  addestramento in corso, e il click va a vuoto (un "Click ignored" sul `trainButton` al primo giro di
  ogni sessione: -27 15:35, -28 15:39 e 17:07). Non spende niente: saltare il click quando
  `MagicQuarters.TrainingEnd` è nel futuro, al prossimo deploy che serve comunque;
- AutoRetreat: clicca `goBackStage` 5 volte di fila e 2 vanno a vuoto mentre lo stage cambia (-27 e -28
  dalle 15:46). Non l'ho toccato io e non costa niente.

## Stato (28/11 02:53, branch `game-state`)

**Il test di 12 ore gira su Steam-27 e, dal 27/11 alle 12:01, anche su Steam-28** (scelta lasciata a
me dall'utente: un secondo account per più varietà; Steam-26 resta su main come confronto; DLL di main
di Steam-28 in `C:\Repos\FirestoneBot-test-backup\2026-11-27-steam28`, di main per la flotta in
`...\2026-11-28-fleet-main`). **È ripartito alle 02:53 (ultimo deploy, -27 alle 02:52 e -28 alle
02:53) e finisce il 28/11 alle 14:53.** I deploy sono descritti in "Fatto il 28/11 alle 02:53", "Fatto
il 27/11 alle 20:43" e "alle 21:36", quelli delle 15:35-15:47 in "Fatto il 27/11 alle 15:47", in testa
al file.

Alle 15:09 era ripartito per la seconda serie di richieste dell'utente:
- **Eventi.** Su Steam-27 tre eventi (Mini, New Player, Trick or Treat) non sono attivi per l'account,
  e ogni ora si apriva la lista per scoprirlo: 22 s all'ora. Ora decide il gioco: `MiniEventsHandler`
  (evento in corso e non bloccato), la lista `calendarEvents` del calendario (dal 12:09: il solo
  `currentCalendarEvent` diceva di no per Decorated Heroes, che c'era) e `AnniversaryEventHandler`.
  Con "non attivo" passa ancora una volta ogni 6 ore e dopo ogni avvio, e confronta il proprio dato
  con la carta trovata.
- **Quests** non apre le schede senza una quest completata e non riscossa (`QuestHandler`), a meno
  che il badge sia acceso.
- **Oracle's Gift** non rifà la visita dopo ogni riavvio: il NextRunTime salvato è già il prossimo reset.
- **Pharaoh's Vault** (token da girare, Ancient Coins contro `pharaohsVaultCost`), **Awakening**
  (Arcane Crystals contro `AwakeningMech.awakenCostBasic`) e **War Machine Rarity**
  (`CanAnyWarmachineRarityUpgrade`, il controllo del gioco stesso) non aprono senza niente da fare.
- **Hall of Heroes, lettura completa dai dati.** Prima confrontata con lo schermo: 13 eroi su 13 uguali,
  slot per slot, su Steam-27 e -28. Il tetto di un incantesimo è `enchantsPerRarity` (2) × il numero
  della rarità (Rare 6, Uncommon 4); `gearTier` conta da 0. Ora la lettura di ogni 24 ore viene dai
  dati, nell'ordine dell'istantanea; lo schermo resta per un eroe nuovo. Il giro completo è passato da
  3-5 minuti a 27 s, che sono la rarità degli eroi.
- Chaos Rift non è toccato: non ho trovato con certezza la valuta dei colpi.

Alle 11:28 era ripartito per le richieste dell'utente su guardiani, Arena of Kings e Hall of Heroes:
- **Guardian Evolution** non apre Magic Quarters se il gioco (`GuardianDeck`) non ha guardiani pronti
  a evolvere; se ce ne sono, clicca solo quelli. Prima erano 10-15 s a giro per trovarne nessuno. Alle
  11:10 aveva Vermilion all'evoluzione 4 e Grace all'1, come legge lo schermo, quindi 0,1 s.
- **Guardian Holy Upgrade** non apre senza Orbs of Light per il livello più economico.
- **Guardian Training** confronta il proprio timer (`MagicQuartersInfoSubmenu.lastTrainGuardinaTime` +
  `TrainCooldownDuration`) con lo schermo prima di usarlo. Alle 11:10 nessun mismatch.
- **Arena of Kings** legge la potenza degli avversari dal loro `HDSAccount.warMachineArenaPower`, non
  più dal testo, che dopo un reroll può essere vuoto. Il tempo resta comunque nelle 5 battaglie.
- **Hall of Heroes** ogni 6 ore guardava i sigilli dei 13 eroi (circa 95 s) per poi non comprare
  niente. Ora legge dal gioco (`HeroServer`: `gearPower`, incantesimi, `gearSealList`/`jewelSealList`)
  i sigilli posseduti e il livello che chiedono, e prende i saldi da `CurrencyHandler`. Senza sigilli
  da usare e senza niente che i saldi comprino non apre; quando apre, visita solo gli eroi con un
  sigillo usabile. Si fida dei sigilli dei dati solo se coincidono con quelli visti l'ultima volta nelle
  schede (`seals_seen`): alle 11:29 erano Benedictus Chest 3/6 e Blaze Boots 3/6 in entrambi, quindi
  0,1 s. `hasSeal` indica il possesso (`quantity` era 0 su tutti), e gli oggetti vanno letti da
  `heroGears`, perché `gearsDict` è vuoto. La lettura completa di ogni 24 ore confronta i livelli e la potenza del gear dei
  dati con lo schermo, prima di poterla sostituire. I dati elencano anche gli eroi non posseduti, per
  questo il controllo è solo che quelli dell'istantanea ci siano tutti.
- **Daily Missions** (anche su main) rifaceva un giro ogni 5 minuti nella prima ora dopo il reset
  anche a missioni già fatte: alle 10:01 ne aveva combattute 3, poi 10 giri da 16 s fino alle 11:00.
  Ora con `HasAvailable` falso non ci riprova.
- Collector Quest resta com'è: il tempo è l'apertura dei forzieri, già 10 alla volta.

Alle 08:33 era ripartito per Forbidden Knowledge e Beasts, chiesti dall'utente:
- **Forbidden Knowledge** apriva l'anteprima di ogni nodo delle tre tavole: 40-80 s a giro per non
  comprare niente. Ora legge dal gioco i tomi di ogni dio, i nodi sotto il tetto della tavola e il
  costo (1 tomo). Apre solo le tavole dove c'è da comprare, e lì solo i nodi sotto il tetto. Alle 08:34
  ha trovato 0 tomi per tutti e tre gli dei e ha fatto un giro da 0,1 s.
- **Beasts** selezionava tutte e 30 le bestie: 25-60 s per 2-3 possedute. Ora le riconosce dal codice
  di ogni avatar, legge le Soul Embers e il livello più economico, e senza niente da comprare né bestie
  da liberare non apre. Alle 08:34 aveva 9 Soul Embers contro 10, quindi 0,1 s.

Alle 07:19 era ripartito per il Personal Tree chiesto dall'utente. Ricontrollava ogni 6 ore, così alle 07:21 c'erano 1978 gettoni fermi da prima
contro i 1700 del nodo successivo. Ora legge gettoni e costo dal gioco ogni 30 minuti e apre l'albero
solo quando può comprare. L'ordine è prima i rami prioritari, poi il più economico: niente più scaglioni
di 5. Dati del gioco alle 07:21: albero a 27, `upgradesAllowed` 15, prioritari tutti a 15/15. Costo
600 + 100 a livello, XP 100 + 10 a livello. Il più economico dà quindi anche più XP per gettone (210 per
1700 contro 250 per 2100).

Alle 05:46 era ripartito una seconda volta (la prima era alle 04:01) perché i pulsanti degli slot di esperimenti e ricerche si
aggiornano un momento dopo l'apertura della schermata. Letti subito, mostravano ancora il prezzo in
gemme dentro la finestra gratuita: esperimento a 2m50s e a 44 s dalla fine, ricerca a 59 s, con giri a
vuoto ogni 2 minuti fino al badge. Prima l'attesa fissa di 3 s lo nascondeva. Ora, quando i dati dicono
che uno slot è nella finestra, il bot aspetta (al massimo 5 s) che il pulsante mostri Free o Claim.

La prima partenza era delle 02:25 con il commit `b239ace`. La ripartenza delle 04:01 era per due
correzioni:
- `OpenMenu(Alchemist, 0, …)` apriva Transmute, cioè il sottomenu 0 dell'enum. Gli slot degli
  esperimenti non c'erano e il task ripartiva ogni 2 minuti, finché non si accendeva il badge. Ora la
  navigazione passa `SubmenuNum.Experiments`.
- Oracle's Gift: dalle 03:25 l'ultimo claim risultava "prima di oggi" anche con il dono già preso (6 → 6),
  e il task ripartiva ogni 30 minuti. Ora fa una sola visita per giornata di gioco.

La sonda della fase 0 resta attiva per tutto il test, per la verifica; va tolta prima del merge.

**Fatto e verificato su Steam-27:**
- **A, badge dal gioco.** `HasNotification` coincide con la barra badge per badge, e `[Rail] Lit` ora
  viene dal gioco. In 3 ore nessun mismatch.
- **B, timer dal gioco.** Spedizioni, missioni, esperimenti, ricerca, Engineer, rituale e rinnovo delle
  quest, tutti confrontati con lo schermo (vedi "Fase 0, risultati").
- **C, decisioni dai dati:**
  - Oracle's Gift decide dall'ultimo claim;
  - Empower decide senza aprire il Temple e registra la curva (`[Empower] curve`);
  - Meteorite non apre la Library se non c'è niente da comprare;
  - le due ricerche scelgono il nodo dai dati di ogni nodo, senza aprire anteprime;
  - le spedizioni scelgono per token all'ora;
  - Map Missions apre solo le missioni finite o accelerabili gratis;
  - System Mail parte solo con mail da riscuotere;
  - Path of Glory non si apre se il gioco conta 0 premi;
  - gli eventi non si visitano se lo stato delle sfide non è cambiato (al massimo 3 ore);
  - Enlighten a ×20/×10/×5/×1 col pulsante quantità del gioco.
- **D, navigazione.** Prima `NavigationControl.OpenMenu`, poi la chiamata HUD, poi i click, e il bot
  ricorda per ogni menu cosa ha funzionato:
  - **le chiamate HUD (`AdventureScreen.Open*`) non hanno mai aperto niente;**
  - `OpenMenu` apre Tavern, ExoticMerchant, WorldMap, Alchemist e ForbiddenKnowledge;
  - MagicQuarters, Library, TownGuild, Awakening e PirateShip vanno ai click.

  La città e la gilda si aprono solo se servono.
- **E, attese.** Dopo un click il bot aspetta finché il blocco attività del gioco è spento e la pila
  dei menu è ferma, da 0,5 a 2 s; se il click non avviene non aspetta.
- **F, connessione.** **G, errori del server** (Harmony, solo log): nessun errore in 3 ore. **H, build
  del gioco** all'avvio, con WARNING se cambia.
- **Watchdog:** chiude con `CloseToMainScreen` quello che resta nella pila dei menu (menus/Achievements
  sopra Magic Quarters aveva nascosto il HUD per un minuto).

**Fase 0, risultati:**
- `serverTimeNow` è in secondi Unix (7 s di differenza dall'ora del PC); tutti i timestamp sono in
  quella unità.
- I `timeLeft` sono in secondi e si aggiornano anche a schermata chiusa.
- `HasNotification` e `IsNotificationActive` dicono la stessa cosa. `IsNotificationActive` è statico, e
  `HasNotification` lancia un'eccezione per un tipo non registrato (`PartyManagement`).
- I nomi delle ricerche nei dati sono chiavi (`cat1/mana_heroes_`). Il nodo si riconosce dall'oggetto
  del popup di anteprima (`FirestoneResearchPreviewPopup.firestoneResearch`), e ogni nodo a schermo ha il
  suo (`FirestoneResearchInteraction.firestoneResearch`).
- Tutte le spedizioni rendono 0,1 token al secondo; lo spareggio sulle monete prende la più lunga.
- Dopo i click, l'apertura di un menu tiene attivi i blocchi `MenuInitialization` e `Addressables` per
  0,7-1,2 s; un click dentro una schermata si assesta in 0,1-0,2 s.

**Velocità.** Steam-27 è passato da 9,7 a **3,43 min/h** di navigazione (23:22-02:09). Nelle stesse ore,
con main, Steam-26 era a 5,24 e Steam-28 a 9,30.

**Visto durante il test, non è del branch (non si tocca fino al merge):**
- **`FirestoneResearch.IsUnlocked`** lancia `KeyNotFound '0'` su un nodo. Il nodo viene scartato e la
  scelta va avanti; c'è un `fallback research state` per sessione.
- **Il badge di Oracle's Gift** resta acceso dopo la riscossione, come su main (Steam-26: doni 2 → 2).
  Dopo ogni riavvio il bot fa una sola visita.
- **Il rinnovo delle settimanali** segnala `mismatch`: lo schermo legge "5d 6h" come 5 giorni e 6 minuti,
  il valore del gioco è giusto.

**Rimandato (resa bassa, rischio non nullo):**
- **Hall of Heroes dai dati.** I dati ci sono: `Hero.heroGearItems`, `HeroServer.heroGears` e
  `jewelsRaw`; poi `GearItem.EnchantLevel`, `enchantCost`, `enchantsPerRarity` e `itemRarity`; gli slot
  si chiamano come gli enum `GearSlot` e `JewelSlot`. Manca la potenza del gear per eroe e i blocchi
  dei tier. La lettura completa avviene una volta ogni 24 ore, e ogni click viene già verificato sullo
  schermo.
- **Costi letti come testo** (Awakening, Evolution, rarità delle War Machines, quantità di Free
  Pickaxes): non costano navigazione.
- **Stage di AutoRetreat dai dati:** gioco e barra coincidono sempre, e la barra non costa niente.

## Perché: la misura di partenza (Steam-27, 26/11 17:08-22:03)

In 4 ore e 55 minuti ci sono state 163 esecuzioni di task e **47,6 minuti passati a navigare**
(9,7 min/h). Si somma ogni `[Task] <nome> finished in <s>s` del `Latest.log`. Dove va il tempo:

| Task | Esecuzioni × media | Totale | Perché |
|---|---|---|---|
| Map Missions | 17 × 31,5 s | 8,9 min | apre la mappa a ogni giro |
| Talents | 10 × 45,3 s | 7,6 min | **spreco**: ha 45 punti che non può spendere, il badge `TalentAvailable` resta acceso e il task rigira ogni 30 min (`BadgeCooldown`) senza fare niente |
| Oracle's Gift | 14 × 23,8 s | 5,6 min | **spreco**: non legge il timer ("renews 01/01/0001"), quindi riprova ogni 2 minuti |
| Meteorite Research | 5 × 66 s | 5,5 min | |
| Hall of Heroes | 1 × 255,5 s | 4,3 min | legge circa 15 testi per eroe |
| Empower | 38 × 5,5 s | 3,5 min | apre il Temple ogni 5 minuti per leggere due numeri |
| 4 task eventi | 25 × ~8 s | 3,8 min | aprono l'evento ogni ora |
| Forbidden Knowledge, Firestone Research, System Mail | | 3,5 min | |

I task che già oggi leggono dal gioco (Amulet Shop, War Machines, Warfront Campaign, Blueprints)
durano **0,1 s**. L'obiettivo del branch: che il maggior numero di task diventi come loro, cioè che apra
una schermata solo quando c'è davvero qualcosa da fare, ci arrivi con una sola chiamata e ne esca con
un'altra.

Il log ha anche un altro costo fisso: a ogni scan, per ogni badge, due `[FAILED] Path broken`
(radice `leftSideUINew` e radice di riserva `leftSideUI`). Con la leva A spariscono.

## Contesto per chi parte da zero

- **Il codice del gioco:** `CoreASM.dll` e `SharedASM.dll`, che il bot vede come
  `Il2CppCoreASM.dll` e `Il2CppSharedASM.dll` (namespace `Il2Cpp`). Gli enum come `MenuName` stanno
  in `SharedASM`.
- **Elenco di tipi, campi e metodi:** `tests/GameApi/CoreASM.dll.api.txt` e
  `tests/GameApi/SharedASM.dll.api.txt`, con le stringhe in `strings.txt`. Sono gitignorati. Se
  mancano, si rigenerano col comando in testa a `tests/GameApi/dump_api.py` (REA è installato,
  `rea`; va lanciato da PowerShell, da Git Bash i path si rompono). Per cercare un tipo:
  `awk '$0=="type NomeTipo"{p=1;print;next} /^type /{p=0} p' tests/GameApi/CoreASM.dll.api.txt`.
- **Come si arriva ai dati:**
  - `GameInitialize.HandlerLoader.<handler>`: circa 100 handler, il bot oggi ne usa circa 15.
  - `GameInitialize.CoreFunctionality`, che contiene `serverUpdate`, `socketAPI`, `navigationControl`
    e `playerActivityBlocker`.
  - I valori `Obscured*` si leggono con `.GetDecrypted()`.
  - Esempi da copiare: `WarfrontLiberationMissions.HasAvailable`
    ([WarfrontDailyMissions.cs:36](src/GameModel/Features/Map/WarfrontCampaign/WarfrontDailyMissions.cs#L36)),
    `MapMission.FreeSquads`, `TreeOfLife.PersonalLevel`, `BotManager.LogSnapshot`
    ([BotManager.cs:228](src/Core/BotManager.cs#L228)).
- **Scheduler:** un task è pronto quando `NextRunTime` è passato oppure quando il suo badge è visibile
  (con `BadgeCooldown` di 30 min, [BotTask.cs:181](src/Core/Tasks/BotTask.cs#L181)). Si aggancia su
  `IsNotificationVisible()` (virtual, [BotTask.cs:190](src/Core/Tasks/BotTask.cs#L190)) e sul setter
  di `NextRunTime` ([BotTask.cs:73](src/Core/Tasks/BotTask.cs#L73)).
- **I click di oggi non usano il mouse:** `GameButton.Click()` chiama `button.onClick.Invoke()`, cioè
  lo stesso codice del bottone, e poi aspetta `interaction_delay` (1 s, limitato tra 0,5 e 5,
  [BotSettings.cs:44](src/Core/BotSettings.cs#L44)). Il tempo quindi non se ne va nei click, ma nella
  navigazione (3-5 click per arrivare a un sottomenu), nell'attesa fissa dopo ogni click e
  nell'apertura di schermate solo per leggerle.

## Regole

1. **Le chiamate dirette sono permesse solo se il gioco stesso le usa dalla sua UI:**
   - `NavigationControl.OpenMenu`, `CloseToMainScreen`, `CloseLastOnMenuStack`, che sono quelle che
     usano le scorciatoie dei badge;
   - i listener dei bottoni, come oggi;
   - i getter che leggono e basta (`HasNotification`, `HasAvailableMissions`, `IsCrystalAlive`, ...).

   Sono vietati:
   - le funzioni che parlano col server (`SocketAPI.SendSocketMessage`, `ServerCalls`, `ServerAPI`);
   - i metodi `*Callback` (sono le risposte del server);
   - qualsiasi scrittura di campi del gioco.

   Le condizioni che controllerebbe il bottone si controllano anche nelle chiamate dirette: bottone
   attivo (`GameButton` lo fa già), `PlayerActivityBlocker.IsActive()` falso, server connesso.
   **Perché:** ACTk in questa build controlla solo valori in memoria, velocità e orologio, non chi
   chiama un metodo. Il rischio è il server, che non deve ricevere richieste che la UI in quel momento
   non manderebbe.
2. **Se una lettura fallisce, si torna a quello di oggi.** Un aggiornamento del gioco può rinominare un
   campo, e il metodo del bot che lo usa lancia `MissingFieldException`/`MissingMethodException` quando
   viene compilato al volo. Quindi:
   - ogni accesso al gioco va in un metodo piccolo e separato, chiamato dentro un try/catch che
     restituisce `null`;
   - con `null` si usa la strada di oggi, che resta nel codice;
   - ogni fallback va scritto nel log come `[GameState] fallback <cosa>: <eccezione>`, **una volta per
     sessione**.
3. **Verifica gratis.** Quando un task apre comunque la schermata per agire, confronta il dato del
   gioco con quello a schermo e, se non coincidono, scrive `[GameState] mismatch <cosa>: game=<x>
   ui=<y>`. Non serve aprire schermate in più per verificare.
4. **Tempo.** `timeStarted`, `lastClaim` e simili sono probabilmente secondi del server (lo conferma la
   fase 0). Si convertono con `CoreFunctionality.serverUpdate.serverTimeNow`, mai con l'orologio del PC:
   `DateTime.Now + TimeSpan.FromSeconds(fine - serverTimeNow)`.
5. **Niente opzioni di configurazione nuove per accendere o spegnere il branch.** Per tornare indietro
   si rimette la DLL di main. L'unica cosa che si può cambiare dal cfg è `interaction_delay`, che
   esiste già.

## Come si lavora: branch, Steam-27, 12 ore

1. **`git status` prima di tutto.** Il 26/11 c'erano modifiche non committate di un'altra sessione (sui
   Talents): non vanno toccate né incluse. Si parte da main aggiornato.
2. **Branch:** `git switch -c game-state`, poi `git push -u origin game-state`. Tutti i commit vanno sul
   branch, e su main non va niente fino al merge.
3. **Build contro il gioco della box** (le cartelle reali di PC-Guesly hanno ancora gli assembly del
   25/09):
   ```powershell
   $env:COMMON_DIR = "C:\Program Files (x86)\Steam-17\steamapps\common"
   dotnet build C:\Repos\FirestoneBot\src\firebot.csproj -c Release `
     -p:Il2CppAssembliesDir="C:\Sandbox\dmari\SteamB27\drive\C\Program Files (x86)\Steam-27\steamapps\common\Firestone\MelonLoader\Il2CppAssemblies"
   dotnet test C:\Repos\FirestoneBot\tests\Firebot.TalentEngine.Tests
   ```
   Il csproj referenzia già sia `Il2CppCoreASM` sia `Il2CppSharedASM` (dove sta `MenuName`).
4. **Deploy solo su Steam-27**, la più avanti di livello tra 20 e 28 (223):
   - backup delle due DLL di `C:\Program Files (x86)\Steam-27\steamapps\common\Firestone\Mods` in
     `C:\Repos\FirestoneBot-test-backup\<data>-steam27`;
   - rinomina e copia come in MULTI_INSTANCE_SETUP.md, "Aggiornare il bot", punto 4 (box senza
     `Mods`: basta quello reale);
   - `taskkill /F /T` sul `Firestone.exe` di Steam-27; il watchdog lo riavvia in circa 2 minuti.
   - **Durante il test nessun deploy di main deve toccare Steam-27**: se serve un deploy alla flotta,
     va saltata.
5. **Fase 0 (sonda) su Steam-27 per circa un'ora**, poi le leve, una alla volta, con build e deploy su
   Steam-27 a ogni passo.
6. **Le 12 ore** partono con tutte le leve dentro. Steam-27 si confronta con Steam-26 e Steam-28, che
   girano sullo stesso PC nelle stesse ore:
   - **Errori:** nessuna eccezione nuova; ogni `[GameState] fallback` spiegato; nessun
     `[GameState] mismatch` aperto; nessun errore del server in più (leva G).
   - **Lavoro fatto:**
     - giornaliere e settimanali reclamate;
     - liberation fatte;
     - `[Rail] Lit:` vuoto, tranne `Warmachines`;
     - missioni, spedizioni, ricerche ed esperimenti mai fermi più di uno scan dopo la fine;
     - numeri dello `Snapshot` (stage, gemme, ...) in linea coi vicini.
   - **Velocità:** minuti per ora di navigazione rispetto ai 9,7 di partenza. Obiettivo: almeno la
     metà. Il conteggio dei minuti è il regex `\[Task\] (.+?) finished in ([\d.]+)s` sul
     `Latest.log` della box, aperto in condivisione (`[IO.File]::Open(path,'Open','Read','ReadWrite')`).
7. **Se va tutto bene:**
   - `git switch main`, `git merge --no-ff game-state`, `git push`;
   - deploy su tutta la flotta, su entrambi i PC, come al solito;
   - azzerare lo stato dei task che cambiano scheduling (`last_done_date`, `next_run_time_internal`),
     così rifanno subito il controllo di oggi;
   - aggiornare README e TESTING.md.

   **Se qualcosa non va:** si rimettono su Steam-27 le DLL del backup, si corregge sul branch e si
   rifanno le 12 ore.

## Fase 0: sonda (sul branch, non cambia il comportamento)

Un blocco in `LogSnapshot` (una volta all'ora) e un log ogni volta che un task apre o chiude una
schermata. Deve rispondere a:

- `NotificationHandler.HasNotification(t)` o `IsNotificationActive(t)`: quale corrisponde a
  `Notifications.Lit`?
- Unità dei `timeLeft` (secondi?) e se si aggiornano anche a schermata chiusa
  (`AlchemyExperimentHandler.UpdateRemainingTimes()` fa pensare di sì).
- `serverTimeNow` è in secondi Unix? È la base di `timeStarted` e `lastClaim`?
- **Mappa della navigazione:** `navigationControl.menuStack` / `GetLastMenuOnStack()` scritti a ogni
  schermata che il bot apre oggi danno la coppia `(MenuName, sottomenu)` di ogni schermata, cioè gli
  argomenti di `OpenMenu`. I sottomenu sono enum come `EngineerMech+SubmenuNum`.
- `playerActivityBlocker.activeBlockReasons` durante i click: quali ragioni compaiono, e quando
  tornano vuote.

Il risultato va scritto in questo file, in una sezione "Stato".

## Le leve

**A. Badge dal `NotificationHandler`.**
- Il gioco calcola i badge in `HandlerLoader.notificationHandler`, con
  `HasNotification(NotificationType)` e `IsNotificationActive(NotificationType)` (entrambi con corpo
  nativo). L'enum `NotificationType` ha circa 50 voci.
- I nomi dei badge del bot (`Paths.BattleLoc.NotificationsLoc`) coincidono con l'enum, tranne tre:
  `Warmachines` → `WarMachines` (basta `Enum.TryParse` con `ignoreCase`), `OracleRituals` →
  `OracleRitual`, `OracleBlessings` → `OracleBlessing`.
- Una modifica in `BotTask.IsNotificationVisible()` copre i 26 task con `NotificationBadgeName`. Quelli
  con `NotificationPaths` sono elencati nella tabella più sotto.
- Poi `Notifications.Lit` legge dal gioco, e i path della barra restano solo per cliccare dove
  serve ancora.

**B. Timer dai dati, per `NextRunTime`.** I campi sono nella tabella. Lo schema è
`NextRunTime = daGioco ?? daUi`. È soprattutto il controllo "è pronto?" che non apre più niente.

**C. Decisioni dai dati.** Quantità, costi, livelli e nomi oggi letti come testo (`GetParsedText`,
`ChallengeParser`) si leggono dagli handler e da `CurrencyHandler.GetCurrency(...)`, che è già in uso.
Togliendo il testo si toglie anche la dipendenza dall'inglese indicata nel README: eventi, shop degli
eventi e priorità delle ricerche passano agli enum (`CalendarEventChallenges`,
`FirestoneResearchType`, ...).

**D. Navigazione in una chiamata.**
- `CoreFunctionality.navigationControl.OpenMenu(MenuName, submenu, subSubmenu, GameItemCode)` apre
  qualsiasi menu (261 in `MenuName`). `CloseToMainScreen()` chiude tutto, `CloseLastOnMenuStack()`
  chiude l'ultimo. `menuStack` dice cosa è aperto senza cercare GameObject.
- Sostituisce le catene Town → edificio → sottomenu di `Town.cs`, `TownGuild.cs`, `Library.cs`,
  `WorldMap.cs`, `CharacterScreen.cs`, ... e buona parte del lavoro di chiusura del `Watchdog`.
- Prima scelta, quando esiste: le funzioni dedicate che chiamano i bottoni del HUD,
  `GameInitialize.MenuLoader.adventureScreen.Open*`. Sono `OpenLibrary(LibraryMech+SubmenuNum)`,
  `OpenMagicQuarters(...)`, `OpenAlchemist(...)`, `OpenExoticMerchant(...)`, `OpenTavern(...)`,
  `OpenPirateShip(...)`, `OpenOracleMech(...)`, `OpenWorldMap(...)`, `OpenGuildExpeditions()`,
  `OpenEngineer()`, `OpenArenaOfKings()`, `OpenChaosRift()`, `OpenForbiddenKnowledge()`,
  `OpenScarabGame(...)`, `OpenBattlePass(...)`, `OpenStore(...)`, `OpenTempleOfEternals()`, ... (36
  in tutto). I sottomenu sono enum tipizzati, quindi niente numeri da indovinare. `OpenMenu` resta per
  le schermate che non ne hanno una.
- Per i task delle quest (Collector, Gamer, Merchant, Miner): `QuestHandler.dictQuestGoToAction`
  (`Dictionary<QuestCode, Action>`) è il bottone "Go" di ogni quest, cioè il gioco stesso che porta
  dove serve.
- `menuUnlocked` (`Dictionary<(MenuName,int), Func<bool>>`) e
  `UnlockMechanismsHandler.MechanismsLevelUnlocks` dicono se una funzione è sbloccata. Possono
  sostituire i `MinimumCharacterLevel` presi dal wiki.

**E. Attese sullo stato invece che fisse.**
- `CoreFunctionality.playerActivityBlocker.IsActive()` e `activeBlockReasons` (ragioni come
  `MenuInitialization`, `AdventureScreenSubmenuTransition`, `ChestOpening`, `WFBattle`) dicono quando
  il gioco sta ancora lavorando. Si aggiungono i flag "in attesa del server" dei singoli handler:
  `OracleHandler.waitingServerReply`, `CalendarEventsHandler.WaitingServerReply`,
  `DailyLoginHandler.WaitingServerReply`, `AlchemyExperimentHandler.ServerStartExperimentPending`,
  `MeteoriteResearchHandler.ServerCompleteMeteoriteResearchPending`,
  `ArcaneCrystalHandler.servingHitsInProgress`.
- Dopo un click: `Poll.Until(nessun blocco && stato atteso, pollSeconds 0.1)` al posto
  dell'`interaction_delay` fisso, che resta come limite massimo.
- Dopo questa leva, su Steam-27 si prova `interaction_delay = 0.5`.

**F. Connessione.**
- Oggi il bot non sa se il gioco è disconnesso: in `src` non c'è niente su disconnect/reconnect.
- `CoreFunctionality.socketAPI.IsConnectedAndNotReconnecting()` (con corpo nativo),
  `isWaitingToReconnect` e `socketDataLoaded` servono a `BotManager` per non avviare task durante
  una disconnessione e per scrivere nel log quanto dura.

**G. Risposte del server con Harmony (solo postfix, solo log).** `0Harmony` è referenziato nel csproj
ma non è usato.
- `SocketAPI.ServerErrorMessage(string Error)` e `ShowDefaultServerError()`: ogni rifiuto col suo
  codice. Possono sostituire `WarfrontLiberationMissions.FightRefused`
  ([WarfrontDailyMissions.cs:41](src/GameModel/Features/Map/WarfrontCampaign/WarfrontDailyMissions.cs#L41))
  e danno il conteggio degli errori per il criterio delle 12 ore.
- Callback di conferma (`AlchemyExperimentHandler.StartExperimentCallback`,
  `CompleteExperimentCallback`, ...): dicono "fatto" senza interrogare la UI. In IL2CPP un metodo può
  essere inlineato, quindi si verifica con un log alla prima chiamata che la patch scatti davvero.

**H. Versione del gioco all'avvio.**
- `CoreFunctionality.buildSettings.gameVersion` / `gameVersionText`, più data e dimensione di
  `GameAssembly.dll`, scritti una volta all'avvio. Il report giornaliero segnala quando cambiano.
- Il 3.64 è passato inosservato per due giorni. Il 06/11 una build ha cambiato le regole senza
  cambiare versione: per questo serve anche il file.

## Task per task

Tempi: Steam-27, 26/11. "Pronto" è la condizione per far partire il task senza aprire niente; "Dati"
sono i valori per decidere cosa fare; per la navigazione vale la leva D per tutti. Le voci marcate con
(?) sono dedotte solo dal nome.

| Task | Tempo | Pronto (gioco) | Dati (gioco) |
|---|---|---|---|
| **Quests** | 6 × 8,2 s | badge `Quests`; `QuestHandler.CheckAllClaimed(QuestResetCode)` | `QuestHandler.quests` (già in uso); rinnovo da `serverUpdate.nextGameDay`/`nextGameWeek` invece di `CharacterScreen.cs:41` |
| Collector | | quest in `QuestHandler.quests` | `GearChestHandler.gearChests`, `HasChestBoxes(code, n)` invece di aprire la borsa |
| Gamer / Beer Exchange | 3 × 8,4 s | badge `BeerExchange`; `NotificationHandler.BeerExchangeCheck()` (?) | token e birra da `CurrencyHandler`; `TavernDataHandler.tavernData` |
| Ancient Artifact | 1 × 5,2 s | badge `AncientArtifact`; `HasAncientArtifactNotification()` | `AncientArtifactHandler.ancientArtifacts` |
| Merchant | | quest | oggetti posseduti da `InventoryItemsHandler.inventoryItems` (Midas' Touch, pergamene) invece dei nomi inglesi |
| Miner | | badge `ArcaneCrystal`, quest | `ArcaneCrystalHandler.IsCrystalAlive()`, `currentCrystalState`, `servingHitsInProgress` |
| **Daily Store Offers** | | badge `CheckIn`, `MysteryBox` | `DailyLoginHandler.dailyLoginData`; `MysteryBoxDailyClaimHandler.lastClaim`/`expectClaim` |
| Engineer | 1 × 7,1 s | `NotificationHandler.canClaimTools` (bool) | timer da `WarMachineHandler.lastClaimedTime`/`timeReq` (?), invece di `Engineer.cs:14` |
| Guardian Training | 1 × 8,4 s | badge `GuardianTraining` | `GuardianServerHandler.guardiansDict`; non `MagicQuartersInfoSubmenu`, che è un componente della UI |
| Guardian Evolution | 2 × 10,7 s | badge `GuardianEvolution` (+ `GuardianRarity`?) | `GuardianServer.evolution`, Strange Dust da `CurrencyHandler`; costo invece di `MagicQuarters.cs:31` |
| Experiments | 3 × 10,8 s | badge `Experiments` | `AlchemyExperimentHandler.experimentSlot[...]`: `timeLeft`, `timeRequired`, `ExperimentState` |
| **Firestone Research** | 2 × 36,9 s | badge `FirestoneResearch`; `FireStoneResearchCheck()` (?) | `FirestoneResearchHandler.firestoneResearchSlots[]` (`timeLeft`, `researchOnProgress`), `researchTrees`; priorità con l'enum `FirestoneResearchType` (`RainingGold`, `Prestigious`, `FirestoneEffect`) invece di `FirestoneResearchTask.cs:63` |
| **Meteorite Research** | 5 × 66 s | badge `MeteoriteResearch` | `MeteoriteResearchHandler.researchTrees`, `ServerCompleteMeteoriteResearchPending`; meteoriti da `CurrencyHandler` |
| **Empower** | 38 × 5,5 s | nessuna schermata: `HandlerLoader.firestones.firestonesEarnedThisRun` contro i Firestones posseduti (`CurrencyHandler`) + lo stallo di AutoRetreat | stesso, invece di `TempleOfEternals.cs:22-35` |
| Arena of Kings | | badge `ArenaTokens` | token da `CurrencyHandler`; `ArenaOfKingsRankHandler` |
| Pirate's Prize | 1 × 5,7 s | badge `PiratesPrize`; `piratePrizeNotification` | `PirateShipHandler.currentTier`, `currentMilestone`, `unlockedTiers` |
| **Oracle Rituals** | 4 × 8,4 s | badge `OracleRitual`; `hasRitualNotification` | `OracleHandler.activeRitual` (`timeStarted`, `state`), `ritualDuration`, `ritualsQueued`, invece di `Rituals.cs:53-63` |
| **Oracle's Gift** | 14 × 23,8 s | badge `OraclesGift`; `hasOracleGiftNotification` | `OraclesGiftDailyClaimHandler.lastClaim`/`expectClaim`: corregge il "renews 01/01/0001" |
| Oracle Blessings | | badge `OracleBlessing`; `OracleHandler.hasAtLeastOneBlessingNotification`, `blessingsNotifications` | `OracleHandler.blessings` |
| Amulet Shop | 5 × 0,1 s | già dal gioco | già dal gioco |
| **System Mail** | 1 × 63,7 s | `SystemMailHandler.unseenMails` / `systemMails` non vuoto | `systemMails` |
| War Machines / Rarity / Blueprints | 0,1 / 8,3 / 0,1 s | badge `WarMachines`, `WarMachinesRarity` | `WarMachineHandler.warMachineData` (già in uso); costo della rarità invece di `WarMachines.cs:52` |
| Seen Badges | 1 × 4,4 s | flag "visto" di `NotificationHandler` (`arcaneCrystalSeen`, `meteoriteReseachSeen`, `tavernSeen`, ...) | |
| **Expeditions** | 6 × 7,2 s | badge `Expeditions` | `HandlerLoader.guildManagement.expeditionData` (`activeExpedition`, `activeExpeditionStartTime`, `activeExpeditionTimePassed`, `expeditionsTimeRefreshed`), durate in `ExpeditionsHandler.expeditionTimeReq`, invece di `Expedition.cs:16,20` |
| Tree of Life | 1 × 12,3 s | già dal gioco (`ToLPersonalUpgradeHandler`) | già dal gioco |
| Free Pickaxes | 1 × 4,3 s | badge `FreePickaxes` | quantità dal gioco invece di `FreePickaxes.cs:11` (campo da cercare) |
| Awakening | 1 × 5,3 s | badge `Awakening` | `AwakeningHandler`, livelli in `HeroServerHandler.heroesDict`; costo invece di `Awakening.cs:71` |
| Chaos Rift | | badge `ChaosRift`, `ChaosRiftSupplies` | `ChaosRiftHandler.canAttackUntilTime`, `currentBuffEndTimer`, `moonstonesPerRefresh`; Moon Stones da `CurrencyHandler` |
| **Forbidden Knowledge** | 1 × 72,4 s | badge `ForbiddenKnowledge` | `ForbiddenKnowledgeHandler.darkGodDicts`, `upgradeCost`, soft cap per dio |
| Guardian Holy Upgrade | | badge `GuardianHolyUpgrade` | `GuardianServer.holyDamageUpgrades`, Orbs of Light da `CurrencyHandler` |
| **Map Missions** | 17 × 31,5 s | badge `MapMissions` | `MissionsHandler.missionsInProgress[]` (`realTimeReq`, `timePassed`, `timeStarted`, `missionState`), `missionsOnMap` (già in uso), `TimeLeftForNewMissions`, `availableSquads` (già in uso), invece di `MapMission.cs:10`, `MissionPreview.cs:21`, `MissionPin.cs:18` |
| Warfront Campaign Loot | 1 × 3,3 s | `WFCampaignMissionHandler.hasClaimedLoot`, `lastLootClaimTimestamp` | invece di `WarfrontLoot.cs:12` |
| Warfront Campaign | 0,1 s | già dal gioco | già dal gioco |
| Warfront Daily Missions | | già dal gioco (`HasAvailableMissions`) | rifiuto del server dalla leva G invece del popup |
| **Talents** | 10 × 45,3 s | `TalentsHandler.availablePoints`, `FinishedTheTalentTree()`: punti che il planner può spendere, non il badge. Corregge lo spreco di 7,6 min ogni 5 h | `TalentsHandler.talents`, `usedTalentPoints`, `talentTierReqs`. Attenzione: c'è lavoro in corso sui Talents su main |
| **Hall of Heroes** | 1 × 255,5 s | badge `HallOfHeroes`, `hallOfHeroesHeroIdNotifications` | livelli di gear, incantesimi e rarità da `HeroServerHandler.heroesDict` / `heroDeck` invece dei 15 `GetParsedText` di `HallOfHeroes.cs` (campi da cercare nel dump: `HeroServer`, tipi gear e jewel) |
| Formation | | già dal gioco | già dal gioco |
| Path of Glory | 5 × 3,1 s | `BattlePassHandler.unclaimedRewards` > 0 | `freeRewardsClaimed`, `goldenRewardsClaimed` |
| Pharaoh's Vault | | badge `PharaohsVault`, `ScarabGameMilestones`; `HasPharaohsVaultNotification()` | `ScarabGameHandler`, Noble Tokens da `CurrencyHandler` |
| Scarab free token | 1 × 12,5 s | `ScarabGameHandler.expectDailyFreePharaohTokenClaim`; badge `ScarabGameShopFreeToken` | |
| Beasts | | badge `ScarabGameBeastRelease`; `HasScarabGameBeastReleaseNotification()` | `BeastHandler.dictBeast`, Soul Embers da `CurrencyHandler` |
| **Eventi** (Decorated Heroes, Mini Event, New Player, Trick or Treat) | 25 × ~8 s | `CalendarEventsHandler.calendarEvents` (evento attivo), `challengeNotificationsEnable`, `medalsNotifications`; `MiniEventsHandler` (da esplorare) | `CalendarEventsHandler.eventChallengesProgress` (`Dictionary<CalendarEventChallenges, CalendarEventChallengeData>`) invece di titolo e progresso in testo (`DecoratedHeroesShop.cs:38`, `ChallengeParser`, `EventTask.cs:67`). Per il 03/12 c'è anche `FrostfireFestivalHandler` (EVENTS_PLAN.md, sezione 4) |
| Emblem Market | 1 × 7,6 s | da cercare nel dump | |
| AutoRetreat | continuo | `HandlerLoader.mainBattle.CurrentStageNumber`, `MaxStageNumber`, `CurrentStageWave` invece della barra dello stage (`StageProgress`, che dà -1 quando la barra non c'è) | |
| Hero Upgrade | continuo | `NotificationHandler.UpgradesNotificationCheck` / badge degli upgrade (?) | livelli degli eroi da `heroDeck` invece di `HeroUpgrade.cs:167` |
| Flying Bonus Hunter | continuo | `BeerHandler.dragonBeerHandler` / `femaleDragonBeerHandler`, `FallingItemHandler` (?) invece di cercare i GameObject a ogni scan | |

## Flussi nuovi (decisi dall'utente il 26/11, fanno parte del branch)

Questi cambiano cosa fa il bot, non solo come lo fa. Le scelte qui sotto sono già approvate.

1. **Ricerca (Firestone e Meteorite).**
   - **Oggi:** apre le anteprime dei nodi una per una e sceglie per nome inglese, se no il primo
     della griglia. Su Steam-27, il 26/11, la scelta ha preso 55 s per Meteorite (17:19:39 →
     17:20:34, "node #12, priorityRank=none") e 28 s per Firestone ("#13, priorityRank=none"), ogni
     ora.
   - **Firestone, dai dati:** `FirestoneResearchHandler.researchTrees[currentTree]` ha ogni nodo con
     `level`, `levelCap`, `realTimeRequired`, `researchState`, `IsUnlocked`,
     `firestoneResearchType` (enum) e `researchTier`. La scelta si fa in memoria, poi un solo click sul
     bottone del nodo (`firestoneResearchInteraction`). **Deciso:** prima le priorità di oggi, ma per
     enum (`RainingGold`, `Prestigious`, `FirestoneEffect`, ...); fuori dalle priorità, il
     `realTimeRequired` più corto invece del primo della griglia. Così si completano più livelli
     all'ora e i tier si sbloccano prima (`tierNTreeMUnlock`).
   - **Meteorite, dai dati:** `MeteoriteResearchHandler.researchTrees` ha `levelUpCost`,
     `effectPerLevel`, `level`/`maxLevel`, `IsUnlocked()`, `CanLevelUpWithMeteorite()` e il
     prerequisito (`meteoriteUnlocking` + `meteoriteUnlockingLevel`). Se nessun nodo costa al massimo
     saldo meno riserva, non si apre niente. Su Steam-27 il saldo era 3384 con riserva 3000: 384
     spendibili, e 66 s ogni ora per probabilmente non comprare niente.
2. **Talents** (c'è un'altra chat che ci lavora).
   - `TalentsHandler.talents` (`Dictionary<TalentType, Talents>`) ha, per ogni nodo, `level`,
     `levelCap`, `unlocked` e `talentTier`. Ci sono anche `talentTierReqs`, `talentsCapDict`,
     `availablePoints`, `usedTalentPoints` e `FinishedTheTalentTree()`. Le 89 voci di `TalentType`
     sono gli 89 nodi di `TalentTreeData`.
   - Questo sostituisce le soglie dei tier prese dal wiki, i blocchi "talento precedente" scoperti
     cliccando e la cache "cached maxed".
   - Il task è pronto solo se esiste un nodo `unlocked`, sotto il cap e con il tier raggiunto. Oggi
     gira 45 s ogni 30 min su Steam-20, -27 e -29 con `TalentAvailable` acceso.
   - Prima di toccarli sul branch: se il lavoro dell'altra chat è già su main, si fa rebase del
     branch su main e queste letture si aggiungono al loro codice. Se no, ci si mette d'accordo con
     l'utente per non lavorare in due sugli stessi file.
3. **Spedizioni.**
   - **Oggi:** parte la prima in attesa.
   - **Dai dati:** `guildManagement.expeditionData.expeditions` le elenca tutte con
     `expeditionTokenReward`, `guildCoinReward` e `timeReq`. I token spedizione sono la risorsa che
     frena (Personal Tree a 50 prima delle War Machines).
   - **Deciso:** si fa partire quella con più token all'ora (`expeditionTokenReward / timeReq`); a
     parità, quella con più monete.
4. **Empower.**
   - Il gioco ha un suo segnale: il badge `TemplePrestige` (`NotificationHandler.PrestigeNotificationCheck`,
     `prestigeNotificationMultiplier`), visto decine di volte per istanza il 26/11. Il bot non lo usa.
   - Con `firestones.firestonesEarnedThisRun` letto gratis a ogni scan (oggi il Temple si apre 38
     volte in 5 ore) si può misurare quanto crescono i Firestones nella run, e fare il reset quando la
     crescita rallenta invece di aspettare il muro.
   - **Deciso:** durante le 12 ore la regola del reset resta quella di oggi, e il bot registra la
     curva: una riga `[Empower] curve` per scan, con minuti della run, log10 di
     `firestonesEarnedThisRun`, log10 dei Firestones posseduti e `TemplePrestige` acceso o spento.
   - Dopo le 12 ore si scrive la regola nuova coi numeri (reset quando la crescita rallenta) e si
     mostra all'utente prima di attivarla.
5. **Oracle's Gift:** non legge il timer ("renews 01/01/0001") e riprova circa ogni 2 minuti; il
   badge è acceso ora su Steam-19 e -23. Si sistema con `OraclesGiftDailyClaimHandler.lastClaim` /
   `expectClaim`.
6. **Dungeon del Warfront:** `DungeonMissionsHandler.HasAvailableMissions()` è pronto per quando gli
   account arrivano a 70 stelle (CAMPAIGN_PLAN.md, sezione 7). Il 26/11 su PC-Guesly avevano da 30 a
   37 stelle: non serve ancora.
7. **Tower of Souls:** `TowerOfSoulsHandler` genera Orbs of Prophecy gratis (`orbsOfProphecyGenerated`,
   `maxFreeOrbsOfProphecy`). Il bot non lo tocca e il badge non si è mai visto su 17-31, quindi è
   probabilmente bloccato. Si controlla lo sblocco (`UnlockMechanismType.TowerOfSouls`); priorità
   bassa.

## Funzioni nuove trovate nel dump (da decidere con l'utente, fuori dalle 12 ore)

Sono cose che il bot oggi non fa. Ognuna va decisa con l'utente prima di scriverla.

1. **Frostfire Festival (dal 03/12): urgente, va su main senza aspettare il branch.**
   - `HandlerLoader.frostfireFestivalHandler` risponde alle domande della sezione 4 di
     EVENTS_PLAN.md: `InProgress()`, `CanClaimNow()`, `todayClaimed`, `daysClaimed`,
     `IsLastDayOfEvent()`, `GetAvailableFrostfirePacksCount()`, `openedBoxes`/`IsBoxOpened(n)`.
     `HasEventManagerPopupNotification()` dice anche quando la carta dell'evento ha qualcosa da fare.
   - È un regalo al giorno. Ogni regalo può dare tra l'altro 1.000 token spedizione, la risorsa che
     frena.
   - Il task è pronto con `InProgress() && CanClaimNow()`; il server lo chiama
     `ClaimFrostfireFestivalDaily`.
2. **Routine di push con pergamene e consumabili.**
   - **Cosa dice la guida:** `docs/firestone_guida_F2P.md`, sezione "Consumabili di gold, pergamene e
     routine di push". Scroll of Speed, Damage e Health si attivano insieme (un talento triplica il
     danno quando sono attive tutte e tre), e i consumabili di gold si usano dal più piccolo, solo con
     le pergamene attive.
   - **Oggi:** il bot non usa mai un consumabile. In più `MerchantQuestTask` vende Scroll of Health e
     Scroll of Damage, mentre la guida dice di tenerne qualcuna.
   - **Dati:**
     - `HandlerLoader.scrollsHandler.scrollItems` (`amount`, `itemDuration`, `timePassed`),
       `AllScrollsActive()`, `AncientKnowledgeActive`;
     - `inventoryItemsHandler.inventoryItems` (`amount`, `effectDuration`, `timePassed`);
     - lo stage da `mainBattle`.
   - **Momento naturale:** quando AutoRetreat riprova il muro (ogni 30 min), prima di risalire. Si usa
     il bottone "usa" dell'oggetto (`ScrollInteraction` / `InventoryItemInteraction`), mai
     `UseTheItemAskServer`.
   - **Richiesta dell'utente del 28/11:** almeno una volta al giorno, al muro, un push al massimo stage
     con le tre pergamene (Speed, Damage, Health) e i cinque oggetti (Drums of War, Dragon Armor,
     Guardian's Rune, Totem of Agony, Totem of Annihilation); poi usare in modo mirato Pouch, Bucket,
     Crate, Barrel of Gold e Midas Touch, che oggi si accumulano. Vincoli dell'utente: **Midas Touch
     scarso e prezioso** (con parsimonia, solo quando serve: dal 28/11 il Merchant non lo vende più);
     **Strange Dust solo per guardiani ed eroi, niente Alchimista**.
   - **Cosa dicono le guide** (`docs/firestone_guida_F2P.md` "Consumabili di gold, pergamene e routine
     di push", "Empower e Temple of Eternals"; `docs/guida_steam_completa.md` "Items & Scrolls"):
     - routine prima dell'Empower (F2P, V5): al massimo, le tre pergamene, spingi ancora e upgrade, un
       Pouch finché durano, poi Empower;
     - consumabili d'oro = 5 / 15 / ~30 / 60 minuti di oro allo stage attuale; solo con le pergamene
       attive, sullo stage ottimale, dal più piccolo (il Pouch sblocca gli special upgrade, il Barrel
       per ultimo); Drums of War aumenta l'oro dei consumabili;
     - la guida Steam: i consumabili accorciano il tempo per il muro ma non lo spostano, meglio
       venderli per upgrade permanenti; la F2P: vendere Barrel e Crate, tenere Pouch e Bucket;
     - Scroll of Speed ~triplica l'oro dello stage per 60 s; un talento triplica il danno con tutte e
       tre attive; i totem "da F2P non usarli, o solo al tetto di progressione".
   - **Cosa dicono i dati** (28/11): i trovati crescono come l'oro della run alla ~0,4
     (`SpecialGameStatistics.save.totalGoldThisRun`): al muro l'oro arriva lineare, quindi minuti
     d'oro in più (consumabili, pergamene) fanno crescere i trovati prima dell'Empower; per lo stesso
     motivo lo stage di farm si sceglie sull'oro al minuto, non sui trovati.
   - **Stato:** il probe `items` (dal 28/11 04:00) dice quantità, durate e dove sono i pulsanti "usa";
     il push non c'è ancora. Proposta: un push al giorno al muro, solo su -27/-28 per la prova, ogni
     consumo confermato dalla quantità che scende; i consumabili d'oro dal Pouch, con le pergamene
     attive e allo stage di farm scelto; Midas solo se la prova mostra che alza i trovati.
3. **Premi che il bot non chiede mai.** Nel catalogo delle richieste al server (`SocketFunctions`, 250
   metodi) ci sono claim che nessun task fa:
   - `ClaimMemberRewardsFromGuildItems`: oggetti distribuiti dalla gilda ai membri, menu
     `GuildDistribution`. Va chiesto all'utente se la sua gilda li distribuisce.
   - `ClaimFreeOrbsOfProphecy`: Tower of Souls, vedi "Flussi nuovi", punto 7.
   - `ClaimCampaignRewards`: da capire con una sonda (`HDSAccount.ClaimCampaignRewardsCallback`).

## Ordine dentro il branch

1. Fase 0 (sonda).
2. A (badge): una modifica, 26 task, più il log più pulito.
3. F (connessione).
4. I due sprechi misurati: Talents (vedi "Flussi nuovi", punto 2) e Oracle's Gift.
5. Ricerca e spedizioni coi dati, più la curva dell'Empower ("Flussi nuovi", punti 1, 3 e 4).
6. B e C sugli altri task in grassetto nella tabella, dal più costoso.
7. D (navigazione), partendo da Map Missions e Library.
8. E (attese) e `interaction_delay = 0.5`.
9. G e H.
10. Le 12 ore.

## Cosa non fare

- Non chiamare funzioni di rete, callback del server o metodi fuori dalla lista della regola 1.
- Non scrivere campi del gioco.
- Non togliere la strada di oggi (UI) dal codice: resta come riserva anche dopo il merge.
- Non fare deploy del branch su altre istanze prima della fine delle 12 ore.

## Altro dal dump (REA), non ancora in una leva

- `FunctionsSent` e `FunctionsReceived`: l'elenco di tutti i messaggi del protocollo col server.
  Con un postfix di sola lettura su `SocketAPI.SendSocketMessage` si contano le richieste per task,
  utile a dimostrare che il branch non manda più richieste di main.
- `DOTween` (le animazioni della UI) ha un `timeScale`: renderebbe istantanee le transizioni dei menu.
  È solo grafica lato client, ma qualche flusso del gioco potrebbe aspettare la fine di
  un'animazione. Da provare solo dopo le 12 ore, e solo se le attese della leva E non bastano.
- REA può anche decompilare il codice nativo (`rea decompile`, serve Ghidra con Java 21: oggi c'è
  il 17). Servirebbe a leggere il corpo di un metodo prima di chiamarlo, per esempio cosa controlla
  `OpenMenu`. Non serve per le leve qui sopra.

## Domande aperte

- `HasNotification` o `IsNotificationActive`? Lo dice la fase 0.
- Unità e aggiornamento di `timeLeft`, base dei timestamp. Lo dice la fase 0.
- Argomenti di `OpenMenu` per ogni schermata. Lo dice la fase 0 (`menuStack`).
- Campi ancora da trovare: quantità di Free Pickaxes, Emblem Market, dati gear e jewel di Hall of
  Heroes, costi di Awakening ed Evolution.
- `VariousVariables` (`lastEmpowermentTimestamp`): il timestamp sta in un `Save` annidato.
