# Firebot — piano di lavoro

Mod per Firestone Idle RPG, pensato per girare in tanti bot in parallelo (~20 istanze
contemporanee) — ogni scelta di design tiene conto del CPU/RAM aggregato su quella scala.
Codice in `src/` (namespace `Firebot`, dll `firebot.dll`, hotkey `F7`,
`FirebotPreferences.cfg`). Storicamente questo file documentava una riscrittura completa
("v2") che affiancava una versione precedente più datata finché non fosse pronta a
sostituirla — la riscrittura è ora l'unico codice esistente, la versione precedente è stata
rimossa dal repository (rimane solo nella cronologia git). Le note tecniche sotto, comprese
quelle che confrontano scelte con "la versione precedente", restano come cronologia utile a
capire perché certe cose sono fatte in un certo modo — non descrivono più una convivenza reale
tra due codebase. L'unica cautela rimasta: **la build non si copia da sola nella cartella Mods
del gioco** (`src/Directory.Build.props` punta a `dist/` locale) — così una build a metà non
sovrascrive mai silenziosamente il firebot.dll che gira davvero sui bot in produzione. Quando
una modifica è pronta per andare live, si copia `dist/firebot.dll` in `Firestone/Mods/` a mano
(o lo faccio io su richiesta).

## Terminale di stato e file di configurazione

- **Tabella di stato task** (`Next Run` / `Time Left` / `Task` / `Status` / `Last Run`): stampata nel
  log dopo ogni esecuzione — è `BotManager.PrintTasksStatusTable()`. Compare per ogni `BotTask` (i
  task "commissione" dalla 2 in poi). `Hero Upgrade` non ci compare perché non è un task schedulato
  ma un'azione continua in background (gira sempre, non ha un "prossimo controllo").
- **File di configurazione per abilitare/disabilitare i singoli task**: `UserData/
  FirebotPreferences.cfg`, una sezione `[nome_task]` con `enabled = true/false` per ognuno, generata
  automaticamente al primo avvio (via MelonPreferences, non va scritta a mano).

## Architettura: le due scelte chiave

Modello a due livelli:
- **BotTask** (scheduler unico, un task alla volta, `NextRunTime` per il prossimo controllo) per le
  "commissioni" periodiche (Town, negozi, claim vari).
- **GameElement / GameButton / GameText / CachedGameButton / GameNotificationButton** — i
  primitivi che risolvono i path e clickano.

Le due scelte pensate esplicitamente per il CPU/RAM aggregato su ~20 bot in parallelo:
1. **Upgrade eroi in battaglia**: gira come coroutine parallela allo scheduler principale (deve
   girare *durante* la battaglia indipendentemente dalle commissioni), ma con due accorgimenti
   contro lo spreco: (a) i bottoni vengono risolti una volta e riusati tra i cicli invece di essere
   ricreati ogni giro, (b) il ritmo di polling è a intervallo configurabile (default 5s) invece che
   continuo.
2. **Paths.cs**: diviso per schermata (`Infrastructure/Paths/Battle.cs`, `Paths/Town.cs`, ...),
   popolato in modo incrementale — solo i path che un task effettivamente usa, niente path "morti"
   copiati preventivamente. Fonte: `docs/index.html` + `docs/screens/*.html` (la mappa completa già
   fatta) + scansioni UnityPy mirate per ogni schermata nuova.

## Lista dei task (in ordine di implementazione — dal più semplice)

