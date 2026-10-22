# Piano: la formazione migliore per ogni account

> Scritto il 2026-10-22, solo analisi: niente codice ancora. Oggi il bot legge la formazione ma non la
> cambia mai. Il piano gli fa scegliere, ogni giorno e per ogni account, i 5 eroi, il loro ordine e il
> leader che spingono più in alto lo stage, partendo dai numeri veri di quell'account.
> Niente nomi fissi: lo stesso codice deve dare Leo come tank su Steam-0 la mattina del 22/10 e Boris
> la sera, quando il gear di Boris è salito; e Solaine leader prima, Cirilo dopo.

## 0. Come partire

Da incollare in una sessione nuova di Claude Code (cartella `C:\Repos\FirestoneBot`):

```text
Leggi FORMATION_PLAN.md e implementalo con la skill ponytail attiva, seguendo le regole di
TESTING.md (test solo su Steam-0, path e dati verificati dal vivo, commit e push per ogni passo
verificato). Parti dalla fase 0 (sonda in sola lettura), poi il planner con i test (fase 1) e il
dry run sulla flotta (fase 2). Fermati e chiedimi prima di cambiare davvero una formazione (fase 3):
voglio guardare il gioco mentre lo fa. Aggiorna TESTING.md, MULTI_INSTANCE_SETUP.md e questo piano.
```

## 1. Obiettivo e principio

**Obiettivo:** il muro più alto possibile in ogni avventura (tra un Empower e l'altro). Ogni stage
dà il 25% di oro in più (wiki `Battlefield`), quindi spingere più in alto è anche farmare di più: un
solo obiettivo, non due.

**Principio:** il bot non sceglie per nome o per ruolo "consigliato". Legge dal gioco le statistiche
attuali di ogni eroe di quell'account, calcola un punteggio per ogni formazione possibile e prende la
migliore. Poi controlla sul campo, al muro dell'avventura successiva, se la scelta ha pagato davvero,
e se no torna indietro. Il modello propone, il muro conferma.

## 2. Come funziona il gioco (i fatti su cui poggia il modello)

### 2.1 La squadra e le posizioni

Fonti: wiki `docs/wiki/pages/Party.html`, `Battlefield.html`; guida Steam (`docs/guida_steam_completa.md`,
"Party / Builds").

- **5 eroi**, ognuno in uno slot numerato da 1 a 5.
- **Due zone.** I melee stanno davanti, i ranged e gli spellcaster dietro; i melee vengono messi
  sempre prima dei ranged. Si possono scambiare gli eroi solo dentro la propria zona. Quindi con 2 melee
  gli slot 1-2 sono loro e i ranged sono 3-5; con 1 melee lo slot 1 è suo.
- **Bersagli.** I nemici melee colpiscono solo le prime posizioni. I nemici ranged e spellcaster
  scelgono il bersaglio con queste probabilità: slot 1 50%, 2 20%, 3 15%, 4 10%, 5 5%.
  Le abilità "taunt" dei tank ("Force all enemies to attack you", vedi 4.3) per qualche secondo portano
  tutti i colpi sul tank.
- **Nella pratica:** il tank va nello slot 1; l'eroe più prezioso e più fragile (di solito il leader,
  che fa più danno) va nell'ultimo slot.

### 2.2 Leader e team bonus

- Il leader prende **+50% a tutti gli attributi** ed è potenziato dalle ricerche "Leadership". Gli
  altri quattro prendono invece il **Team Bonus**. Leadership e Team Bonus crescono con Firestone
  research, Exotic upgrade, Talenti, carte della Taverna, artefatti e albero di gilda. "Se il team bonus
  è abbastanza grande, il leader avrà statistiche più basse degli altri" (guida Steam, "Roles").
- Il gioco tiene i due moltiplicatori in memoria: `HeroDeck.leadershipMultiplier` e
  `HeroDeck.teamBonusMultiplier` (sezione 3). Il confronto tra i due decide che tipo di leader serve
  (sezione 5.4), ed è diverso per ogni account.
