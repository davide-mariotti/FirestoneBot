# Piano: acquisti nell'Emblem market dell'Exotic Merchant

> Scritto il 2026-10-01. Niente è ancora implementato: questo file contiene tutto quello che serve a una
> sessione nuova per creare il task, provarlo su Steam-0 e distribuirlo. Le regole di lavoro sono quelle
> di TESTING.md ("Per l'agente": Contesto, Regole, Comandi): valgono anche qui, in particolare i path si
> scrivono solo dopo averli visti dal vivo e mai si spendono gemme.

## 0. Come partire

Da incollare in una sessione nuova di Claude Code (cartella `C:\Repos\FirestoneBot`), avviata con i
permessi che le evitano di chiedere conferma a ogni comando:

```text
Leggi EMBLEM_MARKET_PLAN.md e seguilo dall'inizio alla fine, con le regole di TESTING.md e la skill
ponytail attiva. Prima la sonda dal vivo su Steam-0 (sezione 3), poi il task (sezione 4), il test
(sezione 5) e la distribuzione su Steam-0..16 (sezione 6). Fai un commit per ogni passo verificato
dal vivo, aggiorna TESTING.md e MULTI_INSTANCE_SETUP.md, poi push. Se la sonda mostra qualcosa che
contraddice questo piano (un costo in gemme, un popup di conferma con altre valute, forzieri
ordinati in modo diverso), fermati e chiedimi.
```

## 1. Cosa deve fare il task (decisioni dell'utente, 01/10)

- **Dove**: Città → Exotic Merchant → terza scheda, "Emblem market". Dentro ci sono tre categorie (le
  schede verticali sul carretto): gear (Emblem of Courage), jewels (Emblem of Valor), celestials
  (Emblem of Brotherhood).
- **Cosa compra**: in **gear** e in **jewels**, solo il forziere di **rarità più alta disponibile** in
  quel momento, e nient'altro. La rarità **non si deduce dal nome** del forziere (regola esplicita
  dell'utente): va presa da un dato del gioco, da scegliere con la sonda (sezione 3). Oggi, secondo il
  wiki e le immagini dell'utente, vuol dire Epic in gear e Golden in jewels; quando un account sblocca i
  Diamond (100 stelle della campagna), Diamond.
- **Quanto**: tutti i lotti che gli emblemi bastano a pagare, senza riserva (gli emblemi non servono ad
  altro, secondo il wiki). Se non bastano per un lotto del forziere migliore, non compra niente in
  quella categoria: **mai** un forziere più economico al suo posto.
