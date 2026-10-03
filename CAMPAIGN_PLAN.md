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
ponytail attiva. Le decisioni sono già prese (sezione 8): parti dalla sonda dal vivo su Steam-0
(sezione 3), poi il task (sezione 4) un passo alla volta, il test (sezione 5) e la distribuzione
(sezione 6). Fai un commit per ogni passo verificato dal vivo, aggiorna TESTING.md e
MULTI_INSTANCE_SETUP.md, poi push. Se la sonda contraddice il piano (un costo, un limite di
battaglie, una formazione che cambia anche l'Arena), fermati e chiedimi.
```

## 1. Cosa deve fare il task (richiesta dell'utente, 03/10)

Un task nuovo e separato, `WarfrontCampaignTask` (gruppo Warfront, livello 50, come Daily Missions).

- **Formazione**: schierare la formazione più potente possibile con le War Machine dell'account.
  Cinque macchine, nell'ordine **tank, poi damage, poi healer**, e in ogni macchina gli eroi della
  crew. Sempre almeno un tank e un healer, se l'account li ha (sezione 8).
- **Progressione**: far avanzare la campagna il più possibile, in due direzioni: la missione più
  alta non ancora vinta (Easy) e, sulle missioni già fatte, la difficoltà successiva (una stella in
  più).
- **Senza far perdere tempo al bot**: tentare una battaglia solo quando la potenza della formazione
  lo giustifica, in base alla potenza dei nemici di quella missione. Le soglie si prendono dal gioco
  se possibile, altrimenti dalla formula della wiki (sezione 2.1, già verificata).
- **Perché**: ogni vittoria dà una stella e una ricompensa unica (forzieri jewel, Emblem of Valor,
  blueprint). Le stelle sbloccano altre missioni giornaliere: liberation a 5, 10, 20, 40, 60, 80, 110,
  155, 190, 319 e dungeon a 70 e 120 (sezione 2.6). Danno anche funzioni nuove (sezione 2.1). Steam-0
  ne ha 42: le prossime soglie sono 60 (una liberation in più), 70 (il primo dungeon) e 100 (il
  forziere Diamond, anche nell'Emblem market).
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
  - nessun "squadra migliore" automatico. L'unico `bestSquadButton` del gioco è del Tower of Souls
    (`TowerOfSoulsSubmenu`, `SelectTOSFormationViewController`, `TowerOfSoulsHandler.GetBestSquad`,
    formazione di eroi), letto dai metadati dell'assembly il 3/10. Nessun membro "best", "auto",
    "recommend" od "optimal" nelle classi di War Machine, formazione, squadra, crew o campagna.
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

### 2.6 Dalla guida Steam (settembre 2026, gioco 9.1.1; testo in `docs/guida_steam_completa.md`)

Sezioni "Quick Hints 4", "Hero Equipment 3", "Personal Tree vs WM", "Warfront/Warmachine Expansion 1
e 2", "Pirate ship / Mercenaries" e "Unlocks". Le citazioni sono tra virgolette.

**Perché le stelle contano.** "Your first goal should be to reach 190 campaign stars to unlock all
daily missions (liberation/dungeon)". Le liberation missions giornaliere si aggiungono a 5, 10, 20, 40,
60, 80, 110, 155, 190 e 319 stelle; i dungeon a 70 (Ethereal Cavern) e 120 (Dragon's Lair). A 100
stelle il forziere Diamond (anche nell'Emblem market: "3 Diamond Chests = 5000 Emblems of Valour"),
a 190 Insane, a 319 Nightmare e una liberation in più. Ogni stella aumenta quindi le ricompense di
tutti i giorni: conferma l'ordine dalla soglia più bassa (sezione 8).

**Le War Machine, classi e classifica della guida:**

- Damage singolo: "Cloudfist > Talos > Aegis > Firecracker", e "any single damage is better than any
  multi damage" (opinione dell'autore; Cloudfist "on the same level as Thunderclap").
- Damage multiplo: "Thunderclap > before Judgement and > Harvester".
- Tank: "Goliath > Fortress/Earthshatterer". Goliath per l'autocura del 10%, che sale con la rarità
  ("Already at rarity level 2, the chance of triggering the self-healing ability is 28%").
- Healer: "Hunter > Sentinel > Curator".
- **Le prime 5 macchine arrivano sempre in quest'ordine di classe**: damage singolo, damage singolo,
  tank, damage multiplo, healer ("Which damage or which tank/healer you exactly get is random, the
  order isn't"). Dalla sesta è casuale. Quindi un account con 5 macchine ha già esattamente un tank e
  un healer, e la scelta del passo A conta solo dalla sesta (Steam-0 ne ha 6).

Il task non usa la classifica come regola: sceglie per battle power dell'account (sezione 4.2), che è
quello che la soglia confronta. La classifica dice però che la regola "un tank e un healer sempre in
squadra" (sezione 8) è coerente con la guida.

**La crew, con la formula:**

- "crew member bonus = (1 + specialization bonus) * jewel bonus"; "crew bonus = crew member 1 bonus +
  crew member 2 bonus + ..."; "Battle attribute = basic attribute * (1 + crew bonus)".
- "The specialization bonus is 40% if the hero's specialization matches the attribute (damage
  specialization matches damage, healer specialization matches health and tank specialization matches
  armor)". "If the hero doesn't own any jewels for the attribute, the jewel bonus is 40%".
- Ogni eroe in crew aggiunge quindi **almeno +40% a ognuno dei tre attributi** della macchina. Un posto
  vuoto perde quel +40%: la regola "nessun eroe libero" è la mossa che rende di più ed è certa.
- Il bonus crew di ogni eroe si legge nel gioco: "Town -> Hall of Heroes -> first tab of the hero ->
  scroll down a bit to the crew bonus" (nel dump statico `infoSubmenu/.../crewBonus/unlocked`).
  Con questi numeri, la crew ottimale (quale eroe su quale macchina) si potrebbe calcolare senza
  provare a mano: è la strada per la crew fine (sezione 7).
- Posti: "4 members; 5 members: engineer level 30; 6 members: engineer level 60".
- Gioielli: per gli eroi in una macchina damage quelli di danno, in tank o healer salute e armatura;
  tier 2 prima dei tier 1. Riguarda Hall of Heroes, non questo task.

**Squadre.** Ingegnere, secondo tab "Active Squad": si mettono gli eroi nelle macchine e si creano
**squadre alternative**, per esempio "1st squad for the campaign … and 1 other squad for the arena,
where you can switch from 'Battle Attributes' to 'Arena Attributes' under 'Select Crew'". Il task deve
modificare solo la squadra attiva usata dalla campagna (sezione 3, punto 7).

**Altri dati utili:**

- "Globally, stats are multiplicative in the Campaign and additive in Arena (PvP)".
- La potenza: `power = (10 * damage)^0.7 + (1 * health)^0.7 + (10 * armor)^0.7`. "Base power" è senza
  crew (classifica), "Battle power" con la crew (campagna), "Arena power" con la formula dell'arena.
- Muri della campagna: secondo la sezione "Personal Tree vs WM", "Upgrading our tanks by 15(!) levels
  provides us with a mere progress of 5 normal missions on average". La progressione è lenta, ed è il
  motivo della regola dei ritentativi (5% o 24 h).
- Bottone di velocità: la guida lo dà da 1 stella ("Campaign Stars: 1; ×1, x2 and x4 battle speed
  button"), la wiki da 20. Su Steam-0 (42 stelle) c'è in ogni caso; la sonda guarda se resta impostato.
- Preferite: il terzo tab dell'Ingegnere sceglie 5 macchine preferite, che ricevono l'80% dei
  componenti dai forzieri jewel. Riguarda la crescita delle macchine (`WarMachinesTask`), non questo
  task.

**Contratti** (non è di questo piano, ma riguarda il task di rarità del 3/10): "beginner should only
get the first mercenary for 400 contracts and then use the contracts for the hero rarities", e "The
best choice here is to get Cirilo as the first mercenary". Steam-0 ha già Cirilo, quindi il task di
rarità è in linea con la guida. Un account senza nessun mercenario dovrebbe invece tenere 400
contratti per il primo: decisione per l'utente.

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
   - il bottone "Save changes" e cosa succede chiudendo senza salvare.
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
7. **Squadre** (sezione 2.6): l'Ingegnere ha un tab "Active Squad" con squadre alternative (es. una
   per la campagna e una per l'Arena). Quale squadra usa la campagna, come si riconosce quella attiva,
   e cambiarla tocca le formazioni dell'Arena (`attackerFormation`, `defenderFormation`)? Il task deve
   modificare solo la squadra della campagna.
8. **Bonus crew**: il tab info dell'eroe in Hall of Heroes mostra il "Crew Bonus" (danno, salute,
   armatura). Basta annotare path e testi: serve solo per la crew fine (sezione 7), non per questa
   versione.

### 3.1 Risultati della sonda (03/10, Steam-0, 14:46 e 14:52)

Due giri di una classe temporanea (tolta prima del commit), tutti gli altri task spenti. Battle power
796.762 (Goliath salito al livello 24), 42 stelle.

1. **Dati, senza UI.** `GameInitialize.HandlerLoader` (statico) dà `WFCampaignMissionHandler.missionsDict`
   (90 missioni, chiave e `missionIndex` **da 0**: la missione 32 è l'indice 31),
   `WFCampaignStarHandler.totalStars`, `WarMachineHandler.myFormationBattlePower` (uguale al testo
   "Battle power" della squadra) e `warMachineData` (macchine, formazione attiva, squadre di riserva).
   Per missione: `modesWon` (coincide con le stelle: Easy 1-31 e Normal 1-11, 42) e `modePowerReqDict`.
   `modeState` invece è vecchio (Easy 32 "PowerRequired" con la potenza sopra la soglia): non si usa.
2. **Soglie.** `modePowerReqDict` coincide con la formula della sezione 2.1 su tutte le 450 coppie
   missione/difficoltà, quindi il task usa il dato del gioco e la formula non serve.
3. **Sblocco.** Normal di m richiede solo Easy di m (Normal 13 mostra la potenza richiesta con Normal
   12 non vinta). Sulla mappa è attivo solo il pin della missione successiva (32); il pin di una
   missione più avanti è nascosto, ma il suo popup si apre lo stesso e mostra Easy cliccabile se la
   potenza basta: l'ordine di sblocco lo controlla il task.
4. **Pin e popup.** Pin: `menusRoot/mapRoot/mapElements/warfrontCampaignMissions/warfrontMission (i)`,
   componente `WFCampaignMissionMapInteraction`; `OnPointerClick` apre il popup anche col pin fuori
   schermo o nascosto. Popup `popups/WFCampaignMissionPreview`: `bg/closeButton`,
   `bg/changeFormationButton` ("Battle formation"), `bg/battleSimulation/totalPower/powerNumberTMP`, e
   `bg/modes/easyMode` … `hardMode` (`WFCampaignModeInteraction`, campo `mode`) con `fightButton`
   cliccabile solo se sbloccata e con la potenza sufficiente. Stati: `wonObj`, `unlockedObj`,
   `powerReqObj`, `lockedObj`. Nessun costo: sotto Easy 32 solo le ricompense (3 forzieri jewel,
   1.680 Emblem of Valor, 420 blueprint).
5. **Battaglia.** Il `fightButton` apre direttamente `menus/WFBattle` (niente `WFBattleSim`). Easy 32,
   soglia 422.800, potenza 796.762 (1,88×): **persa** in 32 s, popup `popups/WFBattleDefeat` ("Do not
   let this take you down", `bg/closeButton` "OK", lo stesso path di Daily Missions); dopo l'OK resta
   solo `menus/WorldMap`, il popup della missione si chiude da solo. Stelle 42 → 42. C'è
   `battleCanvas/bottomRightSideUI/changeSpeedButton`. Il popup di vittoria non si è ancora visto.
6. **Squadra** (`menus/SelectWarMachines`, dal popup o dall'Ingegnere): 5 posti
   `bg/formationSpots/warMachineFormationMenuSpot (0..4)` (`WarMachineFormationSettingSpot`, numeri 1-5;
   su Steam-0 il tank è nel posto 0 e l'healer nel 4), il mazzo
   `bg/warmachinesDeck/warMachinesScroll/Viewport/grid` (`WarMachineSelectInteraction`, `clickButton`),
   `bg/formationData/formationStatusText` ("Battle power: 796.762"), `bg/formationData/saveChanges`
   (non cliccabile senza modifiche), `closeButton`. Crew: `crew/editButton` di un posto apre
   `popups/SelectWarMachineHeroes` (`bg/setCrewButton` "Save changes", `bg/closeButton`,
   `bg/heroListScroll/Viewport/grid/heroSelect (i)`), che mostra solo gli eroi di quella crew e quelli
   liberi. 4 posti crew per macchina (il 5° col lucchetto, ingegnere sotto il 30). Su Steam-0 la
   formazione è già tank → damage → healer (Goliath, Cloudfist, Talos, Thunderclap, Hunter) e tutti e
   10 gli eroi sono in una crew; fuori resta Aegis (potenza senza crew 4.079).
7. **Squadre e Arena.** 3 squadre: la prima attiva, la seconda con solo Cloudfist, la terza vuota.
   Campagna, Daily Missions e Arena usano tutte la squadra attiva (`warMachineData.formationList`; le
   anteprime di campagna, Daily Missions e Arena la leggono con lo stesso
   `SetPlayerUsingTheFormationList`, e `SelectWarMachinesMech.OpenOrigin` ha Engineer, WarfrontMission,
   Tower e Arena per lo stesso menu). Cambiare la formazione cambia anche quella dell'Arena: è la
   contraddizione con il piano, decisa dall'utente (sezione 8, punto 5).
8. **Potenza con e senza crew.** `WarMachine.power` comprende la crew (Goliath 326.756 contro
   `powerNoCrew` 10.285): per scegliere le macchine si confronta `powerNoCrew`, altrimenti una macchina
   nuova, senza crew, non entrerebbe mai.

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
  - per ogni missione e difficoltà persa, la potenza della squadra e l'ora della sconfitta
    (es. `"33:0@785790@2026-10-04T10:15;12:1@785790@2026-10-04T10:17"`).
- Nessuna chiave di configurazione oltre `enabled`, salvo che i test la rendano necessaria.

### 4.2 Passo A: la formazione più forte

Si fa al primo giro, poi solo quando la potenza di una macchina è cambiata: livelli di
`WarMachinesTask`, rarità di `WarMachineRarityTask`, gioielli degli eroi da Hall of Heroes.

Il gioco non ha un "squadra migliore" automatico per le War Machine (vedi 2.4), quindi la scelta la
fa il task:

1. **Macchine**: il tank con la battle power più alta, l'healer con la battle power più alta, e le 3
   più potenti fra tutte le altre (anche altri tank o healer). Senza tank o senza healer
   nell'account, quel posto va alla macchina più potente rimasta. In campo nell'ordine tank → damage
   → healer: davanti il tank, perché gli healer curano la macchina più avanti.
2. **Crew**: ogni eroe libero va in un posto vuoto. Per la formula della guida (sezione 2.6) ogni
   eroe in crew aggiunge almeno +40% a ognuno dei tre attributi della macchina, quindi un posto vuoto
   è la perdita più grande. I posti si riempiono partendo dalle macchine in campo, la prima davanti.
   Nessuno spostamento fine tra macchine in questa versione (sezione 8). La riga di log scrive la
   potenza di ogni macchina, così si potrà decidere coi numeri.
3. "Save changes" e controllo: la battle power salvata non deve essere più bassa di prima. Se lo è, si
   rimette la formazione di prima e si scrive un `[FAILED]`.
4. **Riga di log**: `Campaign formation: Goliath 315764, Cloudfist …, Hunter … (tank, damage, healer), battle power A -> B, free heroes placed N.`

La formazione è la stessa che usa Daily Missions (da confermare nella sonda), che quindi ne
beneficia. Il commento di `WarfrontDailyMissionsTask` ("The formation is set up by hand once") va
aggiornato.

### 4.3 Passo B: le battaglie

1. **Stato**: missioni e difficoltà vinte (dai dati del gioco della sonda, o dai pin), stelle totali,
   battle power della squadra.
2. **Candidati**: ogni coppia (missione, difficoltà) che non è vinta, è sbloccata (sezione 2.1:
   missione precedente a Easy, difficoltà precedente sulla stessa missione), ha la potenza richiesta
   non oltre la battle power, e non è in attesa dopo una sconfitta. Una sconfitta si ritenta quando la
   battle power è salita almeno del 5% rispetto a quella della sconfitta **oppure** sono passate 24 h,
   quello che arriva prima: le battaglie hanno una parte casuale (overdrive al 25%), quindi un
   tentativo al giorno a parità di potenza vale il minuto che costa. Nel cfg, per ogni sconfitta,
   potenza e ora (es. `"33:0@785790@2026-10-04T10:15"`).
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
- Non ritenta in loop una missione persa: vedi la regola del 5% o 24 h.

## 5. Test (TESTING.md, "Fase 2", solo Steam-0)

1. **Formazione**: un giro con il task isolato (gli altri task spenti, come nel punto 3 della Fase 2).
   Battle power prima e dopo, formazione nell'ordine giusto, nessun eroe libero. Daily Missions al
   giro dopo combatte con la formazione nuova.
2. **Battaglie**: Easy 32 per prima; poi le altre del giro fino alla prima sconfitta. Per ogni
   battaglia, la riga di log e il contatore delle stelle.
3. **Giro a vuoto**: con niente da tentare il giro deve durare pochi secondi.
4. **Sconfitta**: la missione persa non va ritentata al giro dopo con la stessa potenza, ma sì dopo
   24 h o con il 5% di potenza in più.
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
- **Crew calcolata**: con il "Crew Bonus" di ogni eroe letto da Hall of Heroes e la formula della
  guida (sezione 2.6: bonus di specializzazione 40% sull'attributo giusto, somma dei membri, battle
  attribute = basic × (1 + crew bonus), potenza `(10d)^0,7 + h^0,7 + (10a)^0,7`), l'assegnazione eroi →
  macchine che massimizza la battle power si calcola senza provarla a click. Poi si applica solo la
  differenza. Da fare se i numeri del log mostrano che la crew conta.
- **Insane e Nightmare** (190 e 319 stelle): il task li gestisce già se le soglie e gli stati vengono
  dal gioco. Con la sola formula vanno aggiunti i due moltiplicatori, che sono già nella tabella.

## 8. Decisioni (03/10, l'utente ha lasciato la scelta all'agente)

1. **Healer obbligatorio: sì.** In formazione vanno il tank più potente, l'healer più potente, poi le
   3 macchine più potenti fra le restanti. Se l'account non ha tank o healer, il posto va alla più
   potente rimasta. Motivo: gli healer curano ogni round la macchina più avanti, e alla soglia (80%
   della potenza nemica) la tenuta conta più di qualche punto di battle power. Su Steam-0 (6
   macchine) ne resta fuori una sola. Se i test mostrano sconfitte con margini ampi, si confronta con
   le 5 più potenti in assoluto.
2. **Ritentativi: 5% di potenza in più oppure 24 h**, quello che arriva prima. Motivo: le battaglie
   hanno una parte casuale (le abilità scattano col 25% di overdrive), quindi un tentativo al giorno a
   parità di potenza vale il minuto che costa. Una sconfitta con potenza ferma, invece, non si
   ritenta a ogni giro.
3. **Ordine: dalla soglia più bassa alla più alta.** Motivo: per i dungeon (70) e il Diamond (100)
   conta il numero di stelle, non quali missioni. Le battaglie più probabili danno più stelle e più in
   fretta, e alternano da sole missioni nuove e difficoltà nuove.
4. **Crew: nessun eroe libero, nessuno spostamento fine** in questa versione. Motivo: per la formula
   della guida ogni eroe in crew vale almeno +40% su ogni attributo della macchina, e con più posti
   che eroi (Steam-0: 10 eroi, 20 posti sbloccati) conta soprattutto che ogni eroe sia in una crew. Gli
   spostamenti fini costano molti click per ogni prova. Si potranno calcolare dai bonus crew (sezione
   7). La riga di log con la potenza di ogni macchina serve a decidere dopo coi numeri.
5. **Formazione condivisa con l'Arena (utente, 03/10, dopo la sonda): il task la modifica lo
   stesso.** La squadra attiva è una sola per campagna, Daily Missions e Arena; l'Arena combatte con
   la stessa squadra più forte (su Steam-0 l'ordine delle macchine per potenza Arena è lo stesso di
   quello senza crew).
6. **Ritentativi (utente, 03/10, dopo la sconfitta di Easy 32 a 1,88×): la regola del punto 2, più
   uno scarto.** Finché una sconfitta è in attesa (potenza sotto il +5% e meno di 24 h), non si tenta
   nessun candidato con un margine (battle power / potenza richiesta) non più alto di quello della
   sconfitta. Su Steam-0 oggi, persa Easy 32 a 1,88×, Normal 12 (1,04×) non si tenta.
