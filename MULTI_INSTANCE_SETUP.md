# Setup multi-istanza — guida di replica per un secondo PC (istanze 17-34)

Questo file documenta **ogni singola configurazione** presente sul PC "principale" (che fa
girare le istanze Steam-0..Steam-16) necessaria per far girare Firebot su un secondo PC con le
istanze Steam-17..Steam-34, con lo stesso comportamento e la stessa stabilità. Non è un readme
di progetto (per quello vedi README.md/TESTING.md) - è un **elenco di verifica** pensato per
essere letto dopo un `git pull`, punto per punto, per controllare cosa manca o cosa va rifatto
sul nuovo PC.

Scritto il 2026-09-26, aggiornato il 2026-09-30 sera (sfide degli eventi, Map Missions `desc`) e
l'1/10 (quest rilette dopo il reset, Arena of Kings, riserva di vendita del Merchant). Se qualcosa qui non corrisponde più a quanto
trovi nel repo o sul PC principale, fidati di quello che vedi dal vivo, non di questo file (che è
una fotografia di un momento preciso).

## Aggiornare il bot su istanze già in funzione

Se il PC è già configurato e le istanze girano con una versione vecchia del bot, basta questo (il
resto della guida serve per un setup da zero). Esempio per Steam-17..34; per un altro intervallo
cambia i numeri, e `-From` è anche la cella in alto a sinistra della griglia.

1. `git pull` nella cartella del repo.
2. Build (punto 4.3): `$env:COMMON_DIR = "C:\Program Files (x86)\Steam-17\steamapps\common"`, poi
   `dotnet build "C:\Repos\FirestoneBot\src\firebot.csproj" -c Release`. Deve dare 0 errori.
3. Ferma tutte le istanze (`Ferma_Tutte_Istanze_Firestone.bat`, o `Start.exe /box:SteamB<N>
   /terminate` per ognuna) e controlla che non resti nessun `Firestone.exe`.
