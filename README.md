# Firebot

Automation bot for Firestone Idle RPG, focused on automating repetitive tasks through MelonLoader.

Firebot runs in the background and supports any game language.

## Quick Start

1. Install [MelonLoader V0.7.2+](https://github.com/LavaGang/MelonLoader/releases/latest) in `Firestone.exe`.
2. Download the latest Firebot release and extract it into the Firestone root folder.
3. Launch the game and press `F7` to toggle Firebot.

---

## About

This project is a mod for Firestone Idle RPG using [MelonLoader](https://github.com/LavaGang/MelonLoader).

> **Note:** Firebot is currently supported only on **Windows**. It works with Firestone installations from **Steam** and **Epic Games**. The mod works in **any game language**, at **any resolution**, and can run in the **background**.

## Disclaimer: Not a Cheat

Firebot **is not a cheat**. It does not modify game resources, grant unfair advantages, interfere with server logic, or alter game files. The bot only automates actions that a player could perform manually, without bypassing any security or protection mechanisms of the game.

> **Transparency:** Firebot is open source, and its code is publicly available for review and audit.

---

## Features

Every automation below runs on its own schedule once enabled, is individually configurable, and shows up as its own section in `FirebotPreferences.cfg` - see [Configuration](#configuration).

- **Easy Start/Stop**: Turn the bot on or off during gameplay with a hotkey (default `F7`, `shortcut_key`). You can also auto-start and adjust bot timings with `auto_start`, `start_bot_delay`, `scan_interval`, `interaction_delay`, and `max_task_runtime`.
- **Low Resource Mode**: Caps the frame rate and forces the lowest graphics quality at startup (`low_resource_mode`, `target_frame_rate`, `render_quality_level`) - the bot reads game state directly, not rendered pixels, so this only affects CPU/GPU load, not bot behavior. On by default; matters most when running several instances on the same machine.
- **Free Speedups**: Uses free speedups (no gems) whenever a timer is close enough to finish, based on `free_speedup_seconds` - applies across research, missions, experiments, and map reset timers.

**In battle**

- **Hero Upgrade**: Upgrades the leader and every hero slot during battle. Starts and stops together with the main bot; optionally target only specific slots.
- **AutoRetreat**: When the current battle stage stops advancing for a while (a difficulty wall), steps back a few stages to keep farming quickly until the next Temple of Eternals reset/empower.
- **Flying Bonus Hunter**: Taps the flying dragon-with-beer and meteorite-hunter bonuses (4 variants) when they cross the battle screen - polls far more often than other background checks (`poll_seconds`, default 2) since these are only visible for a few seconds.

**Daily quests** (6 of the 9 in-game daily quests, automated end to end)

- **Collector**: Opens chests (gear/jewel/celestial), keeping a configurable reserve of common ones.
- **Gamer**: Plays Tavern card draws with Game Tokens, topping up from Beer automatically when running low.
- **Merchant**: Sells eligible items and upgrades at the Exotic Merchant.
- **Miner**: Hits the Arcane Crystal at the Guild.
- **Liberator**: Fights and waits out real Warfront liberation battles.
- Daily/weekly quest reward claiming for every completed quest, including the 3 quests (Trainer/Conqueror/Expeditioner) that complete themselves via the automations below.

**Town buildings**

- **Engineer**: Collects ready tools, and levels up every owned War Machine in the Workshop.
- **Guardian Training**: Starts training in Magic Quarters, with guardian selection and optional Strange Dust usage.
- **Alchemist**: Runs experiments, optionally focused on specific resources.
- **Oracle Rituals**: Collects completed rituals and starts new ones.
- **Library Research**: Starts and collects both Firestone and Meteorite research, always prioritizing "Raining Gold" when it's an available option.
- **Temple of Eternals (Empower)**: Resets/prestiges once your Firestone find ratio and adventure time both clear their configured thresholds.
- **Free Pickaxes**: Claims free pickaxes once your preferred quantity is banked.
- **Scarab's Game**: Claims the daily free shop gift, spins the slot with free Noble Tokens, and opens Pharaoh's Vault once enough Ancient Coins are banked.
- **Hall of Heroes**: For every hero, unlocks gear tiers and enchants gear/jewels - gear tier 2/3 and all jewels for every hero (their bonuses are account-wide), gear tier 1 only for heroes in your active battle formation (it only benefits the hero wearing it).

**Map & Warfront**

- **Map Missions**: Collects finished missions and starts new ones, in your preferred time order (`asc`/`desc`).
- **Warfront Campaign & Daily Missions**: Collects campaign loot and fights daily/liberation missions.
- **Arena of Kings**: Fights the weakest of the opponents shown each reroll, widening its power tolerance the longer a token goes unused so it's never wasted.

**Guild**

- **Expeditions**: Finishes and restarts expeditions automatically.
- **Tree of Life (Personal)**: Buys Personal Tree upgrades, prioritizing Raining Gold / Firestone Finder / Firestone Effect.
- **Awakening**: Spends Arcane Crystals on hero leveling, always using the biggest multiplier you can currently afford.
- **Chaos Rift**: Auto-attacks the shared boss, then spends earned Dark Rune on Tomes of Power only.
- **Forbidden Knowledge**: Upgrades every affordable node across all 3 god boards, then recruits once maxed.
- **Guardian Holy Upgrade**: Spends Orbs of Light on each guardian's holy damage upgrade.

**Character**

- **Talents**: Spends talent points following a curated investment order.
- **Path of Glory (Battle Pass)**: Claims both free and (if owned) Golden track rewards.

---

## Downloads

- **MelonLoader**: <https://github.com/LavaGang/MelonLoader/releases/latest>
- **Latest Firebot Release**: <https://github.com/davide-mariotti/firestoneBot/releases/latest>

---

## How to Use (Prebuilt Release)

If you want to use the pre-built mod (no manual compilation), follow this step-by-step guide.

### 1) Install MelonLoader (Required)

1. Download [MelonLoader V0.7.2+](https://github.com/LavaGang/MelonLoader/releases/latest).
2. Run the MelonLoader installer.
3. In the installer, keep **Enable Nightly builds** checked and select a `0.7.2-ci` (or newer) version.
4. When asked for the game executable, select your `Firestone.exe` file (inside your Firestone install folder).
5. Finish installation and wait until the installer confirms success.

<p align="center">
   <img src="docs/molonloader-a.png" alt="MelonLoader installer - game selection" width="40%" />
   <img src="docs/melonloader-b.png" alt="MelonLoader installer - Enable Nightly builds" width="40%" />
</p>

<p align="center">
   <sub>Left: game selection in installer | Right: keep <strong>Enable Nightly builds</strong> checked</sub>
</p>

Quick check: after installation, the game folder should contain MelonLoader-related files/folders (for example `MelonLoader`).

### 2) Install Firebot Files (Required)

1. Download the latest Firebot package from [Releases](https://github.com/davide-mariotti/firestoneBot/releases/latest) (example: `v0.2.7-alpha.zip`).
2. Extract the zip contents into the Firestone root folder (same folder as `Firestone.exe`).
3. Allow overwrite if Windows asks.

The zip already includes the correct structure (`Mods`, `UserData`).

### 3) First Launch (Required)

1. Start Firestone normally (through **Steam** or **Epic Games**).
2. Wait for the game to fully load.
3. Press **F7** to toggle Firebot on/off.

### 4) How to Update Firebot (When Needed)

When a new Firebot version is released, you do not need to reinstall everything.

1. **Close the game completely**.
2. Download the new release package from [Releases](https://github.com/davide-mariotti/firestoneBot/releases/latest).
3. Replace only this file in your game folder: `Mods/firebot.dll`.
4. Start the game once so Firebot can load the new version.

If the new version includes additional configuration options, they will be added automatically to your existing `FirebotPreferences.cfg` on the first execution.

When you want to adjust these options, follow the safe configuration flow in section `5) Configure Firebot correctly`.

### 5) Configure Firebot Correctly (Required)

The configuration file is:

`Firestone/UserData/FirebotPreferences.cfg`

To ensure your configuration is applied safely, always use this sequence:

1. **Close the game completely**.
2. Edit `FirebotPreferences.cfg`.
3. Save the file.
4. Open the game again.

If you change settings while the game is open, the new values may not be applied correctly.

### 6) Troubleshooting with MelonLoader Logs (Optional)

- Main runtime log: `Firestone/MelonLoader/Latest.log`
- Use this log if Firebot does not load, does not start with `F7`, or behaves unexpectedly.
- In most cases, checking this file is the fastest way to identify installation or configuration issues.

### Example Bot Info Log

<p align="center">
   <img src="docs/bot-log-example.png" alt="Bot Info Log Example" width="90%" />
</p>

### 7) If the Game Updates and Firebot Stops Working

Use the methods below only if the game changes version and Firebot stops working.

#### Method 1: Assembly Cache Cleanup

1. Close the game completely.
2. Go to your game folder and delete all contents inside `MelonLoader/Il2CppAssemblies`.
3. In `MelonLoader/Dependencies/Il2CppAssemblyGenerator`, keep only:
   - `Il2CppAssemblyGenerator.deps.json`
   - `Il2CppAssemblyGenerator.dll`
4. Delete `MelonLoader/Dependencies/AssemblyUnhollower` (if it exists).
5. Start the game again.
6. Replace only `Mods/firebot.dll` with the latest version.

#### Method 2: Reinstall MelonLoader

1. Close the game completely.
2. Delete the `MelonLoader` folder from the game root.
3. Reinstall MelonLoader as described in this README.
4. Replace only `Mods/firebot.dll` with the latest version.
5. Start the game again.

Important: no matter which method you choose, always replace only `Mods/firebot.dll` to update Firebot.
If Firebot is working normally, do not run these recovery methods.

---

## Configuration

Configuration reference file: `Firestone/UserData/FirebotPreferences.cfg`.

For the safe editing workflow (close game -> edit -> reopen), follow section `5) Configure Firebot correctly`.

All configuration is done directly in this file (no in-game UI yet - see [Open Points](#open-points)).

The file is generated the first time Firebot runs, then grows automatically as new features are added - no need to write it by hand. It's organized as one section per feature (e.g. `[map_missions]`, `[hero_upgrade]`), each with its own `enabled` toggle plus whatever settings that feature exposes (a time order, a resource filter, a claim threshold, and so on). Every setting is documented with a comment directly above it in the generated file, including its default value and valid range - that's the authoritative reference, since it's always in sync with the version of Firebot you're running.

Top-level settings that apply to the whole bot, always under `[firebot_settings]`:

```toml
[firebot_settings]
# Determines if the bot logic should be initialized and started automatically upon game launch.
auto_start = false
# The initial cooldown (in seconds) before the bot begins execution.
# Useful for preventing conflicts while Unity is still loading the initial scene.
# Clamped between 10.0 and 120.0 seconds.
start_bot_delay = 10.0
# The interval (in seconds) between each BotManager verification cycle.
# Lower values make the bot more responsive but may impact FPS performance.
# Clamped between 5.0 and 3600.0 seconds.
scan_interval = 5.0
# The delay (in seconds) between individual UI interactions (clicks, transitions).
# Ensures the game processes the command before the next action is taken. 
# Clamped between 0.5 and 5.0 seconds.
interaction_delay = 1.0
# Maximum time (in seconds) a single task is allowed to run before it is aborted.
# Clamped between 10.0 and 3600.0 seconds.
max_task_runtime = 120.0
# Enables verbose logging and StackTrace display in the console for easier bug identification.
debug_mode = false
# The physical key used to manually toggle the bot's execution state during gameplay.
shortcut_key = "F7"
# Some timers in the game can be sped up for free if the remaining time is below this threshold (default: 170 seconds = 2 minutes and 50 seconds). The maximum allowed value is 180 seconds (3 minutes). Set to 0 to disable free speedup. Adjust this value to account for lag or future game changes. Affects firestone researches, missions, experiments, and map reset timers. If the remaining time is less than or equal to this value, the speedup is free (no gems required).
free_speedup_seconds = 170.0
# When enabled, caps the game's frame rate and forces the lowest graphics quality level at startup. The bot reads game state directly from the Unity scene hierarchy, not from rendered pixels, so visual quality/frame rate have no effect on bot functionality - only on CPU/GPU load. Recommended when running several simultaneous instances on the same machine.
low_resource_mode = true
# Frame rate cap applied when low_resource_mode is enabled. Clamped between 5 and 60. Default: 15.
target_frame_rate = 15
# Unity quality level index applied when low_resource_mode is enabled (0 = lowest/fastest). Clamped between 0 and 5. Default: 0.
render_quality_level = 0
```

Every other feature is disabled by default (`enabled = false`) until you turn it on in its own section.

---

## Installation (From Source)

1. Clone the repository:

   ```bash
   git clone https://github.com/davide-mariotti/firestoneBot.git
   ```

2. Navigate to the project directory:

   ```bash
   cd firestoneBot
   ```

3. Configure the path to your Firestone Idle RPG game directory by editing the `src/Directory.Build.props` file if needed:
    - By default, the path is set to `C:\Program Files (x86)\Steam\steamapps\common\Firestone` (Windows, standard Steam library location). If your game is installed elsewhere (a custom Steam library folder, or Epic Games), change the `<GameRoot>` property in this file to the correct path.
    - You can also set the environment variable `COMMON_DIR` to override the base directory instead of editing the file. In this case, the game path will be `$(COMMON_DIR)\Firestone`. This is the easiest way to target a specific install when you have multiple side-by-side Steam installations (e.g. `Steam-0`, `Steam-1`, `Steam-2`, each with its own account/library) - just point `COMMON_DIR` at the one you want to build for.
      - Example (custom Steam library on a different drive):

          ```xml
          <GameRoot>D:\SteamLibrary\steamapps\common\Firestone</GameRoot>
          ```

      - Example (numbered Steam installs, PowerShell):

          ```powershell
          $env:COMMON_DIR = "C:\Program Files (x86)\Steam-0\steamapps\common"
          dotnet build src/firebot.csproj -c Debug
          ```

4. Build the project using your preferred method (e.g., Visual Studio, `dotnet build src/firebot.csproj -c Debug`).
    - On a successful build, `firebot.dll` is copied automatically into `dist/` at the repo root - never directly into `<GameRoot>\Mods`, so an in-progress build can't silently replace whatever's currently running live. Copy `dist/firebot.dll` into `<GameRoot>\Mods` by hand once you're ready to run it.

---

## Contributing

Contributions are welcome! Please submit a pull request or open an issue for suggestions or improvements.

## Open Points

- **In-game configuration UI**: all settings are edited directly in `FirebotPreferences.cfg` today; a graphical, in-game configuration screen (no `.cfg` editing needed) is not built yet.
- **Daily Rewards & Value Bundles**: the Store's daily check-in and free mystery box are not currently claimed - the HUD button that opens the Store doesn't respond to any click method this bot supports.
- **Soulstones** (Hall of Heroes, unlocks at character level 200): tier-unlock and enchanting are structurally present in the game but intentionally out of scope for now.
- **War Machines bulk multiplier**: leveling always uses single clicks; the screen's bulk-quantity button exists but isn't wired up yet, so leveling several times takes more clicks than strictly necessary.
- **Talents**: implemented but disabled by default pending further validation on accounts with pre-existing manual investment.

---

## Bug Reporting & Feature Requests

Found a bug or have an idea for a new feature? Please open a ticket on our GitHub Issue Tracker!

**Before submitting a bug report:**

1. Check if the issue has already been reported.
2. Ensure you are using the latest version of Firebot.
3. Attach your **MelonLoader Log** file (`MelonLoader/Latest.log`) if the game crashed or the bot failed.

[**Open a New Issue**](https://github.com/davide-mariotti/firestoneBot/issues/new/choose)

---

## Technical Details

- Firebot is implemented as a mod using MelonLoader, enabling automation of repetitive tasks within the game client itself.
- All actions performed by the bot simulate clicks and commands that a user would normally do, without modifying server data or circumventing security systems.
- The code is open source and auditable, ensuring transparency about its operation.