| # | Task | Stato precedente | Note |
|---|------|-------------|------|
| 1 | **Hero Upgrade** (in battaglia) | Esiste (AutoUpgrade) | Porting + fix performance sopra. **Prossimo/fatto per primo.** |
| 2 | **Oracle's Gift** (claim giornaliero) | Path già noto, mai un task dedicato | Singolo bottone, notification-driven — primo task nuovo, prova del pattern BotTask "claim semplice". |
| 3 | **Daily Rewards + Value Bundle giornaliero** | DailyRewards esiste, i due bottoni (`grid/dailyRewardsButton`, `grid/valueBundleDailyButton`) sono nuovi | Fatto. Il claim nel tab "Pacchetti Giornalieri" è solo la mysteryBox gratuita (`freeText`) — gli slot numerati `valueBundle (0)/(1)/(2)` accanto sono acquisti veri, mai toccati. |
| 4 | **Quest giornaliere** (claim `quest (N)/claimedText`) | Non esiste | Fatto. Screen `Character` → tab Missioni → sotto-tab Giornaliere, 9 slot fissi (`quest (0)`-`(8)`), click su tutti i `claimButton` (no-op sicuro su quelli non completi). Root path di `Character` non verificato dal vivo (nessun precedente) — inferito per coerenza con Store/OracleStore. **Esteso al Task 16** con le quest settimanali, classe rinominata `DailyQuestsTask` → `QuestsTask`. |
| 5 | **Free Pickaxes** | Esiste | Fatto. Aggiunta anche la navigazione manuale esplicita (rightSideUI/guildButton → TownGuild/guildShop → GuildShop/supplies), non presente né in precedenza né nella prima porting: prima c'era solo il path via notifica. Nessun NotificationPath, di proposito: il claim è a soglia (`pickaxe_claim_threshold`), quindi il badge attivo non garantisce che ci sia abbastanza da riscattare — stessa scelta della versione precedente. |
| 6 | **Engineer** | Esiste | Fatto. Porting + stessa aggiunta del Task 5: navigazione manuale esplicita (rightSideUI/townButton → TownIrongard/townBg/parent/engineer), non presente nella versione precedente (solo notifica). NotificationPath presente: claim singolo a cooldown fisso, badge affidabile (a differenza di Free Pickaxes che è a soglia). |
| 7 | **Expeditions** | Esiste (rinominato meglio: la versione precedente lo aveva in un file con nome sbagliato) | Fatto. Porting + stessa aggiunta dei task precedenti: navigazione manuale esplicita (rightSideUI/guildButton → TownGuild/expeditions), non presente nella versione precedente. NotificationPath mantenuto dalla versione precedente nonostante il claim+start combinati (comportamento già live-testato, non modificato). |
| 8 | **Guardian Training** (Magic Quarters) | Esiste | Fatto. Porting + navigazione manuale esplicita (rightSideUI/townButton → TownIrongard/townBg/parent/magicQuarters), non presente nella versione precedente. Logica di selezione guardiano e "strange dust" invariata dalla versione precedente. |
| 9 | **Oracle Rituals** | Esiste | Fatto. Porting + navigazione manuale esplicita (rightSideUI/townButton → TownIrongard/townBg/parent/oracle), non presente nella versione precedente. Schermata "Oracle" (rituali) distinta da "OracleStore" (Task 2, negozio regali). |
| 10 | **Experiments** (Alchemist) | Esiste | Fatto. Porting + navigazione manuale esplicita (rightSideUI/townButton → TownIrongard/townBg/parent/alchemist). Comportamento di default invariato dalla versione precedente: se `resource_type` non è configurato, non claima/avvia nulla (sono risorse limitate reali - Dragon blood/Strange dust/Exotic coin - richiede opt-in esplicito, stesso principio del Task 3 sugli acquisti). Aggiunto `BotSettings.FreeSpeedupSeconds` (mancava finora, usato anche dai task futuri 11/12). |
| 11 | **Map Missions + Warfront Campaign Loot** | Esistono entrambi | Fatto. Portati come 2 task separati (stessa granularità della versione precedente: MapMissions è badge-driven con `NotificationPath`, WarfrontCampaignLoot è a solo cooldown, girano su schedule indipendenti quindi unirli non avrebbe risparmiato click). Aggiunta navigazione manuale esplicita comune (rightSideUI/mapButton → WorldMap, poi il tab giusto: submenuButtons/mapMissionsButton o /warfrontCampaignButton), non presente nella versione precedente. Path dei pin missione (`menusRoot/mapRoot/mapElements/missions`) confermati dalla versione precedente: vivono fuori da menuCanvas, sulla mappa di sfondo sempre visibile, non dentro il tab. **Default `mission_time_order` cambiato da `desc` (identico alla versione precedente, verificato nel codice) ad `asc`** su richiesta esplicita dell'utente — resta comunque configurabile in entrambi i modi. |
| 12 | **Firestone Research** | Esiste | Fatto, poi rivisto su richiesta dell'utente. Navigazione manuale esplicita (rightSideUI/townButton → TownIrongard/townBg/parent/library → submenuButtons/firestoneResearch), non presente nella versione precedente. Nota: la schermata Library ha 2 tab (meteoriteResearch/firestoneResearch) e quello giusto NON è selezionato di default all'apertura (docs/screens/Library.html) — mancava nel primo giro, trovato da un audit e corretto (click sul tab aggiunto). Path del tab non verificato dal vivo (nessun precedente, solo fonte doc). **Priorità talenti rivista** (non più "livello più basso, poi tempo minore" come in precedenza): ora sceglie sempre e solo il talento con tempo di completamento minore tra tutti quelli sbloccati, su tutti e 3 gli alberi (prima ne scansionava solo uno, quello lasciato selezionato) — evita di portare un singolo talento a lvl 20 in 10 giorni quando nello stesso tempo si potrebbe alzare l'intera alberatura, stesso principio dietro anche il Task 14. **Rivista di nuovo** dopo che l'utente ha condiviso una guida esterna con tips (note.com) che indica "Raining Gold" come priorità assoluta (★★★★★): ora, tra i talenti sbloccati, se uno qualsiasi si chiama "Raining Gold" vince sempre a prescindere dal tempo di completamento; altrimenti resta il criterio precedente (tempo minore). Richiede lettura del nome del nodo (`researchName`, mai letto prima) dal popup `FirestoneResearchPreview`. |
| 14 | **Meteorite Research** (secondo tab della Library, aggiunto dopo la lista iniziale) | Non esiste, mai implementata (badge notifica presente ma "Rimossa dal bot, feature mai raggiunta") | Fatto. Nessun precedente — path trovati con una nuova scansione UnityPy diretta sugli asset di gioco (non solo dai doc, che non avevano catturato il popup di anteprima né il costo per nodo). 5 alberi × 13 nodi, click su un nodo apre sempre `MeteoriteResearchPreview` (mai un upgrade diretto) che mostra costo/stato sblocco — a differenza della ricerca standard qui non c'è modo noto di leggere il saldo delle pietre di meteorite senza aprire la Library (nessuna barra valute persistente trovata nemmeno sulla schermata di battaglia, verificato: `topRightSideUINew`, la variante live, non ha alcun contatore valuta). Di conseguenza il task scansiona sempre tutti e 5 gli alberi cercando il nodo sbloccato più economico e tenta il click (no-op sicuro se non ancora permesso), poi riprova dopo `recheck_interval_minutes` (default 60) invece di controllare di continuo. Nessun NotificationPath (soglia di valuta, stesso motivo di Free Pickaxes/Empower); notifica opportunistica aggiunta ma non verificata dal vivo (nessun precedente). **Stessa revisione priorità del Task 12**: "Raining Gold" vince sempre sul nodo sbloccato tra tutti gli alberi se presente, altrimenti resta il più economico. |
| 15 | **Path of Glory** (Battle Pass, aggiunto dopo la lista iniziale) | Non esiste, mai implementata | Fatto. Nessun precedente. 35 gradini (tier), claim sia sulla traccia gratuita che su quella Golden/premium (quest'ultima è comunque solo un claim, non un acquisto — l'acquisto vero è `getGoldenPassButton`, mai toccato; il bottone claim Golden è un no-op sicuro se non si possiede il pass, esattamente come ogni altro claim in questo codebase). Mai toccato nemmeno `instantCompleteNextMilestoneButton` (skip a pagamento). Il bottone HUD `pathOfGloryButton` (in `bottomSideUIDesktop`, **non verificato dal vivo** — variante HUD diversa da `bottomSideUINew` mai usata da alcun codice precedente, ambiguità già segnalata nei doc ma mai risolta) fa sia da ingresso al menu sia da notifica (badge direttamente sul bottone, non sulla rail `leftSideUINew` — confermato assente dalla lista completa di 48 badge), quindi qui non esiste un `Notifications.X` opportunistico separato: il click sull'HUD è già il fast path, usato anche come `NotificationPath` per lo scheduling a priorità. Percorsi interni ai singoli tier (nodi decorativi omessi dai doc) confermati con una scansione UnityPy mirata. Ottimizzazione applicata: controllo `IsClickable()` prima di ogni click (invece di clickare sempre e aspettare comunque il delay) per non sprecare tempo sui 35×2 bottoni quando la maggior parte sono no-op. |
| 16 | **Quest settimanali** (estensione del Task 4, trovato scandagliando la "bibbia" per altri claim "free") | Non esiste | Fatto. `DailyQuestsTask` rinominata `QuestsTask`, aggiunto il tab Settimanali (`bg/submenuButtons/weeklyButton`, 10 slot invece di 9, stessa struttura identica). Il countdown di rinnovo (`questsRenewBg/questsRenewText`) è condiviso tra i due tab — letto separatamente dopo aver selezionato ciascuno, `NextRunTime` = il minimo tra i due. |
| 17 | **Warfront — Missioni Giornaliere/Liberazione** (estensione dell'area Warfront Campaign, trovato scandagliando la "bibbia") | Path definiti nella versione precedente (`WFDailyMissions`/`WFLiberationMissions`) ma mai collegati a nessun task | Fatto. Nuovo task separato da WarfrontCampaignLootTask (badge diverso: sta direttamente sul bottone `dailyMissionsButton`, non sulla rail `leftSideUINew`, stesso caso di Path of Glory). Struttura confermata con scansione UnityPy mirata (i doc non avevano queste popup): il popup `WFDailyMissions` ha in realtà 2 categorie, "Liberation Missions" e "Dungeon Missions" — per ora collegata solo la prima (quella richiesta), la seconda è una scoperta bonus non ancora sfruttata. Dentro "Liberation Missions" si clicca `fightButton` su ognuna delle 10 missioni (no-op sicuro se bloccata/già vinta) — nessuna valuta/costo in tutto il flusso, confermato via scansione. |
| 18 | **Scarab Game — omaggio giornaliero** (ScarabGameShop tab "Saldi", trovato scandagliando la "bibbia") | Non esiste, mai implementata | Fatto, poi corretto su segnalazione dell'utente. Nessun precedente. Stesso pattern del mystery box (Task 3): bottone chiamato `purchaseButton` ma con etichetta `freeText` = "Gratis" a confermarlo. **Correzione navigazione**: inizialmente avevo assunto nessun ingresso manuale permanente (solo le notifiche) — l'utente ha segnalato e la wiki conferma (firestone-idle-rpg.fandom.com/wiki/Tavern: "the scarab's game is a part of the Tavern") che l'ingresso vero è Town → Taverna (`townBg/parent/tavern`) → bottone "shop" della Taverna stessa → schermata Scarab's Game → suo bottone "shop" → ScarabGameShop. Aggiunta anche soglia di livello 60 (dato preso dall'infobox della wiki, stesso pattern di livello 200 già usato per Oracle's Gift). |
| 13 | **Empower** (Temple of Eternals, aggiunto dopo la lista iniziale) | Esiste (`TempleOfEternalsTask`) | Fatto. Non è un boost temporaneo: è il reset/prestige del Tempio degli Eterni (banca le Firestone trovate nell'avventura corrente, alza il moltiplicatore permanente, riparte l'avventura). Gate a scelta dell'utente confermata: stessa logica della versione precedente (rapporto Firestone trovate/possedute, default 2.0x, `min_reset_ratio`) + minuti minimi/massimi di avventura (`min_adventure_minutes`/`max_adventure_minutes`, default 60/120) — non un check su "monete e gemme guadagnate" perché il pannello non espone nessuna statistica "gemme guadagnate" (le gemme sono la valuta premium usata altrove per acquisti reali) né un rapporto per l'oro (solo un totale assoluto). Navigazione manuale esplicita aggiunta (rightSideUI/townButton → TownIrongard/townBg/parent/templeOfEternals). Notifica aggiunta (`TemplePrestige`) ma **non verificata dal vivo**: a differenza di ogni altro task, la la versione precedente non aveva mai usato una notifica per questa feature, il badge esiste solo nella scansione statica dei doc — deliberatamente NON usata per lo scheduling (`NotificationPath`), solo come fast-path opportunistico in `Execute()`, stessa scelta di Free Pickaxes visto che è comunque un task a soglia. `AutoRetreat.OnAdventureReset()` è collegato (vedi sezione BotActions sotto). |

| 19 | **Quest "Collector"** (apri tutte le chest tranne 10 comuni) | Non esiste | Fatto. Richiesta esplicita dell'utente: automatizzare 6 delle 9 quest giornaliere (le altre 3 — Trainer/Conqueror/Expeditioner — si completano da sole con task già esistenti). Nuova schermata `Inventory` (mai mappata prima, né in una versione precedente né nei doc) trovata con scansione UnityPy mirata: apribile dalla schermata di battaglia (`bottomSideUIDesktop/inventoryButtonUI`), tab "Chests"/"Items". Scansiona genericamente tutti gli slot in `submenus/items/ScrollView/Viewport/Content` invece di elencare a mano le rarità (l'utente ha confermato che sono indifferenti, vanno aperte tutte) — evita di dover indovinare i nomi esatti delle rarità oltre "comune". Tiene sempre `min_common_chest_reserve` (default 10) chest comuni di scorta, il resto (comuni oltre soglia + tutte le altre rarità) aperto interamente. Flusso di apertura (`GameModel/Features/Inventory/ChestOpening.cs`): click sullo slot apre `ChestOpenPreview` (bottoni x1/x10), che dopo il primo click passa a `ChestOpening` (stessa coppia di bottoni, per continuare senza tornare indietro) — usa sempre il batch più grande disponibile (x10 finché possibile, poi x1) fino a raggiungere la quantità target. **Esteso su richiesta dell'utente**: apre anche jewel/celestial chest (prima escluse di proposito, dato che la quest "Collector" conta solo le gear chest) perché altrimenti restano inutilizzate in inventario — arrivano tipicamente dalle ricompense del Pharaoh's Vault (Task 26). |
| 20 | **Quest "Gamer"** (10 partite in Taverna) + sotto-task **BeerExchange** | Non esiste | Fatto. Soglia di livello 15 (dalla wiki). L'utente ha corretto un mio errore iniziale: le partite costano **game token**, non birra (la birra si accumula passivamente e si scambia per token, non il contrario). `GamerQuestTask` gioca fino a 10 volte lasciando sempre almeno `min_token_reserve` (default 5) token per il giorno dopo. Il rifornimento token è un task separato, `BeerExchangeTask` (badge dedicato confermato dall'utente: `leftSideUINew/notifications/.../Gamer/BeerExchange`), che apre Taverna → Mercato e compra ripetutamente il pacchetto da 5 token finché conviene. **Punto NON verificato dal vivo, a rischio**: il pacchetto da 5 token in `TavernMarket` ha due bottoni di acquisto (uno birra, uno gemme) e la scansione UnityPy non è riuscita a risolvere staticamente quale sprite valuta è associato a quale — assunto (non confermato) che `purchaseButton` sia birra e `purchaseButtonBulk` sia gemme, per posizione/ordine coerente con la wiki. Solo il bottone assunto-birra è collegato al click automatico; l'altro è deliberatamente mai referenziato da nessun task (nome `BuyFiveTokensWithGemsBtnDoNotUse`). **Da verificare dal vivo prima di lasciare il task attivo senza supervisione**, altrimenti rischio di spendere gemme al posto della birra. |
| 21 | **Quest "Merchant"** (vendi oggetti) | Non esiste | Fatto. Soglia di livello 30 (dalla wiki). Nuova schermata `ExoticMerchant` (mai mappata prima) raggiunta da Town → `townBg/parent/exoticMerchant`, tab "Sell"/"Upgrades". Prima usa tutti gli oggetti "oro" via `Inventory.UseAllGoldItems()` (stesso pattern generico per nome usato in Collector, i nomi esatti degli slot non risolvibili staticamente). Poi vende in x1 ogni oggetto rimasto nella griglia (l'utente ha confermato: solo x1, mai batch x5/x10/etc). Infine fa **un solo upgrade**, il primo disponibile nell'albero corrente (non una scansione del più economico su tutti gli alberi) — semplificazione esplicitamente autorizzata dall'utente ("vedi te [...] se è meglio e più semplice fare l'upgrade solo del primo disponibile va bene lo stesso"). **Rischio segnalato e poi chiuso**: una guida esterna (note.com) avvertiva di non vendere mai equip con Gold Gain; l'utente ha recuperato dalla wiki la lista completa vendibile all'Exotic Merchant (scroll di velocità/danno/salute, Midas' Touch, War Banner, Dragon Armor, Guardian's Rune, Totem of Agony/Annihilation, oltre agli oggetti oro) — sono tutti buff/debuff temporanei a tempo (60-120s) o gli oggetti oro già esclusi, nessun vero equip permanente (che nella wiki è una categoria "Gear" separata, mai in vendita qui). Nessuna modifica necessaria, il Merchant attuale è già sicuro. |
| 22 | **Quest "Miner"** (colpisci il Cristallo Arcano) | Non esiste | Fatto. Soglia di livello 50 (dalla wiki, stessa soglia di sblocco dei piccoli/pickaxe). Nuova schermata `ArcaneCrystal` (mai mappata prima) raggiunta da Town → Gilda → `arcaneCrystal`, click ripetuto su `hitButton` **5 volte** (richiesto esplicitamente dall'utente, click singoli anche se esiste un bottone x5 "è indifferente, come ti torna meglio" — scelti i click singoli per riusare lo stesso `GameButton.Click()` di tutto il resto senza un path aggiuntivo). Nessun controllo di scorta piccozze: l'utente ha confermato che il task periodico esistente per reclamarle (Task 5, Free Pickaxes) ne accumula abbastanza da non andare mai in negativo. |
| 23 | **Quest "Liberator"** (estensione del Task 17) | — | Fatto. L'utente ha corretto un'assunzione iniziale: non è un claim istantaneo, c'è una vera battaglia da aspettare. Formazione della squadra impostata una tantum manualmente dall'utente (non più richiesta al bot). Scansione UnityPy mirata su `fightButton` di `WFLiberationMissions` ha trovato la catena: apre `WFBattleSim` (anteprima formazione con bottone `fightBtn` = "start" vero e proprio, mai toccato `changeAttackerFormationButton`) → battaglia reale a schermo intero `WFBattle` (nessun path definito, nulla da cliccare: il suo `closeButton` abbandonerebbe la battaglia) → si risolve in `WFBattleWon` o `WFBattleDefeat`, entrambi con solo un `closeButton`. `WarfrontDailyMissionsTask` ora, per ogni missione, clicca `fightButton`, poi (se `WFBattleSim` è apparso) clicca `fightBtn` e attende in polling (ogni 2s, tetto di sicurezza ~5 minuti mai osservato ma tenuto generoso) che uno dei due popup risultato diventi visibile, poi chiude quello apparso prima di passare alla missione successiva. Nessuna quest dedicata per **"Prophet"**: già coperta dal Task 9 (Oracle Rituals, che reclama e riavvia continuamente) — con un solo rituale attivo alla volta da 40 minuti, 2 completamenti al giorno avvengono da soli. |

| 24 | **Talenti** (Character → tab Talenti, richiesto dall'utente) | Non esiste | Fatto. Albero da 46 tier / 89 nodi singoli / 2037 punti totali (confermato via wiki + scansione UnityPy: la somma dei nodi per tier nella tabella della wiki fa esattamente 89, combaciando 1:1 con `talentInteraction (0)`-`(88)` trovati dal vivo in ordine di tier). Ordine di investimento trascritto da `docs/talents-guide.html` (a sua volta trascritta da una guida Steam dall'utente, "numeri non ricalcolati" per sua stessa nota). L'algoritmo (`GameModel/Features/Character/Talents.cs`) precalcola, per ogni voce della guida, a quale nodo del catalogo corrisponde e il rango-obiettivo che deve raggiungere (con un puntatore per nome che avanza alla copia successiva solo quando quella corrente risulta già al massimo simulato) — gestisce così anche le voci della guida palesemente imprecise (es. "Trainer Skills" citata 4 volte per un totale di 62 punti, ma l'unico nodo reale ha un cap di 25: la 3ª visita lo satura già, la 4ª diventa un no-op). Verificato indipendentemente con una simulazione Python parallela alla logica C# (stessi dati, stesso algoritmo) prima di committare — tutte le voci marcate "massimo" nella guida raggiungono esattamente il cap reale del nodo corrispondente, buona conferma che il catalogo è corretto. La guida copre solo i primi ~420 dei 2037 punti totali (fino a circa il Tier 24): **su richiesta esplicita dell'utente, il task si ferma e lascia i punti non spesi una volta esaurita la sequenza nota**, invece di proseguire con un'euristica generica — un reset dell'intero albero costa 100 gemme reali (dalla wiki), quindi meglio non spendere a caso oltre il punto coperto dalla guida. **Due voci della guida rinominate per posizione**: la guida originale usa "Leader — Auto Abilities" e "Party — Auto Abilities", nomi che non esistono nella wiki attuale — mappate su `Heroes Auto Abilities` (tier 3) e `Guardian Auto Attack` (tier 5), gli unici due talenti "auto" da 1 punto/1 max dell'intero albero, nello stesso ordine in cui li cita la guida. **Confermato corretto dall'utente dopo il primo giro.** La voce "scelta libera" (341-350, Critical damage/Critical chance, 11 pt) è risolta su Critical damage, scelta arbitraria. Rango corrente letto aprendo il popup `TalentPreview` di ogni nodo (stesso pattern di `MeteoriteResearchPreviewLoc`/`FirestoneResearchPreviewLoc`) — formato esatto del testo (`talentLevelText`, "5" vs "5/25") non verificato dal vivo, parsing difensivo. **Meccanica di salvataggio confermata dall'utente**: `upgradeTalentButton` mette solo "in sospeso" il punto, va cliccato `talentsSaveButton` per renderlo effettivo — il task lo clicca dopo OGNI singolo nodo visitato (non una volta sola a fine giro), così quando il piano rivisita lo stesso talento più avanti (capita spesso, vedi `Talents.Plan`) il rango letto alla riapertura è sempre quello realmente salvato, senza dover indovinare se l'anteprima mostri il valore "in sospeso" o quello di base. Mai toccato `resetTalentsButton` (reset intero albero, 100 gemme) né `downgradeTalentButton` (rimuove un punto già investito). Task completamente stateless: ad ogni esecuzione riparte dall'inizio della sequenza e si ferma alla prima voce non ancora soddisfatta, senza salvare progressi tra un'esecuzione e l'altra — `ponytail:` un po' di click ridondanti su voci già completate ad ogni giro (soprattutto a personaggio alto livello), da ottimizzare con un indice di ripresa persistito se/quando diventa un problema reale. **Comportamento su account con percorso diverso da quello preimpostato** (domanda dell'utente): funziona comunque, senza bisogno di conoscere la storia dell'account, perché ogni voce confronta solo il rango ATTUALE del nodo col rango-obiettivo di quella voce — un talento già più avanti del previsto risulta "niente da investire" e viene saltato (differenza negativa/zero, nessun click), uno indietro viene solo completato fino allo stesso obiettivo. L'unico caso non risolvibile così: un talento della guida il cui specifico predecessore diretto nel proprio ramo (non il totale cumulativo dell'albero, vedi wiki) non ha mai ricevuto nemmeno 1 punto nella storia reale dell'account resta bloccato — prima si fermava l'intero giro su quel punto (`break`), ora salta solo quella voce e prova le successive (`continue`), così un ramo bloccato non impedisce agli altri di procedere. |
| 26 | **Pharaoh's Vault + spin Scarab Game** (richiesto dall'utente dopo aver condiviso una guida esterna con tips, note.com) | Non esiste | Fatto. Separato da `ScarabGameFreeTokenTask` (Task 18), che reclama solo un omaggio slegato nello shop e non tocca affatto le slot machine né il vault. Confermato via wiki: si gira la slot con Noble Token (10 gratis/giorno) o Pharaoh Token (comprati, valuta reale — mai toccati), ogni 5000 Ancient Coin accumulate si apre il Pharaoh's Vault per 5 ricompense casuali. **Punto di sicurezza chiarito direttamente dall'utente**: il bottone di spin consuma sempre prima i Noble Token gratuiti e diventa semplicemente non cliccabile una volta esauriti — non passa mai automaticamente ai Pharaoh Token, quindi cliccarlo "finché cliccabile" è sicuro come ovunque altrove nel progetto. Usa sempre la puntata/quantità massima disponibile sia per lo spin che per l'apertura del vault (bottoni `changePlayQuantity`/`changeQuantity`, ciclano il moltiplicatore) — scelta esplicita dell'utente, dato che è un moltiplicatore proporzionale (stessa resa attesa per token speso, solo meno click). Formato esatto delle etichette di quantità (es. "x10" vs "10") non verificato dal vivo — il ciclo si ferma appena vede "10" nel testo o dopo un numero massimo di tentativi, per sicurezza. Le ricompense del vault includono jewel/celestial chest (ora aperte da Collector, Task 19 esteso) e Sigilli di Profezia per rilasciare Beast (sistema a parte, esplicitamente lasciato fuori per ora su scelta dell'utente). |

| 27 | **Arena of Kings** (PvP war machine, richiesto dall'utente) | Non esiste | Fatto. Livello 80 (wiki, confermato dall'utente). Ingresso trovato via UnityPy: Town → `battles` (edificio "Battles", mai mappato prima) → popup hub `WFMenuSelection` (2 opzioni: `campaign`/`arena`, solo la seconda collegata) → schermata `ArenaOfKings`. Strategia dell'utente: confronta il "power" di ognuno dei 3 avversari mostrati (sempre e solo 3 per la wiki) col proprio, sceglie il più debole se ce n'è uno strettamente inferiore, altrimenti fa reroll (gratis ogni 5s per la wiki) e riprova. **Punto tecnico rilevante trovato scansionando**: la schermata mostra DUE valori di potenza affiancati, `battlePower` (combattimento normale) e `arenaPower` (specifico per l'arena, formula diversa per la wiki) — usato solo il secondo, sarebbe stato facile sbagliare guardando solo i nomi in UnityPy senza controllare i componenti. **Correzione rispetto all'idea iniziale dell'utente**: perdere una battaglia NON abbassa il rank (la wiki è esplicita: "if you lose, your ranks won't change" — solo vincere scambia i rank con l'avversario), quindi il fallback "combatti a caso dopo un timeout" non serve a "scendere in classifica e trovare avversari più deboli" come pensava l'utente — corretto e confermato con l'utente prima di implementare. Fallback progressivo su soglie di potenza concordato con l'utente: 0-3 min solo avversari strettamente più deboli; 3-5 min accetta fino al +5%; 5-7 min fino al +10%; 7-9 min fino al +20%; oltre i 9 min combatte comunque il migliore dei 3 disponibili (mai lasciare un token inutilizzato). Logica di scelta verificata con una simulazione Python indipendente (8 casi di test su tutte le fasce) prima di committare. Dopo la scelta: popup `AOKBattlePreview` (stesso pattern di `WFBattleSim` — bottone "fightBtn", formazione mai toccata, impostata una tantum dall'utente) poi attesa in polling (stesso schema di Liberator) del popup `AOKBattleResult` — a differenza di Warfront qui è UN SOLO popup con sezioni "won"/"lost" e un solo bottone di chiusura condiviso, non due popup separati. **Timeout dedicato**: in una giornata sfortunata la ricerca può arrivare vicino a 9 minuti per singolo token, fino a 5 token/giorno — su richiesta dell'utente ("metti il watchdog come pensi sia meglio per questo task") aggiunto `BotTask.MaxRuntimeSeconds` (nuova proprietà virtuale, default null = usa il limite globale `max_task_runtime`), che questo task imposta al massimo consentito dal clamp esistente (3600s) invece di alzare il limite globale per tutti — vedi sezione Organizzazione. Il Watchdog stesso non ha richiesto modifiche: è già generico (scansiona qualunque popup/menu con un `bg/closeButton` o `closeButton`, senza bisogno di conoscere i nomi specifici) e tutte le nuove schermate di questo task seguono già quella convenzione standard. |
| 28 | **Mailbox** (reclamo ricompense, richiesto dall'utente) | Non esiste | Fatto. Nome interno reale della schermata `SystemMail`, non "Mailbox" — trovato via UnityPy. Bottone d'ingresso sulla schermata di battaglia (`leftSideUINew/mail`, stessa regione con un precedente già provato usata per le notifiche — stessa ambiguità già vista altrove: esiste un secondo bottone strutturalmente identico su `bottomLeftSideUI/mail`, mai usato prima e senza precedente, scelto quello su `leftSideUINew`). La wiki elenca cosa arriva per posta: ricompense Arcane Crystal, ricompense ai traguardi di livello personaggio, ricompense di rank da Arena of Kings, ricompense Battle Pass non riscattate a fine mese. **Struttura scoperta scansionando**: non è una lista con un bottone claim per riga — è un pannello a due parti, lista scrollabile (`mailList`) + un UNICO pannello di dettaglio condiviso (`mailFullView`, fratello della lista, non un popup separato) che si aggiorna cliccando ogni voce della lista; il bottone claim vive lì dentro. Non è stato possibile confermare dal vivo se reclamare rimuove il messaggio dalla lista o lo lascia (letto/reclamato) — la scansione gestisce entrambi i casi: rilegge la lunghezza reale della lista ad ogni iterazione e avanza oltre l'indice 0 solo quando lì non c'è più nulla da reclamare (se la lista si accorcia, il prossimo messaggio scorre in posizione 0 e viene preso al giro successivo; se non si accorcia, il messaggio già reclamato viene rivisitato una volta in più — bottone claim non più cliccabile, no-op sicuro — prima di passare avanti). Verificata con una simulazione Python indipendente (8 casi: lista che si accorcia/non si accorcia, vuota, mista) prima di committare. Mai toccato `deleteButton` (trovato proprio accanto al claim) — l'utente ha chiesto solo di reclamare, cancellare è un'azione distruttiva non richiesta. |
| 29 | **Tree of Life — Personal** (upgrade con Expedition Token, richiesto dall'utente) | Non esiste | Fatto. Livello 10 (infobox wiki). Ci sono in realtà due alberi (Personal e Guild, condiviso con la gilda) — collegato solo il Personal su richiesta esplicita dell'utente, mai toccato il Guild (richiede rango di leader/ufficiale, coinvolge risorse condivise). Ingresso: Gilda → `treeOfLife` (mai mappato prima) → schermata `TreeOfLife` → tab `personalTree`. **Catalogo di 20 upgrade confermato via UnityPy + wiki**: la tabella "Personal Tree" della wiki elenca esattamente 20 righe, combaciando 1:1 con `treeOfLifePersonalUpgrade (0)`-`(19)` trovati dal vivo in ordine di ramo (sinistro/centrale/destro) — stessa tecnica di validazione già usata per Talents. **Nessun popup di anteprima trovato** per questa feature (a differenza di Talents/Firestone Research/Meteorite Research) — il rango corrente si legge direttamente sul nodo della griglia (`levelBg/level`) e si assume che il click sul nodo acquisti direttamente il livello successivo, no-op sicuro se non permesso/non affordabile — flag per verifica dal vivo, è l'unica feature-albero di questa sessione senza popup. **Priorità**: su richiesta dell'utente, "Raining Gold", "Firestone Finder" e "Firestone Effect" hanno sempre priorità sugli altri 17 upgrade (come gruppo, non rango stretto tra loro) — stessa logica già usata per Firestone/Meteorite Research ("Raining Gold vince sempre"), generalizzata da un singolo nome a un piccolo insieme. Dato che il costo di ogni upgrade scala solo in base al proprio livello attuale (stessa formula uniforme per tutti e 20, confermato dalla wiki), il rango attuale letto sul nodo funge anche da criterio di economicità per lo spareggio, sia dentro il gruppo prioritario che nel resto. Verificata con una simulazione Python indipendente (4 casi: priorità batte non-priorità più economico, fallback al più economico quando tutta la priorità è satura, spareggio dentro il gruppo prioritario, niente cliccabile) prima di committare. |
| 30 | **Awakening** (potenziamento eroi con Arcane Crystal, richiesto dall'utente) | Non esiste | Fatto. Livello 50 (wiki). Le ricompense in cristalli arrivano per mail — reclamate già dal Task 28 (Mailbox), che reclama genericamente tutto il reclamabile — qui vengono solo spesi. **Nessuna logica di selezione eroe necessaria**: per la wiki l'eroe risvegliato è scelto casualmente dal gioco stesso, con più probabilità verso gli eroi con livello di risveglio più basso (meccanismo di recupero automatico, come le Sacred Cards) — quindi a differenza di Talents/Research/Tree of Life qui non c'è nessun nodo/priorità da decidere. **Moltiplicatore**: su richiesta esplicita dell'utente ("cercando di usare il moltiplicatore il più possibile"), il task riseleziona sempre il moltiplicatore più alto disponibile (x160 in giù fino a x1, sempre disponibile per la wiki) PRIMA di ogni singolo risveglio, non solo all'inizio — il moltiplicatore non cambia l'efficienza (costo e xp scalano insieme, confermato dalla wiki) ma riduce i click, e riselezionare ad ogni giro permette di scendere naturalmente di moltiplicatore quando i cristalli non bastano più per quello più alto invece di fermarsi in anticipo. A differenza di ScarabGame/PharaohsVault (un solo bottone che cicla), qui la wiki/UnityPy hanno confermato 8 bottoni distinti con nome esplicito per ogni livello (x1/x2/x5/x10/x20/x40/x80/x160) — molto più affidabile del ciclo alla cieca usato altrove, si prova dal più alto scendendo fino al primo cliccabile. Mai toccato `autoAwakenToggle` (trovato vicino al bottone chiudi) — sembra un modo integrato di auto-risveglio ma persistenza/interazione con la selezione moltiplicatore non note, preferito il flusso esplicito già coerente col resto del codebase. |
| 31 | **Hall of Heroes — gear tier unlock + gear/jewel enchant** (`HallOfHeroesTask`, richiesto dall'utente come task di chiusura, chiedendo esplicitamente una proposta di logica) | Non esiste | Fatto per intero (gear + jewels in un solo task/giro per eroe — inizialmente pianificato come 2-3 task separati, poi unificato: stessa schermata, stessa navigazione per eroe, avrebbe raddoppiato inutilmente i click rivisitare ogni eroe una seconda volta solo per i jewel). Nessun livello minimo per la schermata (infobox wiki Hall of Heroes: "Unlocks at All Levels"; Jewels invece sbloccano a livello 50 per la loro wiki, ma non gestito come gate separato — i relativi bottoni sono comunque no-op sicuri se non ancora sbloccati). **Ingresso, corretto dopo una prima scansione incompleta**: il primo giro di UnityPy non aveva trovato nessun ingresso Town/Character (aveva controllato solo le building icon già mappate nel codebase, non un dump completo) e concludeva erroneamente che l'unico ingresso fosse la notifica; l'utente ha segnalato dal vivo che si entra da Town, e un dump completo di `townBg/parent` (24 icone) ha confermato `hallOfHeroes` come 9ª icona, mai grepped prima perché nessun task la referenziava ancora. Ingresso reale: Town → `townBg/parent/hallOfHeroes` (stesso pattern esplicito di ogni altro edificio), con l'icona sulla rail di notifica (`leftSideUINew/notifications/.../HallOfHeroes`) tenuta solo come fast-path opportunistico, come per tutti gli altri task — non più come unico ingresso. Selettore eroi (`characterListScrollView/.../heroGrid`, figli `hero (N)` + un `allHeroesButton` finale filtrato per nome) scansionato per intero — il task gira su ogni eroe del roster. Per ciascuno: **1) sblocco tier gear**, tab `gear` → **la vista attiva di default è `galleryView`, non `listView`** (trovato scansionando: `listView` risultava inattivo nel dump, sarebbe stato un errore facile costruire i path su quello) → due bottoni fissi e distinti `unlockTier2Button`/`unlockTier3Button`, cliccati se cliccabili — nessuna soglia di power/meteoriti calcolata a mano, lo stato cliccabile del bottone del gioco stesso incorpora già entrambi i requisiti (stesso principio di PharaohsVault/Awakening/Tree of Life). **2) enchant gear**, tab `enchanting` (sottomenu separato da `gearSubmenu`, confermato via UnityPy) → griglia di 8 slot fissi `gear (0)`-`(7)` senza intestazioni intermedie (a differenza della griglia di sblocco tier), ordine confermato contro la tabella tier della wiki: 0-2 Weapon/Chest/Boots (T1), 3-5 Wrist/Shoulder/Belt (T2), 6-7 Ring/Relic (T3). Gli slot 3-7 (T2/T3) sono collegati per ogni eroe senza condizioni — la wiki (tabella Bonuses) conferma che i loro bonus valgono per TUTTI gli eroi mentre T1 vale solo per l'eroe che lo indossa, quindi T2/T3 vince sempre a prescindere dall'eroe. **Gli slot 0-2 (T1) sono collegati SOLO per gli eroi nella formazione di battaglia attuale**, in base alla scelta esplicita dell'utente (AskUserQuestion: "Squadra attuale (dinamico)" contro una lista fissa di eroi) — implementato dopo aver trovato la schermata separata **`Party`** (editor della formazione, apribile da `bottomSideUIDesktop/menuButtons/partyButtonUI`, stesso pattern già noto per `inventoryButtonUI`): la sua lista eroi (`deck/heroScroll/.../heroGrid`, card `deckSlot (N)`) mostra un badge `activeIcon` acceso solo per gli eroi attualmente in uno dei 5 slot di formazione. **Punto NON verificato dal vivo, assunzione centrale di questa parte**: né la lista di Hall of Heroes né quella di Party mostrano il nome testuale dell'eroe (solo icona/sprite) — non esiste quindi un modo pulito per incrociare le due liste per nome, e il task assume che elenchino gli eroi nello stesso ordine (stessa fonte dati, due schermate diverse) per poter riusare l'indice letto in Party dentro il ciclo su Hall of Heroes. Se il targeting T1 risultasse sugli eroi sbagliati, il problema è probabilmente qui. Click diretto sul nodo slot per ogni livello di enchant (stesso pattern "nessun popup" di Tree of Life, no-op sicuro quando non più permesso/affordabile), non verificato dal vivo se lo slot stesso porti il componente Button — stessa fiducia già accordata al click su `SystemMail`/nodi Tree of Life. **3) enchant jewel**: tab `enchanting` → categoria `jewels` → griglia `jewel (0)`-`(5)` (2 tier di 3 slot ciascuno: Ankh/Rune/Idol, poi Talisman/Necklace/Trinket, wiki confermata). **Nessun bottone di sblocco tier trovato** per i jewel (a differenza di gear/soulstones, controllato sia in `galleryView` che `listView`) — il tier 2 mostra solo una card `tier2Locked` senza azione, quindi qui non serve nessuno step di sblocco. **Scope deciso senza chiedere all'utente** (semplificazione dichiarata): la wiki Jewels dice che il bonus di un jewel vale solo "quando l'eroe è nell'equipaggio" di una War Machine — stessa logica del T1 gear, ma qui tutti e 6 gli slot sono collegati per ogni eroe senza filtrare per equipaggio, perché a differenza dei Void Crystal (gear) gli Ethereal Shard (jewel) non hanno nessun altro uso secondo la wiki: non c'è un costo-opportunità reale da proteggere restringendo il target, e la schermata dell'equipaggio War Machine non è mai stata indagata. Da rivedere se in pratica risulta uno spreco significativo. Soulstones (tier-unlock e enchant strutturalmente presenti, stessa forma di gear) confermati ma esplicitamente lasciati fuori — richiesta dell'utente, feature da livello 200. **Timeout dedicato**: `MaxRuntimeSeconds` a 1800s, dato che il giro su tutti gli eroi del roster × navigazione a schermate ripetuta può allungarsi, stesso meccanismo già introdotto per Arena of Kings (Task 27). |
| 32 | **War Machines — level up** (`WarMachinesTask`, richiesto dall'utente dopo Hall of Heroes: "analizza la parte di war machine e trova una logica anche lì di lvl up") | Non esiste | Fatto. Livello 50 — **non confermato in modo indipendente dalla wiki** (la pagina wiki War Machines, una pagina "Mechanics", non ha un infobox "Unlocks at"), dedotto dal fatto che la wiki dice "war machines are unlocked by the Engineer" e l'edificio Engineer ha già una soglia di livello 50 confermata (Task 6) — stessa soglia riusata, da verificare se sbagliata. **Ingresso**: Town → Engineer (riusa `TownScreen.OpenEngineer`, già confermato dal vivo dal Task 6) → bottone `warMachinesButton` in fondo alla schermata Engineer → schermata `WarMachines`. Esiste anche un popup hub separato `GarageSelection` (3 opzioni: engineer/garage/trainingBase) che porterebbe probabilmente allo stesso posto, ma non usato qui — preferito riusare un percorso già live-confermato piuttosto che uno nuovo e incerto. **Meccanica di livello (wiki confermata)**: ogni salita di 100 xp costa Expedition Token + 3 componenti (Screw/Cog/Metal, ottenuti passivamente dalle jewel chest, distribuiti automaticamente dal gioco tra le macchine possedute) — entrambi i requisiti incorporati nello stato cliccabile del bottone `upgradeButton`, nessuna soglia calcolata a mano. **Schermata a 4 tab** (`garage`/`workshop`/`blueprintUpgrades`/`rarityUpgrades`, scansionata per intero via UnityPy): `garage` è solo un pannello statistiche in sola lettura (nessun bottone), `workshop` è l'unico con l'azione di livello (`upgradeButton`), `blueprintUpgrades`/`rarityUpgrades` sono meccaniche wiki separate (spesa valuta per singolo attributo, salti di rarità) non richieste e lasciate fuori scope. Selettore macchine (`warMachinesScrollView/.../grid`, figli `warMachineSquare (N)` + `nextWarMachineUnlock`/`allWarMachinesButton` finali filtrati per nome, stesso pattern di Hall of Heroes) scansionato per intero — gira su **tutte le macchine possedute**, non solo le 5 in formazione: a differenza del gear T1 degli eroi, la wiki non dà nessuna indicazione che livellare una macchina in panchina sia sprecato (i componenti sono già bilanciati automaticamente dal gioco su tutte le macchine possedute, con priorità a quelle sbloccate di recente) — nessuna ragione trovata per restringere lo scope come fatto per il T1. **Moltiplicatore non collegato**: esiste un `changeQuantityButton` (bottone singolo che cicla, come ScarabGame/PharaohsVault, non come gli 8 bottoni distinti di Awakening) ma il formato dell'etichetta del valore massimo non è confermato dal vivo — ciclarlo alla cieca senza una condizione di stop verificata rischierebbe di fermarsi su un valore arbitrario invece del massimo. Lasciato non collegato: click ripetuti su `upgradeButton` raggiungono lo stesso risultato finale, solo in più passaggi — possibile ottimizzazione futura di click-count, non una lacuna funzionale. **Timeout dedicato**: `MaxRuntimeSeconds` a 1800s, stesso motivo/meccanismo di Arena of Kings (Task 27) e Hall of Heroes (Task 31). |

## Punti aperti

Cose non ancora implementate, concrete:

- **Chaos Rift** (Task 25): non automatizzato. Trovata solo l'icona d'ingresso in Gilda (`chaosRift`)
  — la vera schermata di attacco/Dark Trial non è ancora stata individuata.
- **Soulstones** (Hall of Heroes, sblocco a livello 200): sblocco tier ed enchant sono strutturalmente
  presenti nel gioco (stessa forma del gear) ma volutamente fuori scope per ora, su richiesta
  esplicita dell'utente.
- **Moltiplicatore bulk di War Machines** (`changeQuantityButton`, Task 32): il livellamento usa solo
  click singoli. Il bottone che cicla la quantità esiste ma non è collegato — il formato dell'etichetta
  del valore massimo non è confermato dal vivo. Nessuna lacuna funzionale, solo più click del
  necessario per livellare più volte in un colpo solo.
- **Bonus volanti** (`FlyingBonusHunter`, BotAction, 2026-09-20): **Implementato.** Trovati dal vivo
  tramite una ricerca ricorsiva per nome su tutta la scena (Python/UnityPy non più disponibile su
  questa macchina per rifare la scansione statica usata la prima volta) — sono figli diretti di
  `battleRoot/battleMain/battleCanvas` (non sotto `SafeArea` come ogni altro path di questo file),
  quindi in realtà parte del Canvas UI e non oggetti 3D fuori dal Canvas come si pensava all'inizio.
  Trovata anche una quarta variante mai documentata prima, `CoworkerMeteoriteHunter`, oltre alle 3
  note (`DragonWithBeer`, `FemaleDragonWithBeer`, `MeteoriteHunter`). Ognuno dei 4 container di primo
  livello È ESSO STESSO un bottone reale (componente `Button`, `interactable=True` quando
  effettivamente in volo) — nessun trucco con collider/fisica necessario, click diretto come ogni
  altro elemento di questo codebase. Essendo visibili solo per pochi secondi, non è uno scheduled
  `BotTask` ma un loop indipendente (stesso pattern di `HeroUpgrade`/`AutoRetreat`) con poll ogni 2s
  di default. **Bug trovato e corretto durante lo sviluppo**: un controllo diagnostico temporaneo su
  `TryGetComponent<CircleCollider2D>` ha mandato in eccezione l'intera coroutine del bot (crash
  nell'interop generico di Il2Cpp), tenendolo offline per ~12 minuti prima di essere notato e
  rimosso. Config section `[flying_bonus_hunter]` — nome scelto identico alla vecchia sezione
  pre-rewrite per continuità. Solo un click per bersaglio, nessuna gestione di un eventuale popup di
  conferma (mai osservato dal vivo se esiste). Abilitato e testato solo su Steam-0.

## Primo giro di test dal vivo (2026-09-15/17): bug reali trovati e corretti

Prima di questo giro, nessun task era mai stato testato dentro il gioco vero (solo verifica statica
via UnityPy + build pulita). Il pattern emerso testando i primi task uno alla volta: la navigazione
arriva quasi sempre nel posto giusto, ma il bottone di claim non fa nulla. Cause reali trovate,
diverse tra loro nonostante il sintomo identico:

- **Free Pickaxes**: `ClaimBtn` puntava all'intero contenitore `freePickaxe` (zero componenti,
  nemmeno un Button — confermato via UnityPy) invece del `purchaseButton` annidato dentro
  `claimBg`. Stesso schema di Oracle's Gift sotto.
- **Oracle's Gift**: `OraclesGiftBtn` puntava al contenitore `oraclesGift`, che ha SÌ un suo Button
  (apre/seleziona la card) ma non è quello che riscatta il regalo — il vero claim è sul
  `purchaseButton` annidato. Punto sottile: qui il contenitore aveva comunque un Button, solo quello
  sbagliato, non semplicemente "zero componenti" come Pickaxes.
- **Character (Task Quests/Talents)**: `CharacterLoc.Root` era `menus/Character`, ma la schermata è
  in realtà istanziata sotto `popups/Character` — confermato dal log reale
  (`[FAILED] Path broken: ... menus/Character/bg/submenuButtons/quests`) e per analogia con
  Expeditions/EmpowerPopup/LockedGuardian, già correttamente sotto `popups/` in questo stesso file.
- **TownIrongard** (l'hub con Library/MagicQuarters/Tavern/Oracle/Alchemist/TempleOfEternals/
  ExoticMerchant/Battles/WarMachines/HallOfHeroes): era stato cambiato da `menus/TownIrongard` a
  `popups/TownIrongard` per analogia con Character, ma **non era corretto**. Live test
  (2026-09-17, Guardian Training): `popups/TownIrongard/townBg/parent/magicQuarters` non risolveva
  affatto (nodo assente), mentre il sweep generico del Watchdog - che enumera i figli reali di
  `menus/` - ha trovato un figlio chiamato proprio `TownIrongard` con `closeButton` risolvibile
  (solo inattivo). Riportato a `menus/TownIrongard`.
- **Rail di notifica battaglia / bottone mail** (`NotificationsLoc`, bottone mail): storia lunga,
  vedi la voce dedicata più sotto ("HUD a doppia variante") per la soluzione finale - i tentativi
  precedenti (`leftSideUINew`, poi `leftSideUI`+`bottomLeftSideUI`) erano ciascuno corretto solo in
  metà delle sessioni live testate.
- **HUD a doppia variante (2026-09-17, System Mail)**: un dump dal vivo dei figli reali di
  `SafeArea` (diagnostico temporaneo in `SystemMailTask`) ha mostrato `leftSideUINew` popolato
  (mail, notifications, chat, ...) mentre `leftSideUI`/`bottomLeftSideUI` erano vuoti o quasi - un
  retest a distanza di minuti, stesso timing dall'avvio, ha mostrato l'esatto opposto. L'utente ha
  confermato: **il gioco sceglie tra due varianti HUD parallele in base alla risoluzione/aspect
  ratio del client**, non è un problema di versione. Conclusione: qualsiasi path sotto una di
  queste regioni è corretto solo per metà dei client. Corretto sistematicamente (non solo per la
  posta): aggiunto `GameModel/Primitives/UiVariantButton.cs`, un helper che prova un elenco di
  path candidati in ordine e usa il primo effettivamente visibile (click) o segnala visibile se
  uno qualsiasi lo è (per la programmazione basata su notifica in `BotTask.IsNotificationVisible`).
  Applicato a: bottone mail (`LeftSideUINewLoc`/`BottomLeftSideUILoc.MailBtn`) e a **tutta** la
  rail di notifica (`NotificationsLoc`, ~20 badge, usata sia come fast-path in `Notifications.cs`
  sia come segnale di priorità in ogni task che imposta `NotificationPath`) - quest'ultimo era
  sempre silenziosamente fallito finora (mai un crash, solo il fast-path mai scattato) quindi il
  fix è puro guadagno, nessun rischio di regressione. **Non ancora esteso** all'ambiguità analoga
  Desktop/Mobile/New della barra inferiore (`BottomSideUIDesktopLoc`/`BottomSideUINewLoc`, Path of
  Glory/Inventory/Party/Hero Upgrade) - nessun test dal vivo di oggi l'ha toccata, da verificare in
  un giro dedicato prima di applicare lo stesso pattern lì.
- **Oracle Rituals / Experiments / Firestone Research** (`ClaimBtn` in `OracleLoc.RitualLoc`,
  `AlchemistLoc.ExperimentsLoc`, `LibraryLoc.ResearchPanelLoc`): un tentativo di fix intermedio
  (analisi esterna, poi verificata) aveva reso questi path assoluti (prefissati con `Root`), ma il
  codice li usa tutti come **suffisso relativo** passato a `GameButton(path, parent)` con un
  elemento-figlio specifico (rituale/slot/nodo) come parent — renderli assoluti costruiva un path
  doppio e rotto. Ripristinati come frammenti relativi; corretto anche `NextRunTimeTxt` di Oracle
  Rituals (`timeBg/timeLeft`, non `timeLeftText` — quel nome esiste solo un livello più in basso,
  per singolo rituale).
- **Meteorite Research**: un'analisi esterna segnalava che `MeteoriteResearchPreview.Close` non
  veniva mai chiamato per i nodi non ancora sbloccati — verificato sul codice attuale, il `Close` è
  già fuori dall'`if` e viene sempre eseguito. Nessuna modifica necessaria, la segnalazione era
  basata su una versione diversa del file o era semplicemente sbagliata.
- **Awakening**: moltiplicatore confermato funzionante, ma il task passava al giro successivo prima
  che l'animazione di risveglio (spine, cristalli + bagliore sull'eroe) finisse, annullando
  visivamente l'azione. Aggiunta un'attesa dedicata dopo il click (`Awakening.Awaken()`, ~2.5s,
  stima non misurata a fotogramma) invece di allungare `interaction_delay` globale (rallenterebbe
  ogni click di ogni task). Altre azioni con animazioni vistose (apertura Pharaoh's Vault, apertura
  chest, spin dello slot) non sono state toccate preventivamente — nessuna segnalazione concreta
  ancora, ma da tenere d'occhio nei prossimi test.

**Metodo usato per trovare questi bug, riutilizzabile per i prossimi**: i log di MelonLoader
(`MelonLoader/Logs/*.log` + `Latest.log`, con `debug_mode = true`) registrano ogni path che non si
risolve (`[FAILED] Path broken: ...`) — cercare quelli relativi ai propri task è molto più rapido e
affidabile che ipotizzare alla cieca. `tools/inspect_claims.py` risolve anche il vero nome dello
script Unity dietro ogni `MonoBehaviour` (non solo la label generica), quindi distingue in modo
affidabile un vero `UnityEngine.UI.Button` da un contenitore che sembra cliccabile ma non lo è.

Un'analisi esterna (Gemini) ha contribuito a individuare correttamente Character/TownIrongard/
Oracle's Gift/leftSideUI, ma ha anche introdotto 3 regressioni (i tre `ClaimBtn` resi erroneamente
assoluti sopra) applicando lo stesso fix per analogia senza controllare l'uso reale di ciascun
percorso — ogni sua proposta è stata riverificata con UnityPy/log prima di essere accettata, non
presa per buona.

## BotActions portate dalla versione precedente (audit su richiesta dell'utente, non nella lista task originale)

Confronto sistematico tra `BotActions/` + `Core/` della versione precedente e il codice riscritto,
per trovare meccaniche perse durante il porting. Trovate 3 cose: **AutoSkill** (spam combo abilità
in battaglia) scartata su richiesta dell'utente ("lo fa già il gioco di default"); le altre due
portate:

- **`Core/Watchdog.cs`**: rete di sicurezza generica (non specifica di una feature) che scansiona
  popup/eventi/menu rimasti aperti per errore e li chiude. La versione precedente la esegue prima E
  dopo ogni singola task schedulata — mancava del tutto nel codice riscritto inizialmente
  (`BotManager` eseguiva le task senza nessuna pulizia preventiva). Portata identica, collegata in
  `BotManager.BotSchedulerLoop()` come nell'originale.
- **`BotActions/AutoRetreat.cs`**: rileva quando lo stage non avanza per `stall_minutes` (muro di
  difficoltà) e arretra di `retreat_stages` per continuare a farmare invece di restare bloccato fino
  al prossimo Empower. Porting diretto, nessuna modifica — già efficiente nell'originale (polling
  ogni 30s, si mette in pausa da solo mentre gira una task schedulata). Ora `EmpowerTask` chiama
  `AutoRetreat.OnAdventureReset()` dopo un empower riuscito, per riarmare il rilevamento (era rimasto
  un TODO dal Task 13, visto che AutoRetreat non era ancora stato portato quando quel task fu scritto).

**Bug trovato e corretto durante l'audit**: `HeroUpgrade.Initialize()` non veniva mai chiamato da
nessuna parte in `Main.cs` (la versione precedente lo fa per `AutoUpgrade`) — la categoria di configurazione del Task 1
non veniva mai creata, quindi non era possibile abilitarlo da file di config. Aggiunta la chiamata
mancante insieme a `AutoRetreat.Initialize()`.

## Organizzazione: gruppi tematici e soglie di livello

Richiesto dall'utente una volta che i task hanno iniziato a essere tanti (26): prima l'ordine di
comparsa nel file di config e nella tabella di stato a terminale era quello — arbitrario — restituito
dalla reflection su `assembly.GetTypes()`, e la tabella di stato era per giunta ordinata per
`NextRunTime`, quindi le righe saltavano di posizione ad ogni stampa. Aggiunto:

- **`TaskGroup`** (enum in `Core/Tasks/BotTask.cs`): `Quests, Town, Guild, Map, Warfront, Character, ScarabGame`
  — `Map` e `Warfront` separati su segnalazione dell'utente: si raggiungono dalla stessa schermata
  `WorldMap` (due tab), ma sono funzionalmente due feature diverse — Map Missions è sbloccata da subito
  e dispone le missioni in ordine di tempo (venivano ordinate così anche in precedenza, comportamento portato identico), Warfront
  Campaign è un sotto-sistema slegato (war machines) sbloccato a livello 50. Raggrupparle insieme
  avrebbe nascosto che non c'entrano l'una con l'altra oltre a condividere la schermata.
  — ordine di dichiarazione = ordine di comparsa ovunque. Ogni `BotTask` deve dichiarare
  `internal override TaskGroup Group => TaskGroup.X;` (proprietà astratta, quindi il compilatore
  obbliga a classificare ogni task nuovo, non è possibile dimenticarselo). `Quests` raggruppa le 6
  task che reclamano/fanno progredire le 9 quest giornaliere (Collector/Gamer/BeerExchange/Merchant/
  Miner/QuestsTask) indipendentemente dalla schermata di gioco che usano — è l'unico raggruppamento
  "per scopo" invece che "per area di gioco", proprio perché le quest erano il caso concreto lamentato
  dall'utente ("non voglio trovarmele sparse in qua e la").
- **`BotManager.Initialize()`** ora ordina le istanze per `(Group, SectionTitle)` PRIMA di chiamare
  `InitializeConfig` — le categorie MelonPreferences finiscono nel file .cfg nell'ordine in cui
  vengono create, quindi questo ordina anche il file di config, non solo la tabella a terminale.
- **`PrintTasksStatusTable`** ora itera `Tasks` così com'è (già ordinato) invece di riordinare per
  `NextRunTime` ad ogni stampa — una task resta sempre nella stessa riga tra una stampa e l'altra.
- **`SectionTitle`** (nome mostrato ovunque) diventato `"{Gruppo} - {NomeUmanizzato}"`, es.
  `"Quests - Collector"`, `"Town - Oracle Rituals"`, `"Scarab Game - Pharaohs Vault"` — con
  un'eccezione per evitare la ridondanza `"Quests - Quests"` su `QuestsTask` (mostra solo `"Quests"`).

**Soglia di livello generica**: nuova `protected virtual int MinimumCharacterLevel => 0;` sulla
classe base, applicata automaticamente da `IsReady()`/`IsNotificationVisible()` — una task sotto
soglia non viene mai considerata pronta, senza che debba più gestirselo da sola con un controllo
manuale a inizio `Execute()` (pattern che 6 task duplicavano identico: `MinerQuestTask`,
`PharaohsVaultTask`, `ScarabGameFreeTokenTask`, `GamerQuestTask`, `MerchantQuestTask`,
`OraclesGiftTask` — rimosso da tutte e sostituito con la sola proprietà). Reagisce entro un ciclo di
scan (pochi secondi) al superamento soglia, invece di aspettare fino a `RecheckDelayBelowLevel`
(tipicamente 1h) come nel vecchio pattern per-task.

Soglie aggiunte ex-novo (nessun controllo prima), tutte da infobox "Unlocks at" della wiki tranne
dove specificato:
| Task | Soglia | Fonte |
|---|---|---|
| Expeditions | 10 | wiki Expeditions |
| Free Pickaxes | 50 | detto dall'utente stesso ("Pickaxes unlock at character level 50"), più specifico del 10 generico della Guild Shop |
| Warfront Campaign Loot | 50 | wiki Warfront Campaign |
| Warfront Daily Missions (Liberator) | 50 | wiki Warfront Campaign |
| BeerExchange | 15 | stessa Taverna di Gamer |
| Engineer | 50 | wiki Engineer |
| Experiments (Alchemist) | 120 | wiki Alchemist, combacia anche con la guida note.com |
| Oracle Rituals | 200 | stesso edificio "Oracle" di Oracle's Gift, già a 200 |
| War Machines | 50 | non da un infobox wiki diretto (pagina Mechanics, nessuno) - dedotto da "unlocked by the Engineer" + soglia 50 già confermata per l'edificio Engineer |

**Non gestito, segnalato**: la wiki usa "Stage N" (stage di battaglia raggiunto, non livello
personaggio) per lo sblocco di Library — sia Firestone che Meteorite Research, Stage 45 — e Temple of
Eternals/Empower, anch'esso Stage 45. È un asse diverso dal livello personaggio e leggerlo richiede
essere sulla schermata di battaglia (stesso meccanismo di `StageProgress` usato da AutoRetreat), non
compatibile con il controllo generico appena aggiunto senza altro lavoro — lasciati senza soglia per
ora (Empower ha comunque un suo gate a parte su rapporto Firestone/minuti avventura, quindi il rischio
pratico è basso). Magic Quarter (Guardian Training) è "Stage 1" quindi di fatto sempre sbloccato,
nessuna soglia necessaria.

**Timeout per-task**: `BotTask.MaxRuntimeSeconds` (proprietà virtuale, default `null`), letta da
`BotManager.RunSafe` solo per l'esecuzione del task vero e proprio (non per i cleanup del Watchdog,
che restano sempre sul limite globale `max_task_runtime`). Aggiunta per Arena of Kings (Task 27,
impostata a 3600s, il massimo consentito dal clamp) invece di alzare il limite globale — così gli
altri task mantengono un timeout più stretto come rete di sicurezza. Riusata anche da Hall of Heroes
(Task 31, 1800s — il giro su tutti gli eroi del roster può allungarsi) e da War Machines (Task 32,
1800s, stesso motivo ma sulle macchine possedute). Il Watchdog stesso non necessita di alcuna
modifica per nessun task: è generico by design (scansiona `popups/`/`menus/` cercando `bg/closeButton`
o `closeButton` su ogni figlio, senza bisogno di conoscere i nomi specifici) e chiude automaticamente
anche le schermate più recenti finché seguono quella convenzione standard, come fanno tutte finora.

