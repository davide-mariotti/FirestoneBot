# Piano: progressione nella campagna del Warfront

> Scritto il 2026-10-03, da implementare in una sessione nuova. Le regole di lavoro sono quelle di
> TESTING.md ("Per l'agente": Contesto, Regole, Comandi) e valgono anche qui: i path si scrivono solo
> dopo averli visti dal vivo, mai gemme né acquisti, una correzione alla volta con la prova nel commit.
> Punto di ritorno: il tag `pre-campaign-2026-10-03` (commit a13bdfd) e la copia di cfg e DLL di
> Steam-0 in `C:\Repos\FirestoneBot-test-backup\2026-10-03-pre-campaign`.

## 0. Come partire

Da incollare in una sessione nuova di Claude Code (cartella `C:\Repos\FirestoneBot`), avviata con i
permessi che le evitano di chiedere conferma a ogni comando:

```text
Leggi CAMPAIGN_PLAN.md e seguilo dall'inizio alla fine, con le regole di TESTING.md e la skill
ponytail attiva. Prima fammi le domande della sezione 8 che non hanno ancora risposta, poi la sonda
dal vivo su Steam-0 (sezione 3), il task (sezione 4) un passo alla volta, il test (sezione 5) e la
distribuzione (sezione 6). Fai un commit per ogni passo verificato dal vivo, aggiorna TESTING.md e
MULTI_INSTANCE_SETUP.md, poi push. Se la sonda contraddice il piano (un costo, un limite di
battaglie, una formazione che cambia anche l'Arena), fermati e chiedimi.
```

## 1. Cosa deve fare il task (richiesta dell'utente, 03/10)

Un task nuovo e separato, `WarfrontCampaignTask` (gruppo Warfront, livello 50, come Daily Missions).

- **Formazione**: schierare la formazione più potente possibile con le War Machine dell'account.
  Cinque macchine, nell'ordine **tank, poi damage, poi healer**, e in ogni macchina gli eroi della
  crew.
- **Progressione**: far avanzare la campagna il più possibile, in due direzioni: la missione più
  alta non ancora vinta (Easy) e, sulle missioni già fatte, la difficoltà successiva (una stella in
  più).
- **Senza far perdere tempo al bot**: tentare una battaglia solo quando la potenza della formazione
  lo giustifica, in base alla potenza dei nemici di quella missione. Le soglie si prendono dal gioco
  se possibile, altrimenti dalla formula della wiki (sezione 2.1, già verificata).
- **Perché**: ogni vittoria dà una stella e una ricompensa unica (forzieri jewel, Emblem of Valor,
  blueprint). Le stelle sbloccano funzioni (sezione 2.1). Steam-0 ne ha 42: a 70 si aprono i dungeon
  delle missioni giornaliere, a 100 il forziere Diamond dell'Emblem market.
- **Mai**: spendere qualcosa (le battaglie della campagna dovrebbero essere gratuite: la sonda lo
  conferma), cliccare `getMore` sui contatori, chiudere una battaglia in corso (il `closeButton` di
  `WFBattle` la abbandona, vedi `Paths.WFBattleLoc`), toccare le formazioni dell'Arena.

## 2. Cosa si sa già

### 2.1 Dalla wiki (`docs/wiki/pages/Warfront_Campaign.html`, `War_Machines.html`)

- **Struttura**: 90 missioni, 5 difficoltà (Easy, Normal, Hard, Insane, Nightmare). Aree: 1-12
  Riverlands, 13-22 Frostfire, 23-26 Ebony Jungle, 27-29 Trinity Islands, 30-39 Ebony Jungle, 40-50
  The Cauldron, 51-57 Silverwood, 58-64 Celenis, 65-70 Hinterlands, 71-80 Dreadland, 81-85 Doomfire
  Island, 86-90 Eastrock Island. Insane si sblocca a 190 stelle, Nightmare a 319.
- **Sblocco**: la missione m+1 richiede m vinta almeno a Easy; una difficoltà richiede quella prima
  vinta sulla stessa missione (popup: "Beat previous difficulty"). Da verificare se Normal di m
  richiede anche Normal di m-1.
