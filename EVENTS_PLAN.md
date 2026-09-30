# Piano: completare le sfide degli eventi, non solo reclamarle

> Piano da eseguire con Claude Code, scritto il 2026-09-30. Non c'è ancora niente di implementato. Si
> parte dalla **Fase 0** (sezione 7, verifiche nel gioco): finché non è chiusa, la logica è bloccata.

## 1. Contesto

- **Oggi il bot reclama soltanto.** `DecoratedHeroesEventTask`, `MassProductionEventTask` e
  `SigilsOfProphecyEventTask` aprono l'evento, cliccano i claim già pronti e comprano nello shop di
  scambio. Una sfida si completa solo se i task normali fanno, per caso, quello che chiede.
- **I mini-eventi coperti sono 2 su 11.** Tutti i mini-eventi usano la stessa schermata
  (`events/MiniEvents`), ma i task la aprono solo per nome ("Mass Production", "Sigils of Prophecy").
  Le sfide degli altri 9 (Stardust, Primordial Elements, Ethereal Miners, Team Effort, Mechanical
  Superiority, World Domination, Guardians of Destiny, Blessing of the Eternals, Champions of
  Alandria) non vengono mai reclamate. È il guadagno più facile di tutto il piano.
- **Tipi di evento** (wiki, pagina Events):
  | Tipo | Quando | Cosa c'è da fare | Coperto oggi |
  |---|---|---|---|
  | Decorated Heroes | mesi dispari, 2 settimane, livello 50 | 8 sfide giornaliere, 3 livelli ciascuna, stelle per lo shop e una medaglia (Fate +5/10/25%) | claim e shop |
  | Mini-eventi (11) | ogni 5 giorni, per 3 giorni | 1 sfida casuale al giorno per giocatore (contratti e premio dell'evento) | claim di 2 su 11 |
  | Calendario (Valentine, Spring, Tropicana, Space, Halloween, Winter) | mesi pari, 2 settimane | nessuna sfida: la valuta arriva da sola, si spende nello shop di scambio | no (vedi sezione 8) |
  | New Player / anniversario | una tantum | check-in, milestone a tempo online, shop | sì |
- **Precedente**: le quest giornaliere (`DailyQuestTask`) già leggono "fatto/obiettivo" dalla
  schermata, fanno solo quello che manca e rileggono. Qui si fa lo stesso con le carte degli eventi.
- **Valgono le regole di TESTING.md**: path solo se visti dal vivo o nel dump, navigazione esplicita,
  debug dalle righe `[FAILED]`, **mai gemme**, commit per ogni correzione verificata.

## 2. Comportamento voluto

1. A ogni giro di un evento il bot legge le carte delle sfide: testo, "fatto/obiettivo" e livello.
2. Riconosce il tipo di sfida dal testo con una tabella di espressioni regolari. Un testo che non
   riconosce va nel log come `[INFO] Challenge not handled: '<testo>'` e resta com'è.
3. Per le sfide **azionabili subito** fa esattamente quello che manca, entro il budget della risorsa
   (sezione 4), poi riapre l'evento e reclama.
4. Per le sfide **a tempo** (missioni, ricerche, spedizioni, tempo online) non fa niente di nuovo: le
   completano i task che già girano. Al massimo le favorisce (sezione 5, Fase 5).
5. Ogni azione scrive una riga nel log: `Event 'Decorated Heroes': 'Hit the arcane crystal 10
   times.' 5/10 -> 10/10 (5 pickaxes)`.

## 3. Le sfide e cosa fare per ognuna

Testi dal wiki (pagine Decorated Heroes Event e Mini-Events) e dallo screenshot del 30/09. Il testo
esatto va confermato in Fase 0: nel gioco è "Hit the arcane crystal 10 times.", "Play 12 times with
the cards at the tavern.", "Complete all scout missions." (per le missioni esplorative si contano i
cicli completi).

### Azionabili subito, con codice che esiste già

| Sfida | Dove compare | Azione | Codice da riusare | Risorsa |
|---|---|---|---|---|
| Hit the arcane crystal N times | DH (5/10/15), mini (4) | N colpi, uno alla volta | `ArcaneCrystal.Hit` (colpo confermato dai picconi, 30/09) | picconi |
| Play N times in the tavern | DH (4/8/12), mini (5) | N giocate a x1 | `GamerQuestTask` (giocata singola) | gettoni gioco |
| Open N chests | mini (2) | N forzieri dei più economici | `ChestOpening.OpenDownTo` (Collector) | forzieri Wooden/Iron |
| Sell N items at the exotic merchant | mini (5) | vende Midas' Touch, Health, Damage | `ExoticMerchant.TrySellOne` (Merchant) | oggetti |
| Complete 1 upgrades at your personal tree of life | mini (1) | un acquisto | `TreeOfLifeTask` (scelta per priorità) | gettoni spedizione |
| Complete 1 meteorite researches | mini (1) | un livello | `MeteoriteResearchTask` | meteoriti (sopra la riserva) |

