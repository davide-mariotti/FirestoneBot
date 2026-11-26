# Piano: leggere lo stato dal gioco, non dallo schermo

> Scritto il 2026-11-26, da un'analisi con REA della build del 06/11 (metadata IL2CPP v31).
> Solo analisi: niente codice ancora. L'utente vuole che il bot dipenda il meno possibile da
> schermate, testi e badge visibili, e legga i dati dagli oggetti del gioco.
>
> I nomi e i tipi di campi e metodi sono **osservati** nei metadata. Il loro significato (unità dei
> timer, quando si aggiornano, base dei timestamp) è **dedotto dai nomi**. Ogni fase parte quindi in
> ombra: legge il dato del gioco accanto a quello di oggi, li scrive entrambi nel log e decide ancora
> con quello di oggi finché non coincidono.

## Contesto per chi parte da zero

- Il codice del gioco è in `CoreASM.dll`, che il bot vede come `Il2CppCoreASM.dll` (interop di
  MelonLoader, namespace `Il2Cpp`). Non è `Assembly-CSharp`.
- **Elenco completo di tipi, campi e metodi:** `tests/GameApi/CoreASM.dll.api.txt`, con le stringhe in
  `tests/GameApi/strings.txt`. Sono gitignorati (repo pubblico). Se mancano, o dopo un aggiornamento
  del gioco, rigenerali col comando in testa a `tests/GameApi/dump_api.py`. REA è installato (`rea`,
  npm globale). Da Git Bash i path si rompono, quindi va lanciato da PowerShell. Il gioco da usare è
  quello di una box (build più nuova), per esempio
  `C:\Sandbox\dmari\SteamB17\drive\C\Program Files (x86)\Steam-17\steamapps\common\Firestone`.
  Per cercare: `awk '$0=="type NomeTipo"{p=1;print;next} /^type /{p=0} p' CoreASM.dll.api.txt`.