- **Battaglia**: si vince battendo le 5 macchine nemiche entro 20 round. Le macchine nemiche sono
  identiche fra loro, senza abilità speciali.
- **Nemici**: `attributo = base × moltiplicatore difficoltà × 1,2^(m-1) × 3^floor((m-1)/10)`, con base
  Damage 260, Health 1.560, Armor 30 e moltiplicatori Easy 1, Normal 360, Hard 2.478.600, Insane
  5,8×10^12, Nightmare 2,92×10^18.
- **Potenza di una macchina**: `(10×damage)^0,7 + health^0,7 + (10×armor)^0,7`. La squadra è la
  somma delle 5 macchine.
- **Potenza richiesta**: 30% della potenza nemica per le missioni 1-10 Easy, 50% per le 11-30 Easy,
  80% per tutte le altre. Il valore è arrotondato per difetto alle centinaia. Nel calcolo della
  potenza il fattore ogni 10 missioni è 2, non 3.
- **Verifica**: la formula dà **esattamente** il "Power required 56.463.500" che il gioco mostra per
  Riverlands 1 Hard (screenshot dell'utente, Steam-0). Codice usato per il conto:

  ```python
  MULT = [1, 360, 2478600, 5.8e12, 2.92e18]          # Easy .. Nightmare
  def enemy_power(m, d):
      f = MULT[d] * 1.2 ** (m - 1) * 2 ** ((m - 1) // 10)
      return 5 * ((10 * 260 * f) ** 0.7 + (1560 * f) ** 0.7 + (10 * 30 * f) ** 0.7)
  def required(m, d):
      pct = 0.3 if d == 0 and m <= 10 else 0.5 if d == 0 and m <= 30 else 0.8
      return math.floor(enemy_power(m, d) * pct / 100) * 100
  ```

- **Soglia e vittoria**: alla soglia la squadra ha l'80% della potenza nemica (missioni dalla 31 Easy
  in su, e da Normal in poi). La soglia permette il tentativo, non garantisce la vittoria: quanto
  margine serve davvero si misura nei test (sezione 5).
- **Stelle**: una per missione e difficoltà vinta. Sbloccano: 5 le liberation missions, 20 il
  bottone di velocità x2/x4, 70 i dungeon, 100 il forziere Diamond, 145 Opal, 190 Emerald e Insane,
  319 Platinum e Nightmare. Ogni 5 stelle sale il livello della campagna, che dà perk: Raining Gold e
  All Attributes ×2 per livello, più Miner, Explorer, Librarian, Alchemy, Cheers e Battle Cry.
- **War Machine**: 13 macchine, ognuna con una specializzazione.
  - Tank: Earthshatterer, Fortress, Goliath.
  - Healer: Curator, Hunter, Sentinel. Ogni round curano la macchina più avanti per il 200% del loro
    danno.
  - Damage: Aegis, Cloudfist, FireCracker, Talos, Harvester, Judgement, Thunderclap.
- **Crew**: fino a 6 eroi per macchina. I gioielli degli eroi aumentano gli attributi della
  macchina, e la crew entra nella battle power: `battle attribute = basic × (1 + crew bonus)`.

### 2.2 I numeri di Steam-0 oggi

Dagli screenshot dell'utente: 42 stelle (Easy fino alla 31, Normal fino alla 11), battle power della
squadra 785.790, prossima missione Ebony Jungle 32. Soglie dalla formula:

| Missione | Easy | Normal |
|---|---|---|
| 11 | | 676.400 |
| 12 | | 768.500 |
| 13 | | 873.100 |
| 31 | 372.200 | |
| 32 | 422.800 | |
| 33 | 480.400 | |
| 34 | 545.800 | |
| 35 | 620.100 | |
| 36 | 704.500 | |
| 37 | 800.400 | |
| 40 | 1.173.900 | |
| 41 | 2.166.600 | |