## Come procediamo

Un task alla volta. Per ognuno: path nuovi in `Infrastructure/Paths/`, classe task, build, verifica
rapida che compili, poi passo al successivo solo dopo conferma.

## Stato attuale

- [x] Scheletro progetto (csproj, Directory.Build.props, Main.cs, core infra)
- [x] Task 1: Hero Upgrade
- [x] Task 2: Oracle's Gift
- [x] Task 3: Daily Rewards + Value Bundle
- [x] Task 4: Quest giornaliere
- [x] Task 5: Free Pickaxes
- [x] Task 6: Engineer
- [x] Task 7: Expeditions
- [x] Task 8: Guardian Training
- [x] Task 9: Oracle Rituals
- [x] Task 10: Experiments
- [x] Task 11: Map Missions + Warfront Campaign Loot
- [x] Task 12: Firestone Research
- [x] Task 13: Empower (Temple of Eternals)
- [x] Task 14: Meteorite Research
- [x] Task 15: Path of Glory (Battle Pass)
- [x] Task 16: Quest settimanali (estensione Task 4)
- [x] Task 17: Warfront - Missioni Giornaliere/Liberazione
- [x] Task 18: Scarab Game - omaggio giornaliero
- [x] Task 19: Quest "Collector" - apertura chest
- [x] Task 20: Quest "Gamer" + sotto-task BeerExchange
- [x] Task 21: Quest "Merchant" - vendita oggetti
- [x] Task 22: Quest "Miner" - Cristallo Arcano
- [x] Task 23: Quest "Liberator" (estensione Task 17) - Prophet coperta dal Task 9, nessun task nuovo
- [x] Task 24: Talenti (Character → tab Talenti)
- [ ] Task 25: Chaos Rift (attacco al boss condiviso con Moon Stone gratuite) - individuata la schermata icona (`chaosRift`, Gilda) ma non ancora la vera schermata di attacco/Dark Trial, serve altra indagine prima di implementare
- [x] Task 26: Pharaoh's Vault + spin Scarab Game
- [x] Task 27: Arena of Kings (PvP war machine)
- [x] Task 28: Mailbox (reclamo ricompense)
- [x] Task 29: Tree of Life - Personal (upgrade con Expedition Token)
- [x] Task 30: Awakening (potenziamento eroi con Arcane Crystal)
- [x] Task 31: Hall of Heroes - gear tier unlock + enchant gear (T2/T3 sempre, T1 solo squadra attuale) + enchant jewel (tutti gli slot) - Soulstones restano fuori scope (lvl 200+)
- [x] Task 32: War Machines - level up (tab workshop, tutte le macchine possedute) - blueprintUpgrades/rarityUpgrades restano fuori scope
