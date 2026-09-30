# Piano: completare le sfide degli eventi, non solo reclamarle

> Piano da eseguire con Claude Code, scritto il 2026-09-30. **Stato al 30/09 sera**: Fase 0 chiusa
> (risultati in sezione 7), Fasi 1 e 2 fatte e verificate su Steam-0. Fase 3 fatta: cristallo e
> taverna verificati su Decorated Heroes; forzieri, vendite, Tree of Life e meteoriti scritti ma mai
> visti dal vivo (compaiono solo nei mini-eventi, il prossimo è Stardust il 04/10). Fase 4 in attesa
> del sì dell'utente.
>
> **Regola dell'utente (30/09)**: per ogni sfida si fa quello che chiede, né più né meno: solo il
> valore che sblocca il claim del livello in corso.

## 0. Come partire

Da incollare in una sessione nuova di Claude Code (cartella `C:\Repos\FirestoneBot`), avviata con i
permessi che le evitano di chiedere conferma a ogni comando:

```text
Implementiamo le sfide degli eventi seguendo EVENTS_PLAN.md, con le regole di TESTING.md (test
solo su Steam-0, path verificati dal vivo o nel dump, commit per ogni correzione verificata),
skill ponytail attiva. Parti dalla Fase 0 (sezione 7) e vai avanti fase per fase (sezione 9).
Il budget è quello della sezione 4, già deciso; per la Fase 4 (illuminazione, donazione,
upgrade) chiedimi prima.
```

Cosa sapere prima di cominciare, oltre a TESTING.md (sezione "Per l'agente": comandi, regole, dump
dal vivo):

- **Solo Steam-0.** Le istanze Steam-1..16 girano con versioni diverse del bot (TESTING.md, "Da fare
  e rimandato") e si aggiornano tutte insieme al prossimo riavvio completo: non toccarle.
- **Decorated Heroes ha i giorni contati**: dura 2 settimane nei mesi dispari ed era in corso il
  30/09. Come prima cosa, guarda nel gioco quanto manca: se finisce prima della Fase 3, le verifiche
  di DH si spostano sui mini-eventi (uno ogni 5 giorni) o a novembre.
- **Pezzi già pronti da riusare**, oltre a quelli della tabella in sezione 3:
  - `IconSprite.NameAt(path)`: nome dello sprite della valuta su un bottone. Ogni spesa nuova lo
    controlla prima del click (`strangeDust64`, `toolsIcon64`, `soulEmber64`, `meteorite64`; mai
    `gem64`) e conferma dal contatore che scende, come `BeastsTask` e `WarMachineRarityTask`.
  - `SpeedUpButton.IsFree(path)`: un'accelerazione si preme solo se non mostra il prezzo in gemme.
  - `Poll.Until` / `Poll.ClickUntil`: attese limitate per le schermate lente (il Tempio, 30/09).
  - Controlli silenziosi: `GameElement.FindTransform(path)` con `gameObject.activeInHierarchy`, per
    gli stati normali che `IsVisible` scriverebbe come `[FAILED]`.
  - Elenchi con figli dallo stesso nome o che si riordinano: clic dal `Transform` per indice e
    ricerca ripetuta a ogni giro (`HallOfHeroes.TrySelectHero`, `Beasts.TrySelect`).
- **Strange Dust**: dal 30/09 `GuardianEvolutionTask` la spende per le evoluzioni (300-600). Serve
  saperlo quando si chiede all'utente l'illuminazione (Fase 4).
- **Sonde**: per la Fase 0 serve un task temporaneo (`ProbeTask`) che scrive nel log il sottoalbero
  della schermata con testi, sprite e stato dei bottoni. Non va committato; dopo averlo tolto, a
  gioco chiuso, va cancellata dal cfg di Steam-0 la sua sezione `[probetask]`.

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
3. Per le sfide **azionabili subito** fa esattamente quello che manca, fino in fondo e senza riserve,
   subito dopo le quest giornaliere (sezione 4), poi riapre l'evento e reclama.
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
| Complete 1 meteorite researches | mini (1) | un livello | `MeteoriteResearchTask` | meteoriti |

### Azionabili, ma con una schermata nuova

| Sfida | Dove compare | Azione | Costo | Da decidere |
|---|---|---|---|---|
| Enlighten guardians N times | DH (1/2/3), mini (2) | illuminazione del guardian di `guardian_index` | 20 Strange Dust ciascuna (wiki) | sì o no (la dust serve anche alle evoluzioni automatiche) |
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

## 4. Budget delle risorse (deciso con l'utente il 30/09)

**Finire le sfide ha la priorità massima**: i claim sono il motivo di tutto il piano. Come per le quest
giornaliere, non si tiene nessuna riserva: se la sfida chiede 15 colpi al cristallo se ne fanno 15, se
chiede 8 forzieri se ne aprono 8, se chiede 15 giocate in taverna se ne fanno 15, finché la risorsa
c'è.

- **Sopra gli eventi ci sono solo le quest giornaliere**, perché sbloccano le settimanali, che hanno
  i premi migliori. Un'azione di un evento che usa la stessa risorsa di una quest giornaliera (picconi
  e Miner, gettoni e Gamer, forzieri e Collector, oggetti e Merchant) parte solo quando la quest di
  oggi è già fatta (giorno di gioco, reset alle 10:00: `last_done_date` del task uguale a oggi); se
  quella quest oggi non c'è o non si può fare, non blocca niente. Le giocate e i colpi delle quest
  contano anche per gli eventi, quindi l'evento chiede solo la differenza (DH vuole 12 giocate: 10 le
  fa Gamer, l'evento ne aggiunge 2).