Con 785.790 sono tentabili Easy 32-36 e Normal 12: fino a 6 stelle, se le battaglie si vincono. Hard
parte da 56,5 milioni per la missione 1, quindi è lontano. La potenza richiesta sale di ~13,6% a
missione e di un ulteriore ×1,62 ogni 10 (alla 11, 21, 31…).

### 2.3 Dalle immagini dell'utente (03/10, Steam-0)

1. **Mappa della campagna** (Mappa → tab Warfront): "Campaign level 9" in alto a sinistra con le
   stelle del livello; il contatore delle stelle totali in alto a destra (42); "Next loot" con Claim
   (lo fa già `WarfrontCampaignLootTask`); bottoni Leaderboards e Daily missions; zoom in basso.
2. **Popup della missione** ("Ebony Jungle - Mission 32"): "Power 785.790" (la squadra), "Battle
   Formation", e tre colonne di difficoltà con la ricompensa. "Easy" è un bottone verde; Normal e
   Hard sono "Locked - Beat previous difficulty".
3. **Popup di una missione già fatta** ("Riverlands - Mission 1"): Easy e Normal "Completed"; Hard con
   "Power required 56.463.500" e il bottone grigio.
4. **Squad** (da "Battle Formation"): "Battle power: 785.790", "Save changes", e la lista "Choose your
   war machines" con livello e spunta delle 5 scelte (6 macchine su Steam-0, livelli 23, 23, 22, 22,
   16, 14). Ogni macchina in campo ha una fila di crew: avatar degli eroi, posti vuoti, lucchetto e
   matita.
5. **Select crew** (matita su Goliath): attributi della macchina, Power 315.764, "Selected heroes"
   (4 posti più lucchetto), "Save changes", "Order by" con tre bottoni (danno, salute, armatura?) e la
   lista degli eroi selezionabili. Su Steam-0 tutti e 10 gli eroi sono già in qualche crew.

### 2.4 Dal codice del gioco (nomi negli assembly IL2CPP di MelonLoader, non ancora visti dal vivo)

In `MelonLoader\Il2CppAssemblies\Il2CppCoreASM.dll` ci sono le classi e i campi che servono. È la
stessa strada dell'Emblem market, che legge `chestType` da `ExoticEmblemMarketGearChestInteraction`
con `GetComponentsInChildren<…>()` (`src/GameModel/Features/Town/ExoticMerchant.cs`):

- **Missioni**:
  - `WFCampaignMissionHandler` con `missionsDict`: dizionario int → `IWFCampaignMission` (`area`,
    `missionID`, `MissionIndex`);
  - `WFCampaignMissionMapInteraction`: con ogni probabilità il pin di una missione sulla mappa;
  - `WFCampaignMissionsWonRaw`, `missionsWon`, `modesWon`, `campaignStarsDict`, `totalStars`.
- **Popup**: `WFCampaignMissionPreviewPopup` e `WFCampaignMissionPreviewViewController`, con
  `EasyMode`, `normalMode`, `hardMode`, `insaneMode`, `nightmareMode`, `modeState`, `powerReqText`,
  `myFormationBattlePower`.
- **Difficoltà**: `WFCampaignModeInteraction` e `WFCampaignModeState`, `BattleDifficulty`.
- **Soglie**: `modePowerReqDict`, `modePowerMultiplier`, `modeDataDict`, `PowerRequired`. È la potenza
  richiesta per difficoltà, cioè la soglia direttamente dal gioco.
- **Formazione**:
  - `SquadsEngineerSubmenu`, `warMachineFormation`, `WarMachineFormationSettingSpots`,
    `formationSpot`, `BattleFormationSlot`;
  - `currentFormationPowerBattle`, `formationBattlePowerText`;
  - `editFormationButton`, `editCrewButton`, `addCrewButton`, `setCrewButton`;
  - `crewHeroIds`, `tempCrewHeroIds`, `crewBonuses`, `heroesOnCrewList`;
  - un `bestSquadButton`: se è della formazione del Warfront e fa "squadra migliore" da solo, il
    passo A si riduce a un click (da vedere nella sonda).
