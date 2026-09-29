# Piano: upgrade automatici di gear e gioielli nella Hall of Heroes

> Stato al 2026-09-29: implementato e verificato su Steam-0, tranne lo sblocco dei tier (nessun eroe
> dell'account ne ha uno bloccato). Esito della Fase 0 in fondo alla sezione 8; esito del test dal
> vivo nella riga Hall of Heroes di TESTING.md.

## 1. Contesto

- **Il task esiste già**: `src/Tasks/Character/HallOfHeroesTask.cs`. È spento (`DefaultEnabled => false`)
  e non è mai girato dal vivo. Il modello è in `src/GameModel/Features/Character/HallOfHeroes.cs`, i path in
  `src/Infrastructure/Paths/HallOfHeroes.cs`. Va **riscritto**, non affiancato da un task nuovo. Oggi ha
  tre difetti:
  1. **Non clicca niente.** `SlotButton(i)` è `new GameButton(parent: GearGrid.GetChild(i))`, cioè la
     riga `gear (N)`. Il `Button` però sta in `gear (N)/mainBg/enchantItem`, e `GameElement.TryGetComponent`
     cerca solo sull'elemento stesso, quindi `IsClickable()` risponde sempre false.
  2. **Riempie uno slot alla volta**, fino a 50 click, seguendo l'ordine della griglia: il primo eroe
     prende tutti i cristalli e il Ring arriva quarto.
  3. **Potenzia il tier 1 solo per chi è in formazione.** Così gli eroi in panchina non arrivano mai alla
     gear power che serve per sbloccare i tier 2 e 3.
- **Precedente per lo snapshot**: `TalentsTask` salva nel cfg un campo auto-managed
  (`known_maxed_nodes`) per non dover rileggere il gioco. Qui si fa lo stesso con lo stato degli eroi.
- **Precedente per i test**: `tests/Firebot.TalentEngine.Tests` include `src/Utilities/TimeParser.cs`
  con `<Compile Include=... Link=...>`. Il pianificatore nuovo è un file puro (niente Unity, niente
  MelonLoader) e si include nello stesso modo. Niente assembly nuovo e nessuna dll in più da distribuire.
- **Valgono tutte le regole del repo** (TESTING.md, sezione regole):
  - un path si corregge solo dopo averlo visto dal vivo o nel dump;
  - navigazione esplicita a ogni passo: eroe → scheda → categoria, senza mai dare per scontato che una
    scheda sia già aperta;
  - il debug parte dalle righe `[FAILED]` del log;
  - mai spendere gemme.

  I commenti nel codice si scrivono in inglese, nello stile dei file vicini. Le scorciatoie volute si
  segnano con un commento `ponytail:` che dica fin dove reggono.

## 2. Comportamento voluto

1. **Snapshot salvato.** Ogni 24 h il bot legge tutti gli eroi (livelli di enchant, tier bloccati, gear
   power, formazione) e salva il risultato nel cfg dell'istanza. Tra una lettura completa e l'altra non
   rifà il giro degli eroi.
2. **Spesa pianificata.** A ogni giro legge solo i saldi (Void Crystal ed Ethereal Shards). Con la
   tabella dei costi e i livelli dello snapshot decide prima **esattamente** quali upgrade fare. Esempio:
   con 95 VC fa un livello da 30 e uno da 60, poi si ferma, perché con i 5 che restano non si compra
   niente. Poi esegue solo quegli upgrade, visitando ogni eroe una volta sola.
3. **Prima di tutto si sbloccano i tier.** Gli eroi sotto la gear power richiesta (1300 per il tier 2,
   6600 per il tier 3) ricevono i Void Crystal per primi, finché non arrivano alla soglia e sbloccano il
   tier.
4. **Decide lo schermo.** Prima di ogni click il bot rilegge dal gioco livello e costo. Se non
   coincidono con lo snapshot, corregge lo snapshot e non clicca. Uno snapshot vecchio (upgrade fatti a
   mano, pezzi nuovi dai forzieri) costa al massimo una visita inutile, mai una spesa sbagliata.
5. **Gioielli**: stessa logica, con livellamento uniforme su tutti gli eroi.
6. **Soul stone**: fuori scope, perché non sono ancora sbloccate. La struttura regge una terza categoria
   quando serve.

## 3. Dati

### Snapshot nel cfg

Due campi auto-managed in `[hallofheroestask]`, sul modello di `known_maxed_nodes`:

```ini
# (auto-managed, don't edit) - every hero's enchant levels, locked tiers, gear power and formation
hero_snapshot = "Ledra*@5400:6,5,5,4,4,3,L,L;3,3,2,2,2,1|Talia@1250:2,1,1,L,L,L,L,L;0,0,0,L,L,L"
# (auto-managed, don't edit) - when hero_snapshot was last fully rebuilt
hero_snapshot_time = "2026-09-29T08:00:00.0000000+02:00"
```

- `hero_snapshot_time` usa il formato `"O"`, come `next_run_time_internal` in `BotTask`.
- Gli eroi sono separati da `|`. Ogni eroe è scritto così: nome, `*` se è in formazione, `@` + gear
  power, `:`, gli 8 slot gear, `;`, i 6 slot gioiello.
- Ordine degli slot gear: Weapon, Chest, Boots (tier 1), Wrist, Shoulder, Belt (tier 2), Ring, Relic
  (tier 3). Ordine dei gioielli: Ankh, Rune, Idol (tier 1), Talisman, Necklace, Trinket (tier 2).
  **Va confermato in Fase 0**: oggi è dedotto dal wiki.
- Valore di ogni slot:
  - un intero: il livello di enchant;
  - `L`: tier bloccato;
  - `M`: slot che ora non si può potenziare (al massimo per la sua rarità, oppure vuoto; come si
    riconosce lo dice la Fase 0).
- La chiave è il **nome** dell'eroe, non la posizione: la griglia potrebbe riordinarsi (Fase 0). Dai
  nomi si tolgono i caratteri `|:;,@*`.
- Se lo snapshot è illeggibile o ha un numero di slot sbagliato, `Parse` restituisce null e parte la
  lettura completa.

### Costi: tabella, non formula

La tabella viene dal wiki (pagine Gear e Jewels, che hanno la stessa). Costo per passare dal livello L
al livello L+1 del tier 1:

```
30, 60, 120, 240, 480, 960, 1920, 3840, 5760, 8640, 12960, 19440, 38880, 77760, 116640, 174960
```

Il tier 2 costa il doppio, il tier 3 il quadruplo; i gioielli hanno solo tier 1 e 2. Dal livello 16 in
poi lo slot non si può più potenziare. Durante la lettura completa ogni `costText` letto si confronta
con la tabella: se non coincide, il bot scrive una riga `[FAILED]` nel log e usa il valore dello
schermo. Serve a scoprire eventuali sconti che il wiki non riporta.

### Soglie di gear power

`Tier2PowerRequired = 1300` e `Tier3PowerRequired = 6600` (valori del wiki; la guida F2P dice
1400/6700). Se in Fase 0 il testo `tierNLocked/desc` mostra la soglia, usa quella e aggiorna le costanti.

## 4. Priorità

### Classe 0: sblocco dei tier (solo Void Crystal)

Un eroe è "in sblocco" quando ha un tier bloccato **e** la sua gear power è sotto la soglia di quel
tier: 1300 se è bloccato il tier 2, altrimenti 6600.

- **Ordine degli eroi**: prima quello più vicino alla soglia (`soglia − power` più piccolo), perché
  sblocca prima.
- **Dentro l'eroe**: lo slot sbloccato più economico, cioè solo il tier 1 finché il tier 2 è bloccato,
  poi tier 1 e tier 2. A parità di costo vince il tier 1, che dà più power per cristallo (8:6:5 secondo
  la guida Steam). Contano tutti e tre i pezzi del tier 1, chest e boots compresi, anche per gli eroi
  fuori formazione.
- **Dopo ogni upgrade** il bot va in scheda Gear → Galleria e rilegge la power. Se l'eroe ha raggiunto
  la soglia e il pulsante di sblocco è cliccabile, sblocca il tier. I meteoriti sono già protetti da
  `MeteoriteResearchTask.min_meteorite_reserve`. Poi aggiorna lo snapshot e ricalcola l'ordine: un eroe
  che ha appena sbloccato il tier 2 resta in sblocco verso i 6600.
  `// ponytail:` rileggere la power costa 4 click in più per upgrade, ma capita poche volte nella vita
  di un eroe. Se diventa lento, il guadagno si può stimare dalla power del singolo pezzo (vista elenco).
- **Soglia raggiunta ma sblocco fallito** (meteoriti insufficienti, popup valuta): per questo giro
  l'eroe esce dalla classe 0, e il log scrive `waiting for meteorites`.

### Classe 1: livellamento normale

Livello effettivo = livello attuale − vantaggio dello slot. Vince il livello effettivo più basso. A
parità vince l'eroe in formazione, poi il costo minore, poi il nome.

| Slot | Su chi | Vantaggio |
|---|---|---|
| Ring | tutti | +1 |
| Wrist | tutti | 0 |
| Relic | tutti | −1 |
| Weapon | solo formazione | −1 |
| Chest, Boots | solo formazione | −2 |
| Shoulder, Belt | tutti | −3 |

Perché questa logica, da non sostituire: i bonus dei tier 2 e 3 valgono per tutti gli eroi e si
moltiplicano tra un eroe e l'altro, e ogni livello circa raddoppia sia l'effetto sia il costo. Quindi
conta il livello, non l'eroe che porta il pezzo: l'eroe conta solo per il tier 1. I vantaggi sono
costanti nel codice (sono loro la manopola da regolare), non opzioni del cfg.

### Gioielli

Tutti i vantaggi a 0, su tutti gli eroi: è stato deciso di non filtrare per equipaggio delle War
Machine. A parità vince il costo minore, poi la formazione, poi il nome. Nel dump il tier 2 dei
gioielli non ha un pulsante di sblocco (`jewels/.../tier2Locked` contiene solo `desc`): gli slot
bloccati restano `L` e si saltano.

### Regola di stop

Il pianificatore prende il candidato migliore. Se costa più del saldo rimasto, **si ferma** invece di
ripiegare su candidati peggiori ma più economici: così i cristalli si accumulano per il Ring e per gli
sblocchi. Va scritta come un solo `break`, così è facile cambiarla. Se la classe 0 si ferma per
mancanza di fondi, in quel giro la classe 1 dei gear non parte.

## 5. Il giro del task

Parte ogni 6 h o quando compare il badge (`RecheckDelay` e `NotificationBadgeName`, come oggi).

> **Badge acceso: già coperto.** Il badge della Hall of Heroes può restare acceso anche quando non
> c'è niente da comprare, per esempio per un pezzo nuovo. Dal 29/09 `BotTask.BadgeCooldown` fa
> ripartire un task dal badge al massimo ogni 30 minuti. Prima, nei log del 26-29/09, Chaos Rift e
> Awakening ripartivano ogni 40-50 s. Non serve niente di specifico per questo task: basta
> verificare nel test dal vivo che non riparta a distanza di secondi.

1. **Serve una lettura completa?** Sì se:
   - lo snapshot è vuoto o illeggibile;
   - `hero_snapshot_time` ha più di 24 h (costante `FullScanInterval`);
   - il numero di eroi nella griglia è diverso da quello nello snapshot, cioè è stato sbloccato un eroe
     nuovo. Il conteggio si fa senza click.

   Se la Fase 0 mostra che la griglia non contiene tutti gli eroi, il conteggio si fa solo durante la
   lettura completa, e per un eroe nuovo si aspetta al massimo 24 h.
2. **Lettura completa** (solo se serve):
   - dal Party legge chi è in formazione;
   - per ogni eroe:
     - lo seleziona e ne legge il nome;
     - Gear → Galleria: legge la power e i riquadri `tier2Locked`/`tier3Locked`, e sblocca il tier se la
       power è alla soglia;
     - Enchanting → Gear: legge livello, costo e stato delle 8 righe;
     - Enchanting → Jewels: lo stesso per le 6 righe;
   - salva lo snapshot dopo ogni eroe, ma aggiorna `hero_snapshot_time` solo a lettura finita: così un
     giro interrotto da `MaxRuntimeSeconds` al giro dopo ricomincia da capo;
   - alla fine scrive nel log una tabella leggibile, con una riga per eroe. Accanto a ogni valore
     letto (livello, costo, saldo, power) scrive anche il **testo grezzo** del gioco. Nei log del
     26-29/09 Empower ha letto i Firestone come 0 per 416 volte su 416 senza che nessuno se ne
     accorgesse, perché il log mostrava solo il numero già interpretato. Un valore illeggibile
     (fallback 0) va scritto come `[FAILED]`, non preso per buono.
3. **Saldi**: legge Void Crystal ed Ethereal Shards dal contatore in alto. Quale valuta compare in quale
   scheda lo dice la Fase 0.
4. **Classe 0** (sblocco dei tier), dal vivo come descritto nella sezione 4.
5. **Pianificazione** con `EnchantPlanner.Plan`: gear con il saldo di Void Crystal rimasto, gioielli con
   il saldo di Shards. Se tutti e due i piani sono vuoti (il caso normale), il bot chiude la Hall of
   Heroes e finisce in pochi click.
6. **Esecuzione, un eroe alla volta.** Raggruppare per eroe non cambia cosa si compra, perché il piano è
   già dentro il saldo. Dentro lo stesso eroe però va mantenuto l'ordine del piano.
   - Seleziona l'eroe con tutta la catena: eroe → Enchanting → categoria.
   - Per ogni passo rilegge dallo schermo livello e `costText`:
     - se coincidono col piano: clicca, rilegge il livello (deve essere salito di 1), aggiorna lo
       snapshot e scrive nel log una riga come `Ring Ledra 4→5 (1.920 VC)`;
     - se non coincidono: corregge lo snapshot, non clicca, rilegge il saldo e rifà il piano (al
       massimo 5 volte per giro).
   - Se compare `CurrencyMissingPopup`: lo chiude, scrive `[FAILED]` nel log e smette di spendere
     quella valuta. Vuol dire che il saldo o la tabella erano sbagliati.
7. **Chiusura**: salva lo snapshot, chiude Hall of Heroes e Town, imposta `NextRunTime`.

## 6. File da toccare

| File | Cosa |
|---|---|
| `src/Infrastructure/Paths/HallOfHeroes.cs` | Path relativi alla riga: `mainBg/enchantItem`, `mainBg/enchantItem/costText`, `mainBg/gearItem/enchantLevelBg/enchantLevel`, `mainBg/jewelItem/enchantLevelBg/enchantLevel`. Altri path: il contatore `counters/counterInteraction/quantity`, la power `galleryView/powerBg/Image/power`, i riquadri `tier2Locked`/`tier3Locked`, il testo col nome dell'eroe (Fase 0) e, se serve, `goForthHero` (Fase 0). |
| `src/GameModel/Features/Character/HallOfHeroes.cs` | Corregge `SlotButton` (deve puntare a `mainBg/enchantItem`). Aggiunge `SlotLevel`, `SlotCost`, lo stato dello slot, `Balance`, `GearPower`, `IsTierLocked` e `HeroName`. Toglie `AlwaysEnchantSlots` e `ActivePartyOnlyGearSlots`. |
| `src/Tasks/Character/EnchantPlanner.cs` (**nuovo**, puro) | Contiene `record HeroState(Name, InFormation, GearPower, int[] Gear, int[] Jewels)` con le costanti `Locked` e `Unavailable`, e le funzioni `Cost(tier, level)`, `GearTier(slot)`, `JewelTier(slot)`, `Plan(heroes, category, long balance)`, `GatingOrder(heroes)`, `NextGatingSlot(hero)`, `Format` e `Parse`. `Plan` restituisce una lista di `Step(Hero, Category, Slot, FromLevel, Cost)`. Nessun `using` di Unity o MelonLoader. |
| `src/Tasks/Character/HallOfHeroesTask.cs` | Riscrive `Execute` come nella sezione 5. Aggiunge le due entry auto-managed in `OnConfigure`. Toglie `EnchantSlot` e `MaxEnchantIterationsPerSlot`. |
| `tests/Firebot.TalentEngine.Tests/Firebot.TalentEngine.Tests.csproj` | Aggiunge `<Compile Include="..\..\src\Tasks\Character\EnchantPlanner.cs" Link="EnchantPlanner.cs" />`. |
| `tests/Firebot.TalentEngine.Tests/EnchantPlannerTests.cs` (**nuovo**) | I test della sezione 7. |
| `tools/ConfigTemplate/FirebotPreferences.template.cfg` | Aggiunge i due campi nuovi sotto `[hallofheroestask]`. |
| `TESTING.md`, `README.md` | Aggiorna la riga Hall of Heroes. Toglie la voce "Hall of Heroes, prima di accenderlo" da "Da fare e rimandato". In "Da segnalare subito" la voce sul tier 1 fuori formazione diventa "fuori formazione, tranne gli eroi in sblocco tier". |

`DefaultEnabled` resta `false` finché non passa il test dal vivo della sezione 9.

## 7. Test (xunit, nel progetto che esiste già)

Pochi test, solo sulla logica pura:

1. **L'esempio dell'utente**: due gioielli al livello 0, uno di tier 1 (costo 30) e uno di tier 2 (costo
   60), saldo 95. Il piano deve essere [30, 60], con 5 di avanzo e nessun altro passo.
2. **Regola di stop**: il candidato migliore costa più del saldo, uno peggiore costerebbe meno. Il piano
   deve essere vuoto.
3. **Vantaggi**: con Ring e Wrist allo stesso livello viene prima il Ring; con il Ring un livello sopra
   il Wrist viene prima il Wrist.
4. **Tier 1**: per un eroe fuori formazione `Plan` non lo sceglie mai, mentre `NextGatingSlot` lo
   restituisce se l'eroe è sotto soglia.
5. **`Cost`**: qualche valore della tabella del wiki (T1 livello 0 = 30, T3 livello 3 = 960, T1 livello
   8 = 5760, T2 livello 12 = 77760).
6. **Snapshot**: `Parse(Format(x))` restituisce x; un testo rovinato dà null.

Come chiede TESTING.md, `dotnet test tests\Firebot.TalentEngine.Tests` deve passare e la build deve
dare 0 warning.

## 8. Fase 0: verifiche nel gioco (BLOCCANTE: vanno fatte prima di scrivere la logica)

Il gioco installato ha una struttura diversa da quella di `docs/screens/HallOfHeroes.html`:
`gearSubmenu` ed `enchantingSubmenu` ora sono prefab separati. Per il dump statico serve
`python tools\unity_ui_mapper.py enchantingSubmenu gearSubmenu`.

Il resto va visto dal vivo:
1. aggiungere log temporanei (`Logger.Info(Watchdog.DumpChildrenRecursive(path, 4))` e letture di
   testo);
2. fare la build e il deploy su Steam-0 come spiega TESTING.md;
3. leggere il log;
4. **togliere i log**.

Da verificare:

1. **Dove stanno a runtime** `gearSubmenu` ed `enchantingSubmenu`: sotto `HallOfHeroes/submenus/bg/`,
   come assumono i path attuali?
2. **Quale slot corrisponde a quale indice**: leggere `effectDescription` di `gear (0..7)` e
   `jewel (0..5)`, e confermare o correggere l'ordine della sezione 3.
3. **Griglia degli eroi**: quanti figli `hero (N)` ci sono a runtime (il prefab ne ha 5, più
   `allHeroesButton`), e se l'ordine cambia dopo aver selezionato un eroe. Se sono solo 5, o se si
   riordinano, gli eroi si scorrono con `characterAnimateButton/navigation/goForthHero` finché il nome
   non si ripete, e si contano così.
4. **Nome dell'eroe**: il path del testo sotto `base/characterPreview/standardLevelProgress`.
5. **Party**: se le carte `deckSlot(N)` mostrano il nome dell'eroe. Se non lo mostrano, verificare che
   l'ordine coincida con quello della Hall of Heroes.
6. **Righe di enchant**:
   - formato di `enchantLevel` (al livello 0 compare o è nascosto?);
   - formato di `costText` (`1.920`? `1,92K`?);
   - come si presenta uno slot **al massimo** e uno **vuoto**;
   - se `enchantItem` resta cliccabile quando non si può pagare (e quindi apre il popup valuta).
7. **Contatore in alto**: quale valuta mostra nella scheda gear e quale in quella gioielli, e in che
   formato. `GetParsedDoubleAbbreviated` (`StringUtils.ParseAbbreviated`) conosce K/M/B/T e,
   dal 29/09, la notazione a lettere oltre T (`aa` = 1e15, … `bl`: era il bug di Empower). Per altri
   formati va esteso.
8. **Galleria**:
   - formato di `power`;
   - cosa dice il testo `tierNLocked/desc` (contiene la soglia?);
   - se lo sblocco chiede conferma con un popup;
   - cosa succede se mancano i meteoriti.
9. **Tabella dei costi**: confrontarla con `costText` su qualche slot.

Ogni risultato va scritto in TESTING.md o nel commento del path corrispondente, come si è fatto per gli
altri path già verificati.

**Esito (29/09, Steam-0, dump dal vivo):**

1. Sì: `submenus/bg/gearSubmenu` e `submenus/bg/enchantingSubmenu`, creati la prima volta che si apre il
   loro tab.
2. Ordine confermato dalle descrizioni. A runtime le righe si chiamano come lo slot (`Weapon`…`Relic`,
   `Ankh`…`Trinket`) e il gioco le **riordina** (prima le incantabili): si indirizzano per nome.
3. La griglia ha tutti gli eroi (10 celle attive, gli stessi di `goForthHero`), in un ordine che non
   cambia selezionandoli. Le celle clone hanno tutte lo stesso nome (`heroSquare(Clone)`, la prima
   spenta), quindi si cliccano dal Transform e non dal path.
4. `base/characterPreview/standardLevelProgress/bg/name`.
5. Le carte `deckSlotN` non hanno il nome e sono in un altro ordine. La formazione si legge da
   `bg/parallaxBg/layers/heroSlots/heroSlotN`: lo spine dell'eroe è un figlio che si chiama come lui.
   Si caricano uno alla volta in 2-4 s (anche i tick del deck e il "Deployed: N/5").
6. `enchantLevel` nascosto al livello 0; `costText` `240`, `1.920`; slot vuoto = riga spenta; slot al
   massimo per la rarità = `extraInfo` "This enchanting requires higher rarity item."; `enchantItem`
   non interattivo quando il saldo non basta (nessun popup).
7. Due contatori sempre visibili, in tutte le schede: `counters/currencyInteraction (VoidCrystal)/quantity`
   e `(EtherealShard)/quantity`, formato `3.010`.
8. Power in `galleryView/gear/unlocked/powerGearTMP` (`74.950`; `powerBg/Image/power` è solo
   l'etichetta "Power"). Testo dei riquadri, popup di conferma e meteoriti: non visti.
9. Tabella confermata su una quarantina di `costText` (tier 1, 2 e 3, gear e gioielli).

## 9. Test dal vivo prima di accenderlo sul fleet

Su Steam-0, con `[hallofheroestask] enabled = true`, deve succedere questo:

- **primo giro**: lettura completa, snapshot scritto e tabella nel log, con valori uguali a quelli che
  si vedono nel gioco;
- **upgrade**: ogni upgrade nel log corrisponde al piano, e alla fine il saldo è sotto il costo del
  prossimo candidato;
- **errori**: nessun `CurrencyMissingPopup` e nessun `[FAILED]` nuovo;
- **secondo giro entro 24 h**: niente lettura completa, e se non c'è niente da comprare il bot chiude in
  pochi click;
- **upgrade fatto a mano**: il bot corregge lo snapshot senza fare click sbagliati;
- **tier 1 fuori formazione**: potenziato solo sugli eroi in sblocco tier.

## 10. Decisioni già prese (non riproporle)

- Snapshot nel cfg come per i talenti; lettura completa ogni 24 h (costante) e subito se cambia il
  numero di eroi.
- Regola di stop, non ripiego.
- Vantaggi come nella tabella della sezione 4; gioielli su tutti gli eroi.
- Priorità assoluta allo sblocco dei tier (gear power 1300/6600).
- Soul stone e filtro per equipaggio delle War Machine: fuori scope.
