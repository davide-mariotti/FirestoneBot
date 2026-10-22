# Piano: l'amuleto del giorno

> Scritto il 2026-10-22, solo analisi: niente codice ancora. Ogni giorno lo Store offre un amuleto
> permanente ("Amulet of the Day"); quelli in gemme costano 2.000 gemme. Questo piano dice quali
> comprare sui nostri account, quando, e come costruire il task in modo che non sprechi mai gemme.
> Le gemme si accumulano lentamente o si pagano con soldi veri, quindi ogni scelta qui è a favore della sicurezza.

## 0. Come partire

Da incollare in una sessione nuova di Claude Code (cartella `C:\Repos\FirestoneBot`):

```text
Leggi AMULETS_PLAN.md e implementalo con la skill ponytail attiva, seguendo le regole di TESTING.md
(test solo su Steam-0, path verificati dal vivo, commit e push per ogni passo verificato). Parti
dalla fase 1 (sonda in sola lettura) e fermati a chiedermi prima di accendere gli acquisti veri
(fase 3). Aggiorna TESTING.md, MULTI_INSTANCE_SETUP.md e questo piano.
```

## 1. Cosa sappiamo

### Il gioco

- 25 amuleti, ognuno con un bonus permanente (wiki `docs/wiki/pages/Shop.html`, sezione Amulets). 19 si
  pagano in **2.000 gemme** o soldi veri, 6 solo in **20 golden key**.
- Lo Store mostra **un amuleto al giorno**, che cambia al reset delle 10:00 (guida Steam,
  `docs/guida_steam_completa.md` "Amulets"). Chi non passa quel giorno se lo perde.
- Il gioco tiene tutto in memoria, quindi il bot può leggere lo stato invece di leggere il testo localizzato a schermo
  (classi viste negli assembly Il2Cpp della 3.61, `Il2CppCoreASM.dll`, da verificare compilando):
  - `GameInitialize.HandlerLoader.AmuletHandler` (`Il2Cpp.AmuletHandler`): `amulets` (lista di
    `Il2Cpp.Amulet`), `CounAmuletsOwned()`, `IsAmuletUnlockedByCharacterLevel(AmuletCode)`,
    `GetAmuletUnlockLevel(AmuletCode)`.
  - `Il2Cpp.Amulet`: `code` (`AmuletCode`), `HaveAmulet()`, `purchaseMethod`, `level`.
  - `GameInitialize.HandlerLoader.AmuletDataHandler` (`Il2Cpp.AmuletDataHandler`):
    `TryGetDailyAmuletCode(out AmuletCode)` è l'amuleto di oggi; `GetDailyEligibleCount()` conta
    quelli che possono uscire, probabilmente i non posseduti già sbloccati: da verificare.
  - `Il2Cpp.AmuletCode`: `AmuletOfConquest`, `AmuletOfWar`, `AmuletOfKnowledge`, `AmuletOfAlchemy`,
    `AmuletOfTheSky`, `AmuletOfExploration`, `AmuletOfCartography`, `AmuletOfLiberation`,
    `AmuletOfThePioneers`, `AmuletOfTheQuartermaster`, `AmuletOfInsight`, `AmuletOfAstrology`,
    `AmuletOfTheSeven`, `AmuletOfTinkering`, `AmuletOfProduction`, `AmuletOfClarity`,
    `AmuletOfGreed`, `AmuletOfMidas`, `AmuletOfPower` (gemme); `AmuletOfLuck`, `AmuletOfTheKing`,
    `AmuletOfTheQueen`, `AmuletOfSpeed`, `AmuletOfDamage`, `AmuletOfHealth` (golden key); `None`.
  - `Il2Cpp.AmuletStoreInteraction`, il componente sulla carta dell'amuleto del giorno: `amulet`,
    `purchaseButtonGems`, `purchaseButtonMoney`, `purchaseButtonKeys`, `purchaseCostGemsText`,
    `claimedObj`.
  - Le gemme si leggono come nello snapshot orario: `HandlerLoader.CurrencyHandler.GetCurrency(CurrencyCode.Gem).Amount`
    (`src/Core/BotManager.cs`, `LogSnapshot`).