- **Celestials**: esclusi (l'utente ha chiesto solo gear e jewels). La categoria ha anche un lucchetto
  finché non si sblocca.
- **Apertura**: dopo gli acquisti, lo stesso task apre dall'inventario i forzieri appena comprati,
  esattamente il numero comprato in quel giro (non quelli arrivati da altre fonti).
- **Mai**: gemme (il bottone `getMore` dei contatori apre l'acquisto di valuta: non va mai cliccato),
  offerte a pagamento, altre schede del mercante. Ogni acquisto passa dal controllo dell'icona della
  valuta (sezione 4).

## 2. Cosa si sa già

### Dal wiki (`docs/wiki/pages/Exotic_Merchant.html`, `Currencies.html`)

L'Emblem market si sblocca al **livello personaggio 65**.

| Categoria | Valuta | Offerte (lotto x quantità per prezzo) |
|---|---|---|
| gear | Emblem of Courage (dal livello 65: missioni e pescate di carte) | Common 4x 2.000 · Uncommon 4x 3.000 · Rare 5x 5.000 · **Epic 3x 5.000**. Legendary e oltre non in vendita |
| jewels | Emblem of Valor (dal livello 50: campagna, liberazioni, loot della campagna) | Wooden 4x 2.000 · Iron 4x 3.000 · **Golden 5x 5.000** · Diamond 3x 5.000, solo con 100 stelle della campagna. Opal e oltre non in vendita |
| celestials | Emblem of Brotherhood | Comet, Lunar, Solar, Nebula (con oracolo 98): esclusa |

Nota: Rare ed Epic costano uguale (5.000); il prezzo non indica la rarità, e nemmeno la quantità del
lotto è affidabile. Va usato un dato del gioco (sezione 3).

### Dalle immagini dell'utente (01/10)

Gear: Epic, Rare in alto, Uncommon, Common in basso, tutti con l'icona dell'Emblem of Courage
(scudo); contatore in alto a destra con "+" (8.878). Jewels: Golden, Iron in alto, Wooden in basso,
icona grigia dell'Emblem of Valor (6.840); il Diamond non compare (account senza 100 stelle). Le
schede verticali sul carretto hanno badge rossi; la terza ha un lucchetto (celestials).

### Dal dump degli asset (`docs/screens/ExoticMerchant.html`, struttura del prefab, non verificata dal vivo)

Radice: `menusRoot/menuCanvasParent/SafeArea/menuCanvas/menus/ExoticMerchant` (già in
`src/Infrastructure/Paths/ExoticMerchant.cs`, verificata dal vivo).

- `submenus/submenuButtons/emblemMarket`: la scheda (overlay lock se non sbloccata, badge in
  `notification/amountTMP`).
- `emblemMarketInfoButton`, `categoryButtons/gear`, `categoryButtons/jewels`,
  `categoryButtons/celestials` (quest'ultima con lock).
- `categories/gearCategory/.../productGrid/exoticMerchantEmblemGearChest (0..3)`: nome in `chestName`,
  icona in `gearChest` (o simile), acquisto in `purchaseButton` con `costText` e `currencyIcon`.
- `categories/jewelsCategory` (inattiva finché non selezionata): `exoticMerchantEmblemJewelChest (0..3)`,
  stessa struttura. Nota: 4 celle nel prefab ma 3 visibili sull'account dell'immagine (Diamond
  nascosto?).
- Il dump elide parte dei path (`.../productGrid`): il path completo va letto dal vivo.

### Codice da riusare

- Navigazione: `TownScreen.Open`, `TownScreen.OpenExoticMerchant`, `ExoticMerchant.Close`
  (`src/GameModel/Features/Town/ExoticMerchant.cs`, usati da `MerchantQuestTask`).
- Controllo della valuta: `IconSprite.NameAt(path)` (come `exoticCoin64` per gli upgrade esotici in
  `MerchantQuestTask.BuyUpgrades`, `meteorite64` in Hall of Heroes, `toolsIcon64` in War Machine Rarity).
- Conferma dell'acquisto dal contatore che scende, come `ExoticMerchant.CoinCount` e il ciclo di
  `BuyUpgrades` (clic, `Poll.Until` contatore < prima, altrimenti stop).
- Apertura dei forzieri: `ChestOpening.OpenDownTo(slot, 0, onOpened, maxToOpen)` in
  `src/GameModel/Features/Inventory/Inventory.cs`; per aprire la scheda dei forzieri vedi
  `CollectorQuestTask.OpenChestsTab` (privato: estrarlo o rifarlo in 3 righe). Gli slot gear sono già
  mappati (`Paths.InventoryLoc.EpicChestSlot` = `/Epic`); quelli jewel no (sezione 3).
- Controlli silenziosi: `GameElement.FindTransform(path)` e `activeInHierarchy`, per gli stati normali
  (categoria nascosta, forziere bloccato) che `IsVisible` scriverebbe come `[FAILED]`.
- Registrazione: `BotManager` carica da solo ogni sottoclasse non astratta di `BotTask`; la sezione del
  cfg prende il nome della classe in minuscolo (`EmblemMarketTask` → `[emblemmarkettask]`). Dopo il
  task, il log d'avvio dirà `Enabled tasks: 39 of 41`.

## 3. Sonda dal vivo (prima di scrivere il task)

Su Steam-0 (livello oltre 100), con il metodo "Dal vivo" di TESTING.md: un task o un passo temporaneo
che apre Città → Exotic Merchant → Emblem market, seleziona gear e poi jewels, e scrive nel log per
ogni categoria tutti i figli con nome, `activeInHierarchy`, testo (`TMP_Text`), stato dei `Button`
(`interactable`) e nome dello sprite delle icone (`IconSprite.NameAt`). Le righe di dump si tolgono
prima del commit. Da ricavare:

1. **Il dato della rarità indipendente dal nome**. Candidati, da confrontare con quello che si vede:
   l'ordine delle celle nella griglia (nelle immagini va dalla più rara alla meno rara), il nome dello
   sprite dell'icona del forziere, un componente o un campo di rarità sulla cella
   (`ExoticEmblemMarket...`, vedi il dump). Scegliere il più robusto e scriverlo nel commento del codice
   con la prova vista. Se serve leggere un campo del gioco, la classe sta negli assembly interop di
   `MelonLoader\Il2CppAssemblies` (per esempio `Il2CppCoreASM.dll`, che il progetto oggi non
   referenzia: aggiungerlo solo se davvero necessario).
2. **Come appare un forziere non disponibile** (il Diamond senza 100 stelle): cella nascosta, cella con
   lucchetto o bottone non cliccabile. La regola "più raro disponibile" deve saltarlo.
3. **Lo sprite delle due valute** sul `currencyIcon` del `purchaseButton` (nome atteso tipo
   `emblemOfCourage64`, da leggere) e il **path del contatore** di ogni emblema (in alto a destra; per
   le monete esotiche è `counters/currencyInteraction (ExoticCoin)/quantity`, per gli emblemi va
   cercato). Il formato del numero è "8.878" (punto delle migliaia).
4. **Cosa fa il click su `purchaseButton`**: compra subito o apre un popup di conferma. Se il popup
   mostra un costo in gemme o qualunque cosa diversa dagli emblemi, fermarsi e chiedere all'utente.
   Per non spendere nella sonda, guardare prima su un account con meno di 5.000 emblemi, oppure fare un
   solo acquisto vero quando si arriva al passo 4 della sezione 5.
5. **Gli slot dell'inventario** dei forzieri comprati: `/Epic` per gear è già noto; per i jewel
   (Golden, Diamond) fare un dump della scheda forzieri dell'inventario (`Paths.InventoryLoc.ContentRoot`)
   dopo un acquisto. Attenzione: Collector tratta oggi `jewelChest` e `celestialChest` come slot da
   aprire sempre (`CollectorQuestTask.NonGearChestSlots`): verificare se i jewel comprati finiscono lì
   (allora li aprirebbe già Collector) o in slot per rarità.
6. **Quali account hanno il mercato**: livello 65 o più e scheda senza lucchetto. Per vedere un Diamond
   serve un account con 100 stelle della campagna: se nessuno ce l'ha, la regola resta generica e il
   caso va annotato come "da verificare" in TESTING.md.

Scrivere i path trovati in `src/Infrastructure/Paths/ExoticMerchant.cs` (una classe `EmblemMarketLoc`
dentro `ExoticMerchantLoc`), con un commento per quelli verificati e i valori visti.

## 4. Il task

- **File**: `src/Tasks/Town/EmblemMarketTask.cs`; la parte di schermata in
  `src/GameModel/Features/Town/ExoticMerchant.cs` (o un `EmblemMarket.cs` accanto, se cresce); i path in
  `src/Infrastructure/Paths/ExoticMerchant.cs`.
- **Classe**: `EmblemMarketTask : BotTask`, `Group => TaskGroup.Town`, `MinimumCharacterLevel => 65`.
  Nessuna opzione nel cfg oltre a `enabled` (le regole della sezione 1 sono costanti, non
  configurazioni). `NextRunTime` dopo ogni giro: 6 ore (gli emblemi arrivano dalle missioni, un lotto
  costa 5.000). Se aprire tanti forzieri supera i 120 s globali, `MaxRuntimeSeconds` come in
  `MeteoriteResearchTask`.
- **Giro**:
  1. Città → Exotic Merchant → scheda Emblem market. Scheda bloccata o assente: chiudere e riprovare
     dopo 6 ore, senza `[FAILED]` rumorosi (controllo silenzioso).
  2. Per gear e poi jewels: selezionare la categoria, attendere che la griglia sia popolata (`Poll.Until`,
     come la lista di Pirate's Prize), scegliere la cella disponibile di rarità più alta con il dato della
     sonda.
  3. Prima di ogni click: icona della valuta uguale allo sprite dell'emblema della categoria, costo letto
     da `costText`, contatore ≥ costo. Altrimenti stop per quella categoria.
  4. Click; conferma solo se il contatore scende del costo entro qualche secondo (`Poll.Until`);
     altrimenti stop. Ripetere finché il contatore basta. Una riga per categoria:
     `[INFO] Emblem market gear: 'Epic chest' (rarity <dato>), 5000 each, emblems 8878 -> 3878, 1 lot(s).`
  5. Chiudere il mercante e la Città.
  6. Se ha comprato qualcosa: Inventario → scheda forzieri → `ChestOpening.OpenDownTo` sugli slot dei
     forzieri comprati, con `maxToOpen` = lotti x quantità del lotto (3 per Epic, 5 per Golden, 3 per
     Diamond: leggere la quantità dalla cella, "x3", invece di scriverla fissa). Riga:
     `[INFO] Emblem market: opened 3/3 '/Epic'.`
- **Mai**: `getMore`, `unlockOfferButton`, celestials, le altre schede. Nessuna riserva da
  configurare.
- **Test unitario**: solo se la scelta della rarità diventa una funzione pura con un caso non banale
  (per esempio "salta le celle bloccate"); in quel caso un test nel progetto
  `tests/Firebot.TalentEngine.Tests` sul modello dei test esistenti.

## 5. Test (TESTING.md, "Fase 2")

1. `git pull`, poi build (0 errori, 0 avvisi) e `dotnet test tests\Firebot.TalentEngine.Tests`.
2. Fermare Steam-0, backup di cfg e DLL in `C:\Repos\FirestoneBot-test-backup\`, cfg con tutti i task
   spenti tranne `[emblemmarkettask]` (`enabled = true`, `next_run_time_internal = ""`). Il cfg si
   modifica solo a gioco chiuso, in UTF-8 senza BOM.
3. Deploy, avvio, lettura del log. Prima prova con emblemi insufficienti o con la scheda già vista:
   nessun click di acquisto, giro chiuso, nessun `[FAILED]` nuovo.
4. Prova vera: almeno un lotto comprato, controllando nel log che sia il forziere giusto, che il
   contatore sia sceso esattamente del costo, che nient'altro sia stato toccato e che i forzieri
   comprati siano stati aperti (e solo quelli).
5. Secondo giro subito dopo (con `next_run_time_internal = ""` a gioco chiuso): con meno di un lotto
   di emblemi non deve comprare niente.
6. Riaccendere gli altri task (valori del backup), commit con la prova vista nel messaggio.

Da segnalare subito all'utente se succede: qualunque spesa di gemme, un forziere diverso dal più raro
disponibile, un acquisto confermato senza calo del contatore.

## 6. Distribuzione e documenti

- **Template**: aggiungere a `tools/ConfigTemplate/FirebotPreferences.template.cfg` la sezione
  `[emblemmarkettask]` con `enabled = true` (come `[minieventtask]`).
- **Flotta Steam-0..16** (questo PC): procedura "Aggiornare il bot su istanze già in funzione" di
  `MULTI_INSTANCE_SETUP.md`. Promemoria: Steam-1..16 sono in Sandboxie (box `SteamB<N>`); log, cfg e
  `Mods` autorevoli sono in `C:\Sandbox\Admin\SteamB<N>\drive\C\Program Files (x86)\Steam-<N>\...`;
  fermare e avviare da PowerShell (`Start.exe /box:SteamB<N> /terminate` e
  `Start.exe /box:SteamB<N> "...\Steam-<N>\steam.exe" -silent -applaunch 1013320`, 20 s tra
  un'istanza e l'altra), mai da Git Bash. Al primo avvio la chiave `emblemmarkettask.enabled` manca:
  il bot crea la sezione, poi `apply_template.ps1 -From 0 -To 16 -Check` deve dare 0 differenze.
  Controllo: `Started. Enabled tasks: 39 of 41` su ogni istanza, nessun `threw` né `timed out`, e
  dopo il primo giro una riga `Emblem market` sugli account a livello 65 o più.
- **TESTING.md**: una riga nuova nella tabella "Town" (sezione `[emblemmarkettask]`, livello 65,
  default on, stato e prove viste), la sezione nella "Configurazione di riferimento" se serve, e la
  decisione dell'utente in "Da segnalare subito" (mai un forziere diverso dal più raro).
- **MULTI_INSTANCE_SETUP.md**: nel punto 5 della procedura di aggiornamento, la chiave mancante
  `emblemmarkettask.enabled` attesa al primo avvio; nella sezione 9 ("Bug già risolti") o in un punto
  nuovo, cosa fa il task e il controllo da fare sul secondo PC (Steam-17..34).
- Commit per ogni passo verificato, poi push.