- **Come si arriva ai dati:** `GameInitialize.HandlerLoader.<Handler>` (circa 100 handler, il bot oggi
  ne usa circa 15) e `GameInitialize.CoreFunctionality.<x>` (`serverUpdate`, `socketAPI`). I valori
  `Obscured*` vanno letti con `.GetDecrypted()`. Ci sono già esempi da copiare:
  `WarfrontLiberationMissions.HasAvailable`
  ([WarfrontDailyMissions.cs:36](src/GameModel/Features/Map/WarfrontCampaign/WarfrontDailyMissions.cs#L36)),
  `MapMission.FreeSquads`, `TreeOfLife.PersonalLevel`, `BotManager.LogSnapshot`
  ([BotManager.cs:228](src/Core/BotManager.cs#L228)).
- **Scheduler:** un task è pronto quando `NextRunTime` è passato oppure quando il suo badge sulla barra
  è visibile, con `BadgeCooldown` di 30 minuti
  ([BotTask.cs:181](src/Core/Tasks/BotTask.cs#L181)). I due punti d'aggancio sono
  `IsNotificationVisible()` (virtual, [BotTask.cs:190](src/Core/Tasks/BotTask.cs#L190)) e il setter di
  `NextRunTime` ([BotTask.cs:73](src/Core/Tasks/BotTask.cs#L73)). `WarfrontDailyMissionsTask` fa già
  così: sovrascrive `IsNotificationVisible` chiedendo al gioco invece che al badge.

## Regole per tutte le fasi

1. **Prima in ombra.** Si legge il dato del gioco, lo si scrive nel log accanto a quello di oggi e si
   decide ancora con quello di oggi. Si cambia solo dopo che coincidono per qualche ora su una box,
   poi su tutti gli account. Il primo test va fatto su una box, poi la flotta: vedi TESTING.md e le
   solite regole di rollout.
2. **Se la lettura fallisce, si torna a quello di oggi.** Un aggiornamento del gioco può rinominare un
   campo. MelonLoader rigenera l'interop, e il metodo del bot che lo usa lancia
   `MissingFieldException`/`MissingMethodException` quando viene compilato al volo. Ogni lettura va
   quindi in un metodo piccolo e separato, chiamato dentro un try/catch che restituisce `null`: con
   `null` si torna alla lettura dalla UI. Ogni fallback va scritto nel log **una volta per sessione**,
   così un aggiornamento che rompe qualcosa si vede dal primo giorno.
3. **Solo lettura.** Mai chiamare metodi del gioco che fanno qualcosa (claim, start, buy) e mai
   scrivere campi. Il README promette che il bot fa solo quello che farebbe un giocatore. Si possono
   chiamare solo metodi che leggono, come `HasAvailableMissions()`, già in uso.
4. **Tempo.** I `timeStarted`/`lastClaim` sono probabilmente secondi del server, non ancora
   verificato. Si convertono con l'orologio del gioco,
   `GameInitialize.CoreFunctionality.serverUpdate.serverTimeNow`:
   `DateTime.Now + TimeSpan.FromSeconds(fine - serverTimeNow)`. Mai confrontarli con l'orologio del PC.

## Fase 0: sonda (non cambia il comportamento)

Una riga in più in `LogSnapshot` (una volta all'ora), dentro il suo try/catch, con i valori grezzi dei
campi delle fasi 1 e 2 accanto a quelli che il bot legge oggi. Serve a rispondere a queste domande:

- `NotificationHandler.HasNotification(t)` e `IsNotificationActive(t)`: quale dei due corrisponde al
  badge acceso sulla barra (`Notifications.Lit`)?
- I `timeLeft` sono in secondi? Si aggiornano anche a schermata chiusa?
  `AlchemyExperimentHandler.UpdateRemainingTimes()` fa pensare di sì.
- `serverTimeNow` è in secondi Unix? È la base di `timeStarted`?

Basta lasciarla girare qualche ora su Steam-0. Il risultato va scritto in questo file, in una sezione
"Stato", come negli altri `*_PLAN.md`.

## Fase 1: badge dal `NotificationHandler` (una modifica in `BotTask`)

È la leva più grossa. Il gioco calcola i badge in `HandlerLoader.NotificationHandler`, con
`HasNotification(NotificationType)` e `IsNotificationActive(NotificationType)`: entrambi hanno un corpo
nativo. L'enum `NotificationType` ha circa 50 voci, e i nomi dei badge che il bot usa
(`Paths.BattleLoc.NotificationsLoc`) coincidono con le voci dell'enum, tranne tre:

| Nome nel bot | `NotificationType` |
|---|---|
| `Warmachines` | `WarMachines` (basta `Enum.TryParse` con `ignoreCase`) |
| `OracleRituals` | `OracleRitual` |
| `OracleBlessings` | `OracleBlessing` |

- In `BotTask.IsNotificationVisible()`: se `NotificationBadgeName` corrisponde a una voce dell'enum, si
  chiede al gioco. Se l'handler è `null` o la lettura fallisce, si guarda la barra come oggi. Una sola
  modifica copre i 26 task con `NotificationBadgeName`.
- Vanno mappati a mano i task con `NotificationPaths`:
  - `DailyStoreOffersTask` → `CheckIn` e `MysteryBox`
  - `ChaosRiftTask` → `ChaosRift` e `ChaosRiftSupplies`
  - `PharaohsVaultTask` → `PharaohsVault` e `ScarabGameMilestones`
  - `GuardianEvolutionTask` → `GuardianEvolution` (e forse `GuardianRarity`)
  - `SystemMail`, `PathOfGlory`, `EventTask` non hanno una voce: restano sulla UI o passano alla
    fase 2 (`BattlePassHandler.unclaimedRewards`).
- Effetto collaterale utile: `Notifications.Lit` può leggere i badge accesi dal gioco invece che dalla
  barra. L'utente vuole 0 badge su tutti gli account, tranne War Machines, quindi conviene metterlo nel
  report giornaliero.
- Si cambia con calma: badge e barra devono coincidere per qualche giorno.

## Fase 2: timer dai dati invece che dal testo (`NextRunTime`)

Oggi molti task aprono la schermata solo per leggere un timer come testo (`GameText.Time`). Con il dato
del gioco il task sa quando sarà pronto senza aprire niente.

| Task | Oggi (testo a schermo) | Campo del gioco (da verificare in fase 0) |
|---|---|---|
| Firestone Research | `ResearchPanel.cs:46` | `FirestoneResearchHandler.firestoneResearchSlots[]`: `timeLeft`, `timeStarted`, `researchOnProgress` |
| Experiments | `Experiments.cs:75` | `AlchemyExperimentHandler.experimentSlot[...]`: `timeLeft`, `timeRequired`, `ExperimentState` |
| Map Missions | `MapMission.cs:10`, `MissionPreview.cs:21`, `MissionPin.cs:18` | `MissionsHandler.missionsInProgress[]` (`realTimeReq`, `timePassed`, `timeStarted`, `missionState`), `TimeLeftForNewMissions` |
| Expeditions | `Expedition.cs:16,20` | `HandlerLoader.guildManagement.expeditionData`: `activeExpedition`, `activeExpeditionStartTime`, `activeExpeditionTimePassed`, `expeditionsTimeRefreshed`; durate in `ExpeditionsHandler.expeditionTimeReq` |
| Oracle Rituals | `Rituals.cs:53-63` | `OracleHandler.activeRitual` (`timeStarted`, `state`), `ritualDuration`, `ritualsQueued` |
| Oracle's Gift | `OracleStore.cs:36` | `OraclesGiftDailyClaimHandler.lastClaim`, `expectClaim` |
| Mystery box giornaliera | badge | `MysteryBoxDailyClaimHandler.lastClaim`, `expectClaim` |
| Engineer | `Engineer.cs:14` | `NotificationHandler.canClaimTools` (bool); timer probabilmente in `WarMachineHandler.lastClaimedTime`/`timeReq` (dedotto solo dal nome) |
| Guardian Training | `MagicQuarters.cs:17` | badge `GuardianTraining` (fase 1). `MagicQuartersInfoSubmenu.lastTrainGuardinaTime` è un componente della UI: meglio non usarlo |
| Quests (rinnovo) | `CharacterScreen.cs:41` | `serverUpdate.nextGameDay`, `timeLeftForNextDay`, `nextGameWeek` |
| Empower | `TempleOfEternals.cs:22-35` | `HandlerLoader.firestones.firestonesEarnedThisRun` (ObscuredBigDouble); Firestones posseduti da `CurrencyHandler` |
| Chaos Rift | badge | `ChaosRiftHandler.canAttackUntilTime`, `currentBuffEndTimer` |
| Talents | badge | `TalentsHandler.availablePoints` |
| Path of Glory | badge | `BattlePassHandler.unclaimedRewards` |
| Scarab free token | badge | `ScarabGameHandler.expectDailyFreePharaohTokenClaim` |
| Warfront Loot | `WarfrontLoot.cs:12` | non trovato: domanda aperta |

Schema: `NextRunTime = daGioco ?? daUi`, prima in ombra (si scrivono entrambi). Il passo successivo,
quello che fa risparmiare navigazione, è decidere se il task è pronto **senza aprire la schermata**.

`GameDay` può prendere il reset da `serverUpdate.nextGameDay` invece di calcolarlo. Oggi legge già
l'ora da `TimeLibrary.officialServerTimeResetHour`, quindi il guadagno è piccolo.

## Fase 3: valori letti come testo, e la dipendenza dall'inglese

Il README dice che titoli degli eventi, oggetti dello shop eventi e priorità delle ricerche funzionano
solo in inglese. I dati del gioco usano enum, che non dipendono dalla lingua:

- **Sfide degli eventi:** `CalendarEventsHandler.eventChallengesProgress`
  (`Dictionary<CalendarEventChallenges, CalendarEventChallengeData>`) e `calendarEvents`, al posto di
  titolo e progresso letti come testo (`DecoratedHeroesShop.cs:38`, `ChallengeParser`,
  `EventTask.cs:67`). Per i mini-eventi c'è `MiniEventsHandler`, ancora da esplorare.
- **Priorità delle ricerche:** l'enum `FirestoneResearchType` (`RainingGold`, `Prestigious` = Firestone
  Finder, `FirestoneEffect`, ...) invece dei nomi inglesi in `FirestoneResearchTask.cs:63`.
- **Costi letti come testo:** `Awakening.cs:71`, `HallOfHeroes.cs:96-98`, `WarMachines.cs:52`,
  `MagicQuarters.cs:31`. I campi corrispondenti vanno cercati nel dump, tipo per tipo: non è ancora
  stato fatto.

## Fase 4: connessione col server

Oggi il bot non sa se il gioco è disconnesso: in `src` non c'è niente su
disconnect/reconnect. `GameInitialize.CoreFunctionality.socketAPI` ha
`IsConnectedAndNotReconnecting()` (con corpo nativo), `isWaitingToReconnect` e `socketDataLoaded`.

- `BotManager` non avvia un task mentre il gioco non è connesso, e scrive nel log quanto dura la
  disconnessione.
- Costa poco ed evita click e retry a vuoto durante le riconnessioni.

## Fase 5: osservare le risposte del server con Harmony (solo postfix, solo log)

`0Harmony` è già referenziato nel csproj ma non è usato. I postfix servono solo a osservare, sono
l'ultima fase e sono la parte più rischiosa:

- `SocketAPI.ServerErrorMessage(string Error)` e `ShowDefaultServerError()` (con corpo nativo): dicono
  quando e con quale codice il server rifiuta qualcosa. Possono sostituire il controllo del popup
  `WarfrontLiberationMissions.FightRefused`
  ([WarfrontDailyMissions.cs:41](src/GameModel/Features/Map/WarfrontCampaign/WarfrontDailyMissions.cs#L41))
  e danno l'elenco degli errori su tutta la flotta.
- Callback di conferma, per esempio `AlchemyExperimentHandler.StartExperimentCallback` e
  `CompleteExperimentCallback`: confermano che un'azione è andata a buon fine senza interrogare la UI
  in polling.
- Attenzione: in IL2CPP un metodo può essere inlineato. Prima di contare su una patch, va verificato
  che scatti davvero, con un log alla prima chiamata.

## Cosa non fare

- Non chiamare i metodi del gioco che agiscono (claim, start, buy, upgrade) al posto dei click:
  rompe la promessa del README, e ci sono ACTk e la validazione del server.
- Non scrivere campi del gioco.
- Non togliere la lettura dalla UI finché il dato del gioco non è verificato su tutta la flotta per
  qualche giorno.

## Domande aperte

- `HasNotification` o `IsNotificationActive`? Lo dice la fase 0.
- Unità e frequenza di aggiornamento di `timeLeft`, e base dei timestamp. Lo dice la fase 0.
- Da dove viene il timer del Warfront Loot.
- I costi della fase 3.
- `VariousVariables` (`lastEmpowermentTimestamp`): come ci si arriva. Il campo è
  `HandlerLoader.variousVariables`, ma il timestamp sta in un `Save` annidato.

## Ordine consigliato

Fase 0, poi 1 (badge: un cambio, molti task), poi 4 (connessione: poco codice), poi 2 (timer, task per
task, partendo da Research, Experiments, Map Missions ed Expeditions), poi 3, poi 5.