- Il leader lancia le sue abilità solo a mano, finché non si sblocca il talento "Leader - Auto
  Abilities" (livello talenti 11, guida Steam). Il bot non lancia abilità a mano. Un giocatore sul forum
  Steam: con il lancio manuale Ice Barrage di Solaine resta attiva circa il 75% del tempo, in
  automatico circa il 25%.
  Conseguenza: senza quel talento le abilità del leader **non contano** nel modello; con il talento
  contano con l'uptime automatico.
- I giocatori sul forum fanno due scelte opposte, entrambe sensate a seconda dei moltiplicatori: il
  leader è il damage dealer principale (build da leader), oppure è l'eroe più debole, per distribuire
  il danno (build da team bonus). Sono i due casi della sezione 5.4.

### 2.3 Cosa decide il muro

Fonti: wiki `Battlefield.html`, `Enemies.html`.

- Ogni stage ha 5 ondate. Alla quinta compare il **boss**, che va battuto entro **60 secondi**,
  altrimenti si torna all'ondata 4 e l'avanzata automatica si ferma. È il muro, quello che
  `AutoRetreat` rileva.
- Dopo lo stage 200 i nemici triplicano danno e vita a ogni stage. Ogni 50 e ogni 100 stage ci sono
  moltiplicatori in più, quindi il muro arriva in pochi stage, ed essere un po' più forti sposta il muro
  di poco.
- Quindi il muro dipende da due cose:
  1. il **danno sul singolo bersaglio contro il boss** in 60 secondi, buff e debuff compresi;
  2. la **sopravvivenza** per quei 60 secondi: se il tank muore, il danno crolla.
- La velocità con cui si puliscono le ondate (danno ad area) conta per l'oro al minuto, non per il
  muro. Nel modello serve solo per scegliere tra due formazioni quasi uguali.

### 2.4 La "power" non è la forza in battaglia

La power di un eroe è la somma della power di gear, jewel e soulstone (wiki `Heroes.html`). I jewel
potenziano solo le **War Machine** (wiki `Heroes`, "Equipment"), quindi un eroe con jewel alti sembra
più forte di quanto sia in battaglia.

La mia analisi del 22/10 su Leo e Boris usava la power: andava bene come indizio, perché lì il salto
di Boris era nel gear (da 2-5 a 6-7), ma non come regola. Il bot deve usare gli attributi veri
dell'eroe (danno, vita, armatura, velocità, critico, schivata), che il gioco calcola già.

### 2.5 Rarità e moltiplicatori

- La rarità dell'eroe moltiplica i suoi attributi principali: ×5 uncommon, ×8 rare, ×12 epic, fino a
  ×24 angel. Moltiplica anche quelli di tutti gli eroi (×3 … ×16).
- Ci sono moltiplicatori per specializzazione (damage, tank, healer), stile (melee, ranged,
  spellcaster) e risorsa (rage, energy, mana), in `HeroDeck`. Per questo la stessa formazione rende
  diversamente su account diversi.
- Tutti questi valori sono già inclusi negli attributi calcolati dal gioco: il modello non li
  ricalcola, li legge.

## 3. Cosa si legge dal gioco

Classi trovate negli assembly Il2Cpp della 3.61 (`Il2CppCoreASM.dll`). I nomi vengono dai metadati:
**sono tutti da verificare in fase 0** (che compilino, e che i valori corrispondano a quelli a schermo).

