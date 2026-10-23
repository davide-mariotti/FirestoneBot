# Piano: la squadra del momento e la squadra obiettivo

> Scritto il 2026-10-22, rivisto il 2026-10-23 su indicazione dell'utente: niente controlli sul muro,
> un solo controllo al giorno, e una squadra obiettivo presa dalle guide. Solo analisi: niente codice
> ancora. Oggi il bot legge la formazione ma non la cambia mai.

## 0. Come partire

Da incollare in una sessione nuova di Claude Code (cartella `C:\Repos\FirestoneBot`):

```text
Leggi FORMATION_PLAN.md e implementalo con la skill ponytail attiva, seguendo le regole di
TESTING.md (test solo su Steam-0, path e dati verificati dal vivo, commit e push per ogni passo
verificato). Parti dalla fase 0 (sonda in sola lettura), poi planner e test (fase 1) e il dry run
sulla flotta (fase 2). Fermati e chiedimi prima di cambiare davvero una formazione (fase 3): voglio
guardare il gioco mentre lo fa. Aggiorna TESTING.md, MULTI_INSTANCE_SETUP.md e questo piano.
```

## 1. L'idea in breve

Ogni account ha due squadre:

- **La squadra del momento.** È la migliore che l'account può schierare oggi, calcolata dai numeri veri
  dei suoi eroi. È quella in battaglia.
- **La squadra obiettivo.** È la squadra consigliata dalle guide: eroi a mana, con un tank, tre
  spellcaster e un healer. Ogni account la risolve con gli eroi che ha già sbloccato (sezione 3). Il
  bot non la schiera finché non è la migliore. Però ci **investe sopra**: enchant di tier 1 e rarità
  vanno prima ai suoi eroi.

Con i giorni gli eroi dell'obiettivo crescono. Quando battono quelli del momento, il calcolo li sceglie
da solo, e la squadra del momento diventa la squadra obiettivo. Non serve nessuna regola di "passaggio".

Un solo controllo al giorno, subito dopo la lettura degli eroi della Hall of Heroes. Niente controlli
sul muro, sullo stage o sulle morti.

## 2. Come funziona il gioco (i fatti su cui poggia il calcolo)

### 2.1 Squadra e posizioni

Fonti: wiki `docs/wiki/pages/Party.html`, `Battlefield.html`; guida Steam
(`docs/guida_steam_completa.md`, "Party / Builds").

- **5 eroi**, negli slot da 1 a 5.
- **Due zone.** I melee stanno davanti, i ranged e gli spellcaster dietro; i melee vengono sempre prima
  dei ranged. Si possono scambiare gli eroi solo dentro la propria zona. Con 1 melee, lo slot 1 è il suo.
- **Bersagli.** I nemici melee colpiscono solo le prime posizioni. I nemici ranged e spellcaster
  scelgono il bersaglio così: slot 1 50%, 2 20%, 3 15%, 4 10%, 5 5%. I taunt dei tank ("Force all
  enemies to attack you") per qualche secondo portano tutti i colpi sul tank.
- **Nella pratica:** il tank nello slot 1, l'eroe più prezioso (di solito il leader) nell'ultimo slot.

### 2.2 Leader e team bonus

- Il leader prende **+50% a tutti gli attributi** ed è potenziato dalle ricerche Leadership. Gli altri
  quattro prendono il **Team Bonus**. "Se il team bonus è abbastanza grande, il leader avrà
  statistiche più basse degli altri" (guida Steam, "Roles").
- Il gioco tiene in memoria i due moltiplicatori: `HeroDeck.leadershipMultiplier` e
  `HeroDeck.teamBonusMultiplier`. Il confronto decide che tipo di leader serve (sezione 4.4), ed è
  diverso per ogni account.
- Il leader lancia le abilità solo a mano, finché non si sblocca il talento "Leader - Auto Abilities".
  Il bot non lancia abilità a mano, quindi senza quel talento le abilità del leader non contano nel
  calcolo.

### 2.3 Cosa serve per spingere

- Ogni stage ha 5 ondate; alla quinta il **boss** va battuto entro **60 secondi**. Dopo lo stage 200 i
  nemici triplicano a ogni stage (wiki `Battlefield`, `Enemies`).
- Ogni stage dà il 25% di oro in più: spingere più in alto è anche farmare di più.
- Contano quindi il **danno sul singolo bersaglio** (il boss) con buff e debuff, e la **sopravvivenza** del
  tank per quei 60 secondi.
- La guida Steam ("Builds") ha misurato che una squadra che sopravvive uccide 65-70 nemici al
  minuto, una che muore ("glass cannon") 30-35. Per account che girano h24 da soli conviene la
  squadra che sopravvive: è il motivo dell'healer nella squadra obiettivo.

### 2.4 La "power" non è la forza in battaglia

La power di un eroe somma gear, jewel e soulstone (wiki `Heroes`). I jewel potenziano solo le **War
Machine**. Il calcolo usa gli attributi veri dell'eroe (danno, vita, armatura, velocità, critico,
schivata), che il gioco calcola già con gear, rarità, ricerche e moltiplicatori di ruolo, stile e risorsa.