- Schermata (dump degli asset, `docs/screens/Store.html`, non vista dal vivo): `menus/Store`, tab
  `submenuButtons/grid/amuletsButton`, carta `bg/submenus/amulets/dailyAmuletStore` con
  `purchaseButtonGems`, `purchaseButtonMoney`, `purchaseButtonKeys`, `claimedObj`, e
  `bg/submenus/amulets/timeLeftText`, `noAmuletsText`.

### Come si entra nello Store

Il `storeButton` dell'HUD non apre niente (`Store.Open`, TESTING.md "Daily Store Offers"): oggi il
bot entra solo dai badge CheckIn e MysteryBox. Nel prefab della Città (3.61) c'è
`menus/TownIrongard/townBg/parent/store`, con un Button: è il candidato. Se non apre lo Store, la riserva è
agganciarsi al giro di `DailyStoreOffersTask`, che apre lo Store ogni giorno dopo il reset (il badge
CheckIn si accende alle 10:00, insieme al cambio dell'amuleto).

### La flotta (snapshot del 22/10)

| Istanze | Livello | Gemme |
| --- | --- | --- |
| Steam-0 | 239 | 29 |
| Steam-1..9 | 159-193 | 417-2.198 |
| Steam-10..16 | 139-158 | 1.271-3.716 |
| Steam-17..31 | 94-115 | 264-1.918 |

Senza spese entrano circa 40-100 gemme al giorno, più 210 a settimana dalle quest settimanali: **un
amuleto ogni 3-4 settimane per account**. Il 21-22/10 Steam-0, -2 e -17 hanno perso esattamente 2.000
gemme, quindi l'utente compra anche a mano. Il bot deve leggere dal gioco quali amuleti sono già
posseduti e non tenere un elenco suo.

## 2. Tier per i nostri account

I nostri account girano 24 ore su 24. La guida Steam dice che Knowledge e Alchemy rendono "solo a
chi gioca attivo", perché un giocatore che entra una volta al giorno non sfrutta il 10% in meno.
Il bot invece rilancia ricerche ed esperimenti appena finiscono, quindi per noi le riduzioni di
tempo valgono per intero: un -10% di durata dà circa l'11% di risultati in più, ogni giorno, per sempre.

Ogni amuleto ha anche un **livello minimo utile**: il livello da cui il bot usa la funzione che
l'amuleto potenzia. Sotto quel livello l'amuleto non si compra e non conta tra i mancanti.

### Tier 1: indispensabili (progressione composta)

| Amuleto | Effetto | Liv. utile | Perché |
| --- | --- | --- | --- |
| Conquest | +1 squadra sulla mappa | 1 | Una missione in più in parallelo, sempre. Prima scelta di entrambe le guide |
| War | -10% durata missioni mappa | 1 | Con Conquest moltiplica le missioni al giorno (MapMissionsTask) |
| Knowledge | -10% tempo Firestone research | 1 | La ricerca è la progressione principale, slot sempre pieni (FirestoneResearchTask) |
| Alchemy | -10% durata esperimenti | 120 | Esperimenti sempre in corso: dragon blood, dust, forzieri (ExperimentsTask) |

### Tier 2: utili (valute che il bot raccoglie ogni giorno)

| Amuleto | Effetto | Liv. utile | Perché |
| --- | --- | --- | --- |
| Sky | +10% probabilità di 2 meteoriti invece di 1 | 1 | Meteoriti per research e tier del gear. L'unico 10% fuori dal tier 1 |
| Exploration | 5% ricompense doppie dalle spedizioni | 10 | Spedizioni sempre in corso (ExpeditionTask) |
| Cartography | +5% expedition token | 10 | Token per Tree of Life e War Machines |
| Liberation | 5% ricompense doppie dalle missioni giornaliere War Machine | 50 | WarfrontDailyMissionsTask ogni giorno |
| Pioneers | 5% loot doppio dalla campagna | 50 | WarfrontCampaignLootTask ogni 6 ore |
| Quartermaster | +5% loot dalla campagna | 50 | Come sopra |
| Insight | 5% ricompense doppie dai rituali dell'Oracolo | 200 | Rituali uno dopo l'altro (OracleRitualsTask). Oggi conta solo su Steam-0 |

### Tier 3: secondari (bonus piccoli o su cose marginali)

| Amuleto | Effetto | Liv. utile |
| --- | --- | --- |
| Astrology | 5% ricompense doppie dalle quest giornaliere | 1 |
| The Seven | 5% ricompense doppie dalle quest settimanali | 1 |
| Tinkering | 5% tool doppi dall'Engineer (la guida: utile solo il primo anno) | 50 |
| Production | 5% picconi gratuiti doppi | 50 |
| Clarity | 5% ricompense doppie dall'Arcane Crystal | 50 |
| Greed | +5% guild coin dalle spedizioni | 10 |

### Mai

- **Midas** (+300% oro) e **Power** (+300% attributi): +300% su moltiplicatori che sono già enormi
  non cambia niente. Tutte e due le guide li danno per "not worthwhile".
- **Amuleti in golden key** (Luck, King, Queen, Speed, Damage, Health): fuori da questo piano. Le golden
  key servono agli artifact. Luck (+5% birra) è l'unico consigliato: se lo vuoi, è un'aggiunta a parte
  con una sua riserva di key.

## 3. La regola d'acquisto

L'idea di partenza: comprare il tier 2 solo dopo aver completato il tier 1. Così si ha la certezza di
non spendere male, ma le gemme si fermano. Con 19 amuleti in rotazione un amuleto preciso esce in
media una volta ogni 2-3 settimane, e intanto le gemme oltre le 2.000 restano lì inutilizzate. Una
variante più robusta tiene **una riserva** per i tier superiori, invece di bloccare i tier inferiori:

> Oggi esce l'amuleto A di tier t (non posseduto, livello utile raggiunto, prezzo 2.000 gemme).
> Lo si compra se `gemme - 2.000 >= gem_reserve + 2.000 × H`, dove
> H = numero di amuleti mancanti nei tier sopra t, al massimo t-1.

- **Tier 1**: H = 0. Basta avere 2.000 gemme (più `gem_reserve`).
- **Tier 2**: se manca anche un solo tier 1, servono 4.000 gemme: ne restano 2.000 per quel tier 1
  quando esce. Con tutto il tier 1 posseduto bastano 2.000. È la tua regola ("salta il tier 2 finché manca
  un tier 1") quando le gemme sono poche, e smette di sprecare quando si accumulano.
- **Tier 3**: tiene da parte fino a due amuleti superiori (H ≤ 2), quindi fino a 6.000 gemme.
- **Mai**: non si compra mai.

Esempi (gem_reserve 0):

| Gemme | Mancano | Oggi | Decisione |
| --- | --- | --- | --- |
| 2.150 | Conquest, Sky | War (T1) | Compra: un tier 1 non aspetta nessuno |
| 2.150 | Conquest | Sky (T2) | Salta: servono 4.000 per tenere Conquest coperto |
| 4.300 | Conquest | Sky (T2) | Compra, ne restano 2.300 per Conquest |
| 2.150 | nessun T1/T2 | Astrology (T3) | Compra |
| 5.000 | War, Exploration | Clarity (T3) | Salta: servono 6.000 |

Paletti che valgono sempre:

1. **Al massimo un acquisto per giorno di gioco** (`last_purchase_date` come `DailyStoreOffersTask.last_done_date`).
2. **Solo `purchaseButtonGems`**, mai `purchaseButtonMoney` (soldi veri) né `purchaseButtonKeys`.
3. Prima del click si controllano tre cose, e se una non torna non si compra:
   - `AmuletStoreInteraction.amulet.code` è il codice deciso dal planner;
   - `purchaseCostGemsText` si legge come 2.000;
   - le gemme lette dal gioco coprono la regola.
4. Dopo il click si verifica che `HaveAmulet()` sia vero e che le gemme siano scese di 2.000. Si scrive
   tutto nel log. Se una delle due non torna, il task si spegne per la giornata e scrive `[FAILED]`.
5. `gem_reserve` (default 0) è la soglia sotto cui il bot non scende mai, per l'utente che vuole
   gemme per altro (battle pass, token della Taverna).

## 4. Implementazione

Lo schema è quello dei planner esistenti (`BlueprintPlanner`, `EnchantPlanner`): la decisione è una
funzione pura testata, il task fa solo lettura e click.

1. **`src/Tasks/Town/AmuletPlanner.cs`**, puro, senza tipi del gioco. Usa i nomi del codice come
   stringhe ("AmuletOfConquest"):
   - una tabella `codice -> (tier, livello utile)` con i tier della sezione 2;
   - `Decide(string today, IReadOnlySet<string> owned, int level, double gems, double reserve)`, che
     restituisce `(bool buy, string reason)`. La reason finisce nel log: "tier 2, needs 4000 (Conquest
     missing)", "never", "owned", "below level 120", "not in table".
   - Va aggiunto a `tests/Firebot.TalentEngine.Tests/*.csproj` (`<Compile Include ...>`), con
     `AmuletPlannerTests.cs`: le righe della tabella esempi della sezione 3, più owned, never, sotto
     livello, codice sconosciuto o `None`.
2. **`src/GameModel/Shared/Amulets.cs`**: lettura dal gioco. `Today` (con `TryGetDailyAmuletCode`;
   in Il2CppInterop l'`out` può diventare `ref`, va verificato in compilazione), `Owned` (da
   `AmuletHandler.amulets` filtrando `HaveAmulet()`), `Gems`. Ogni lettura è in un try/catch che
   restituisce "sconosciuto": se non si legge, non si compra.
3. **Paths** (`Menus.cs`, `StoreLoc`): `AmuletsTabBtn`, `DailyAmuletRoot`, `PurchaseGemsBtn`,
   `ClaimedObj`, e `TownIrongardLoc.StoreBtn`. Il commento dice da dove vengono (dump o dal vivo, con la data).
4. **`src/Tasks/Town/AmuletShopTask.cs`**:
   - `DefaultEnabled => false`: spende valuta che si paga, si accende solo a mano;
   - config: `dry_run = true`, `gem_reserve = 0`, `last_purchase_date` (gestito dal bot);
   - una volta per giorno di gioco, dopo il reset (`GameDay`). Legge oggi, posseduti, livello e gemme,
     chiama il planner e scrive sempre una riga:
     `[INFO] Amulet of the day: AmuletOfSky (tier 2), gems 2150, owned 3/19, skip: needs 4000 (AmuletOfConquest missing).`
   - solo se `buy` e non `dry_run`: Città → `store` → tab amuleti, poi i controlli del punto 3.3,
     click su `purchaseButtonGems`, verifica del punto 3.4;
   - `NextRunTime = GameDay.NextReset()`.
5. README (elenco dei task), template dei cfg (`[amuletshoptask]`, enabled false), TESTING.md (riga del
   task), questo piano (stato).

## 5. Fasi e verifiche

1. **Sonda in sola lettura, su Steam-0.** Il task con `dry_run = true`, oppure solo il log, per tre cose:
   - le letture dal gioco: oggi, posseduti, gemme. Gli acquisti fatti a mano dall'utente devono
     comparire come posseduti;
   - se il pulsante `store` della Città apre lo Store;
   - come è fatta la carta dal vivo: tab, `AmuletStoreInteraction.amulet.code` uguale al codice di
     `TryGetDailyAmuletCode`, testo del prezzo.

   Nessun click d'acquisto.
2. **Dry run sulla flotta per 3-7 giorni.** Il log di ogni istanza dice cosa avrebbe comprato. Si
   controlla che le decisioni abbiano senso e che l'amuleto cambi ogni giorno. Il dry run serve anche
   a capire se `GetDailyEligibleCount` esclude i posseduti.
3. **Primo acquisto vero, su Steam-0 o un account scelto dall'utente**, solo dopo il suo ok. Si guarda
   come va il click: popup di conferma? Animazione? `claimedObj` che compare? Si verificano le gemme
   -2.000 e `HaveAmulet()`.
4. **Flotta**: `enabled = true` e `dry_run = false` nel template, `apply_template.ps1`, poi
   distribuzione della DLL come sempre. Nei giorni dopo si controllano le righe `Amulet of the day`
   e gli acquisti.

## 6. Domande aperte per l'utente

- I tier della sezione 2 vanno bene? Soprattutto: War nel tier 1 e Sky in testa al tier 2.
- Quanto `gem_reserve`? Il default proposto è 0: oggi il bot non spende gemme per nient'altro.
- Gli amuleti in golden key (Luck) entrano qui o restano fuori?
- I token della Taverna (1.000 gemme per 20) devono competere con gli amuleti? Per ora no: le gemme
  vanno solo agli amuleti, come dicono entrambe le guide.
