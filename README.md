# Live Stats Overlay

A BepInEx mod for [Sineus Arena](https://store.steampowered.com/) that adds a live, customizable stats panel styled to match the game's own UI.

Docked directly underneath the "Find the boss's lair" objective banner, it shows things the game's own ESC-menu screen doesn't: DPS, kills/minute, combo damage, current HP, match timer, and more - fully customizable via a `[token]` template, LookingGlass-style. Also shows your remaining Rerolls/Skips/Bans on the "Make a choice" screen.

## Features

- DPS (rolling 10s average), kills/minute, current combo damage
- Current/max HP with a color that shifts as you take damage
- Match timer, kills, total damage, and your hero's live stats
- Toggleable Weapons / Passives / Items sections
- Fully customizable `[token]` template with rich text support
- Values color-coded by category (damage, healing, utility, etc.)
- Docks natively under the game's own objective banner
- Rerolls/Skips/Bans counter on the "Make a choice" screen
- Optional integration with [DpsMeter](https://github.com/Snack-tacular/DPSMeter) (used with permission): if both mods are installed, Live Stats automatically docks directly above the DpsMeter window instead of the objective banner. Fully optional and toggleable in settings - LiveStatsOverlay works exactly as before if DpsMeter isn't installed.

## Install

1. Install [BepInEx 5.4.23.3 (Mono/x64)](https://github.com/BepInEx/BepInEx/releases) if you haven't already.
2. Grab the latest release from the [Releases page](../../releases) and extract the `BepInEx` folder into your Sineus Arena install directory, merging with your existing `BepInEx` folder.
3. Launch the game through Steam.

Requires [SineusModding.Api](https://github.com/maanu113/SineusModding.Api) (included in the release zip).

## Controls

- **F9** - show/hide the panel
- **F10** - settings (template editor, section toggles, colors, font size, refresh rate, docking)

## Building from source

```
dotnet build -c Release
```

The `.csproj` references game/BepInEx assemblies via `HintPath` pointing at a local Sineus Arena install (`GameDir` in the `.csproj` - edit if yours is elsewhere), and references `lib/SineusModding.Api.dll` (vendored in this repo) for the shared API.

## License

No license specified yet - all rights reserved by default. Contact the author before redistributing.