- **Arena**: `attackerFormation` e `defenderFormation` sono separate, quindi la formazione di battaglia
  dovrebbe essere un'altra. Da confermare.

### 2.5 Codice da riusare

- **Battaglia**: `WFBattleSim.Fight`, `WFBattle.IsVisible`, `WFBattleResult.IsDecided` e `.Close`
  (`src/GameModel/Features/Map/WarfrontCampaign/WarfrontDailyMissions.cs`), già al lavoro ogni giorno
  in Daily Missions.
- **Navigazione**: `WorldMap.Open` e `WorldMap.OpenWarfrontCampaignTab` (`src/GameModel/Features/Map/WorldMap.cs`).
- **Elementi del gioco**:
  - click su celle senza path univoco: `button.onClick.Invoke()` su un `Transform` (`HallOfHeroes.TrySelectHero`);
  - testi e icone: `GameText`, `IconSprite.NameAt`;
  - attese: `Poll.Until`, `Poll.ClickUntil`.
- **Stato persistente**: una entry auto-gestita del cfg, come `hero_snapshot` di Hall of Heroes.
- **Sonda dal vivo**: lo schema usato il 3/10 per la rarità degli eroi. Una classe temporanea che
  percorre i `Transform` attivi e scrive path, testo TMP, sprite e stato dei bottoni. Va aggiunto il
  dump dei campi dei componenti `WFCampaign*` letti via interop. Si toglie prima del commit.
- **War Machine**: la lista e la potenza di ogni macchina sono anche in `menus/WarMachines`
  (`Paths.WarMachinesLoc.MachineGridRoot`, `bg/warMachinePreview/info/power` nel dump statico), già
  usato da `WarMachinesTask`.

## 3. Sonda dal vivo (Fase 0, su Steam-0, senza combattere)

Come per la rarità degli eroi: build con la sonda, Steam-0 con il task messo in coda, lettura del log,
sonda tolta. Domande a cui rispondere, ognuna con il path o il campo visto:

1. **Pin delle missioni**: dove stanno (probabilmente sotto `menusRoot/mapRoot/...`, come i pin delle
   map missions), uno per missione? Quale componente portano? Danno numero della missione,
   difficoltà vinte e stato? Si aprono con `onClick.Invoke` anche fuori dallo schermo, senza
   scorrere la mappa?
2. **Dati delle missioni**: `missionsDict` e `modePowerReqDict` si leggono? Per 3-4 missioni e
   difficoltà, la potenza richiesta del gioco coincide con la formula della sezione 2.1? Se coincide
   sempre, il task può usare il dato del gioco (meglio) e tenere la formula solo come controllo nel log.
3. **Popup della missione**: path del popup e dei 5 bottoni di difficoltà, testi e stati (completed,
   locked, available, potenza insufficiente), `powerReqText`, potenza della squadra. Cosa apre il click
   su una difficoltà: `WFBattleSim` come le daily, o direttamente `WFBattle`?
4. **Formazione**: da "Battle Formation" (popup) e da Ingegnere → Squads → `editButton`. Servono:
   - i 5 posti e quale è il davanti (dove va il tank);
   - la lista delle macchine con spunta, livello e nome;
   - la potenza di ogni macchina (o dove leggerla);
   - la specializzazione (dal nome con la tabella della wiki, o da un campo);
   - il testo "Battle power", e se si aggiorna prima di "Save changes";
   - il bottone "Save changes" e cosa succede chiudendo senza salvare;
   - `bestSquadButton`: c'è, e cosa fa?
5. **Crew**: il popup "Select crew". Servono:
   - i posti, sbloccati e col lucchetto, e cosa li sblocca;
   - la lista degli eroi: mostra solo quelli liberi?
   - "Order by", "Save changes", la potenza della macchina;
   - un eroe può stare in una sola crew?