### Azionabili, ma con una schermata nuova

| Sfida | Dove compare | Azione | Costo | Da decidere |
|---|---|---|---|---|
| Enlighten guardians N times | DH (1/2/3), mini (2) | illuminazione del guardian di `guardian_index` | 20 Strange Dust ciascuna (wiki) | budget di dust: le evoluzioni automatiche la usano già |
| Donate 500 guild coins to your guild | mini | donazione in Gilda | 500 guild coin | sì o no |
| Get 50 special upgrades | mini | upgrade speciali (`upgradesButtonUI`, mai mappato) | gold | quale upgrade comprare |
| Complete 1 exotic upgrades | mini | un upgrade dall'Exotic Merchant | valuta del merchant | quale upgrade |

### A tempo: le completa il gioco

| Sfida | Dove compare | Chi la fa già | Cosa si può fare in più |
|---|---|---|---|
| Complete N cycles of scout missions / N scout, adventure, war, map missions | DH, mini | `MapMissionsTask` | Fase 5: con la sfida aperta, partire prima con quel tipo di missione |
| Complete N firestone researches | DH (4/6/10), mini (2) | `FirestoneResearchTask` | niente (i tempi sono fissi; si accelera solo gratis) |
| Conduct N alchemy experiments | DH (3/6/9, livello 120) | `ExperimentsTask` | niente senza spendere altre risorse |
| Complete N guild expeditions | DH (5/10/15), mini (3) | `ExpeditionTask` | niente |
| Train guardians N times | mini (1) | `GuardianTrainingTask` | niente (cooldown del gioco) |
| Stay online for N minutes | DH (15/30/60) | nessuno | niente |
| Kill 100 enemies with <eroe> | mini | la battaglia | niente |

## 4. Budget delle risorse (da confermare con l'utente)

Le stesse risorse servono alle quest giornaliere e ad altri task. Proposta:

- **Ordine**: prima le quest giornaliere (partono alle 10:00), poi gli eventi. Le giocate e i colpi
  delle quest contano anche per gli eventi, quindi l'evento chiede solo la differenza (per esempio DH
  vuole 12 giocate: 10 le fa Gamer, l'evento ne aggiunge 2).
- **Picconi**: tutti quelli che servono alla sfida, tenendone 5 per la quest Miner del giorno dopo.
- **Gettoni della taverna**: tutti quelli che servono (non hanno altro uso).
- **Forzieri**: solo Wooden/Iron/Common, mai Common sotto `min_common_chest_reserve`.
- **Oggetti venduti**: solo i tre già ammessi per Merchant.
- **Meteoriti**: mai sotto `min_meteorite_reserve`.
- **Strange Dust per l'illuminazione**: da decidere (per esempio solo se dopo resta abbastanza per la
  prossima evoluzione, che ha la precedenza).
- **Guild coin, gold, valuta del merchant**: solo dopo una decisione esplicita.

## 5. Architettura

- **`ChallengeParser`** (nuovo, puro, testato come `EnchantPlanner`): dal testo e da "5/10" ricava
  `(Kind, Done, Target)`. La tabella delle espressioni è l'unico punto da aggiornare quando il gioco
  cambia un testo. Niente Unity, niente MelonLoader.
- **Lettura delle carte**: `DecoratedHeroesShop.Challenges()` e `MiniEvents.Challenges()` restituiscono
  titolo e progresso di ogni carta (path in `Paths/Events.cs`, dalla Fase 0).
- **Azioni**: un metodo per `Kind` in un solo punto (`EventChallengeActions`), che richiama i modelli
  esistenti (`ArcaneCrystal`, `Tavern`, `ChestOpening`, `ExoticMerchant`, `TreeOfLife`, ...). Non si
  duplica la logica dei task: si estraggono al massimo i pezzi da riusare.
- **Flusso di `EventTask`**: apri l'evento → leggi le carte → chiudi → esegui le azioni che mancano →
  riapri → reclama (i claim di oggi restano) → chiudi. Se non c'è niente da fare, il giro resta quello
  di oggi.