4. Copia **entrambe** le DLL (`firebot.dll` e `Firebot.TalentEngine.dll`, da
   `src\bin\Release\net6.0`) nei **due** `Mods` di ogni istanza, reale e sandbox (punto 5). Nessun
   altro file: MelonLoader e il resto non cambiano. Se il `Mods` del sandbox non esiste (così su
   tutte le 18 istanze del secondo PC l'1/10) il gioco legge quello reale: basta quella copia, non
   va creato.
5. Allinea i cfg a Steam-0 (punto 8.1):
   `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\ConfigTemplate\apply_template.ps1 -From 17 -To 34`
   (da PowerShell, nella cartella del repo). Se segnala chiavi mancanti non è un
   problema: sono sezioni nuove che la DLL crea al primo avvio, col valore di Steam-0. Col repo del
   30/09 sera, su istanze ferme a prima del 30/09, ogni file mostra:
   - `hallofheroestask.enabled: false -> true`: l'accensione di Hall of Heroes;
   - `firebot_settings.start_bot_delay: 30.0 -> 60.0`: il bot aspetta 60 s dall'avvio, così i
     popup d'avvio sono già chiusi;
   - `mapmissionstask.mission_time_order: "asc" -> "desc"`: missioni più lunghe per prime
     (decisione del 30/09, già su Steam-0..16);
   - chiavi mancanti `beaststask.enabled`, `guardianevolutiontask.enabled`,
     `warmachineraritytask.enabled`: i tre task nuovi, accesi di default;
   - chiave mancante `minieventtask.enabled`: il task unico dei mini-eventi, che sostituisce
     Mass Production e Sigils of Prophecy. Le loro sezioni (`[massproductioneventtask]`,
     `[sigilsofprophecyeventtask]`) restano nel cfg senza un task che le legga: non fanno niente.

   Su istanze già aggiornate con la procedura del 30/09 pomeriggio compaiono solo
   `mission_time_order` e la chiave mancante `minieventtask.enabled`. Prima di scrivere conviene
   copiare i cfg da parte, come sul PC principale (`C:\Repos\FirestoneBot-test-backup\2026-09-30-fleet`).

   Non passare l'uscita dello script a `Select-Object -First`: in PowerShell 5.1 lo ferma dopo
   quelle righe e i cfg restanti non vengono scritti.

   Il repo dell'1/10 non aggiunge chiavi; cambiano quattro valori, su ogni file:
   - `empowertask.min_reset_ratio: 2.0 -> 1.0`: l'empower si fa a +100%, come dice la guida (col
     valore vecchio il +200% non arrivava mai e l'empower era sempre forzato a 2 ore);
   - `empowertask.max_adventure_minutes: 120 -> 360`: il limite di tempo resta solo come tetto di
     sicurezza, a 6 ore;
   - `firebot_settings.target_frame_rate: 15 -> 10`: circa il 40% di CPU in meno per istanza (sul PC
     principale da 12,5% a 7,9% di un core), senza effetti sul gioco né sui task;
   - `hero_upgrade.sweep_interval_seconds: 5.0 -> 15.0`: un terzo delle pressioni sui livelli degli
     eroi (in MAX una pressione compra già tutto quello che si può).

   Il repo dell'1/10 sera aggiunge una chiave: `emblemmarkettask.enabled` risulta mancante finché
   l'istanza non parte una volta con la DLL nuova, che crea la sezione già accesa (il task
   dell'Emblem market, punto 12). Non serve rilanciare lo script per questo: il controllo del punto 7
   a istanze avviate deve dare 0 differenze.

   Il repo del 3/10 non aggiunge né cambia chiavi. Porta tre correzioni nel codice: Free Pickaxes
   aspetta che lo shop della gilda mostri la quantità (a 10 fps prima leggeva quasi sempre vuoto e
   non reclamava), Chaos Rift fa tutti i colpi in un giro invece di uno ogni 30 minuti, e Hall of
   Heroes alza la rarità degli eroi con i contratti (punto 8). Il 3/10 pomeriggio si aggiunge
   l'apertura dei forzieri: un forziere che l'account non può ancora aprire (gli Iron sugli account a
   livello 30-59) si salta subito con `can't be opened yet`, invece di 3 tentativi da 20 s che al
   reset mandavano Collector in timeout. Applicato sul secondo PC alle 13:29. Il 3/10 sera si
   aggiunge il task della campagna (punto 13): la chiave `warfrontcampaigntask.enabled` manca finché
   l'istanza non parte una volta con la DLL nuova, come per l'Emblem market. Applicato sul secondo PC
   alle 16:12 (4d0e75b), poi alle 17:31 4680fcf (crew per specializzazione, giorno del bot dalle
   10:02), che non aggiunge chiavi.

   Il repo del 4/10 cambia un valore su ogni file: `warmachinestask.enabled: true -> false`. Il
   livello delle war machine costa gli stessi gettoni spedizione del Personal Tree (500 per 100 xp) e
   il task le livellava a ogni badge, ogni 1-6 ore: i gettoni ora vanno solo all'albero (scelta
   dell'utente). Porta anche tre correzioni nel codice, senza chiavi nuove (punto 14):
   - Awakening usa il toggle `Auto` e scende da solo al moltiplicatore che i cristalli pagano (prima,
     sotto il costo del moltiplicatore più alto, si fermava; sopra faceva un risveglio per giro);
   - Tree of Life va oltre il livello 5 (prima era il tetto fisso del bot), a scaglioni di 5: rami
     prioritari (Raining Gold, Firestone Finder, Firestone Effect, Battle Cry, Miner) fino al
     prossimo multiplo di 5, poi tutti gli altri, poi lo scaglione dopo;
   - Daily Store Offers non riprova più ogni 30 minuti dopo un check-in reclamato tra le 10:00 e le
     10:02.

   **Solo se aggiorni dopo le 10:00 di un giorno in cui le istanze giravano con la versione
   vecchia:** al reset la versione vecchia leggeva le quest di ieri (già complete) e saltava quelle
   del giorno (l'1/10 sul PC principale: Collector su 9 istanze su 17, tutte e quattro su 2). A
   giochi ancora fermi, questo rimette in coda le quattro quest giornaliere su tutte le istanze.
   Una quest già fatta costa solo un controllo di 4 s: il bot la rilegge completa e la segna fatta.

   ```powershell
   $utf8 = New-Object System.Text.UTF8Encoding($false)
   foreach ($n in 17..34) {
     $rel = "Program Files (x86)\Steam-$n\steamapps\common\Firestone\UserData\FirebotPreferences.cfg"
     foreach ($f in "C:\$rel", "C:\Sandbox\$env:USERNAME\SteamB$n\drive\C\$rel") {
       if (-not (Test-Path $f)) { continue }
       $t = [IO.File]::ReadAllText($f)
       foreach ($s in 'collectorquesttask', 'gamerquesttask', 'merchantquesttask', 'minerquesttask') {
         $m = [regex]::Match($t, "(?s)\[$s\]\r?\n.*?(?=\r?\n\[|\z)")
         if (-not $m.Success) { continue }
         $new = $m.Value -replace '(?m)^(next_run_time_internal|last_done_date) = ".*"', '$1 = ""'
         $t = $t.Remove($m.Index, $m.Length).Insert($m.Index, $new)
       }
       [IO.File]::WriteAllText($f, $t, $utf8)
     }
   }
   ```
6. Riavvia in sequenza (`Avvia_Tutte_Istanze_Firestone.bat`, punto 6.3). Se un'istanza resta con
   la cella vuota (il client Steam gira ma `Firestone.exe` no, visto su Steam-14 il 30/09),
   rilanciare il gioco non basta: `Start.exe /box:SteamB<N> /terminate`, poi `Avvia_Steam-<N>.bat`.
7. Verifica, a istanze avviate: `apply_template.ps1 -From 17 -To 34 -Check` deve dire "0 valori da
   cambiare" senza chiavi mancanti (se trova differenze: ferma, rilancia il punto 5, riavvia), e il
   log di ogni istanza (`MelonLoader\Latest.log`, quello nel sandbox) deve contenere
   `Started. Enabled tasks: 39 of 42` (dal 4/10, con War Machines spento; 40 of 42 prima, 39 of 41
   prima del task della campagna, 38 of 40 prima dell'Emblem market) e nessun
   `timed out` o `threw:`.
8. Hall of Heroes (dal 30/09): parte nei primi minuti dopo l'avvio e il primo giro dura da 1,5 a
   4,5 minuti per istanza (legge tutti gli eroi, spende Void Crystal ed Ethereal Shards, sblocca i
   tier pagando in meteoriti). A giri finiti, da PowerShell:

   ```powershell
   foreach ($n in 17..34) {
     $log = "C:\Sandbox\$env:USERNAME\SteamB$n\drive\C\Program Files (x86)\Steam-$n\steamapps\common\Firestone\MelonLoader\Latest.log"
     $t = Get-Content $log
     "{0}: giri={1} sbloccati={2} FAILED={3} threw/timeout={4}" -f $n,
       @($t -match 'Hall Of Heroes finished').Count, @($t -match 'tier [23] unlocked').Count,
       @($t -match 'HallOfHeroesTask.*\[FAILED\]').Count, @($t -match 'threw|timed out').Count
   }
   ```

   (per un'istanza nativa il log è in `C:\Program Files (x86)\Steam-<N>\...`). Ogni istanza deve
   avere `giri=1` e `threw/timeout=0`. L'unico `[FAILED]` atteso è `The formation didn't read`, se
   il giro è partito mentre c'erano ancora i popup d'avvio: il bot usa la formazione dello snapshot
   e al giro dopo la rilegge. Qualunque altro `[FAILED]` di `HallOfHeroesTask`, e soprattutto
   `no unlock popup paid in Meteorites`, va guardato prima di andare avanti. Su Steam-0..16 (30/09):
   giri da 86 a 266 s e 67 tier sbloccati, nessun altro `[FAILED]`.

   Dal 3/10, alla lettura completa (una al giorno: entro 24 h dall'avvio), lo stesso task alza anche
   la rarità degli eroi con i contratti, prima quelli in formazione e poi gli altri, ogni passaggio
   che il gioco offre (Uncommon 30, Rare 70, Epic 150). Ogni eroe ha una riga: o `Hero rarity
   <eroe>: '<rarità attuale>', next '...' for '30' (contracts64), contracts N, '<requisiti>' -
   nothing bought.` (requisito in grigio `#BDBEBD` = non soddisfatto), oppure, per un acquisto,
   `Hero rarity <eroe>: 'Common - <eroe>' -> 'Uncommon - <eroe>', contracts A -> B.` con B = A meno
   il costo. Per vederle subito senza aspettare 24 h: a gioco fermo, nel cfg di un'istanza, sezione
   `[hallofheroestask]`, `hero_snapshot_time = ""` e `next_run_time_internal = ""`.

   ```powershell
   foreach ($n in 17..34) {
     $log = "C:\Sandbox\$env:USERNAME\SteamB$n\drive\C\Program Files (x86)\Steam-$n\steamapps\common\Firestone\MelonLoader\Latest.log"
     "{0}:" -f $n
     (Get-Content $log) -match 'Hero rarity' -replace '.*\[(INFO|FAILED)\] ', '    '
   }
   ```

   Un `[FAILED] Hero rarity` (contatore non sceso esattamente del costo) ferma la rarità per quel
   giro e va guardato. Dopo un passaggio il gioco apre per qualche secondo `menus/Achievements`
   (visto su Steam-0 sia per Cirilo sia per Boris): è normale, si chiude da solo.
9. Beasts, Guardian Evolution e War Machine Rarity (dal 30/09): partono nei primi minuti, 5-50 s
   l'uno, e spendono solo Soul Embers, Strange Dust e Tools. A giri finiti, da PowerShell:

   ```powershell
   foreach ($n in 17..34) {
     $log = "C:\Sandbox\$env:USERNAME\SteamB$n\drive\C\Program Files (x86)\Steam-$n\steamapps\common\Firestone\MelonLoader\Latest.log"
     $t = Get-Content $log
     "{0}: {1} | {2} | evoluzioni={3} rarita={4} | FAILED={5}" -f $n,
       (@($t -match 'Beasts: \d+ owned') -replace '.*\[INFO\] ', '' | Select-Object -Last 1),
       (@($t -match 'War machine rarity: ') -replace '.*\[INFO\] ', '' | Select-Object -Last 1),
       @($t -match 'Guardian \d evolved').Count, @($t -match 'rarity raised').Count,
       @($t -match '(BeastsTask|GuardianEvolutionTask|WarMachineRarityTask).*\[FAILED\]').Count
   }
   ```

   Ogni istanza deve avere una riga `Beasts: N owned ('N/30'), M level(s) bought` (N è il
   contatore del gioco: 0 se l'account non ha ancora bestie), una `War machine rarity: T Tools` con
   un numero (non -1) e `FAILED=0`. Evoluzioni e rarità compaiono solo dove c'è abbastanza dust o
   Tools: su Steam-0..16 il 30/09, 2 evoluzioni (300 Strange Dust) e 2 rarità (10.000 Tools). Sotto
   il livello 60 (Beasts) o 50 (War Machine Rarity) la riga manca perché il task non parte.
10. Sfide degli eventi (dal 30/09 sera, dettagli in `EVENTS_PLAN.md`): Decorated Heroes e i
    mini-eventi leggono le carte delle sfide, fanno quello che manca al livello in corso di quelle
    azionabili (né più né meno), riaprono e reclamano. Colpi al cristallo, giocate, forzieri e
    vendite partono solo dopo la quest giornaliera che usa la stessa risorsa. A giri finiti, da
    PowerShell:

    ```powershell
    foreach ($n in 17..34) {
      $log = "C:\Sandbox\$env:USERNAME\SteamB$n\drive\C\Program Files (x86)\Steam-$n\steamapps\common\Firestone\MelonLoader\Latest.log"
      $t = Get-Content $log
      "{0}: DH={1} Mini={2} azioni={3} claim={4} attese-quest={5} FAILED={6} threw/timeout={7}" -f $n,
        @($t -match 'Decorated Heroes Event finished').Count, @($t -match 'Events - Mini Event finished').Count,
        @($t -match '-> \w+, [1-9]\d* missing').Count,
        ((@($t -match 'ClaimAllChallenges: claimed [1-9]') -replace '.*claimed (\d+).*', '$1') | Measure-Object -Sum).Sum,
        @($t -match 'waits for today').Count,
        @($t -match '(EventTask|MiniEventTask|DecoratedHeroesEventTask).*\[FAILED\]').Count,
        @($t -match 'threw|timed out').Count
    }
    ```

    Ogni istanza deve avere `DH` e `Mini` almeno a 1 (il giro c'è anche se l'evento non è aperto
    all'account: scrive `Not open to this account`), `FAILED=0` e `threw/timeout=0`. Su Steam-0..16
    il 30/09 alle 16:15, con Decorated Heroes in corso (finisce il 02/10 verso le 10:00), quasi
    ogni account ha fatto 6 azioni in 3 giri: 3 illuminazioni (60 Strange Dust), 10 colpi al
    cristallo e 2 giocate, con 5-8 claim. Su Steam-15 la terza illuminazione ha trovato il bottone
    non cliccabile ed è riuscita al giro dopo (ora il log lo dice: `the button isn't clickable`).
    `attese-quest` sopra 0 vuol dire che la quest Gamer o
    Miner di oggi non è ancora finita (su Steam-11 mancavano i gettoni): l'evento riprova al giro
    dopo. Il dettaglio di un'istanza: `Select-String -Path $log -Pattern "Event '"`.
11. Quest giornaliere, Merchant e Arena (dall'1/10). A giri finiti (5-10 minuti dopo l'avvio), da
    PowerShell:

    ```powershell
    foreach ($n in 17..34) {
      $log = "C:\Sandbox\$env:USERNAME\SteamB$n\drive\C\Program Files (x86)\Steam-$n\steamapps\common\Firestone\MelonLoader\Latest.log"
      $t = Get-Content $log
      $q = foreach ($name in 'Collector', 'Gamer', 'Merchant', 'Miner') {
        $last = @($t -match "\] '$name' quest: ") | Select-Object -Last 1
        if ($last) { $last -replace ".*quest: ", "$name " } else { "$name -" }
      }
      "{0}: {1} | riletture={2} arena-uscite={3} threw/timeout={4}" -f $n, ($q -join '; '),
        @($t -match 'right after the reset').Count, @($t -match 'opponents not shown').Count,
        @($t -match 'threw|timed out').Count
    }
    ```

    - Ogni quest mostra l'ultima lettura (`Collector 0/4 -> 4/4.`) oppure `-` se in questa sessione
      non ha girato perché era già fatta. Merchant ora arriva a 10/10 anche senza Midas' Touch e
      Scroll di Health/Damage: finiti quelli, vende Totem of Annihilation, Totem of Agony, Guardian's
      Rune e Dragon Armor (`Merchant: sold 0/N`, poi `sold N/N`). Mai Drums of War, Scroll of Speed,
      gold istantaneo o barili. Gamer sotto 10/10 vuol dire gettoni finiti, come prima.
    - `riletture` sopra 0 si vede solo nei primi 15 minuti dopo le 10:00: una quest letta già
      completa subito dopo il reset viene riletta 5 minuti dopo (`right after the reset - read again
      later`), e la riga successiva della stessa quest deve essere una vera, tipo `0/4 -> 4/4`.
    - `arena-uscite` sopra 0 (solo account a livello 80) vuol dire che Arena of Kings ha trovato gli
      avversari nascosti ed è uscita invece di restare bloccata: il gettone rimasto riparte 6 ore dopo.
    - `threw/timeout` deve essere 0.

    Sul PC principale l'1/10 (Steam-1..16): Merchant da 7-9/10 a 10/10 su 5 istanze con la riserva,
    un'uscita di Arena su Steam-2, nessun errore.
12. Emblem market (dall'1/10 sera, task `[emblemmarkettask]`, solo account a livello 65 o più):
    nell'Exotic Merchant compra con gli Emblem of Courage solo il forziere gear più raro in vendita
    (oggi Epic) e con gli Emblem of Valor solo il jewel più raro (Golden; Diamond con 100 stelle della
    campagna), tutti i lotti che gli emblemi pagano, poi apre esattamente i forzieri comprati. Mai
    celestials, mai un forziere più economico. Parte nei primi minuti dopo l'avvio, poi ogni 6 ore.
    A giro fatto, da PowerShell:

    ```powershell
    foreach ($n in 17..34) {
      $log = "C:\Sandbox\$env:USERNAME\SteamB$n\drive\C\Program Files (x86)\Steam-$n\steamapps\common\Firestone\MelonLoader\Latest.log"
      $t = Get-Content $log
      "{0}: giri={1} FAILED={2} threw/timeout={3}" -f $n, @($t -match 'Emblem Market finished').Count,
        @($t -match 'EmblemMarketTask.*\[FAILED\]').Count, @($t -match 'threw|timed out').Count
      $t -match '\[INFO\] Emblem market' -replace '.*\[INFO\] ', '    '
    }
    ```

    Sugli account a livello 65 o più ci devono essere una riga `Emblem market gear: 'Epic chest'
    (rarity 3 Epic), 5000 each, emblems A -> B, N lot(s).`, una uguale per `jewels` (`'Golden
    chest' (rarity 2 Golden)`) e, per ogni categoria con almeno un lotto, `Emblem market: opened
    X/X '/Epic'` (o `'/Golden'`) con i due numeri uguali. B deve essere A meno 5.000 per lotto e
    restare sotto 5.000. `FAILED=0`. Sotto il livello 65 il task non parte e le righe mancano. Le
    righe `closing the New items popup` sono normali (un gioiello mai avuto). Sul PC principale
    l'1/10: Golden su Steam-1..15 (2-7 lotti), Epic su 7 di loro, nessun `[FAILED]`.
13. Campagna del Warfront (dal 3/10, task `[warfrontcampaigntask]`, solo account a livello 50 o
    più; dettagli in `CAMPAIGN_PLAN.md`): ogni 6 ore combatte le missioni della campagna che la
    battle power raggiunge, dalla potenza richiesta più bassa, fino alla prima sconfitta. Battaglie
    gratuite; una sconfitta si ritenta col 5% di potenza in più o dopo 24 h. Prima delle battaglie
    schiera la squadra più forte (tank davanti, healer in fondo), mette ogni eroe libero in una
    crew e sposta gli eroi sulla macchina della loro specializzazione (Tank sul tank e così via), se
    serve: è la stessa squadra dell'Arena. A giro fatto, da PowerShell:

    ```powershell
    foreach ($n in 17..34) {
      $log = "C:\Sandbox\$env:USERNAME\SteamB$n\drive\C\Program Files (x86)\Steam-$n\steamapps\common\Firestone\MelonLoader\Latest.log"
      $t = Get-Content $log
      "{0}: giri={1} FAILED={2} threw/timeout={3}" -f $n, @($t -match 'Warfront - Campaign finished').Count,
        @($t -match 'WarfrontCampaignTask.*\[FAILED\]').Count, @($t -match 'Campaign.*(threw|timed out)').Count
      $t -match '\[INFO\] Campaign' -replace '.*\[INFO\] ', '    '
    }
    ```

    Su ogni account a livello 50 o più: una riga `Campaign squad: Goliath Tank 2449 [Boris T, …], …;
    strongest: …; N heroes, S crew slots per machine, K crew move(s).` (T/D/H: la specializzazione
    dell'eroe); se `strongest:` non dice `the same` o K è sopra 0, anche `Campaign squad set: …;
    battle power A -> B.` con B più alta di A (sul PC principale, Steam-8: da una macchina a quattro,
    51.583 -> 99.430; Steam-0: due eroi spostati per specializzazione, 800.053 -> 808.914). Al giro
    dopo K deve essere 0. Un `[FAILED] Campaign squad: battle power dropped` dice che le crew salvate
    sono state rimesse. Poi `Campaign: stars …,
    battle power …, unlocked …; K defeat(s) waiting …, C to try.` e, per ogni battaglia, `Campaign
    mission 32 Easy: required …, power …, ratio … -> won/lost, stars A -> B.` (B = A + 1 a ogni
    `won`; dopo un `lost` il giro si ferma; dopo 10 battaglie riparte entro 30 minuti). `FAILED=0`.
    Sotto il livello 50 il task non parte e le righe mancano. Sul secondo PC il 3/10 alle 16:18-16:25
    (tutti e 18 a livello 50 o più): 18 squadre sistemate, quasi tutte da una macchina sola con la
    crew vuota (`heroes in crews 0 -> 4`, battle power da ~1.150 a 1.600-6.700), Steam-17..19 con
    tre macchine (13.290 -> 19.992); 143 battaglie, 133 vinte, `FAILED=0`. Le righe `[Watchdog]
    Closing popup: .../WorldMap/closeButton` dopo ogni battaglia sono del task (`ForceClearAll`),
    non un errore.
14. Tree of Life e Awakening (dal 4/10). Tree of Life gira ogni 6 ore e Awakening sul suo badge (al
    più ogni 30 minuti); per vederli subito, **a giochi fermi**, prima del riavvio del punto 6:

    ```powershell
    $utf8 = New-Object System.Text.UTF8Encoding($false)
    foreach ($n in 17..34) {
      $rel = "Program Files (x86)\Steam-$n\steamapps\common\Firestone\UserData\FirebotPreferences.cfg"
      foreach ($f in "C:\$rel", "C:\Sandbox\$env:USERNAME\SteamB$n\drive\C\$rel") {
        if (-not (Test-Path $f)) { continue }
        $t = [IO.File]::ReadAllText($f)
        foreach ($s in 'treeoflifetask', 'awakeningtask') {
          $m = [regex]::Match($t, "(?s)\[$s\]\r?\n.*?(?=\r?\n\[|\z)")
          if (-not $m.Success) { continue }
          $new = $m.Value -replace '(?m)^next_run_time_internal = ".*"', 'next_run_time_internal = ""'
          $t = $t.Remove($m.Index, $m.Length).Insert($m.Index, $new)
        }
        [IO.File]::WriteAllText($f, $t, $utf8)
      }
    }
    ```

    Poi, a giri fatti (5-10 minuti dopo l'avvio):

    ```powershell
    foreach ($n in 17..34) {
      $log = "C:\Sandbox\$env:USERNAME\SteamB$n\drive\C\Program Files (x86)\Steam-$n\steamapps\common\Firestone\MelonLoader\Latest.log"
      $t = Get-Content $log
      "{0}: albero={1} risvegli={2} warmachines={3} threw/timeout={4}" -f $n,
        @($t -match 'Tree Of Life finished').Count, @($t -match '\[INFO\] Awakening for').Count,
        @($t -match 'War Machines finished').Count, @($t -match 'threw|timed out').Count
      $t -match '\[INFO\] (Tree of Life|Awakening)' -replace '.*\[INFO\] ', '    '
    }
    ```

    - Tree of Life: righe `Tree of Life: RainingGold 5 -> 6.` (il nome è quello del gioco:
      `Prestigious` è Firestone Finder) e, a gettoni finiti, una `... at N costs more tokens than are
      left.`: lì il giro si ferma, i gettoni restano per quel nodo. Sotto il livello 5 si comprano
      prima i cinque rami prioritari fino a 5, poi gli altri. Un giro senza nessuna delle due righe
      vuol dire che ogni nodo è al tetto che il livello dell'albero permette. A fine giro c'è sempre
      `Tree of Life: tokens A -> B, N bought.`, il saldo dei gettoni spedizione prima e dopo: da un
      giro all'altro (ogni 6 ore) dice quanti gettoni entrano al giorno. `-1 -> -1` vuol dire che
      l'albero non si è aperto (succede al primo giro dopo l'avvio, coi popup): riprova fra 6 ore.
    - Awakening (solo account a livello 50 o più, e solo con almeno 500 cristalli): `Awakening for
      1.000 (auto): crystals 4604 -> 104.`, cioè tutti i cristalli in un giro; sotto i 500 nessuna
      riga, ed è giusto.
    - `warmachines` deve essere 0: il task è spento.
    - `threw/timeout` deve essere 0.

    Sul PC principale il 4/10: vedi le righe Tree of Life e Awakening di `TESTING.md`.

---

## 0. Specifiche del PC principale (per confronto)

- RAM: ~32 GB, 8 processori logici.
- Disco C: 232 GB totali.
- 17 istanze totali: **Steam-0 nativa** (senza sandbox) + **Steam-1..Steam-16 sandboxate**
  (Sandboxie-Plus, un box per istanza).
- Windows 10.
- .NET SDK: `10.0.401` (verifica con `dotnet --version`).
- MelonLoader: `v0.7.4-ci.2581 Open-Beta`, runtime `net6`, gioco Il2Cpp x64 (verifica aprendo
  un'istanza e controllando la prima riga di `MelonLoader\Latest.log`).
- Sandboxie-Plus: `1.18.4` (verifica con `(Get-Item "C:\Program Files\Sandboxie-Plus\SandMan.exe").VersionInfo.ProductVersion`).
- Steam AppID di Firestone: `1013320`.

Il secondo PC non deve necessariamente avere le stesse specifiche, ma più istanze girano
insieme più servono RAM/disco - vedi punto 6 (pagefile) per la formula.

---

## 1. Prerequisiti per OGNI istanza (17-34)

Ogni istanza è un'installazione Steam **completamente separata**, in una cartella dedicata:

```
C:\Program Files (x86)\Steam-<N>\
```

Per ciascuna, prima di qualunque cosa legata a Firebot:

1. **Installa Steam** in quella cartella (installazione pulita, non una copia di un'altra -
   Steam supporto nativamente installazioni multiple in cartelle diverse sulla stessa macchina).
2. **Fai login** con l'account Steam dedicato a quell'istanza (manuale, richiede le credenziali
   dell'account - non scriptabile in modo sicuro).
3. **Installa il gioco Firestone** (AppID `1013320`) tramite quel client Steam.
4. **Installa MelonLoader** (v0.7.4 Open-Beta, runtime net6, Il2Cpp) nella cartella del gioco
   di quell'istanza (`Steam-<N>\steamapps\common\Firestone\`). Avvia il gioco una volta con
   MelonLoader per fargli generare `MelonLoader\Il2CppAssemblies\` (servono per compilare il
   mod - vedi punto 4).
5. **Decidi se l'istanza sarà nativa o sandboxata.** Sul PC principale solo Steam-0 è nativa
   (probabilmente per comodità di test/debug); tutte le altre sono sandboxate. Per il nuovo
   intervallo 17-34 puoi tenerle **tutte sandboxate** (non c'è un vincolo tecnico che richieda
   un'istanza nativa) - scegli tu, ma sii coerente e documentalo.

---

## 2. Sandboxie-Plus (per le istanze sandboxate)

### 2.1 Installazione

Installa Sandboxie-Plus (stessa versione o compatibile: `1.18.4`) da
`https://sandboxie-plus.com`. Il file di configurazione centrale è:

```
C:\Windows\Sandboxie.ini
```

**Attenzione**: questo file è scrivibile SOLO da Administrators/SYSTEM (verificato con
`Get-Acl`). Qualunque script che lo tocca (compresi quelli sotto) va lanciato da PowerShell
**come Amministratore**.

### 2.2 Un box per istanza, nome = `SteamB<N>`

Convenzione: l'istanza `Steam-17` usa il box Sandboxie `SteamB17`, `Steam-18` -> `SteamB18`,
ecc. Ogni box, sul PC principale, ha questa struttura (esempio reale, `[SteamB1]`):

```ini
[SteamB1]
Enabled=y
BorderColor=#00FF00,ttl,6,192
ConfigLevel=10
AutoRecover=y
BlockNetworkFiles=y
Template=OpenSmartCard
Template=OpenBluetooth
Template=SkipHook
Template=FileCopy
Template=qWave
Template=BlockPorts
Template=LingerPrograms
Template=AutoRecoverIgnore
RecoverFolder=%{374DE290-123F-4565-9164-39C4925E467B}%
RecoverFolder=%Personal%
RecoverFolder=%Desktop%
BoxNameTitle=y
BoxAlias=Steam-1
```

Le ultime due righe (`BoxNameTitle`/`BoxAlias`) sono state aggiunte il 2026-09-26 per mostrare
il nome dell'istanza nel titolo della finestra (es. `[#] [Steam-5] Firestone [#]` invece del
generico `[#] Firestone [#]`) - impostazioni trovate nelle stringhe di `SbieDll.dll`, non
documentate esplicitamente altrove ma confermate funzionanti dal vivo.

`BorderColor` deve essere **diverso per ogni box** (puramente cosmetico, per distinguere le
finestre a colpo d'occhio) - qualunque colore esadecimale va bene, basta non ripeterlo.

### 2.3 Come crearli per il range 17-34

**Opzione automatica** (consigliata): usa lo script incluso in questo repo,
`tools/InstanceProvisioning/add_sandbox_boxes.ps1`. Clona le impostazioni di un box esistente
(default `SteamB1`, cambialo con `-TemplateBox`) e genera i box richiesti con `BorderColor` unico e
`BoxNameTitle`/`BoxAlias` già impostati. Fa un backup automatico di `Sandboxie.ini` prima di
scrivere ed è idempotente (salta i box già esistenti).

Sul secondo PC non esiste un `SteamB1` da clonare: crea prima a mano il box `SteamB17` dalla GUI,
aggiungi nella sua sezione di `Sandboxie.ini` le due righe `BoxNameTitle=y` e `BoxAlias=Steam-17`
(lo script non tocca il box template), poi genera gli altri a partire da quello:

```powershell
# Da PowerShell come Amministratore, con il repo già clonato/aggiornato:
cd C:\Repos\FirestoneBot
.\tools\InstanceProvisioning\add_sandbox_boxes.ps1 -TemplateBox SteamB17 -Start 18 -End 34
```

Dopo l'esecuzione, apri `SandMan.exe` (l'interfaccia di Sandboxie-Plus) e verifica che i box
`SteamB17`..`SteamB34` compaiano nell'elenco, abilitati.

**Opzione manuale**: crea ogni box dalla GUI di Sandboxie-Plus (tasto destro -> "Crea nuovo box"
o duplicando un box esistente), poi aggiungi a mano `BoxNameTitle=y` e `BoxAlias=Steam-<N>` a
ciascuna sezione in `Sandboxie.ini`.

---

## 3. Script di avvio/arresto (uno per istanza + versione "tutte insieme")

Sul PC principale vivono in:

```
C:\Users\Admin\Desktop\Firestone Bots\
```

Questa cartella **non fa parte del repo git** (è locale al PC) - va ricreata sul secondo PC.
Ogni istanza ha due file, `Avvia_Steam-<N>.bat` e `Ferma_Steam-<N>.bat`.

**Istanza sandboxata** (es. Steam-1 / SteamB1):

```bat
@echo off
setlocal
set APPID=1013320
set SANDBOXIE="C:\Program Files\Sandboxie-Plus\Start.exe"
echo Avvio istanza Firestone - Steam-1 (sandbox SteamB1)
%SANDBOXIE% /box:SteamB1 "C:\Program Files (x86)\Steam-1\steam.exe" -silent -applaunch %APPID%
pause
```

```bat
@echo off
setlocal
set SANDBOXIE="C:\Program Files\Sandboxie-Plus\Start.exe"
echo Arresto istanza Firestone - Steam-1 (sandbox SteamB1)
%SANDBOXIE% /box:SteamB1 /terminate
pause
```

**Istanza nativa** (es. Steam-0, se ne vuoi una senza sandbox anche nel nuovo range):

```bat
@echo off
setlocal
set APPID=1013320
echo Avvio istanza Firestone - Steam-0 (nativa)
start "" "C:\Program Files (x86)\Steam-0\steam.exe" -silent -applaunch %APPID%
pause
```

```bat
@echo off
setlocal
echo Arresto istanza Firestone - Steam-0 (nativa)
powershell -NoProfile -Command "Get-Process | Where-Object { $_.Path -like 'C:\Program Files (x86)\Steam-0\*' } | Stop-Process -Force -ErrorAction SilentlyContinue"
pause
```

Esiste anche una coppia "tutte insieme" (`Avvia_Tutte_Istanze_Firestone.bat` /
`Ferma_Tutte_Istanze_Firestone.bat`) che lancia ogni istanza in sequenza con una pausa tra una e
l'altra (20s nella versione attuale) invece che tutte insieme - importante per non saturare la
CPU/il commit di memoria in un colpo solo all'avvio (vedi punto 6).

**Genera tutto questo automaticamente** invece di copiare a mano 36 file, con:

```powershell
cd C:\Repos\FirestoneBot
.\tools\InstanceProvisioning\generate_instance_scripts.ps1 -Start 17 -End 34 -OutDir "C:\Users\Admin\Desktop\Firestone Bots"
```

(Aggiungi `-NativeInstances 17` se vuoi che una specifica istanza sia nativa invece che
sandboxata - per default lo script genera lo schema sandboxato per tutte.)

**Attenzione nota**: sul PC principale lo script "tutte insieme" ha un bug cosmetico - il
banner di testo dice "0-15" ma il codice in realtà copre 0-16; inoltre, se lanciato da un
ambiente non interattivo (es. un tool automatico), stampa un errore cosmetico
"ERRORE: il reindirizzamento dell'input non è supportato" per ogni istanza per via del comando
`pause` finale - l'errore è innocuo, gli avvii effettivi vanno comunque a buon fine. Se generi
uno script "tutte insieme" per 17-34, verifica che il range nel banner di testo sia corretto.

---

## 4. Repo Firebot: build e compilazione

### 4.1 Clona/aggiorna il repo

```powershell
git clone https://github.com/davide-mariotti/FirestoneBot.git C:\Repos\FirestoneBot
# oppure, se già clonato:
cd C:\Repos\FirestoneBot
git pull
```

### 4.2 `Directory.Build.props` (già nel repo, tracciato da git)

Il file `src/Directory.Build.props` **è già nel repo** (non serve ricrearlo) e definisce dove il
build va a cercare i riferimenti (MelonLoader, assembly Il2Cpp) in base a una variabile
d'ambiente `COMMON_DIR`:

```xml
<GameRoot Condition="'$(COMMON_DIR)' != ''">$(COMMON_DIR)\Firestone</GameRoot>
<MelonLoaderNetDir>$(GameRoot)\MelonLoader\net6</MelonLoaderNetDir>
<Il2CppAssembliesDir>$(GameRoot)\MelonLoader\Il2CppAssemblies</Il2CppAssembliesDir>
```

La build resta in `src\bin\` e non viene mai copiata da sola nella cartella `Mods` di un'istanza,
così una modifica a metà non può sovrascrivere il mod che gira sulle istanze. Il deploy è sempre
manuale (vedi punto 5).

### 4.3 Comando di build

Serve **un'istanza di riferimento già avviata almeno una volta con MelonLoader** (per avere
`Il2CppAssemblies` generato) - va bene una qualsiasi tra le 17-34, es. Steam-17:

```powershell
$env:COMMON_DIR = "C:\Program Files (x86)\Steam-17\steamapps\common"
dotnet build "C:\Repos\FirestoneBot\src\firebot.csproj" -c Release
```

Output atteso: `Compilazione completata. Errori: 0`. Produce:
- `src\bin\Release\net6.0\firebot.dll`
- `src\bin\Release\net6.0\Firebot.TalentEngine.dll` (progetto separato, motore albero talenti)

Se vuoi anche eseguire i test unitari dell'allocatore talenti (nessuna dipendenza dal gioco):

```powershell
dotnet test "C:\Repos\FirestoneBot\tests\Firebot.TalentEngine.Tests\Firebot.TalentEngine.Tests.csproj"
```

Atteso: `Superato! - Non superati: 0. Superati: 53.`

---

## 5. Deploy del mod su ogni istanza (manuale, sempre)

**Per ogni istanza sandboxata**, il dll va copiato in **DUE** posti - Sandboxie virtualizza
l'I/O dei file per-percorso: finché la sandbox non ha "toccato" un file scrivendoci sopra, legge
trasparentemente dal percorso reale, ma UNA VOLTA che lo fa (es. al primo avvio dopo aver
scritto qualcosa in quella cartella) il percorso sandbox diventa quello autoritativo per quel
processo. Quindi, sempre entrambi:

```
C:\Program Files (x86)\Steam-<N>\steamapps\common\Firestone\Mods\firebot.dll
C:\Sandbox\Admin\SteamB<N>\drive\C\Program Files (x86)\Steam-<N>\steamapps\common\Firestone\Mods\firebot.dll
```

(Il percorso sandbox usa il nome dell'utente Windows corrente al posto di "Admin" se diverso -
verifica con `dir C:\Sandbox\` quale cartella utente esiste.)

**IMPORTANTE**: `Firebot.TalentEngine.dll` va copiato **ANCHE LUI**, sempre negli stessi due
percorsi accanto a `firebot.dll` - dimenticarlo causa un crash immediato del task Talenti con
`FileNotFoundException` (già capitato il 2026-09-24).

**Per un'istanza nativa** (se ne crei una nel nuovo range), un solo percorso:

```
C:\Program Files (x86)\Steam-<N>\steamapps\common\Firestone\Mods\firebot.dll
C:\Program Files (x86)\Steam-<N>\steamapps\common\Firestone\Mods\Firebot.TalentEngine.dll
```

**Prima di copiare, ferma sempre l'istanza** (il dll è bloccato in memoria mentre il processo
gira - `Ferma_Steam-<N>.bat`), copia, poi riavvia (`Avvia_Steam-<N>.bat`).

Esempio via PowerShell/bash per un'istanza sandboxata:

```powershell
$src = "C:\Repos\FirestoneBot\src\bin\Release\net6.0"
$real = "C:\Program Files (x86)\Steam-17\steamapps\common\Firestone\Mods"
$sandbox = "C:\Sandbox\$env:USERNAME\SteamB17\drive\C\Program Files (x86)\Steam-17\steamapps\common\Firestone\Mods"
Copy-Item "$src\firebot.dll" $real
Copy-Item "$src\Firebot.TalentEngine.dll" $real
Copy-Item "$src\firebot.dll" $sandbox
Copy-Item "$src\Firebot.TalentEngine.dll" $sandbox
```

### 5.1 Verifica dopo ogni deploy

Nel log più recente dell'istanza (`MelonLoader\Logs\<ultimo>.log`, percorso sandbox se
applicabile):

- **Deve essere assente**: `FileNotFoundException` (= un dll manca).
- **Deve essere presente**: `Started. Enabled tasks: N of M loaded.` (= il mod è partito).

---

## 6. Windows: pagefile fisso (CRITICO per molte istanze insieme)

### 6.1 Il problema (diagnosi 2026-09-26, PC principale)

Con 17 Steam + 17 Firestone attivi insieme, il PC principale ha avuto **blocchi totali**
ripetuti (schermo bloccato, `dwm.exe` crasha con `c00001ad`, `steamwebhelper.exe`/VS Code
muoiono con `0xE0000008` = OOM, evento 2004 "memoria virtuale insufficiente" nel log di sistema
ogni 5 minuti). La causa NON era la RAM fisica ma il **commit limit di Windows** (RAM +
pagefile): con pagefile gestito automaticamente, Windows lo limita a 1/8 del volume (su un disco
C: da 232 GB = ~29 GB → commit limit totale ~61 GB), mentre 17+17 processi ne richiedevano
~65 GB.

### 6.2 La soluzione

`tools/Pagefile/set_pagefile.ps1` (già nel repo) imposta un **pagefile fisso da 56 GB**
(iniziale = massimo, niente crescita dinamica) → commit limit ~88 GB. Verificato dal vivo il
2026-09-26: dopo applicarlo e riavviare il PC, tutte e 17 le istanze sono ripartite insieme
senza blocchi anche con soli ~2 GB di RAM "libera" apparente.

```powershell
# Da PowerShell come Amministratore:
cd C:\Repos\FirestoneBot
.\tools\Pagefile\set_pagefile.ps1
# poi RIAVVIA il PC (obbligatorio, il pagefile non cambia a caldo)
```

**Formula per adattare la dimensione** se il secondo PC ha un numero di istanze diverso o un
disco diverso: ogni coppia Steam+Firestone attiva contemporaneamente ha bisogno di circa
~3.8 GB di commit aggiuntivo (stima empirica: Firestone.exe ~2.4 GB + quota-parte di
steamwebhelper.exe). Per 18 istanze (17-34): ~68 GB di commit extra necessario oltre alla RAM
fisica del PC - imposta `-SizeMB` di conseguenza (default script: `57344` = 56 GB, passa un
valore diverso con `.\set_pagefile.ps1 -SizeMB <N>` se serve più margine). Lascia sempre almeno
~10 GB di spazio libero su disco oltre al pagefile.

Verifica dopo il riavvio:

```powershell
(Get-CimInstance Win32_OperatingSystem).TotalVirtualMemorySize / 1MB   # atteso ~RAM+pagefile, es. ~88 GB
Get-CimInstance Win32_PageFileSetting | Select-Object Name, InitialSize, MaximumSize
```

### 6.3 Avvio: sempre in sequenza, mai tutto insieme

Anche con il pagefile risolto, avvia le istanze **una alla volta con una pausa** (5-20 secondi
tra una e l'altra), mai tutte in un colpo solo - riduce il picco di CPU/disco durante il boot di
ognuna. Gli script "tutte insieme" (punto 3) lo fanno già di default.

---

## 7. Trim di steamwebhelper (pianificato, solo a RAM piena)

`tools/SteamTrim/trim_steam.ps1` svuota il working set dei `steamwebhelper.exe` (uno per istanza
Steam), ma solo quando la RAM fisica occupata arriva al 90%. Il resto del tempo non fa niente. Non
tocca mai `steam.exe` o `Firestone.exe`, salta il processo GPU di steamwebhelper e quelli sotto i
150 MB. Non sostituisce il pagefile del punto 6: libera RAM fisica, il commit limit resta quello.

```powershell
# Una volta per PC, senza Amministratore: attività pianificata FirebotSteamTrim, ogni 10 minuti
powershell -NoProfile -ExecutionPolicy Bypass -File C:\Repos\FirestoneBot\tools\SteamTrim\trim_steam.ps1 -Install
# Soglia diversa: aggiungi -MinRamPercent 85. Rimozione:
schtasks /Delete /TN FirebotSteamTrim /F
```

Ogni trim eseguito aggiunge una riga a `%TEMP%\trim_steam.log`. Lanciato a mano (senza
`-Install`), fa un giro subito, se la RAM è sopra soglia.

Il vecchio trim, più aggressivo (tutti i processi "steam", ogni 20 minuti, attività `PCRamTrim`),
è stato tolto dal repo: il 25/09 aveva fatto crashare steamwebhelper e Firestone. Se dopo aver
installato questo i crash tornano, è il primo sospettato. Su un PC dove erano stati usati gli script
vecchi:

```powershell
# L'attività pianificata del vecchio trim (ora punta a uno script che non c'è più):
schtasks /Delete /TN "PCRamTrim" /F
# Se era stato lanciato 3_disable_sysmain.ps1, per tornare al default di Windows (da Amministratore):
Set-Service SysMain -StartupType Automatic; Start-Service SysMain
```

---

## 8. Configurazione del bot per-istanza (`FirebotPreferences.cfg`)

Ogni istanza genera il proprio file al primo avvio:

```
<Steam-N>\steamapps\common\Firestone\UserData\FirebotPreferences.cfg
```

(percorso sandbox per le istanze sandboxate, come sempre). Contiene una sezione per ogni task
del bot. Un file nuovo nasce con i default del codice, un file già esistente tiene i valori che ha:
in entrambi i casi va poi allineato a Steam-0 con lo script del punto 8.1. Un task sotto il suo
livello di sblocco resta fermo da solo.

Impostazioni globali del bot stesso (non per-task) sono nella sezione `[firebot_settings]` -
tra queste, `low_resource_mode = true` (default) che il codice applica automaticamente
(nessun intervento manuale necessario): abbassa qualità grafica, blocca il framerate a 15fps e
riduce la finestra a 640x480 per ogni istanza, per ridurre il carico CPU/GPU con molte istanze
insieme - già nel codice (`src/Core/BotSettings.cs`), arriva con il `git pull`.

### 8.1 Template di configurazione base

`tools/ConfigTemplate/FirebotPreferences.template.cfg` (nel repo) è la configurazione di Steam-0
(allineata il 2026-09-29; elenco completo in `TESTING.md`, "Configurazione di riferimento"): quali
task sono accesi e con quali parametri. Le righe marcate `(auto-managed, don't edit)` sono vuote
apposta: sono lo stato di avanzamento di ogni account, che il bot scrive da solo.

Non si copia a mano: `tools/ConfigTemplate/apply_template.ps1` porta al template ogni istanza di
un intervallo, nel file reale e in quello del sandbox, lasciando lo stato dell'account e il suo
`talentstask.guide_start_index`, e mettendo `window_grid_first_instance` al primo numero
dell'intervallo (vedi 8.2). Scrive in UTF-8 senza BOM e si rifiuta di scrivere se un gioco
dell'intervallo è aperto. Sequenza, dopo il deploy della DLL (punto 5):

1. Avvia ogni istanza una volta e aspetta `Started.` nel log: la DLL nuova aggiunge al cfg le
   sezioni che mancano.
2. Chiudi tutte le istanze.
3. Da PowerShell, nella cartella del repo:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\ConfigTemplate\apply_template.ps1 -From 17 -To 34 -Check  # solo confronto
   powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\ConfigTemplate\apply_template.ps1 -From 17 -To 34         # allinea
   powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\ConfigTemplate\apply_template.ps1 -From 17 -To 34 -Check  # "0 valori da cambiare"
   ```

   `-ExecutionPolicy Bypass` serve perché di norma Windows non esegue script `.ps1` scaricati.

   Se segnala "chiavi mancanti", quell'istanza non ha ancora girato con la DLL nuova: torna al
   passo 1 per lei.
4. Riavvia le istanze in sequenza (punto 6.3). Nel log di ognuna, `Started. Enabled tasks: 40 of 42`
   come su Steam-0.

### 8.2 Griglia delle finestre (monitor 2560x1440)

Con `window_grid_enabled = true` il mod mette la finestra di ogni istanza in una cella fissa di
una griglia, ricavata dal numero `Steam-N` nel percorso. Il passo della griglia è la finestra
**visibile** (area di gioco + barra del titolo + bordo, misurata dal vivo), quindi le righe non si
coprono più la barra grigia a vicenda. Valori verificati il 2026-09-26 con 18 istanze (17-34):
nessuna sovrapposizione, 4 righe che finiscono esattamente sopra la taskbar, ~30 px liberi a
destra.

Da impostare in `[firebot_settings]` di **ogni** istanza, a gioco chiuso (il gioco riscrive il
file quando si chiude) e, per le istanze sandboxate, sia nel file reale sia in quello dentro il
box:

| Chiave | PC principale (Steam-0..16) | Secondo PC (Steam-17..34) |
|---|---|---|
| `low_resource_mode` | `true` | `true` |
| `window_grid_enabled` | `true` | `true` |
| `window_grid_columns` | `5` | `5` |
| `window_width` | `504` | `504` |
| `window_height` | `316` | `316` |
| `window_grid_first_instance` | `0` (default) | `17` |

`window_grid_first_instance` è il numero dell'istanza che va in alto a sinistra: senza, sul
secondo PC Steam-17 finirebbe alla quarta riga, cioè fuori dallo schermo. Con 504x316 ogni
finestra visibile è 506x348: 5 colonne = 2530 px, 4 righe = 1392 px (altezza utile sopra la
taskbar), quindi bastano per 20 istanze. Con i vecchi 512x384 le finestre di una riga coprivano la
barra del titolo di quella sopra e la quarta riga usciva dallo schermo.

Sul secondo PC anche la console di MelonLoader è nascosta, con `hide_console = true` nella sezione
`[console]` di `UserData\Loader.cfg` di ogni istanza. Il debug del bot era spento
(`debug_mode = false`); dal 2026-09-29 il fleet segue Steam-0 e `apply_template.ps1` lo accende
(`true`): le righe `[DEBUG]` servono per leggere i log di un test su larga scala.

---

## 9. Bug già risolti nel codice (arrivano automaticamente col `git pull`, nessuna azione)

Questi sono già fissati nel repo - il secondo PC li ottiene semplicemente aggiornando e
ricompilando, elencati qui solo per completezza/consapevolezza:

- **Talent Tree**: motore di allocazione punti talento a priorità configurabile (prima non
  esisteva / era disabilitato ovunque).
- **WorldMap.Open più robusto**: click con retry/simulazione invece di un click semplice, stesso
  pattern già usato altrove per problemi di "click che non fa nulla".
- **Quest giornaliere - bug data di reset**: 4 task (Collector/Gamer/Merchant/Miner Quest)
  usavano la data di calendario (mezzanotte) invece del vero reset di gioco (10:00 locali) per
  tracciare "già fatto oggi" - causava quest bloccate a 0 dopo il reset reale. Risolto con
  `src/Utilities/GameDay.cs`.
- **Allineamento alla guida F2P** (2026-09-28): nuovi default e logiche di spesa più prudenti -
  l'elenco, con cosa riverificare, è in `TESTING.md`.
- **Sfide degli eventi** (2026-09-30): Decorated Heroes e un task unico per tutti i mini-eventi
  completano le sfide azionabili prima di reclamarle - vedi `EVENTS_PLAN.md` e il punto 10 della
  procedura di aggiornamento.
- **Reset delle 10:00, Arena e Merchant** (2026-10-01): le quest lette già complete subito dopo
  il reset vengono rilette (prima saltavano fino al giorno dopo); Arena of Kings esce se gli
  avversari restano nascosti (prima poteva bloccare il bot per un'ora); la quest Merchant si
  completa con i potenziamenti da battaglia quando finiscono gli oggetti ammessi. Controlli nel
  punto 11 della procedura di aggiornamento.
- **Emblem market e apertura dei forzieri** (2026-10-01 sera): task nuovo che spende gli emblemi nel
  forziere più raro di gear e jewels e apre i forzieri comprati (punto 12 della procedura di
  aggiornamento; chiave nuova `emblemmarkettask.enabled`, accesa). L'apertura dei forzieri, comune a
  Collector, ora chiude il popup "New items" che prima la fermava a metà (sul PC principale 9
  aperture di Golden su 15).

---

## 10. Checklist finale di verifica (dopo aver seguito tutti i punti sopra)

Per OGNI istanza 17-34, in ordine:

- [ ] Steam installato in `Steam-<N>\`, login effettuato, Firestone installato.
- [ ] MelonLoader installato nel gioco, avviato almeno una volta (genera `Il2CppAssemblies`).
- [ ] Box Sandboxie `SteamB<N>` esistente e abilitato (se sandboxata), con `BoxNameTitle=y` e
      `BoxAlias=Steam-<N>` impostati.
- [ ] `Avvia_Steam-<N>.bat` / `Ferma_Steam-<N>.bat` presenti e funzionanti.
- [ ] `firebot.dll` + `Firebot.TalentEngine.dll` presenti in **entrambi** i percorsi (reale +
      sandbox) di `Mods\`.
- [ ] Log più recente: nessun `FileNotFoundException`, presente `Started. Enabled tasks:`.
- [ ] Titolo finestra mostra `[Steam-<N>]` (conferma che Sandboxie applica la config).
- [ ] `FirebotPreferences.cfg` allineato a Steam-0: `apply_template.ps1 -From 17 -To 34 -Check`
      dice "0 valori da cambiare", e il log dice `Started. Enabled tasks: 40 of 42`.

A livello di sistema (una tantum, non per-istanza):

- [ ] `dotnet --version` disponibile, build del mod riuscita senza errori.
- [ ] Pagefile fisso impostato (punto 6), PC riavviato dopo, verificato con
      `Win32_PageFileSetting`.
- [ ] Test con TUTTE le istanze avviate insieme in sequenza (non tutte in un colpo) - nessun
      blocco, memoria libera verificata con `Get-CimInstance Win32_OperatingSystem`.