6. **Battaglia** (una sola, Easy 32 su Steam-0, solo dopo aver visto i punti 1-3):
   - quanto dura;
   - c'è il bottone x2/x4 (Steam-0 ha più di 20 stelle) e resta impostato?
   - popup di vittoria (`WFBattleWon`) con forzieri o ricompense da chiudere;
   - il contatore delle stelle sale;
   - nessun costo e nessun limite di tentativi.
7. **Arena**: cambiare la formazione di battaglia tocca le formazioni dell'Arena? (Attesa: no.)

## 4. Il task

### 4.1 Struttura

- `src/Tasks/Map/WarfrontCampaignTask.cs`, `src/GameModel/Features/Map/WarfrontCampaign/` per il
  modello (accanto a `WarfrontDailyMissions.cs`), path in `src/Infrastructure/Paths/Map.cs`.
- **Quando**: ogni 6 h (`RecheckDelay`), e acceso di default nel template. Un giro senza niente da
  tentare deve costare pochi secondi.
- **Durata**: `MaxRuntimeSeconds` alto quanto basta per qualche battaglia (es. 900 s, da tarare sulla
  durata vista nella sonda).
- **Stato nel cfg**, auto-gestito:
  - la potenza delle macchine all'ultima ottimizzazione della formazione;
  - per ogni missione e difficoltà persa, la potenza della squadra al momento della sconfitta
    (es. `"33:0@785790;12:1@785790"`).
- Nessuna chiave di configurazione oltre `enabled`, salvo che i test la rendano necessaria.

### 4.2 Passo A: la formazione più forte

Si fa al primo giro, poi solo quando la potenza di una macchina è cambiata: livelli di
`WarMachinesTask`, rarità di `WarMachineRarityTask`, gioielli degli eroi da Hall of Heroes.

1. Se la sonda trova un `bestSquadButton` che fa questo lavoro, si usa quello e si legge la battle
   power prima e dopo.
2. Altrimenti:
   - **Macchine**: le 5 con la battle power più alta, in campo nell'ordine tank → damage → healer
     (davanti il tank: gli healer curano la macchina più avanti).
   - **Crew**: ogni eroe libero va in un posto vuoto, perché un eroe fuori da ogni crew non dà
     niente. Spostamenti più fini, come un eroe dove i suoi gioielli rendono di più, solo se i test
     mostrano che cambiano la battle power in modo apprezzabile: si provano leggendo la potenza prima
     e dopo.
3. "Save changes" e controllo: la battle power salvata non deve essere più bassa di prima. Se lo è, si
   rimette la formazione di prima e si scrive un `[FAILED]`.
4. **Riga di log**: `Campaign formation: <macchine in ordine>, battle power A -> B.`

La formazione è la stessa che usa Daily Missions (da confermare nella sonda), che quindi ne
beneficia. Il commento di `WarfrontDailyMissionsTask` ("The formation is set up by hand once") va
aggiornato.

### 4.3 Passo B: le battaglie

1. **Stato**: missioni e difficoltà vinte (dai dati del gioco della sonda, o dai pin), stelle totali,
   battle power della squadra.
2. **Candidati**: ogni coppia (missione, difficoltà) che non è vinta, è sbloccata (sezione 2.1:
   missione precedente a Easy, difficoltà precedente sulla stessa missione), ha la potenza richiesta
   non oltre la battle power, e non è già stata persa con una potenza simile. Una sconfitta si ritenta
   solo quando la battle power è salita almeno del 5% rispetto a quella della sconfitta (valore da
   rivedere coi test).
3. **Ordine**: dalla soglia più bassa alla più alta, così le battaglie più probabili vengono prima e
   missioni nuove e difficoltà nuove si alternano da sole. Con Steam-0 oggi: Easy 32 (422.800),
   33, 34, 35, 36, Normal 12 (768.500).
4. **Stop alla prima sconfitta** del giro: i candidati dopo hanno soglie più alte. Al massimo N
   battaglie per giro (es. 10), così il task non occupa lo scheduler per mezz'ora.
