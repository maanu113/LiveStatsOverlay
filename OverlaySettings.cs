using BepInEx.Configuration;
using UnityEngine;

namespace LiveStatsOverlay
{
    /// <summary>All user-facing, persisted settings for the overlay, backed by BepInEx's config file.</summary>
    internal class OverlaySettings
    {
        public readonly ConfigEntry<KeyCode> ToggleOverlayKey;
        public readonly ConfigEntry<KeyCode> ToggleSettingsKey;
        public readonly ConfigEntry<bool> OverlayVisible;
        public readonly ConfigEntry<int> FontSize;
        public readonly ConfigEntry<float> UpdateInterval;

        public readonly ConfigEntry<float> PanelX;
        public readonly ConfigEntry<float> PanelY;
        public readonly ConfigEntry<bool> DockToObjectiveBanner;

        public readonly ConfigEntry<string> Template;
        public readonly ConfigEntry<bool> ColorizeStatValues;
        public readonly ConfigEntry<bool> ShowWeaponsSection;
        public readonly ConfigEntry<bool> ShowPassivesSection;
        public readonly ConfigEntry<bool> ShowItemsSection;
        public readonly ConfigEntry<bool> ShowChoiceCounters;
        private readonly ConfigEntry<int> templateVersion;

        /// <summary>
        /// Bump this whenever DefaultTemplate is redesigned in a way that should
        /// override existing users' saved templates (not just add one token to
        /// them). See <see cref="ForceUpgradeTemplateIfOutdated"/>.
        /// </summary>
        private const int CurrentTemplateVersion = 5;

        public const string DefaultTemplate =
            "<color=#9FB4BA>Time:</color> <color=#FFFFFF>[matchTime]</color>      <color=#9FB4BA>HP:</color> <color=#FFFFFF>[currentHealth]/[maxHealthCurrent] ([healthFraction])</color>\n" +
            "\n" +
            "<color=#9FB4BA>DPS (10s):</color> <color=#FFFFFF>[dps]</color>        <color=#9FB4BA>Kills/min:</color> <color=#FFFFFF>[kpm]</color>\n" +
            "<color=#9FB4BA>Combo Dmg:</color> <color=#FFFFFF>[comboDamage]</color>\n" +
            "<color=#9FB4BA>Kills:</color> <color=#FFFFFF>[kills]</color>        <color=#9FB4BA>Total Damage:</color> <color=#FFFFFF>[totalDamage]</color>\n" +
            "\n" +
            "<color=#9FB4BA>Damage:</color> <color=#FFFFFF>[DamageModifier]</color>   <color=#9FB4BA>Speed:</color> <color=#FFFFFF>[MovementSpeed]</color>   <color=#9FB4BA>Cooldown:</color> <color=#FFFFFF>[Cooldown]</color>   <color=#9FB4BA>Crit:</color> <color=#FFFFFF>[CritChance]</color>";

        public OverlaySettings(ConfigFile config)
        {
            ToggleOverlayKey = config.Bind("General", "ToggleOverlayKey", KeyCode.F9, "Key to show/hide the stats overlay.");
            ToggleSettingsKey = config.Bind("General", "ToggleSettingsKey", KeyCode.F10, "Key to open/close the overlay settings window.");
            OverlayVisible = config.Bind("General", "OverlayVisible", true, "Whether the overlay is visible by default.");
            FontSize = config.Bind("General", "FontSize", 13, "Font size for the overlay text.");
            UpdateInterval = config.Bind("General", "UpdateInterval", 0.25f, "Seconds between stat text refreshes. Lower is more responsive but costs more performance.");

            PanelX = config.Bind("Position", "PanelX", 12f, "X position (pixels from left) of the overlay panel. Only used when DockToObjectiveBanner is false. Drag the panel's title bar in-game to change, or edit here.");
            PanelY = config.Bind("Position", "PanelY", 12f, "Y position (pixels from top) of the overlay panel. Only used when DockToObjectiveBanner is false.");
            DockToObjectiveBanner = config.Bind("Position", "DockToObjectiveBanner", true,
                "If true, the panel automatically positions itself directly underneath the game's own \"Find the boss's lair\" style objective banner, instead of a fixed/draggable position. If the banner can't be found (e.g. very early in a match), falls back to PanelX/PanelY.");

            Template = config.Bind("Display", "Template", DefaultTemplate,
                "The text template shown in the overlay. Use [tokenName] placeholders (e.g. [Health], [MovementSpeed], [kills], [weapons]) " +
                "which are replaced live with the local player's current stats. Supports Unity rich text (<b>, <color=...>, etc). " +
                "Edit here or via the in-game settings window (F10 by default).");

            // Deliberately a new "Sections" config section: the legacy v2.0 [Panels]
            // keys (ShowWeapons etc) still sit orphaned in older config files, and
            // BepInEx would resurrect those stale saved values if we reused the names.
            ShowWeaponsSection = config.Bind("Sections", "ShowWeapons", true, "Show the Weapons line at the bottom of the overlay.");
            ShowPassivesSection = config.Bind("Sections", "ShowPassives", true, "Show the Passives line at the bottom of the overlay.");
            ShowItemsSection = config.Bind("Sections", "ShowItems", true, "Show the Items line at the bottom of the overlay.");

            ShowChoiceCounters = config.Bind("Display", "ShowChoiceCounters", true,
                "On the \"Make a choice\" buff-selection screen, show how many Rerolls/Skips/Bans you have left above the choices.");

            ColorizeStatValues = config.Bind("Display", "ColorizeStatValues", true,
                "Colorize stat values by category, LookingGlass-style: damage stats red, healing/HP green (turning yellow/red as HP drops), " +
                "utility stats teal, kills gold. Turn off for plain white values.");

            templateVersion = config.Bind("Display", "TemplateVersion", 0,
                "Internal - tracks which built-in template you last had, so mod updates that redesign the default template can offer to " +
                "replace an old auto-generated one without touching a template you've hand-edited. Don't edit this directly.");

            ForceUpgradeTemplateIfOutdated();
        }