| Cosa | Dove | Note |
| --- | --- | --- |
| Tutti gli eroi | `HandlerLoader` → `HeroDeck.heroes` (lista di `Il2Cpp.Hero`) | Insieme a `IsUnlocked()`. Mercenari e dark god: da capire se stanno nella stessa lista |
| Attributi di un eroe | `Hero.damageBasic`, `maximumHealthBasic`, `armorBasic`, `attackSpeedBasic`, `criticalChanceBasic`, `criticalDamageBasic`, `dodgeBasic` | Ci sono anche le versioni `...Initial` (valori di base). In fase 0: `Basic` comprende gear, ricerche e rarità? Si confronta con il menu Party ("Order by") |
| Ruolo | `Hero.heroSpecialization`, `attackStyle`, `heroResourceType`, `heroClass`, `characterName`, `HeroCode` | |
| Livello d'oro nell'avventura | `Hero.mainBattleGoldLevel` | Torna a 1 a ogni Empower |
| Leader e team bonus | `HeroDeck.leadershipMultiplier`, `HeroDeck.teamBonusMultiplier` | Il cuore della scelta del leader |
| Rigenerazione delle risorse | `HeroDeck.CalculateResourceRegenerationForAllHeroes` e il configuratore di ogni eroe (`HeroResourceConfigurator`) | Serve per gli uptime delle abilità (5.3) |
| Formazione in battaglia | `BattleHeroManager`, una `BattleHero` per eroe schierato: `hero`, `DPS` (`CalculateDPS`), `IsLeader()` | `DPS` è il valore del gioco: serve per tarare la formula 5.1 |
| Menu Party | `PartyMech`: `meleePositions`, `rangedPositions`, `heroSlotCount`, `AddHeroToParty`, `RemoveHeroFromParty`, `SetLeader`, `AssignLeader`, `ApplyChanges`, `OnSaveChanges`, `IsHeroesAutoAbilitiesTalentUnlocked` | Per applicare il cambio (sezione 8) |
| Stage | `StageProgress.Current` (già nel bot); muro: `AutoRetreat` (già nel bot); "Max Stage Reached" nello snapshot `Statistics` (già nel log) | Per la verifica sul campo (sezione 7) |

Oggi il bot legge già la formazione per nome (`Party.FormationNames`, `HallOfHeroesTask`) e la power
dalla Hall of Heroes, ma non usa nessuno di questi dati di battaglia.

## 4. Le abilità che contano per la formazione

Dal wiki (pagine dei singoli eroi in `docs/wiki/pages`). Qui ci sono solo gli effetti che cambiano il valore
della **squadra** o il danno **sul boss**. I danni propri di ogni eroe sono già nel suo DPS.

### 4.1 Buff a tutta la squadra

| Eroe | Ruolo / risorsa | Abilità | Effetto |
| --- | --- | --- | --- |
| Boris | tank / rage | Anger | +50% danno alla squadra, 25 s |
| Valerius | tank / mana | Blessing of the Defender | +25% danno e +45% armatura alla squadra, 20 s |
| Hongyu | damage melee / rage | Final Hunt | +15% danno agli eroi, 25 s (ricarica 20 s) |
| Muriel | damage melee / energy | Rally | +15% velocità d'attacco alla squadra, 6 s per combo point |
| Danysa | tank / rage | Protect and destroy | +100% danno critico e +10% schivata alla squadra, 30 s |
| Asmondai | tank / rage | Morale | +20% danno e +50% armatura ai **melee**, 25 s |
| Luana | healer / mana | Nature's Favor | +40% armatura a tutti e cura, 10 s (ricarica 40 s) |
| Garret (mercenario) | tank / rage | Last Stand | +50% armatura e +20% velocità d'attacco, 25 s |
| Anzo (mercenario) | tank / rage | Healthy Living | +25% armatura e +50% vita, 30 s |
| Zoruk (mercenario) | healer / rage | Waaaagh!, Cause red goes faster | +25% armatura e cura; +15% velocità d'attacco, 12 s |
| Kramatak (dark god) | damage / energy | Gladiator's vigor | +15% velocità e +30% danno su eroi a caso |
| Yamanoth (dark god) | tank / rage | Seal of the underworld | +30% vita e armatura alla squadra, 20 s |

### 4.2 Debuff sul bersaglio: moltiplicano il danno di tutti sul boss

