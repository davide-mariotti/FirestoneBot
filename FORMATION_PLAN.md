# Piano: la squadra del momento e la squadra obiettivo di ogni account

> Scritto il 2026-10-22, rivisto il 2026-10-23 tre volte su indicazione dell'utente:
> - un solo controllo al giorno, nessun controllo su muro, stage o morti;
> - Cirilo in cima alle scelte (i mercenari possono fare il leader: confermato dall'utente);
> - niente healer fisso;
> - la squadra obiettivo calcolata per ogni account dai suoi moltiplicatori, non uguale per tutti.
>
> Solo analisi: niente codice ancora. Oggi il bot legge la formazione ma non la cambia mai. Cirilo lo
> compra l'utente a mano sugli account che non l'hanno.

## Stato (23/10, Steam-0)

Fasi 0-2 fatte su Steam-0, solo in dry run. `FormationTask` legge e scrive, non cambia niente.

- **Sonda (fase 0).** Gli attributi sono `ObscuredBigDouble` e superano un `double` (danno circa
  10^186), quindi tutto è in log10 (`BigDouble.AbsLog10`). Mercenari e dark god sono nella stessa lista
  `HeroDeck.heroes`. `BattleHeroManager` si raggiunge da `FindObjectOfType<HeroBattleLogic>()`.
- **Formula del danno verificata.** `danno / velocità × (1 + critico × danno critico)` più una costante
  uguale per tutti (291,76) dà esattamente il `BattleHero.DPS` del gioco per gli eroi schierati.
  `damageBasic` non comprende i livelli d'oro, che stanno nella costante. Le abilità non sono nel modello.
- **Fattore del leader:** è `leadershipMultiplier / teamBonusMultiplier`, senza l'1,5 della sezione 4.3.
  Su Steam-0 è 10^(3,36 - 2,18) ≈ 15, e torna col DPS totale.
- **Potenziale:** `damageInitial` × i tre moltiplicatori (`GetSpecializationMultiplier`,
  `GetAttackStyleMultiplier`, `GetResourceMultiplier`). Quello che resta di `damageBasic` (circa 10^160)
  è comune a tutti più l'investimento del singolo eroe, che va da 0 a 4 ordini di grandezza.
- **Planner (fase 1).** `FormationPlanner`: i 5 col valore più alto. Leader il più forte se il fattore
  supera 1, altrimenti il più debole. Melee davanti, il più prezioso in fondo a ogni zona. Non serve una
  ricerca esaustiva finché le abilità non sono nel modello. Test in `FormationPlannerTests`, coi numeri
  veri di Steam-0.
- **Dry run (fase 2) su Steam-0, 23/10 15:08:**
  `now Boris, Talia, Muriel, Solaine, Cirilo (leader Cirilo); best Boris, Muriel, Talia, Solaine, Cirilo
  (leader Talia, x7.01); target Muriel, Talia, Burt, Cirilo, Astrid (leader Talia)`.
  - La squadra schierata era già quella giusta, ma col leader sbagliato: Talia fa circa 10 volte il
    danno di Cirilo, e il consiglio del 22/10 (Cirilo leader) veniva dalla power.
  - L'obiettivo di Steam-0 è rage ed energy: i suoi bonus per rage (10^10,45) ed energy (10^10,26)
    battono il mana (10^8,36) di 100 volte. Le guide puntano sul mana, ma questo account no.
- **Cambio del leader (fase 3, primo pezzo), 23/10 15:23, con l'utente che guardava.** Con
  `dry_run = false` e gli stessi 5 eroi il bot apre Party e tocca il `battleFormationMover` dello slot
  dell'eroe (Talia era in `heroSlot1`). Il pannello del leader mostra allora "Talia | Choose leader |
  ...", e il bot preme `setLeader` solo se il pannello mostra il nome giusto. Poi chiude: niente popup di
  salvataggio, e la rilettura dal gioco dà `leader now Talia`. Il pannello si trova per nome sotto
  `menus/Party` (`Party.SetLeader`).
- **Scambio degli eroi, 23/10 16:01, Steam-28.** Un tocco su una cella del mazzo (`heroGrid`, celle
  `PartyDeckSlotViewController`) toglie o mette quell'eroe. Le celle si riconoscono dal nome dello sprite
  del ritratto: `avatarBurt164(Clone)`, ma anche le skin (`murielTheNurseOfDeathAvatar164`) e il refuso
  del gioco `avatarValerious164`. Il nome contenuto nel ritratto vince, altrimenti si prende l'unico
  ritratto entro 2 lettere (`FormationPlanner.MatchPortrait`). Il mazzo si riempie dopo gli slot, quindi
  va aspettato.
- **Flotta, 23/10 16:06-17:38.** `dry_run = false` su Steam-0..31 (anche nel template). Su 32 istanze, 23
  hanno cambiato squadra o leader, tutte "as planned"; 9 sono rimaste com'erano perché il guadagno era
  sotto il 10%. Il controllo giornaliero è alle 11:02, non alle 10:00 (scelta dell'utente, per non
  sommarsi al reset). Dal 25/10 è alle 09:02 UTC, un'ora dopo il reset: 10:02 con l'ora solare.
- **Verifica del DPS, 23/10 17:49, su Steam-2, -5, -11, -13, -24 e -28** (`Formation dps check` nel log,
  ogni giorno).
  - **Eroi schierati non leader:** allo stesso livello d'oro, `DPS del gioco - offense` è la stessa
    costante al centesimo, anche per gli eroi appena entrati in squadra. Quindi gli attributi degli eroi
    in panchina non sono vecchi.
  - **Leader:** la sua differenza sopra gli altri è esattamente `leadership - team bonus`, anche quando è
    negativa (Steam-2 -0,73, Steam-24 -0,62).
  - **Unica eccezione:** Muriel su Steam-28 ha +0,39 fuori dal modello, un effetto che `damageBasic` non
    contiene.
  - **Livelli d'oro:** 100 livelli d'oro valgono circa 10^1,4, quindi un eroe appena scambiato rende meno
    finché `HeroUpgrade` non lo porta al livello degli altri. Un'ora dopo gli scambi i livelli erano
    allineati entro 100.
- **Investimenti sull'obiettivo (5.1 e 5.2), 24/10, Steam-0.**
  - **Obiettivo nel cfg.** `FormationTask` lo salva in `[formationtask] target` ("nomi | leader") e lo
    sostituisce solo con uno migliore del 20% sul potenziale; la riga del log dice `target new` o
    `target kept`.
  - **Tier 1.** `HallOfHeroesTask` aggiunge gli eroi dell'obiettivo a quelli in formazione (`Invested
    in (formation + target)`), quindi gli enchant di tier 1 vanno a entrambi. Il '*' dello snapshot
    vuol dire "formazione o obiettivo".
  - **Rarità.** L'ordine è obiettivo, poi resto della formazione, poi gli altri.
  - **Primo giro su Steam-0 alle 16:33.** Obiettivo Talia, Ina, Burt, Cirilo, Astrid. Il piano del gear
    spende subito sui tier 1 di Ina e Astrid. Nessuna rarità comprata: mancano i requisiti del gear,
    non i contratti (646).

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

Ogni account ha due squadre, calcolate entrambe dal bot una volta al giorno:

- **La squadra del momento.** È la più forte che l'account può schierare oggi, con gli attributi veri
  dei suoi eroi: gear, enchant e rarità compresi. È quella che combatte.
- **La squadra obiettivo.** È la squadra che diventerebbe la più forte se tutti gli eroi avessero lo
  stesso investimento. Si calcola con gli attributi **di base** degli eroi, moltiplicati per i bonus
  dell'account per specializzazione, stile e risorsa. Quei bonus vengono da artefatti, beast, alberi,
  talenti e carte, e sono diversi per ogni account. Il bot non la schiera per forza, ma **ci investe sopra**:
  enchant di tier 1 e rarità vanno prima ai suoi eroi.

Gli investimenti fanno crescere gli eroi dell'obiettivo. Quando superano quelli del momento, il calcolo li
sceglie da solo, e la squadra del momento diventa l'obiettivo. Se l'account pesca artefatti diversi,
cambiano i suoi bonus e cambia l'obiettivo. Le guide servono a scegliere tra eroi quasi uguali, non a
imporre una squadra.

## 2. Come funziona il gioco

### 2.1 Squadra e posizioni

Fonti: wiki `docs/wiki/pages/Party.html`, `Battlefield.html`; guida Steam
(`docs/guida_steam_completa.md`, "Party / Builds").

- **5 eroi**, negli slot da 1 a 5.
- **Due zone.** I melee stanno davanti, i ranged e gli spellcaster dietro; i melee vengono sempre prima
  dei ranged. Si possono scambiare gli eroi solo dentro la propria zona.
- **Bersagli.** I nemici melee colpiscono le prime posizioni. Quelli a distanza scelgono il bersaglio
  così: slot 1 50%, 2 20%, 3 15%, 4 10%, 5 5%. I taunt dei tank ("Force all enemies to attack you")
  portano per qualche secondo tutti i colpi sul tank.

### 2.2 Leader e team bonus

- Il leader prende **+50% a tutti gli attributi** e le ricerche Leadership. Gli altri quattro prendono il
  **Team Bonus**. "Se il team bonus è abbastanza grande, il leader avrà statistiche più basse degli
  altri" (guida Steam, "Roles").
- Il gioco tiene in memoria i due moltiplicatori: `HeroDeck.leadershipMultiplier` e `teamBonusMultiplier`.
- Il leader lancia le abilità da solo solo col talento "Leader - Auto Abilities". Il bot non lancia
  abilità a mano.
- **I mercenari possono fare il leader** (confermato dall'utente il 23/10).

### 2.3 Healer e tank: cosa dicono le fonti (ricerca del 23/10)

| Fonte | Cosa dice |
| --- | --- |
| [Forum Steam "Is it worth having a healer?"](https://steamcommunity.com/app/1013320/discussions/0/3767860082238723460/), febbraio-marzo 2023 | Impact: "healer o tank sono utili solo in casi molto particolari, di solito no"; "nel finale conta solo il danno, tank e healer non servono"; meglio "finire con eroi a energy", i più veloci e con molte AoE. Scegliere gli eroi col miglior DPS dati gli artefatti. roku750: curare "di solito non vale". Nel finale però usa **Layla come leader**, per l'immunità di squadra: con i suoi artefatti il leader dava poco danno, quindi quello slot era già debole |
| [Guida "Ultimate Guide to FAST Levelling"](https://steamcommunity.com/sharedfiles/filedetails/?id=2134359407), 2020-2022 | "Glass cannon fino in fondo", "l'armatura è inutile", meglio uccidere con un colpo e morire più in alto che sopravvivere più in basso. La cura di Benedictus è "inutile": va davanti a fare da scudo finché arriva Muriel. Boris e Valerius servono per il taunt, che protegge gli eroi ad area |
| [Guida dei moderatori (Verkade)](https://steamcommunity.com/sharedfiles/filedetails/?id=2152293111), luglio 2020, oggi rimossa | L'armatura è "TOTALMENTE INUTILE"; la vita solo per una build bilanciata, da alzare "alla fine" |
| Guida Steam completa (settembre 2026, `docs/guida_steam_completa.md`, "Builds") | Ha misurato le due build: bilanciata 65-70 uccisioni al minuto, glass cannon 30-35. Ma per compensare basta farmare 4 stage più in alto, e l'autore gioca glass cannon: "è la più efficace" |
| Guida F2P (Ashstronox) | Dentro il mana ha usato damage, healer o tank in periodi diversi, seguendo i moltiplicatori che aveva |

**Conclusione: l'healer non va messo per curare.** Agli stage alti gli eroi vengono uccisi con un colpo,
quindi la cura arriva tardi, e conta solo il danno. L'unica eccezione utile è uno slot che fa comunque
poco danno: per esempio il leader quando il team bonus è più alto della leadership (4.4). Lì un
healer con immunità di squadra (Layla) o con stordimento ad area (Belien) vale più del poco danno che
toglie. Anche il tank vale solo per il suo **taunt**, che protegge gli eroi ad area, e per i buff
(Boris +50% danno alla squadra), non per la vita.

### 2.4 Cosa serve per spingere

- Ogni stage ha 5 ondate. Alla quinta il **boss** va battuto entro **60 secondi**. Dopo lo stage 200 i
  nemici triplicano a ogni stage.
- Ogni stage dà il 25% di oro in più: spingere è anche farmare.
- Quindi il calcolo misura il **danno sul boss**, con buff e debuff. Il danno ad area serve solo da
  spareggio, perché pulisce le ondate più in fretta.

### 2.5 La "power" non è la forza in battaglia

La power somma gear, jewel e soulstone (wiki `Heroes`). I jewel servono solo alle War Machine. Il
calcolo usa gli attributi che il gioco calcola per la battaglia.

## 3. Cosa si legge dal gioco

Dai metadati degli assembly Il2Cpp della 3.61 (`Il2CppCoreASM.dll`). **Tutto da verificare in fase 0.**

| Cosa | Dove | Serve per |
| --- | --- | --- |
| Eroi | `GameInitialize.HandlerLoader` → `HeroDeck.heroes` (`Il2Cpp.Hero`), `IsUnlocked()`. Mercenari e dark god: da capire se stanno nella stessa lista | tutte e due |
| Attributi attuali | `Hero.damageBasic`, `maximumHealthBasic`, `armorBasic`, `attackSpeedBasic`, `criticalChanceBasic`, `criticalDamageBasic`, `dodgeBasic` | squadra del momento |
| Attributi di base | `Hero.damageInitial`, `maximumHealthInitial`, `attackSpeedInitial`, `criticalChanceInitial`, `criticalDamageInitial` | squadra obiettivo |
| Bonus dell'account per quell'eroe | `Hero.GetSpecializationMultiplier()`, `GetAttackStyleMultiplier()`, `GetResourceMultiplier()`; i totali in `HeroDeck.specializationDamageTotal`, `specializationTankTotal`, `specializationHealerTotal`, `attackStyleMeleeTotal`, `attackStyleRangedTotal`, `attackStyleSpellcasterTotal`, `resourceRageTotal`, `resourceManaTotal`, `resourceEnergyTotal` | squadra obiettivo |
| Ruolo | `heroSpecialization`, `attackStyle`, `heroResourceType`, `characterName`, `HeroCode` | tutte e due |
| Leader | `HeroDeck.leadershipMultiplier`, `teamBonusMultiplier` | tutte e due |
| Rigenerazione delle risorse | `HeroDeck.CalculateResourceRegenerationForAllHeroes`, `HeroResourceConfigurator` | uptime delle abilità |
| Schierati | `BattleHeroManager` → `BattleHero.hero`, `DPS`, `IsLeader()` | taratura e verifica |
| Menu Party | `PartyMech`: `meleePositions`, `rangedPositions`, `SetLeader`, `AddHeroToParty`, `RemoveHeroFromParty`, `ApplyChanges` | applicare il cambio |

In fase 0 si verifica anche che `damageBasic` sia davvero `damageInitial` × quei tre moltiplicatori ×
gear × rarità × ricerche generali. Se è così, la squadra obiettivo è la stessa formula senza gear e
rarità, cioè senza l'investimento fatto su quell'eroe.

## 4. Il calcolo (uguale per le due squadre, cambia solo l'input)

### 4.1 Danno di un eroe

```text
O(h) = damage × (1 / attackSpeed) × (1 + critChance × critDamage) × A(h)
```

- **Squadra del momento:** gli attributi attuali (`...Basic`). In fase 0 si confronta `O(h)` con il
  `BattleHero.DPS` del gioco per gli eroi schierati.
- **Squadra obiettivo:** gli attributi di base (`...Initial`) × i tre moltiplicatori dell'account
  per quell'eroe.
- `A(h)` conta le abilità che fanno danno all'eroe stesso, con il loro uptime (4.3). Per esempio
  Talia Annihilation 450%, Blaze Dragon Breath ad area.

### 4.2 Effetti sulla squadra e sul boss

| Effetto | Eroi |
| --- | --- |
| Danno alla squadra | Boris Anger +50% 25 s; Valerius +25% 20 s; Hongyu +15% 25 s; Asmondai +20% ai melee |
| Velocità d'attacco della squadra | Muriel Rally +15%; Garret (merc.) +20%; Zoruk (merc.) +15% |
| Critico della squadra | Danysa +100% danno critico 30 s |
| Debuff sul boss (+% danni subiti) | Solaine Freeze +80% 8 s/20 s; Talia Annihilation +60% 8 s/15 s; Astrid +60% 7 s; Blaze +40% 5 s |
| Immunità o controllo della squadra (vale solo nello slot debole, 4.4) | Layla Ward of protection 5 s ogni 20 s; Belien stordisce tutti 6 s; Ledra stordisce tutti 4 s |

Ogni effetto pesa `1 + valore × uptime`:

```text
ciclo  = max(ricarica più lunga, (costo1 + costo2 + costo3) / rigenerazione)
uptime = min(1, durata / ciclo)
```

Gli eroi usano le abilità in ordine (1, 2, 3) appena hanno la risorsa. La rigenerazione si legge dal
gioco: il mana è circa 3-4 al secondo, l'energy 9. Per la rage si usa una stima: 4 al secondo per un
tank, 2 per un damage. Le abilità del leader contano solo col talento "Leader - Auto Abilities".

### 4.3 Il leader

```text
L = 1,5 × leadershipMultiplier      T = teamBonusMultiplier
```

Il danno della squadra è `Σ O(h) × (L se h è leader, altrimenti T)`. Quindi è leader l'eroe che
massimizza `O(h) × (L - T)`:

- **L > T:** il leader è l'eroe col danno più alto. Su Steam-0 il 22/10 sera era Cirilo.
- **L < T:** il leader è l'eroe col danno più basso, cioè lo slot debole. Lì entra un eroe di utilità
  (4.4).

### 4.4 Lo slot debole

Con L < T il leader perde il team bonus, quindi chi fa il leader dà poco danno comunque. In quello
slot il calcolo confronta il danno perso con il valore di un eroe di utilità:

- Layla: immunità a tutti per 5 s ogni 20 s, cioè circa 25% del tempo senza danni;
- Belien: stordisce tutti per 6 s;
- un tank col taunt: protegge gli eroi ad area.

Il valore di un eroe di utilità si stima come una riduzione del tempo perso a morire, quindi un piccolo
moltiplicatore sul danno della squadra (+5% all'inizio). Si tara in fase 2. Con L > T non c'è slot
debole, e la squadra è solo danno.

### 4.5 Il punteggio e la scelta

```text
Score(F) = Σ O(h) × S(h) × Π buff della squadra × Π debuff sul boss × utilità dello slot debole
```

dove `S(h)` è L o T.

- **Nessun vincolo di tank o healer.** Entrano solo se il punteggio li premia (buff, taunt, slot
  debole). Se nessun melee è scelto, i ranged prendono anche lo slot 1.
- **Ricerca esaustiva:** con 30 eroi sono circa 700.000 punteggi, meno di un secondo.

  ```text
  ponytail: ricerca esaustiva; oltre ~40 eroi fissare il leader e cercare i 4 restanti.
  ```

- **Spareggio con le guide:** tra formazioni entro il 3%, vince quella col danno ad area più alto
  (Cirilo, Solaine Ice Barrage, Blaze Dragon Breath, Fini, Molly, Iseris, Astrid). Se c'è ancora
  parità, vince quella con più eroi delle guide: Solaine, Blaze, Cirilo (3.1 sotto).

### 4.6 L'ordine nelle zone

- **Zona melee:** davanti chi ha il taunt o la vita più alta.
- **Zona ranged:** l'eroe col danno più alto (`O(h) × S(h)`) nell'ultimo slot (5% dei colpi), il meno
  prezioso nel primo slot della zona.

### 4.7 Le guide come riferimento (non come vincolo)

Le fonti della ricerca del 22-23/10 dicono tre cose:

- **Solaine e Blaze** sono il nucleo e i leader migliori:
  [guida ufficiale R2 Games](https://fs.r2games.com/content/party_10257.html), guida Steam, forum;
- **Cirilo** è il miglior mercenario: guida Steam completa ("al momento Cirilo è la scelta
  migliore"), moderatori Steam 2024 e 2025,
  [guida giapponese](https://note.com/note_7xyz/n/nff01db1f1e34?hl=en) ("un personaggio rotto");
- **gli artefatti decidono:** chi ha artefatti per energy o tank fa meglio con quegli eroi (forum Steam
  2020 e 2023).

Il calcolo della squadra obiettivo applica proprio quest'ultima regola: i bonus di specializzazione, stile
e risorsa **sono** gli artefatti, le beast e gli alberi dell'account. Solaine, Blaze e Cirilo vincono
dove i bonus li premiano, e perdono dove l'account ha pescato altro. Le guide restano solo nello
spareggio.

## 5. La squadra obiettivo e gli investimenti

### 5.1 Stabilità

Gli investimenti valgono solo se l'obiettivo non cambia ogni giorno. Per questo:

- l'obiettivo cambia solo se il nuovo ha un punteggio di almeno **+20%** sull'attuale
  (`target_min_gain`);
- un eroe esce dall'obiettivo solo se quello che lo sostituisce è almeno del 20% migliore nel suo slot.

I bonus dell'account cambiano lentamente: un artefatto nuovo, un livello di beast. Quindi nella pratica
l'obiettivo si sposta raramente.

### 5.2 Dove si investe

Due modifiche a `HallOfHeroesTask` / `EnchantPlanner`, che oggi usano solo "in formazione":

- **Enchant di tier 1** (statistiche del solo eroe che lo porta): oggi solo per gli eroi in formazione.
  Dopo: per gli eroi in formazione **e** quelli dell'obiettivo, prima quelli dell'obiettivo. I tier
  2 e 3 (bonus a tutti gli eroi) restano come oggi, per tutti.
- **Rarità con i contratti:** oggi prima la formazione, poi gli altri. Dopo: prima l'obiettivo, poi il
  resto della formazione, poi gli altri.

Nello snapshot degli eroi (`hero_snapshot`) c'è già `*` per la formazione. Si aggiunge `+` per
l'obiettivo: `HeroState.InTarget` accanto a `InFormation`.

### 5.3 Esempio: Steam-0 (numeri da leggere in fase 0)

- **Squadra del momento:** sulla power di ieri sera, Cirilo, Solaine, Boris, Leo, Muriel o Talia, con
  leader Cirilo. Con gli attributi veri il calcolo può spostare il quinto posto.
- **Squadra obiettivo:** dipende dai bonus di Steam-0. Se sono soprattutto mana e spellcaster (i
  talenti del bot vanno lì: "Mana Heroes" 40, "Magic Spells" 35), verrà fuori qualcosa come Solaine,
  Blaze, Cirilo più due tra Valerius, Leo e Boris (per i buff e il taunt). Con bonus per energy
  salirebbero Muriel, Astrid e Burt. La fase 0 dice quale dei due.

## 6. Il controllo giornaliero

1. Subito dopo la lettura completa degli eroi di `HallOfHeroesTask`, una volta al giorno, il task legge
   dalla memoria del gioco eroi, attributi e moltiplicatori. Niente schermate.
2. Calcola la squadra obiettivo (con l'isteresi della 5.1) e la segna nello snapshot per gli
   investimenti del giro dopo.
3. Calcola la squadra del momento. La cambia solo se `Score(nuova) ≥ 1,10 × Score(attuale)`
   (`min_gain`), e lo fa subito: un eroe nuovo parte basso di livello d'oro, ma `HeroUpgrade` lo
   porta su.
4. Scrive sempre una riga:

   ```text
   [INFO] Formation: now Cirilo, Talia, Solaine, Boris, Muriel (leader Solaine, 1.00);
   best Boris, Leo, Muriel, Solaine, Cirilo (leader Cirilo, 1.27, +27%) - changed;
   target Boris, Solaine, Blaze, Cirilo, Valerius (leader Cirilo; bonuses: mana x12, spell x8, energy x3).
   ```

## 7. Applicare la formazione nel gioco

Oggi il bot apre il menu Party solo per leggere. Le possibilità, dalla più sicura:

1. **Click sull'interfaccia.** Si toccano gli eroi nel mazzo (`deckSlotHeroButtons`) per aggiungerli o
   toglierli, si seleziona lo slot del leader e si preme `setLeaderButton`, poi si salva (`OnSaveChanges`)
   o si chiude. Da scoprire in fase 0:
   - dove va un eroe aggiunto;
   - come si toglie (c'è `removeFromPartyBg`, quindi forse si trascina);
   - come si cambia l'ordine dentro una zona.
2. **Trascinamento simulato** con gli eventi di Unity sui `battleFormationMovers`. Il bot ha già
   `ClickSimulated` come base.
3. **Ultima scelta:** i metodi di `PartyMech` (`AddHeroToParty`, `SetLeader`, `ApplyChanges`). Solo se 1
   e 2 non bastano, e dopo averli provati su Steam-0.

Dopo il cambio si rilegge la formazione (`Party.FormationNames`, leader da `BattleHero.IsLeader()`).
Se non è quella voluta: `[FAILED]` nel log, nessun nuovo tentativo fino al giorno dopo.

## 8. Implementazione

1. **`src/Tasks/Character/FormationPlanner.cs`**, puro, come `BlueprintPlanner`:
   - `record HeroStats(Name, Spec, Style, Resource, Damage, AttackSpeed, Crit, CritDamage, Health,
     Armor, CanLead)`;
   - la tabella delle abilità (4.2) e la lista delle guide per lo spareggio (4.7);
   - `Best(heroes, current, leaderMult, teamMult, regen, leaderAuto, minGain)`: la formazione in ordine
     di slot, il leader, i due punteggi, la reason. Lo stesso metodo serve per le due squadre:
     cambiano solo gli `HeroStats` in ingresso (attuali o di base × bonus);
   - test in `tests/Firebot.TalentEngine.Tests/FormationPlannerTests.cs`, da aggiungere al csproj:
     - Steam-0 mattina (Leo) e sera (Boris, leader Cirilo);
     - L < T (leader nello slot debole, eroe di utilità);
     - account con bonus energy (obiettivo a energy) e uno con bonus mana (Solaine, Blaze);
     - nessun melee;
     - isteresi al 10% e al 20%;
     - spareggio con le guide.
2. **`src/GameModel/Features/Character/Heroes.cs`**: le letture della sezione 3, ognuna in un
   try/catch. Se una lettura fallisce, quel giorno non si cambia niente.
3. **`src/Tasks/Character/FormationTask.cs`**, acceso di default con `dry_run = true`. Config:
   `dry_run`, `min_gain` (0.10), `target_min_gain` (0.20), `target` e `last_done_date` (gestiti dal
   bot). Gira dopo `HallOfHeroesTask`:
   - con `dry_run`: scrive la riga della sezione 6;
   - senza `dry_run`: applica la squadra con la sezione 7.
4. **`EnchantPlanner` e `HallOfHeroesTask`:** `InTarget` accanto a `InFormation` (5.2), con test in
   `EnchantPlannerTests`.
5. **Paths** del menu Party (mazzo, pulsante leader, salva) in `Party.cs`, verificati dal vivo.
6. README, template dei cfg, TESTING.md, questo piano.

## 9. Fasi

0. **Sonda in sola lettura, su Steam-0.** Un log una tantum con:
   - per ogni eroe: attributi attuali e di base, i tre moltiplicatori, ruolo;
   - i totali dei bonus e i due moltiplicatori leader e team;
   - la rigenerazione;
   - `BattleHero.DPS` degli schierati.

   Si verifica la formula della sezione 3. Con l'utente si prova come si aggiunge, toglie e ordina un
   eroe e come si cambia il leader.
1. **Planner e test**, senza gioco.
2. **Dry run sulla flotta per qualche giorno.** Ogni istanza scrive la riga della sezione 6. Si
   controllano gli obiettivi account per account: devono cambiare con i bonus, non essere tutti uguali.
3. **Primo cambio vero su Steam-0**, con l'utente che guarda.
4. **Investimenti sull'obiettivo** (5.2) e **flotta** (`dry_run = false` nel template).

## 10. Paletti

- Al massimo un cambio di squadra al giorno.
- L'obiettivo cambia solo col +20% (5.1).
- Lettura fallita = nessun cambio quel giorno.
- Formazione riletta diversa da quella voluta = `[FAILED]` e nessun nuovo tentativo fino al giorno dopo.
- La squadra obiettivo indirizza gli investimenti, non decide da sola chi combatte.

## 11. Domande aperte

- `min_gain` al 10% e `target_min_gain` al 20% vanno bene?
- Il Guardian resta fuori?

## Fonti

- Wiki (mirror in `docs/wiki/pages`): `Party`, `Battlefield`, `Enemies`, `Heroes`, pagine dei singoli
  eroi e mercenari; online: [Cirilo](https://firestone-idle-rpg.fandom.com/wiki/Cirilo),
  [Pirate Ship](https://firestone-idle-rpg.fandom.com/wiki/Pirate_Ship).
- Guida Steam completa, `docs/guida_steam_completa.md` (settembre 2026): "Party / Builds", "Builds",
  "Pirate ship / Mercenaries"; [online](https://steamcommunity.com/sharedfiles/filedetails/?id=2788843533).
- Guida F2P, `docs/firestone_guida_F2P.md`.
- [Guida ufficiale R2 Games, "Party"](https://fs.r2games.com/content/party_10257.html).
- [Guida giapponese su note.com](https://note.com/note_7xyz/n/nff01db1f1e34?hl=en).
- [Guida "Ultimate Guide to FAST Levelling"](https://steamcommunity.com/sharedfiles/filedetails/?id=2134359407).
- [Guida dei moderatori (Verkade)](https://steamcommunity.com/sharedfiles/filedetails/?id=2152293111).
- Forum Steam:
  - [healer: ne vale la pena? (2023)](https://steamcommunity.com/app/1013320/discussions/0/3767860082238723460/);
  - [Blaze leader per il finale, gli artefatti decidono (2020)](https://steamcommunity.com/app/1013320/discussions/0/1868371326837727158/);
  - [mercenari: Cirilo il migliore (2024)](https://steamcommunity.com/app/1013320/discussions/0/4333103880864272947/);
  - [squadra dopo il prestige, Cirilo al posto di Vilon (2025)](https://steamcommunity.com/app/1013320/discussions/0/595146751510328450/);
  - [lancio manuale e automatico delle abilità del leader](https://steamcommunity.com/app/1013320/discussions/0/3380536561722803665);
  - [nessun tier fisso](https://steamcommunity.com/app/1013320/discussions/0/1639801448929451852/).
- Assembly Il2Cpp della 3.61 (`Il2CppCoreASM.dll`): `Hero`, `HeroDeck`, `BattleHero`,
  `BattleHeroManager`, `PartyMech`.