        /// <summary>
        /// BepInEx's ConfigEntry only applies a new default to a *new* config key -
        /// once "Template" exists in a user's .cfg file, config.Bind above returns
        /// that saved value verbatim forever and never sees changes made to
        /// DefaultTemplate here. Earlier versions patched individual missing tokens
        /// into the saved template one at a time, which just accumulated cruft
        /// (old + new stat lines side by side) instead of actually delivering a
        /// redesigned template. Instead: if the saved template is still exactly
        /// what a previous DefaultTemplate looked like (i.e. the user never
        /// customized it), replace it outright with the current DefaultTemplate.
        /// If the user has actually hand-edited their template, never overwrite it -
        /// just bump the version marker so we stop asking.
        /// </summary>
        private void ForceUpgradeTemplateIfOutdated()
        {
            if (templateVersion.Value >= CurrentTemplateVersion)
            {
                return;
            }

            bool matchesAnyKnownDefault = Template.Value == DefaultTemplate || KnownPriorDefaults.Contains(Template.Value);
            if (matchesAnyKnownDefault)
            {
                Template.Value = DefaultTemplate;
            }

            templateVersion.Value = CurrentTemplateVersion;
        }

        /// <summary>Prior built-in DefaultTemplate strings, so an unmodified old template can be recognized and safely replaced.</summary>
        private static readonly System.Collections.Generic.HashSet<string> KnownPriorDefaults = new System.Collections.Generic.HashSet<string>
        {
            "<b>Live Stats</b>\nHealth: [Health]\nRegeneration: [HealthRegeneration]\nBarrier: [Barrier]\nCooldown: [Cooldown]\nDamage: [DamageModifier]\nCrit Chance: [CritChance]\nCrit Damage: [CritDamage]\nDuration: [Duration]\nElites Damage: [ElitesDamage]\nSpeed: [MovementSpeed]\nEvasion: [Evasion]\nExperience Bonus: [ExperienceBonus]\nLifesteal Chance: [LifestealChance]\nProjectiles Count: [ProjectileCount]\nArmor: [Armor]\nSize: [Size]\nMagnet: [Magnet]\nJumps: [JumpsCount]\nJump Height: [JumpHeight]\nTower Respawn Time: [TowerCooldown]\nAltar Charge Speed: [AltarChargeSpeed]\nLuck: [Luck]\nBonus Chest Frequency: [ChestChance]\n\n<b>Combat</b>\nKills: [kills]\nTotal Damage: [totalDamage]\n\n<b>Weapons</b>\n[weapons]\n<b>Passives</b>\n[passives]\n<b>Items</b>\n[items]",
            "<b>Live Stats</b>\nObjective: [questStep]\nHealth: [Health]\nRegeneration: [HealthRegeneration]\nBarrier: [Barrier]\nCooldown: [Cooldown]\nDamage: [DamageModifier]\nCrit Chance: [CritChance]\nCrit Damage: [CritDamage]\nDuration: [Duration]\nElites Damage: [ElitesDamage]\nSpeed: [MovementSpeed]\nEvasion: [Evasion]\nExperience Bonus: [ExperienceBonus]\nLifesteal Chance: [LifestealChance]\nProjectiles Count: [ProjectileCount]\nArmor: [Armor]\nSize: [Size]\nMagnet: [Magnet]\nJumps: [JumpsCount]\nJump Height: [JumpHeight]\nTower Respawn Time: [TowerCooldown]\nAltar Charge Speed: [AltarChargeSpeed]\nLuck: [Luck]\nBonus Chest Frequency: [ChestChance]\n\n<b>Combat</b>\nKills: [kills]\nTotal Damage: [totalDamage]\n\n<b>Weapons</b>\n[weapons]\n<b>Passives</b>\n[passives]\n<b>Items</b>\n[items]",
            "<b>Live Stats</b>\nDPS (10s avg): [dps]\nObjective: [questStep]\nHealth: [Health]\nRegeneration: [HealthRegeneration]\nBarrier: [Barrier]\nCooldown: [Cooldown]\nDamage: [DamageModifier]\nCrit Chance: [CritChance]\nCrit Damage: [CritDamage]\nDuration: [Duration]\nElites Damage: [ElitesDamage]\nSpeed: [MovementSpeed]\nEvasion: [Evasion]\nExperience Bonus: [ExperienceBonus]\nLifesteal Chance: [LifestealChance]\nProjectiles Count: [ProjectileCount]\nArmor: [Armor]\nSize: [Size]\nMagnet: [Magnet]\nJumps: [JumpsCount]\nJump Height: [JumpHeight]\nTower Respawn Time: [TowerCooldown]\nAltar Charge Speed: [AltarChargeSpeed]\nLuck: [Luck]\nBonus Chest Frequency: [ChestChance]\n\n<b>Combat</b>\nKills: [kills]\nTotal Damage: [totalDamage]\n\n<b>Weapons</b>\n[weapons]\n<b>Passives</b>\n[passives]\n<b>Items</b>\n[items]",
            "<b>Live Stats</b>  [questStep]\nTime: [matchTime]      HP: [currentHealth]/[maxHealthCurrent] ([healthFraction])\n\nDPS (10s): [dps]        Kills/min: [kpm]\nCombo Dmg: [comboDamage]\nKills: [kills]        Total Damage: [totalDamage]\n\nDamage: [DamageModifier]   Speed: [MovementSpeed]   Cooldown: [Cooldown]   Crit: [CritChance]\n\n<b>Weapons</b>  [weapons]\n<b>Passives</b>  [passives]\n<b>Items</b>  [items]",
            "[questStep]\nTime: [matchTime]      HP: [currentHealth]/[maxHealthCurrent] ([healthFraction])\n\nDPS (10s): [dps]        Kills/min: [kpm]\nCombo Dmg: [comboDamage]\nKills: [kills]        Total Damage: [totalDamage]\n\nDamage: [DamageModifier]   Speed: [MovementSpeed]   Cooldown: [Cooldown]   Crit: [CritChance]\n\n<b>Weapons</b>  [weapons]\n<b>Passives</b>  [passives]\n<b>Items</b>  [items]",
            "<color=#9FB4BA>Time:</color> <color=#FFFFFF>[matchTime]</color>      <color=#9FB4BA>HP:</color> <color=#FFFFFF>[currentHealth]/[maxHealthCurrent] ([healthFraction])</color>\n\n<color=#9FB4BA>DPS (10s):</color> <color=#FFFFFF>[dps]</color>        <color=#9FB4BA>Kills/min:</color> <color=#FFFFFF>[kpm]</color>\n<color=#9FB4BA>Combo Dmg:</color> <color=#FFFFFF>[comboDamage]</color>\n<color=#9FB4BA>Kills:</color> <color=#FFFFFF>[kills]</color>        <color=#9FB4BA>Total Damage:</color> <color=#FFFFFF>[totalDamage]</color>\n\n<color=#9FB4BA>Damage:</color> <color=#FFFFFF>[DamageModifier]</color>   <color=#9FB4BA>Speed:</color> <color=#FFFFFF>[MovementSpeed]</color>   <color=#9FB4BA>Cooldown:</color> <color=#FFFFFF>[Cooldown]</color>   <color=#9FB4BA>Crit:</color> <color=#FFFFFF>[CritChance]</color>\n\n<color=#E6C478><b>Weapons</b></color>  <color=#FFFFFF>[weapons]</color>\n<color=#E6C478><b>Passives</b></color>  <color=#FFFFFF>[passives]</color>\n<color=#E6C478><b>Items</b></color>  <color=#FFFFFF>[items]</color>",
        };
    }
}