| Eroe | Abilità | Effetto | Durata / ricarica |
| --- | --- | --- | --- |
| Solaine | Freeze | il bersaglio subisce +80% danni | 8 s / 20 s |
| Talia | Annihilation | +60% danni subiti | 8 s / 15 s |
| Astrid | Devastating Shot | +60% danni subiti | 7 s |
| Blaze | Fire Blast | +40% danni subiti | 5 s |
| Arvie (mercenario) | Stalk the prey | +8% danni subiti per combo point | 5 s / 6 s |

### 4.3 Tank: taunt e riduzioni del danno

| Eroe | Taunt | Riduzione / cura |
| --- | --- | --- |
| Boris | Shield Wall 30 s / 50 s | -60% danni |
| Leo | Oath of Honor 15 s / 25 s | -40% danni, cura 25% + 40% (Divine intervention) |
| Valerius | Holy Shield 15 s / 25 s | cura 2,5% al secondo |
| Asmondai | Defense 30 s / 50 s | +50% armatura, +40% vita |
| Danysa | Dodge 30 s / 50 s | +40% schivata |
| Randal | Dwarf's Might 30 s / 50 s | +100% vita |
| Panko (mercenario) | One for All 15 s / 25 s | +35% schivata, e Blood Money +50% oro per 15 s |

### 4.4 Healer

Benedictus, Luana, Belien, Layla, Yavo (mercenario), Ledra (dark god), Zoruk (mercenario). Layla
(Ward of protection: 5 s di immunità a tutti ogni 20 s) e Belien (stordisce tutti per 6 s) sono i più
forti in sopravvivenza. Un healer conta nel modello solo quando la sopravvivenza è il limite
(sezione 5.5).

### 4.5 AoE per farmare (solo per scegliere tra due formazioni quasi uguali)

Solaine (Ice Barrage), Blaze (Dragon Breath), Cirilo, Fini, Molly, Burt, Iseris, Randal, Astrid.

La tabella va in codice come dati: `FormationPlanner.Abilities`, con per ogni abilità tipo, valore,
durata, ricarica e costo. Va controllata in fase 0 contro le pagine del wiki: il gioco della 3.61
potrebbe avere numeri diversi da quelli del wiki.

## 5. Il modello

### 5.1 Danno sul singolo bersaglio di un eroe

```text
O(h) = damage × (1 / attackSpeed) × (1 + critChance × critDamage) × A(h)
```

