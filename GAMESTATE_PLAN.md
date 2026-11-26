# Piano: il bot legge e naviga dal codice del gioco, non dallo schermo

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
     metà.
   - **RAM:** memoria privata di Steam-27 contro quella di Steam-26 e Steam-28 a pari uptime
     (`Get-Process Firestone`, `PrivateMemorySize64`). Vedi la sezione "RAM". Il conteggio dei minuti è il regex `\[Task\] (.+?) finished in ([\d.]+)s` sul `Latest.log`
     della box, aperto in condivisione (`[IO.File]::Open(path,'Open','Read','ReadWrite')`).
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

## Flussi da ripensare con i dati (decisioni migliori, non solo più veloci)

Ognuno di questi va deciso con l'utente: cambia cosa fa il bot, non solo come lo fa.

1. **Ricerca (Firestone e Meteorite).**
   - **Oggi:** apre le anteprime dei nodi una per una e sceglie per nome inglese, se no il primo
     della griglia. Su Steam-27, il 26/11, la scelta ha preso 55 s per Meteorite (17:19:39 →
     17:20:34, "node #12, priorityRank=none") e 28 s per Firestone ("#13, priorityRank=none"), ogni
     ora.
   - **Firestone, dai dati:** `FirestoneResearchHandler.researchTrees[currentTree]` ha ogni nodo con
     `level`, `levelCap`, `realTimeRequired`, `researchState`, `IsUnlocked`,
     `firestoneResearchType` (enum) e `researchTier`. La scelta si fa in memoria, poi un solo click sul
     bottone del nodo (`firestoneResearchInteraction`). Proposta: priorità per enum, poi il
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
3. **Spedizioni.**
   - **Oggi:** parte la prima in attesa.
   - **Dai dati:** `guildManagement.expeditionData.expeditions` le elenca tutte con
     `expeditionTokenReward`, `guildCoinReward` e `timeReq`. I token spedizione sono la risorsa che
     frena (Personal Tree a 50 prima delle War Machines), quindi si può scegliere quella con più token
     all'ora, oppure quella che finisce prima del prossimo controllo.
4. **Empower.**
   - Il gioco ha un suo segnale: il badge `TemplePrestige` (`NotificationHandler.PrestigeNotificationCheck`,
     `prestigeNotificationMultiplier`), visto decine di volte per istanza il 26/11. Il bot non lo usa.
   - Con `firestones.firestonesEarnedThisRun` letto gratis a ogni scan (oggi il Temple si apre 38
     volte in 5 ore) si può misurare quanto crescono i Firestones nella run, e fare il reset quando la
     crescita rallenta invece di aspettare il muro.
   - Prima si registra la curva nel log durante le 12 ore, poi si decide coi numeri.
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

## RAM

- **26/11 alle 22:20, su PC-Guesly:** 12 istanze; ognuna 0,85-1,15 GB di working set e 1,4-1,9 GB di
  memoria privata. La memoria privata cresce con l'uptime: circa 1,45 GB appena riavviata, circa
  1,9 GB dopo 5 ore.
- Il riavvio delle 17:00 ha lasciato spente Steam-29..31 ("0,6 GB available, under 1.5"), ed erano
  ancora spente alle 22:20. Il limite della flotta è la RAM.
- **Ipotesi:** aprire i menu carica asset (`PlayerActivityBlockReason.Addressables`) che poi restano
  in memoria, quindi meno aperture vorrebbero dire meno crescita. Il criterio RAM delle 12 ore serve a
  verificarlo.
- **Possibile leva dopo le 12 ore:** `Resources.UnloadUnusedAssets()` dopo `CloseToMainScreen()`. È
  un'API di Unity, solo lato client; va misurata.

## Ordine dentro il branch

1. Fase 0 (sonda).
2. A (badge): una modifica, 26 task, più il log più pulito.
3. F (connessione).
4. I due sprechi misurati: Talents e Oracle's Gift.
5. B e C sui task in grassetto nella tabella, dal più costoso.
6. D (navigazione), partendo da Map Missions e Library.
7. E (attese) e `interaction_delay = 0.5`.
8. G e H.
9. Le 12 ore.

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