## 3. La squadra obiettivo

### 3.1 Cosa dicono le guide

- **Il meta sono gli eroi a mana.** Albero di gilda, albero personale, artefatti e Scarabs hanno bonus
  specifici per il mana. Gli spellcaster puliscono le ondate più in fretta (guida F2P, "Risorse
  degli eroi"; Ashstronox, V5).
- **"Preparati per una squadra di spellcaster a mana"** e porta "Magic Spells" negli Exotic upgrade
  (guida Steam, "Exotic upgrades"). L'autore della guida gioca con 4 spellcaster a mana.
- **Blaze come fonte principale di danno**, con Leadership alzata (guida Steam, Talents, riga "Team
  Bonus").
- **Ashstronox** a 9 mesi usava tank a mana come damage dealer, per i suoi moltiplicatori.
- **Un giocatore allo stage 700+** (forum Steam): Talia, Burt, Muriel, Luana (healer), Solaine leader.
- **I talenti del bot vanno già in questa direzione**: "Mana Heroes" ha priorità 40, "Magic Spells" 35
  (`TalentBuildConfig.cs`).

### 3.2 La squadra tipo: 1 tank, 3 spellcaster e 1 healer, tutti a mana

Ogni ruolo ha una scala: l'eroe più a destra che l'account ha sbloccato è quello dell'obiettivo.

| Ruolo | Scala (stage di sblocco dal wiki) | Perché |
| --- | --- | --- |
| Tank (slot 1) | Boris (2) → **Leo** (150) → **Valerius** (700) | Leo e Valerius sono tank a mana. Valerius dà alla squadra +25% danno e +45% armatura per 20 s. Boris (rage) solo finché manca Leo |
| Spellcaster 1 | **Solaine** (7) | Freeze (+80% danni sul boss), Ice Barrage per le ondate |
| Spellcaster 2 | **Blaze** (400) | Fire Blast (+40% danni subiti), Dragon Breath, critico |
| Spellcaster 3 | Vilon (mercenario a mana, se c'è) → **Iseris** (1.800) | Vilon: Sigil of Flame (+65% danno a un eroe). Iseris: Agony ad area |
| Healer | **Benedictus** (50) → **Luana** (550) → **Belien** (2.050) → **Layla** (3.050) | Tutti a mana. Luana +40% armatura alla squadra, Belien stordisce tutti 6 s, Layla immunità a tutti 5 s ogni 20 s |

- **Leader dell'obiettivo:** Blaze o Solaine, chi dei due ha più danno. Con L < T (sezione 4.4) vale la regola del team bonus anche qui.
- **Posto vuoto:** finché un ruolo non ha nessun eroe sbloccato della sua scala (per esempio lo
  spellcaster 3 prima di Vilon o Iseris), quel posto non è nell'obiettivo. La squadra del momento lo
  riempie con chi rende di più.
- **Mercenari e dark god:** stanno nella scala solo dove indicato (Vilon). Tutti gli altri restano
  candidati per la squadra del momento.
- La tabella va in codice come dati (`FormationPlanner.Target`). Cambiarla è cambiare una riga, se un
  giorno le guide cambiano idea.

### 3.3 Come si investe sulla squadra obiettivo

Due modifiche a `HallOfHeroesTask` / `EnchantPlanner`, che oggi usano solo "in formazione":

- **Enchant di tier 1** (statistiche del solo eroe che lo porta): oggi solo per gli eroi in
  formazione. Dopo: per gli eroi in formazione **e** per quelli dell'obiettivo, prima quelli
  dell'obiettivo. I tier 2 e 3 (bonus a tutti gli eroi) restano come oggi, per tutti.
- **Rarità con i contratti:** oggi prima la formazione, poi tutti gli altri. Dopo: prima l'obiettivo,
  poi il resto della formazione, poi gli altri.

Lo snapshot degli eroi (`hero_snapshot`) ha già `*` per la formazione. Aggiungere un `+` per
l'obiettivo è la modifica più piccola: `HeroState.InTarget` accanto a `InFormation`.

### 3.4 Esempio: Steam-0 il 22/10 sera

Eroi sbloccati: Boris, Burt, Solaine, Talia, Benedictus, Leo, Muriel, Blaze, Luana, Valerius, Astrid,
Ina, Fini, Cirilo.

- **Obiettivo risolto:** Valerius (tank), Solaine, Blaze, posto vuoto (niente Vilon né Iseris), Luana.
- **Oggi in battaglia:** l'obiettivo è lontano. Power: Valerius 192k, Blaze 406k e Luana 144k, contro
  Boris 2,35M e Cirilo 3,33M. La squadra del momento resta quella dei numeri (Boris, Cirilo, Solaine...).
- **Investimenti:** gli enchant di tier 1 e i contratti vanno prima a Valerius, Blaze, Luana e Solaine.
  Oggi invece vanno a chi è in formazione, o al più economico (l'ultimo piano spendeva su Fini).

## 4. La squadra del momento: il calcolo

### 4.1 Danno sul singolo bersaglio

```text
O(h) = damage × (1 / attackSpeed) × (1 + critChance × critDamage)
```

`attackSpeed` nel gioco è in secondi tra due colpi. In fase 0 si confronta `O(h)` con il DPS che il gioco
calcola per gli eroi schierati (`BattleHero.DPS`). Se il rapporto è costante, la formula va bene anche per
chi non è schierato.

### 4.2 Resistenza

```text
EHP(h) = health × M(armor) / (1 - dodge)
```

La formula di `M(armor)` non è nel wiki: in fase 0 si cerca nel gioco (`BattleHero.ReceiveDamage`).
Fino ad allora si usa `M = 1 + armor / health`.

### 4.3 Buff e debuff

Solo gli effetti che cambiano la squadra o il danno sul boss (wiki, pagine degli eroi):

| Effetto | Eroi |
| --- | --- |
| Danno alla squadra | Boris Anger +50% 25 s; Valerius +25% (e +45% armatura) 20 s; Hongyu +15% 25 s; Asmondai +20% ai melee |
| Velocità d'attacco della squadra | Muriel Rally +15%; Garret (merc.) +20%; Zoruk (merc.) +15% |
| Critico della squadra | Danysa +100% danno critico 30 s |
| Debuff sul boss (+% danni subiti) | Solaine Freeze +80% 8 s/20 s; Talia Annihilation +60% 8 s/15 s; Astrid +60% 7 s; Blaze +40% 5 s |
| Armatura e vita della squadra | Luana +40% armatura; Anzo (merc.) +25% armatura, +50% vita; Yamanoth +30% |

Ogni effetto pesa `1 + valore × uptime`, con:

```text
ciclo  = max(ricarica più lunga, (costo1 + costo2 + costo3) / rigenerazione)
uptime = min(1, durata / ciclo)
```

Gli eroi usano le abilità in ordine (1, 2, 3) appena hanno la risorsa (guida F2P). La rigenerazione
si legge dal gioco: il mana è circa 3-4 al secondo, l'energy 9. Per la rage si usa una stima per
ruolo: 4 al secondo per un tank, 2 per un damage. Esempio: con 3 mana al secondo il ciclo di Solaine
è (35 + 50 + 70) / 3 ≈ 52 s, quindi Freeze vale +80% × 15% ≈ +12% sul boss.

### 4.4 Il leader

```text
L = 1,5 × leadershipMultiplier      T = teamBonusMultiplier
```

Il valore della squadra è `Σ O(h) × (L se h è leader, altrimenti T)`. Quindi è leader l'eroe che
massimizza `O(h) × (L - T)`:

- **L > T:** il leader è l'eroe col danno più alto. Su Steam-0 il 22/10 sera è Cirilo, se il gioco
  permette un mercenario leader (da verificare in fase 0); altrimenti Solaine.
- **L < T:** il leader è l'eroe col danno più basso, di solito il tank.
- **L e T entro il 5%:** il leader è il tank, per la vita in più.

### 4.5 Il punteggio e la scelta

```text
Score(F) = Σ O(h) × S(h) × Π buff di danno e velocità × Π debuff sul boss
```

Vincoli:

1. lo slot 1 è sempre il tank melee con l'EHP più alto tra quelli scelti. Se l'account non ha tank,
   è il melee con più EHP;
2. l'EHP del fronte non scende sotto l'80% di quello della formazione attuale.

Si provano **tutte** le formazioni: anche con 30 eroi sono circa 700.000 punteggi, meno di un secondo.

```text
ponytail: ricerca esaustiva; oltre ~40 eroi fissare il tank e cercare i 4 restanti.
```

**Spareggio verso l'obiettivo:** tra due formazioni con punteggio entro il 3% vince quella con più eroi
dell'obiettivo. A parità di forza, così, la squadra si avvicina all'obiettivo, e gli investimenti della
3.3 servono anche oggi.

### 4.6 L'ordine nelle zone

- **Zona melee:** l'EHP più alto nello slot 1, poi gli altri in ordine.
- **Zona ranged:** l'eroe con `O(h) × S(h)` più alto nell'ultimo slot, il meno prezioso nel primo slot
  della zona.

## 5. Il controllo giornaliero

1. Subito dopo la lettura completa degli eroi di `HallOfHeroesTask`, una volta al giorno, il task legge
   dalla memoria del gioco eroi, attributi e moltiplicatori. Niente schermate.
2. Risolve la squadra obiettivo (3.2) e la scrive nello snapshot, per gli investimenti del giro
   successivo (3.3).
3. Calcola la squadra del momento (4.5).
4. La cambia solo se `Score(nuova) ≥ 1,10 × Score(attuale)` (`min_gain`, default 0,10), per non
   oscillare tra due squadre quasi uguali. Il cambio si fa subito, nello stesso giro: un eroe nuovo
   parte basso di livello d'oro, ma `HeroUpgrade` lo porta su.
5. Scrive sempre una riga:

   ```text
   [INFO] Formation: now Cirilo, Talia, Solaine, Boris, Muriel (leader Solaine, 1.00);
   best Boris, Leo, Muriel, Solaine, Cirilo (leader Cirilo, 1.27, +27%) - changed;
   target Valerius, Solaine, Blaze, -, Luana (3 of 4 in place: none).
   ```

## 6. Applicare la formazione nel gioco

Oggi il bot apre il menu Party solo per leggere. Le possibilità, dalla più sicura:

1. **Click sull'interfaccia.** Si toccano gli eroi nel mazzo (`deckSlotHeroButtons`) per aggiungerli o
   toglierli, si seleziona lo slot del leader e si preme `setLeaderButton`, poi si salva (`OnSaveChanges`)
   o si chiude. Da scoprire in fase 0:
   - dove va un eroe aggiunto;
   - come si toglie un eroe (c'è `removeFromPartyBg`, quindi forse si trascina);
   - come si cambia l'ordine dentro una zona.
2. **Trascinamento simulato** con gli eventi di Unity (begin drag, drag, end drag) sui
   `battleFormationMovers`. Il bot ha già `ClickSimulated` come base.
3. **Ultima scelta:** i metodi di `PartyMech` (`AddHeroToParty`, `SetLeader`, `ApplyChanges`). Più
   rapidi, ma saltano i controlli dell'interfaccia. Solo se 1 e 2 non bastano, e dopo averli provati su
   Steam-0.

Dopo il cambio si rilegge la formazione (`Party.FormationNames`, leader da `BattleHero.IsLeader()`).
Se non è quella voluta: `[FAILED]` nel log, nessun nuovo tentativo fino al giorno dopo.

## 7. Cosa si legge dal gioco

Dai metadati degli assembly Il2Cpp della 3.61 (`Il2CppCoreASM.dll`). **Tutto da verificare in fase 0.**

| Cosa | Dove |
| --- | --- |
| Eroi | `GameInitialize.HandlerLoader` → `HeroDeck.heroes` (`Il2Cpp.Hero`), `IsUnlocked()`; mercenari e dark god forse altrove |
| Attributi | `Hero.damageBasic`, `maximumHealthBasic`, `armorBasic`, `attackSpeedBasic`, `criticalChanceBasic`, `criticalDamageBasic`, `dodgeBasic` (in fase 0: comprendono gear, rarità e ricerche? Si confronta col menu Party) |
| Ruolo | `heroSpecialization`, `attackStyle`, `heroResourceType`, `characterName`, `HeroCode` |
| Moltiplicatori | `HeroDeck.leadershipMultiplier`, `teamBonusMultiplier` |
| Rigenerazione | `HeroDeck.CalculateResourceRegenerationForAllHeroes`, `HeroResourceConfigurator` |
| Schierati | `BattleHeroManager` → `BattleHero.hero`, `DPS`, `IsLeader()` |
| Menu Party | `PartyMech`: `meleePositions`, `rangedPositions`, `SetLeader`, `AddHeroToParty`, `RemoveHeroFromParty`, `ApplyChanges`, `IsHeroesAutoAbilitiesTalentUnlocked` |

## 8. Implementazione

1. **`src/Tasks/Character/FormationPlanner.cs`**, puro, come `BlueprintPlanner`:
   - `record HeroStats(Name, Spec, Style, Resource, Damage, Health, Armor, AttackSpeed, Crit,
     CritDamage, Dodge, CanLead)`;
   - la tabella delle abilità (4.3) e quella dell'obiettivo (3.2);
   - `Target(heroes)`: la squadra obiettivo risolta;
   - `Best(heroes, current, target, leaderMult, teamMult, regen, leaderAuto, minGain)`: la squadra del
     momento in ordine di slot, il leader, i due punteggi, la reason per il log;
   - test in `tests/Firebot.TalentEngine.Tests/FormationPlannerTests.cs`, da aggiungere al csproj:
     - i due casi di Steam-0, mattina (Leo) e sera (Boris, leader Cirilo);
     - obiettivo risolto con scale parziali (posto vuoto);
     - L < T;
     - nessun tank;
     - isteresi sotto il 10%;
     - spareggio verso l'obiettivo;
     - mercenario che non può fare il leader.
2. **`src/GameModel/Features/Character/Heroes.cs`**: le letture della sezione 7, ognuna in un try/catch.
   Se una lettura fallisce, quel giorno non si cambia niente.
3. **`src/Tasks/Character/FormationTask.cs`**, acceso di default con `dry_run = true`. Config:
   `dry_run`, `min_gain` (0.10), `last_done_date` (gestito dal bot). Gira dopo `HallOfHeroesTask`:
   - con `dry_run`: scrive la riga della sezione 5 senza toccare niente;
   - senza `dry_run`: applica la squadra con il metodo della sezione 6.
4. **`EnchantPlanner` e `HallOfHeroesTask`:** `InTarget` accanto a `InFormation` (3.3), con i suoi
   test in `EnchantPlannerTests`.
5. **Paths** del menu Party (mazzo, pulsante leader, salva) in `Party.cs`, verificati dal vivo.
6. README, template dei cfg, TESTING.md, questo piano.

## 9. Fasi

0. **Sonda in sola lettura, su Steam-0.** Un log una tantum con:
   - per ogni eroe sbloccato: attributi, ruolo, stile, risorsa;
   - i moltiplicatori e la rigenerazione;
   - `BattleHero.DPS` e il leader;
   - le posizioni melee e ranged.

   Si confronta con il menu Party a schermo. Si prova con l'utente come si aggiunge, toglie e ordina un
   eroe e come si cambia il leader.
1. **Planner e test**, senza gioco.
2. **Dry run sulla flotta per qualche giorno.** Ogni istanza scrive la riga della sezione 5; si
   controllano le scelte account per account.
3. **Primo cambio vero su Steam-0**, con l'utente che guarda.
4. **Investimenti sull'obiettivo** (3.3) e **flotta** (`dry_run = false` nel template).

## 10. Paletti

- Al massimo un cambio al giorno.
- Mai togliere l'ultimo tank melee.
- Lettura fallita = nessun cambio quel giorno.
- Formazione riletta diversa da quella voluta = `[FAILED]` e nessun nuovo tentativo fino al giorno dopo.
- La squadra obiettivo cambia solo dove si investe, mai da sola quello che va in battaglia.

## 11. Domande aperte

- La squadra tipo della 3.2 (tank a mana, 3 spellcaster, healer) va bene, o preferisci 4 damage
  dealer senza healer, come l'autore della guida Steam?
- `min_gain` al 10% va bene?
- Il Guardian resta fuori?

## Fonti

- Wiki (mirror in `docs/wiki/pages`): `Party`, `Battlefield`, `Enemies`, `Heroes`, pagine dei singoli
  eroi e mercenari.
- Guida Steam, `docs/guida_steam_completa.md`: "Party / Builds", "Builds", "Heroes", Exotic upgrade,
  talenti.
- Guida F2P, `docs/firestone_guida_F2P.md`: "Il leader", "Posizione in squadra", "Risorse degli eroi",
  "Leader o team bonus?".
- Forum Steam: [lancio manuale e automatico delle abilità del leader](https://steamcommunity.com/app/1013320/discussions/0/3380536561722803665),
  [leader cambiato sull'eroe col danno più basso](https://steamcommunity.com/app/1013320/discussions/0/1639801448914297514),
  [squadra allo stage 700+, nessun tier fisso](https://steamcommunity.com/app/1013320/discussions/0/1639801448929451852/).
- Assembly Il2Cpp della 3.61 (`Il2CppCoreASM.dll`): `Hero`, `HeroDeck`, `BattleHero`,
  `BattleHeroManager`, `PartyMech`.