- **Nessun minimo** per le sfide: né picconi tenuti per la Miner del giorno dopo, né
  `min_common_chest_reserve`, né `min_meteorite_reserve`. Quelle riserve restano per i task normali
  (Collector sulle chest extra, Meteorite Research), non per le sfide.
- **Cosa resta fisso** (non sono minimi, sono regole di sicurezza):
  - mai gemme né offerte a pagamento, sempre col controllo dell'icona della valuta
    (`IconSprite.NameAt`) prima del click;
  - forzieri dal più economico al più caro;
  - vendite solo dei tre oggetti già ammessi per Merchant (Midas' Touch, Health, Damage).
- **Azioni della Fase 4** (illuminazione dei guardian con Strange Dust, donazione in Gilda,
  upgrade speciali in gold, upgrade dell'Exotic Merchant): si fanno solo dopo il sì esplicito
  dell'utente, azione per azione. Una volta approvate, valgono le stesse regole: niente minimi.

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

### Risultati (Steam-0, 30/09, sonda `ProbeTask` poi tolta)

1. **Decorated Heroes**: finisce il 02/10 verso le 10:00 ("Time left: 1d 19:00" alle 15). Carta
   `challengesLayout/challenge (N)`: testo in `challengeTitleText`, progresso in
   `progressBar/challengeProgressText`. Testi: "Complete all scout missions.", "Enlighten guardians 1
   times.", "Complete 6 firestone researches.", "Conduct 3 alchemy experiments.", "Hit the arcane
   crystal 10 times.", "Play 12 times with the cards at the tavern.", "Complete 10 guild
   expeditions.", "Stay online for 60 minutes.". Il numero nel testo e il denominatore sono
   l'obiettivo del **livello in corso** (10 colpi = livello 2, a 5/10 col livello 1 già fatto);
   il conteggio è dal reset delle 10:00 e prosegue tra i livelli. A sfida finita il progresso dice
   "Completed" e `claimButton` è nascosto. Livelli fatti: `progressMilestones/progress (i)/tickIcon`
   attivo. In fondo, "Challenges will be renewed in: 19:00:53" (le 10:00).
2. **Mini-eventi** (Sigils): `challengesLayout/miniEventChallengeInteraction (N)`, testo in
   `unlocked/challenge/questDescription`, progresso in `unlocked/challenge/challengeProgressBg/progressText`,
   claim in `unlocked/reward/claimButton`, `unlocked/reward/claimedText` attivo se reclamata. Giorno
   bloccato: `unlocked` spento e `locked` acceso (il testo c'è già: il giorno 3 di Sigils su Steam-0
   è "Get 50 special upgrades ."). Visti solo i testi di Steam-0 (le altre istanze non si toccano).
3. **Elenco eventi**: Stardust (dal 04/10) e Primordial elements (dal 09/10) sono già in
   `upcommingEvents`, con `mainElements/lock` acceso e "Starts in: ...". Che aprano MiniEvents si
   vede il 04/10.
4. **Illuminazione**: `Paths.MenusLoc.MagicQuartersLoc.EnlightenmentBtn` (già usato da Guardian
   Training con `use_strange_dust`, spento nel fleet), "Enlightenment 1", `costText` '20',
   `strangeDustIcon` = `strangeDust64`, XP +120.
5. Donazione, upgrade speciali e upgrade dell'Exotic Merchant: in attesa del sì dell'utente.
6. **Conteggio condiviso: sì.** Dopo le quest del mattino DH segnava 5/10 colpi e 10/12 giocate,
   cioè i 5 colpi di Miner e le 10 giocate di Gamer di Steam-0.

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