- `attackSpeed` nel gioco è in secondi tra due colpi (Boris 2,3"), quindi gli attacchi al secondo sono
  `1 / attackSpeed`.
- `A(h)` è il moltiplicatore delle abilità proprie dell'eroe (per esempio Talia: 240%, 330%, 450% con
  l'uptime della 5.3). Si parte da 1 per tutti. In fase 0 si confronta `O(h)` con il `BattleHero.DPS`
  del gioco per gli eroi schierati: se il rapporto è costante tra gli eroi, il DPS del gioco basta e
  `A(h)` serve solo per chi non è schierato.

### 5.2 Resistenza di un eroe

```text
EHP(h) = health × M(armor) / (1 - dodge)
```

`M(armor)` è la riduzione del danno data dall'armatura. La formula esatta non è nel wiki: in fase 0 va
cercata nel gioco (`BattleHero.ReceiveDamage`, `CharacterBattleLogic`) o misurata. Fino ad allora si usa
`M = 1 + armor / health`, una stima che ordina i tank come il menu Party "Order by armor/health".

### 5.3 Uptime di un'abilità

Con il lancio automatico gli eroi usano le abilità in ordine (1, 2, 3) appena hanno la risorsa (guida
F2P). Quindi:

```text
ciclo = max(ricarica più lunga, (costo1 + costo2 + costo3) / rigenerazione)
uptime(abilità) = min(1, durata / ciclo)
```

- La rigenerazione si legge dal gioco: per il mana circa 3-4 al secondo, per l'energy 9 al secondo
  (guida Steam), per la rage dipende dai colpi dati e ricevuti (Boris: 5 per colpo dato, 10 per colpo
  ricevuto, -1,5 al secondo).
- Esempio Solaine: con 3 mana al secondo, ciclo = (35 + 50 + 70) / 3 ≈ 52 s, quindi Freeze (8 s)
  ha un uptime di circa 15%: +80% × 15% ≈ +12% sul boss, non +32% come direbbe la sola ricarica. È per
  questo che la rigenerazione va letta e non stimata.
- Per la rage (tank colpiti in continuazione) all'inizio si usa una stima per ruolo: 4 al secondo per
  i tank sotto taunt, 2 per i damage. Si tara in fase 2 dal log di battaglia, se il gioco espone la
  rage corrente (`HeroResource`).
- Un buff vale `(1 + valore × uptime)`. Buff diversi si moltiplicano; lo stesso buff da due eroi si
  somma: da verificare in fase 0 se il gioco li accumula.

### 5.4 Il leader

```text
L = 1,5 × leadershipMultiplier      (quanto prende il leader)
T = teamBonusMultiplier             (quanto prende ogni non-leader)
```

Il valore della squadra è `Σ O(h) × (L se h è leader, altrimenti T)`. Quindi conviene come leader
l'eroe che massimizza `O(h) × (L - T)`:

- **L > T (build da leader):** il leader è l'eroe col danno più alto. Su Steam-0 la sera del 22/10:
  Cirilo invece di Solaine.
- **L < T (build da team bonus):** il leader è l'eroe con il **danno più basso**, perché fare leader un
  damage dealer gli toglie il team bonus. Di solito è il tank: il +50% gli dà anche più vita.
- **L ≈ T (entro il 5%):** il leader è il tank, per la vita in più.
- **Abilità del leader:** senza il talento "Leader - Auto Abilities", le abilità del leader valgono 0
  nel modello (5.3). Allora un leader con buff importanti (Boris Anger, Freeze di Solaine) perde quel
  valore, e il modello lo vede da solo.
- **Mercenari e dark god come leader:** da verificare in fase 0 se il gioco lo permette. Se no, sono
  esclusi dalla scelta del leader.

### 5.5 Il punteggio di una formazione

Per una formazione F (5 eroi, un leader):

```text
Boss(F)  = Σ O(h) × S(h) × Π buff di danno e velocità (4.1) × Π debuff sul bersaglio (4.2)
Front(F) = EHP dell'eroe in slot 1 × S(slot 1) × Π buff di armatura e vita × taunt (4.3)
Score(F) = Boss(F), con il vincolo Front(F) ≥ 0,8 × Front(formazione attuale)
```

dove `S(h)` è L o T (5.4).

- **Vincolo di sopravvivenza:** la nuova formazione non può avere un fronte più debole del 20% rispetto
  a quella attuale. Così il modello non toglie mai il tank per un damage dealer, e un fronte già solido
  non viene smontato. Se l'account non ha nessun tank melee, slot 1 = il melee con l'EHP più alto.
- **Secondo tank:** entra solo se il suo `O(h) × S(h)` e i suoi buff battono il quinto damage dealer.
  È il caso di Leo contro Talia sulla sera di Steam-0.
- **Healer:** entra solo se la sopravvivenza è il limite. Lo dice la verifica sul campo (sezione 7):
  al muro gli eroi muoiono prima dei 60 s del boss. Da quel momento il vincolo diventa "almeno un
  healer" finché il dato cambia.
- **Spareggio:** tra formazioni con `Boss(F)` entro il 3% si sceglie quella col danno ad area più alto
  (4.5): stesso muro, più oro al minuto.

### 5.6 La ricerca

Si provano **tutte** le formazioni: con 15-30 eroi sono al massimo C(30,5) × 5 leader ≈ 700.000
punteggi, ognuno una manciata di moltiplicazioni, meno di un secondo. Nessuna euristica, nessun "prima
il tank poi il resto": il vincolo della 5.5 e il punteggio bastano, e il risultato è il migliore per il
modello.

```text
ponytail: ricerca esaustiva; se il roster supera ~40 eroi, fissare il tank e cercare i 4 restanti.
```

### 5.7 L'ordine dentro le zone

- **Zona melee (davanti):** l'EHP più alto nello slot 1, poi in ordine decrescente.
- **Zona ranged (dietro):** l'eroe più prezioso nell'ultimo slot (5%), cioè quello con `O(h) × S(h)`
  più alto, di solito il leader; il meno prezioso nel primo slot della zona.

### 5.8 Prova su Steam-0 (con la power al posto degli attributi, solo per illustrare)

| | Mattina 22/10 | Sera 22/10 |
| --- | --- | --- |
| Tank | Leo (1,43M) contro Boris (0,38M): Leo | Boris (2,35M) contro Leo (1,43M): Boris |
| Leader | Solaine (2,89M) contro Cirilo (2,81M): Solaine | Cirilo (3,33M) contro Solaine (2,89M): Cirilo, se il gioco lo permette |
| Quinto | Talia contro Benedictus, molto vicini | Leo (secondo tank) contro Talia (0,83M) |

I test del planner (sezione 9) riproducono questi due casi con attributi sintetici proporzionali, e
devono dare queste risposte.

## 6. Quando cambiare

- **Valutazione:** una volta al giorno, subito dopo la lettura degli eroi di `HallOfHeroesTask`
  (gear, enchant e rarità sono appena cambiati), e dopo ogni aumento di rarità di un eroe. È una
  lettura dalla memoria del gioco, senza schermate: costa niente.
- **Isteresi:** si cambia solo se `Score(nuova) ≥ (1 + min_gain) × Score(attuale)`, con `min_gain`
  0,10 di default. Così la formazione non oscilla tra due quasi uguali ogni giorno.
- **Momento del cambio**, in ordine di preferenza:
  1. **subito dopo un Empower**: tutti gli eroi tornano a livello 1, quindi il cambio non butta i
     livelli d'oro comprati;
  2. **al muro** (AutoRetreat ha appena fatto retreat): la squadra attuale tanto non avanza più, ed è
     il momento in cui una formazione migliore serve. Un eroe nuovo parte da livello 1, ma
     `HeroUpgrade` lo porta su; quanto ci mette va misurato in fase 3;
  3. mai durante un tentativo al boss, e mai mentre un altro task ha un menu aperto.
- **Al massimo un cambio al giorno**, più gli eventuali ritorni indietro della sezione 7.

## 7. La verifica sul campo

Il modello ha stime (uptime, armatura, rage). Il muro no.

- **Cosa si registra.** Per ogni avventura, nel cfg (`formation_history`, ultime 10): formazione,
  leader, stage del muro (quello in cui `AutoRetreat` dichiara il muro), tempo per arrivarci.
  Più, al muro, quanti eroi sono morti durante i tentativi al boss: lo stato di morte si legge
  da `HeroBattleLogic`/`BattleHero` (da verificare).
- **Campione e sfidante.** La formazione attuale è il campione. Quella nuova (lo sfidante) corre
  l'avventura successiva. Se arriva a un muro **più basso di 3 o più stage** rispetto all'ultimo muro
  del campione, si torna al campione e lo sfidante resta bloccato per 7 giorni. Se no, diventa il
  campione.
  Tra un'avventura e l'altra il muro di solito sale da solo, perché il moltiplicatore delle Firestone
  cresce: quindi "non più basso" è già un segnale debole a favore dello sfidante. Un muro più basso è
  un segnale forte contro.
- **Sopravvivenza.** Se al muro muoiono eroi in 2 avventure su 3, il vincolo healer della 5.5 si
  accende per quell'account.
- **Taratura.** Con i dati di più account il dry run (fase 2) permette di confrontare `Boss(F)`
  previsto e muro reale, e di sistemare le stime di uptime della rage e la formula dell'armatura.

## 8. Applicare la formazione nel gioco

È la parte meno nota: oggi il bot apre il menu Party solo per leggere. Le possibilità, dalla più
sicura:

1. **Click sull'interfaccia.** Si toccano gli eroi nel mazzo (`deckSlotHeroButtons`) per aggiungerli
   o toglierli, si seleziona lo slot e si preme `setLeaderButton`, poi si salva (o si chiude, se il
   salvataggio è automatico: `unsavedChanges`, `OnSaveChanges`). Da scoprire in fase 0:
   - il click aggiunge l'eroe nel primo slot libero della sua zona?
   - come si toglie un eroe: c'è `removeFromPartyBg`, che fa pensare a un trascinamento;
   - l'ordine dentro la zona si cambia solo trascinando (`BattleFormationMover`)?
2. **Trascinamento simulato** con gli eventi di Unity (`ExecuteEvents` begin drag, drag, end drag)
   sui `battleFormationMovers`. Il bot ha già `ClickSimulated` (`GameButton`) come base.
3. **Ultima scelta:** chiamare i metodi di `PartyMech` (`AddHeroToParty`, `SetLeader`,
   `ApplyChanges`). È più rapido, ma salta i controlli dell'interfaccia e rischia di non salvare sul
   server. Si usa solo se 1 e 2 non bastano, e solo dopo averlo provato su Steam-0.

Dopo ogni cambio si rilegge la formazione (`Party.FormationNames`, più il leader da
`BattleHero.IsLeader()`). Se non è quella voluta: `[FAILED]`, nessun secondo tentativo fino al giorno
dopo, e la formazione resta quella che il gioco mostra.

## 9. Implementazione

1. **`src/Tasks/Character/FormationPlanner.cs`**, puro, senza tipi del gioco, come `BlueprintPlanner`:
   - `record HeroStats(string Name, int Spec, int Style, int Resource, double Damage, double Health,
     double Armor, double AttackSpeed, double Crit, double CritDamage, double Dodge, bool CanLead)`;
   - `record Ability(...)` e la tabella `Abilities` della sezione 4;
   - `Evaluate(heroes, current, leaderMult, teamMult, regen, leaderAuto, needHealer, minGain)`, che
     restituisce la migliore formazione (5 nomi in ordine di slot, leader), i due punteggi e una reason
     per il log;
   - test in `tests/Firebot.TalentEngine.Tests/FormationPlannerTests.cs`, da aggiungere al csproj:
     i due casi di Steam-0 (5.8), L < T (leader = il più debole), nessun tank, vincolo di
     sopravvivenza, isteresi sotto il 10%, mercenario che non può fare il leader.
2. **`src/GameModel/Features/Character/Heroes.cs`**: legge `HeroDeck` (eroi, attributi, i due
   moltiplicatori, rigenerazione) e `BattleHeroManager` (formazione, leader, `DPS`). Ogni lettura sta in
   un try/catch che restituisce "non letto"; se non si legge, non si cambia niente.
3. **`src/Tasks/Character/FormationTask.cs`**, acceso di default con `dry_run = true`. Config:
   - `dry_run` (true);
   - `min_gain` (0.10);
   - `formation_history`, `pending_formation`, `blocked_until` (gestiti dal bot).
   Dopo `HallOfHeroesTask` (o al primo giro del giorno) calcola e scrive sempre:
   `[INFO] Formation: now Cirilo, Talia, Solaine, Boris, Muriel (leader Solaine, score 1.00), best
   Boris, Leo, Muriel, Solaine, Cirilo (leader Cirilo, score 1.27, +27%): change on next Empower.`
   Non in dry run: applica nel momento della sezione 6, con il metodo della sezione 8, e registra
   l'avventura per la sezione 7.
4. **Aggancio a Empower e al muro**: `EmpowerTask` e `AutoRetreat` alzano un flag (per esempio
   `FormationTask.ApplyPending()`), che il task legge al giro successivo.
5. Paths nuovi in `Party.cs` (`PartyLoc`): mazzo, pulsante leader, pulsante salva, verificati dal
   vivo.
6. README (elenco task), template dei cfg, TESTING.md, questo piano (stato).

## 10. Fasi

0. **Sonda in sola lettura, su Steam-0.** Un log una tantum con:
   - per ogni eroe sbloccato: nome, ruolo, stile, risorsa, gli attributi `Basic` e `Initial`, livello
     d'oro;
   - i due moltiplicatori, la rigenerazione, il talento auto-abilità;
   - per gli schierati: `BattleHero.DPS` e `IsLeader()`; `meleePositions` e `rangedPositions`.

   Si confronta con il menu Party a schermo, fotografato dall'utente o letto dal bot. In più si prova
   a mano, con l'utente, come si aggiunge, toglie, ordina un eroe e come si cambia leader.
1. **Planner e test**, senza gioco.
2. **Dry run sulla flotta per una settimana.** Ogni istanza scrive la riga della 9.3 ogni giorno. Si
   controllano le scelte account per account, e si confrontano punteggio e muro per tarare le stime.
3. **Primo cambio vero su Steam-0**, con l'utente che guarda. Si misura quanto ci mette un eroe nuovo
   a tornare utile (livello d'oro), e se il cambio al muro conviene o è meglio aspettare l'Empower.
4. **Flotta**: `dry_run = false` nel template, poi distribuzione. La verifica della sezione 7 resta
   sempre accesa.

## 11. Paletti

- Mai cambiare durante un tentativo al boss, mai con un altro menu aperto, al massimo un cambio al
  giorno.
- Mai togliere l'ultimo tank melee (vincolo 5.5).
- Se una lettura dal gioco fallisce, nessun cambio quel giorno.
- Se la formazione riletta non è quella voluta, nessun nuovo tentativo fino al giorno dopo, e
  `[FAILED]` nel log.
- Il ritorno indietro della sezione 7 vale sempre, anche quando lo sfidante aveva un punteggio molto
  migliore: il muro ha l'ultima parola.

## 12. Domande aperte

- `min_gain` al 10% va bene, o preferisci cambi più rari, per esempio al 20%?
- Cambio solo dopo l'Empower, oppure anche al muro? La fase 3 dirà quanto costa il cambio al muro.
- Il Guardian resta fuori da questo piano (ha il suo slot e i suoi task)?
- Un passo successivo, non in questo piano: lanciare a mano le abilità del leader (Ice Barrage al 75%
  del tempo invece che al 25%). Cambierebbe la scelta del leader verso chi ha l'abilità migliore.

## Fonti

- Wiki (mirror in `docs/wiki/pages`): `Party`, `Battlefield`, `Enemies`, `Heroes`, le pagine dei
  singoli eroi e mercenari.
- Guida Steam, `docs/guida_steam_completa.md`: "Party / Builds", "Heroes", talenti.
- Guida F2P, `docs/firestone_guida_F2P.md`: "Il leader", "Posizione in squadra", "Leader o team
  bonus?".
- Forum Steam: [lancio manuale e automatico delle abilità del leader](https://steamcommunity.com/app/1013320/discussions/0/3380536561722803665),
  [leader cambiato sull'eroe col danno più basso](https://steamcommunity.com/app/1013320/discussions/0/1639801448914297514),
  [nessun tier fisso, conta il gear dell'account](https://steamcommunity.com/app/1013320/discussions/0/1639801448929451852/).
- Assembly Il2Cpp della 3.61 (`Il2CppCoreASM.dll`): `Hero`, `HeroDeck`, `BattleHero`,
  `BattleHeroManager`, `PartyMech`, `PartyViewController`.
