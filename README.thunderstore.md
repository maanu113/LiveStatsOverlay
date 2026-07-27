# Live Stats Overlay

Requires **Sineus Modding API** (listed as a dependency - your mod manager will install it automatically).

A live stats panel styled like the game's own UI (dark navy card, gold frame, matching colors), docked directly underneath the "Find the boss's lair" objective banner. Only shows during a match.

## Features
- Live combat metrics the game itself doesn't show: DPS (rolling 10s average), kills/minute, current combo damage, current/max HP with color that shifts green → yellow → red as you get hurt, match timer, kills and total damage.
- Values are color-coded by category (damage red, healing green, utility teal, kills gold) - toggleable.
- Big numbers abbreviate: 1234 → 1.2k, 1500000 → 1.5M.
- Weapons / Passives / Items lines can be shown or hidden with checkboxes.
- Toggleable per-weapon DPS and kill count breakdown.
- QOL: on the "Make a choice" screen, your remaining Rerolls / Skips / Bans are shown right above the buttons.
- Optional integration with the separate DpsMeter mod: docks above its window instead of overlapping it.
- Fully customizable `[token]` template, editable live in the settings window.

## Controls
- **F9** — show/hide the stats panel
- **F10** — settings window (template editor, section toggles, colors, font size, refresh rate, docking)

## Configuration
Saved in `BepInEx/config/com.community.sineusarena.livestatsoverlay.cfg`.

## Source
https://github.com/maanu113/LiveStatsOverlay