- **Mini-eventi generici**: un solo task che apre la carta del mini-evento in corso, qualunque sia,
  invece di un task per nome. Nome e giorno si leggono dalla carta.
- **Limiti**: al massimo un'azione per sfida e per giro, e un tetto di passi per giro
  (`MaxRuntimeSeconds` come Hall of Heroes).

## 6. File da toccare

| File | Cosa |
|---|---|
| `src/Tasks/Events/ChallengeParser.cs` (nuovo, puro) | `Kind`, tabella delle regex, `Parse(text, progress)` |
| `tests/Firebot.TalentEngine.Tests/ChallengeParserTests.cs` (nuovo) | i testi reali della Fase 0, uno per tipo, e un testo sconosciuto |
| `src/Infrastructure/Paths/Events.cs` | titolo e progresso delle carte DH (`challengeTitleText`, `progressBar/challengeProgressText`) e dei mini-eventi (`unlocked/challenge/questDescription`, `.../progressText`), dal dump |
| `src/GameModel/Features/Events/DecoratedHeroesShop.cs`, `MiniEvents.cs` | lettura delle carte |
| `src/Tasks/Events/EventTask.cs` | il flusso leggi → agisci → reclama |
| `src/Tasks/Events/EventChallengeActions.cs` (nuovo) | le azioni, riusando i modelli |
| `src/Tasks/Events/MiniEventTask.cs` (nuovo) | sostituisce `MassProductionEventTask` e `SigilsOfProphecyEventTask` |
| `TESTING.md`, `README.md` | righe degli eventi |

## 7. Fase 0: verifiche nel gioco (BLOCCANTE)

Con sonde temporanee (come per Hall of Heroes), su istanze dove l'evento è in corso:

1. **Carte di Decorated Heroes** (in corso il 30/09): testo esatto delle 8 sfide, formato del progresso
   (`5/10`, e cosa mostra a sfida finita: "Completed"?), come si legge il livello (1-2-3), stato del
   claim tra un livello e l'altro.
2. **Carte dei mini-eventi**: testo della sfida di ogni giorno su più account (è casuale per
   giocatore), progresso, carta del giorno bloccato, carta bloccata per livello (Sigils su Steam-15/16).
3. **Mini-eventi non coperti**: che il riquadro nell'elenco eventi abbia il nome dell'evento e apra la
   stessa schermata `MiniEvents`. Serve aspettare che ne parta uno diverso da Sigils (ogni 5 giorni).
4. **Illuminazione dei guardian**: path del bottone in Magic Quarters, costo mostrato, come cambia la
   dust.
5. **Donazione in Gilda, upgrade speciali, upgrade dell'Exotic Merchant**: solo se l'utente li approva
   (sezione 4).
6. **Conteggio condiviso**: confermare che le giocate e i colpi delle quest giornaliere contano anche
   per la sfida dell'evento (lo screenshot del 30/09 mostra DH a 5/10 colpi e 10/12 giocate dopo le
   quest del mattino, quindi sembra di sì).

## 8. Fuori da questo piano

- **Eventi di calendario** (Halloween e gli altri): non hanno sfide. La valuta si raccoglie da sola e
  si spende in uno shop diverso per ogni evento (la capanna della strega, il negozio d'inverno...). Si
  possono aggiungere con `ExchangeTab` quando uno è in corso e se ne vede la schermata dal vivo: il
  prossimo è Halloween (fine ottobre).
- **Pacchetti a pagamento dei mini-eventi**: mai.

## 9. Ordine di lavoro e test dal vivo

1. **Fase 1** (subito, piccola): un task generico per tutti i mini-eventi, solo claim. Verifica: il
   prossimo mini-evento diverso da Sigils viene aperto e reclamato.
2. **Fase 2**: `ChallengeParser` e sola lettura: il log elenca le sfide di ogni evento, riconosciute o
   no, su tutte le istanze. Nessuna azione.
3. **Fase 3**: azioni già pronte (cristallo, taverna, forzieri, vendite, Tree of Life, meteoriti), una
   alla volta, con Decorated Heroes come banco di prova (8 sfide al giorno).
4. **Fase 4**: azioni nuove approvate dall'utente (illuminazione, donazione, upgrade).
5. **Fase 5** (facoltativa): Map Missions preferisce il tipo di missione chiesto da una sfida aperta.

Criteri per ogni fase: nessuna spesa fuori budget, nessun `[FAILED]` nuovo, e per ogni sfida
azionabile la riga `x/y -> y/y` seguita dal claim nel giro successivo.