5. **Una battaglia**: pin, popup, si controlla che il bottone della difficoltà sia cliccabile (il
   gioco conferma soglia e sblocco), click, `WFBattleSim.Fight` se compare, attesa del risultato,
   chiusura del popup di vittoria o sconfitta e di eventuali ricompense.
6. **Riga di log**: `Campaign mission 33 Easy: required 480400 (game 480400), power 785790, ratio 1.64 -> won, stars 42 -> 43.`
   Il campo `game` è la soglia letta dal gioco, se diversa dalla formula. La riga va tenuta così:
   serve a tarare il 5% e a vedere quale margine basta per vincere.

### 4.4 Mai

- Non tocca i bottoni di acquisto o `getMore`, né le formazioni dell'Arena.
- Non chiude una battaglia in corso.
- Non ritenta in loop una missione persa: vedi la regola del 5%.

## 5. Test (TESTING.md, "Fase 2", solo Steam-0)

1. **Formazione**: un giro con il task isolato (gli altri task spenti, come nel punto 3 della Fase 2).
   Battle power prima e dopo, formazione nell'ordine giusto, nessun eroe libero. Daily Missions al
   giro dopo combatte con la formazione nuova.
2. **Battaglie**: Easy 32 per prima; poi le altre del giro fino alla prima sconfitta. Per ogni
   battaglia, la riga di log e il contatore delle stelle.
3. **Giro a vuoto**: con niente da tentare il giro deve durare pochi secondi.
4. **Sconfitta**: la missione persa non va ritentata al giro dopo con la stessa potenza.
5. Aggiornare TESTING.md: una riga nuova in "Map e Warfront", e la riga di Daily Missions se cambia
   la formazione.

## 6. Distribuzione e documenti

- Template (`tools/ConfigTemplate/FirebotPreferences.template.cfg`): `[warfrontcampaigntask]
  enabled = true`. È una chiave nuova: al primo avvio con la DLL nuova la sezione nasce da sola, poi
  `apply_template.ps1 -Check` deve dare 0 differenze (come per l'Emblem market).
- Steam-0..16 con la procedura del 1-2/10 (backup di cfg e DLL, avvio in sequenza, controllo dei log).
- `MULTI_INSTANCE_SETUP.md`: un punto nuovo nella procedura di aggiornamento, con il comando per
  controllare le righe `Campaign …` nei log delle istanze 17-34. Gli account del secondo PC sono a
  livello 30-59: la campagna parte solo da 50.
- README: il task nuovo nell'elenco.

## 7. Dopo (fuori da questo piano)

- **Dungeon** (70 stelle): oggi `WarfrontDailyMissionsTask` fa solo le liberation missions
  (`Paths.WFDailyMissionsLoc`, "Only the liberation missions are wired, not the dungeons"). Quando un
  account arriva a 70 stelle va aggiunto il ramo dei dungeon, con una sonda sua.
- **Insane e Nightmare** (190 e 319 stelle): il task li gestisce già se le soglie e gli stati vengono
  dal gioco. Con la sola formula vanno aggiunti i due moltiplicatori, che sono già nella tabella.

## 8. Domande per l'utente, da fare all'inizio della sessione

1. **Healer obbligatorio?** La formazione "più potente" per battle power potrebbe lasciare fuori
   l'healer (poco danno, quindi poca potenza). Le alternative:
   - (a) le 5 più potenti in assoluto;
   - (b) almeno un tank e un healer se l'account li ha, poi le più potenti.

   Proposta: (b), perché l'healer cura il tank ogni round e la vittoria non dipende solo dalla
   potenza.
2. **Regola dei ritentativi**: una sconfitta si ritenta quando la battle power è salita del 5%.
   Va bene, o meglio una volta al giorno comunque?
3. **Ordine**: dalla soglia più bassa (più stelle in fretta, missioni nuove e difficoltà alternate).
   Oppure prima sempre la missione nuova più alta, per arrivare prima a 70 stelle e ai dungeon?
4. **Crew**: basta "nessun eroe libero", o vuoi anche gli spostamenti fini tra macchine (costano
   molti click e tempo)?
