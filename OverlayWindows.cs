using SineusModding.Api;
using UnityEngine;

namespace LiveStatsOverlay
{
    /// <summary>
    /// Draws the draggable, template-driven live-stats panel and the settings
    /// window (template editor + toggles), persisting panel position and
    /// re-rendering the template text on a configurable interval rather than
    /// every frame.
    /// </summary>
    internal class OverlayWindows
    {
        private const int StatsWindowId = 0xA11CE;
        private const int SettingsWindowId = 0xA11CF;

        private readonly OverlaySettings settings;
        private bool settingsVisible;

        private Rect statsRect;
        private Rect settingsRect = new Rect(320f, 12f, 420f, 420f);
        private Vector2 settingsScroll;
        private Vector2 templateEditScroll;

        private string renderedText = string.Empty;
        private float nextRenderTime;
        private string templateEditBuffer;

        private GUIStyle windowStyle;
        private GUIStyle choiceInlineTextStyle;
        private GUIStyle statsTextStyle;
        private GUIStyle labelStyle;
        private GUIStyle headerStyle;
        private GUIStyle textAreaStyle;
        private bool stylesReady;
        private Texture2D panelTexture;
        private Font uiFont;

        private static readonly BepInEx.Logging.ManualLogSource Log =
            BepInEx.Logging.Logger.CreateLogSource("LiveStatsOverlay.UI");

        private bool objectiveSubscribed;

        private bool isDockedThisFrame;

        public OverlayWindows(OverlaySettings settings)
        {
            this.settings = settings;
            statsRect = new Rect(settings.PanelX.Value, settings.PanelY.Value, 280f, 10f);
            templateEditBuffer = settings.Template.Value;
        }

        public void ToggleSettingsWindow()
        {
            settingsVisible = !settingsVisible;
        }

        public void Update()
        {
            LocalPlayerProgress.EnsureSubscribed();
            EnsureObjectiveSubscribed();
            CombatMeter.Tick();
            StatTokens.ColorizeValues = settings.ColorizeStatValues.Value;

            if (Time.unscaledTime >= nextRenderTime)
            {
                renderedText = StatTemplate.Render(settings.Template.Value) + BuildToggledSections();
                nextRenderTime = Time.unscaledTime + Mathf.Max(0.05f, settings.UpdateInterval.Value);
            }
        }

        /// <summary>
        /// The Weapons/Passives/Items lines live outside the user template so they
        /// can be toggled from the settings window without template editing.
        /// </summary>
        private string BuildToggledSections()
        {
            var sb = new System.Text.StringBuilder();
            if (settings.ShowWeaponsSection.Value)
            {
                sb.Append("\n<color=#E6C478><b>Weapons</b></color>  <color=#FFFFFF>[weapons]</color>");
            }
            if (settings.ShowPassivesSection.Value)
            {
                sb.Append("\n<color=#E6C478><b>Passives</b></color>  <color=#FFFFFF>[passives]</color>");
            }
            if (settings.ShowItemsSection.Value)
            {
                sb.Append("\n<color=#E6C478><b>Items</b></color>  <color=#FFFFFF>[items]</color>");
            }
            if (sb.Length == 0)
            {
                return string.Empty;
            }
            sb.Insert(0, '\n');
            return StatTemplate.Render(sb.ToString());
        }

        private void EnsureObjectiveSubscribed()
        {
            if (objectiveSubscribed)
            {
                return;
            }

            MatchObjective.EnsureSubscribed();
            MatchObjective.StepChanged += (_, __) => nextRenderTime = 0f;
            MatchObjective.BossLairFound += () => nextRenderTime = 0f;
            objectiveSubscribed = true;
        }

        public void Draw()
        {
            EnsureStyles();

            if (settings.OverlayVisible.Value && MatchObjective.IsInMatch)
            {
                Rect bannerRect = default;
                isDockedThisFrame = settings.DockToObjectiveBanner.Value && MatchObjective.TryGetObjectiveBannerScreenRect(out bannerRect);
                if (isDockedThisFrame)
                {
                    // Anchor like a native extension of the objective banner: same
                    // width, right edges flush (the banner sits at the screen's
                    // right edge, so left-aligning ran the panel offscreen).
                    float width = Mathf.Max(240f, bannerRect.width);
                    statsRect = new Rect(bannerRect.xMax - width, bannerRect.yMax + 6f, width, statsRect.height);

                    statsRect = ClampToScreen(statsRect);
                    // Pin the width so auto-layout can't grow the window wider than the banner.
                    statsRect = GUILayout.Window(StatsWindowId, statsRect, DrawStatsWindow, "Live Stats", windowStyle, GUILayout.Width(width));
                }
                else
                {
                    statsRect = ClampToScreen(statsRect);
                    statsRect = GUILayout.Window(StatsWindowId, statsRect, DrawStatsWindow, "Live Stats", windowStyle);
                    PersistPanelPosition();
                }
            }

            if (settings.ShowChoiceCounters.Value && ChoicePanel.TryGetCounts(out int rerolls, out int skips, out int bans))
            {
                DrawChoiceCounters(rerolls, skips, bans);
            }

            if (settingsVisible)
            {
                settingsRect = ClampToScreen(settingsRect);
                settingsRect = GUILayout.Window(SettingsWindowId, settingsRect, DrawSettingsWindow, "Live Stats Overlay - Settings", windowStyle);
            }
        }

        /// <summary>
        /// While the "Make a choice" screen is open, overlays each remaining
        /// count as a small chip sitting INSIDE its own Reroll/Skip/Ban
        /// button, immediately in front of the button's label text -
        /// mirrors the game's own hotkey chip further to the left, so it
        /// reads as part of the button rather than a separate floating
        /// element.
        /// </summary>
        private void DrawChoiceCounters(int rerolls, int skips, int bans)
        {
            DrawChoiceBadge(ChoicePanel.TryGetRerollButtonScreenRect, rerolls);
            DrawChoiceBadge(ChoicePanel.TryGetSkipButtonScreenRect, skips);
            DrawChoiceBadge(ChoicePanel.TryGetBanButtonScreenRect, bans);
        }

        private delegate bool TryGetRect(out Rect rect);

        private void DrawChoiceBadge(TryGetRect tryGetButtonRect, int count)
        {
            if (!tryGetButtonRect(out Rect buttonRect))
            {
                return;
            }

            // No background box - plain colored text sitting right where the
            // button's own label starts (just past the game's hotkey chip,
            // which occupies roughly the left quarter of the button), so it
            // reads as part of the button instead of a separate element
            // overlapping the hotkey chip.
            const float width = 40f;
            const float height = 24f;
            var badgeRect = new Rect(
                buttonRect.xMin + buttonRect.width * 0.24f,
                buttonRect.y + (buttonRect.height - height) / 2f,
                width, height);
            badgeRect = ClampToScreen(badgeRect);

            string color = count > 0 ? "#E6C478" : "#FF6B57";
            string text = $"<color={color}><b>{count}</b></color>";
            GUI.Label(badgeRect, text, choiceInlineTextStyle);
        }

        /// <summary>
        /// Keeps the whole window on-screen. Without this, GUILayout.Window
        /// happily saves/restores a position partially or entirely outside the
        /// current resolution (e.g. a banner-docked panel wider than the space
        /// right of the banner, or a leftover position from another monitor
        /// setup) and part of the panel is cut off with no way to recover it.
        /// </summary>
        private static Rect ClampToScreen(Rect rect)
        {
            float x = Mathf.Clamp(rect.x, 0f, Mathf.Max(0f, Screen.width - rect.width));
            float y = Mathf.Clamp(rect.y, 0f, Mathf.Max(0f, Screen.height - rect.height));
            return new Rect(x, y, rect.width, rect.height);
        }

        private void PersistPanelPosition()
        {
            if (!Mathf.Approximately(statsRect.x, settings.PanelX.Value))
            {
                settings.PanelX.Value = statsRect.x;
            }
            if (!Mathf.Approximately(statsRect.y, settings.PanelY.Value))
            {
                settings.PanelY.Value = statsRect.y;
            }
        }

        private void DrawStatsWindow(int windowId)
        {
            GUILayout.Label(renderedText, statsTextStyle);
            if (!isDockedThisFrame)
            {
                GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
            }
        }

        private void DrawSettingsWindow(int windowId)
        {
            settingsScroll = GUILayout.BeginScrollView(settingsScroll);

            settings.OverlayVisible.Value = GUILayout.Toggle(settings.OverlayVisible.Value, "Overlay Visible");
            settings.DockToObjectiveBanner.Value = GUILayout.Toggle(settings.DockToObjectiveBanner.Value,
                "Dock underneath the \"Find the boss's lair\" objective banner");
            settings.ColorizeStatValues.Value = GUILayout.Toggle(settings.ColorizeStatValues.Value,
                "Colorize values (damage red, HP green, utility teal, kills gold)");
            settings.ShowChoiceCounters.Value = GUILayout.Toggle(settings.ShowChoiceCounters.Value,
                "Show Rerolls/Skips/Bans left on the \"Make a choice\" screen");

            GUILayout.Space(6f);
            GUILayout.Label("Sections", headerStyle);
            settings.ShowWeaponsSection.Value = GUILayout.Toggle(settings.ShowWeaponsSection.Value, "Weapons");
            settings.ShowPassivesSection.Value = GUILayout.Toggle(settings.ShowPassivesSection.Value, "Passives");
            settings.ShowItemsSection.Value = GUILayout.Toggle(settings.ShowItemsSection.Value, "Items");

            GUILayout.Space(6f);
            GUILayout.Label("Template", headerStyle);
            GUILayout.Label(
                "Use [tokenName] placeholders, e.g. [Health] [MovementSpeed] [JumpsCount] [kills] [weapons]. Supports rich text (<b>, <color=...>).",
                labelStyle);

            templateEditScroll = GUILayout.BeginScrollView(templateEditScroll, GUILayout.Height(180f));
            templateEditBuffer = GUILayout.TextArea(templateEditBuffer, textAreaStyle, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply"))
            {
                settings.Template.Value = templateEditBuffer;
                nextRenderTime = 0f;
            }
            if (GUILayout.Button("Reset to Default"))
            {
                templateEditBuffer = OverlaySettings.DefaultTemplate;
                settings.Template.Value = templateEditBuffer;
                nextRenderTime = 0f;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);
            GUILayout.Label("Display", headerStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Font size", labelStyle, GUILayout.Width(100f));
            settings.FontSize.Value = Mathf.RoundToInt(GUILayout.HorizontalSlider(settings.FontSize.Value, 8f, 24f));
            GUILayout.Label(settings.FontSize.Value.ToString(), labelStyle, GUILayout.Width(24f));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Refresh (s)", labelStyle, GUILayout.Width(100f));
            settings.UpdateInterval.Value = GUILayout.HorizontalSlider(settings.UpdateInterval.Value, 0.05f, 2f);
            GUILayout.Label(settings.UpdateInterval.Value.ToString("0.00"), labelStyle, GUILayout.Width(36f));
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);
            GUILayout.Label($"Toggle overlay: {settings.ToggleOverlayKey.Value}", labelStyle);
            GUILayout.Label($"Toggle this window: {settings.ToggleSettingsKey.Value}", labelStyle);
            GUILayout.Label(
                settings.DockToObjectiveBanner.Value
                    ? "Docked underneath the objective banner. Disable docking above to drag it freely."
                    : "Drag the Live Stats panel's title bar to reposition it.",
                labelStyle);

            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private void EnsureStyles()
        {
            // Unity destroys runtime-created Texture2D/Font objects behind our back
            // (scene transitions / Resources.UnloadUnusedAssets during match loading
            // sweep assets that aren't flagged). The styles were previously built
            // exactly once, at the main menu - so by the time the panel actually
            // drew inside a match, the background texture was already dead and the
            // window rendered with no frame/background at all. Liveness-check the
            // texture/font every draw and rebuild when they've been destroyed
            // (Unity's overloaded == null catches destroyed objects).
            if (stylesReady && panelTexture != null && uiFont != null)
            {
                return;
            }

            uiFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Segoe UI", "Segoe UI Semibold", "Arial" }, settings.FontSize.Value);
            if (uiFont != null)
            {
                uiFont.hideFlags = HideFlags.HideAndDontSave;
            }

            // Palette pulled from the game's own ESC-menu stats card and the
            // "Find the boss's lair" objective banner: dark desaturated
            // teal/navy panels with a warm gold accent and off-white text.
            Color panelBg = new Color32(10, 26, 34, 235);
            Color panelBorder = new Color32(198, 158, 74, 255);
            Color bodyText = new Color32(224, 230, 232, 255);
            Color headerText = new Color32(230, 196, 120, 255);

            panelTexture = MakeBorderedTexture(panelBg, panelBorder);

            windowStyle = new GUIStyle(GUI.skin.window)
            {
                font = uiFont,
                fontSize = settings.FontSize.Value + 1,
                fontStyle = FontStyle.Bold,
                // GUI.skin.window's default .border insets are tuned for its own
                // (larger) background texture and break rendering with ours. Use
                // insets matching MakeBorderedTexture's actual 2px border so the
                // 9-slice keeps the gold edge crisp at 2px and stretches only the fill.
                border = new RectOffset(2, 2, 2, 2),
                overflow = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                normal = { background = panelTexture, textColor = headerText },
                onNormal = { background = panelTexture, textColor = headerText },
                hover = { background = panelTexture, textColor = headerText },
                onHover = { background = panelTexture, textColor = headerText },
                active = { background = panelTexture, textColor = headerText },
                onActive = { background = panelTexture, textColor = headerText },
                focused = { background = panelTexture, textColor = headerText },
                onFocused = { background = panelTexture, textColor = headerText },
                padding = new RectOffset(10, 10, 22, 10)
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                font = uiFont,
                fontSize = settings.FontSize.Value,
                normal = { textColor = bodyText }
            };

            headerStyle = new GUIStyle(labelStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = headerText }
            };

            statsTextStyle = new GUIStyle(GUI.skin.label)
            {
                font = uiFont,
                fontSize = settings.FontSize.Value,
                richText = true,
                normal = { textColor = bodyText },
                wordWrap = true
            };

            textAreaStyle = new GUIStyle(GUI.skin.textArea)
            {
                font = uiFont,
                fontSize = settings.FontSize.Value,
                wordWrap = true,
                normal = { textColor = Color.black }
            };

            choiceInlineTextStyle = new GUIStyle(GUI.skin.label)
            {
                font = uiFont,
                fontSize = Mathf.Max(9, settings.FontSize.Value + 2),
                richText = true,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false
            };

            bool rebuilt = stylesReady;
            stylesReady = true;
            Log.LogInfo($"{(rebuilt ? "Rebuilt" : "Built")} overlay styles (font: {(uiFont != null ? uiFont.name : "NULL")}, panel texture: {(panelTexture != null ? "ok" : "NULL")})");
        }

        /// <summary>Builds a small solid-fill texture with a 2px border, used as GUIStyle background (IMGUI has no native rounded/bordered box).</summary>
        private static Texture2D MakeBorderedTexture(Color fill, Color border)
        {
            const int size = 12;
            const int borderWidth = 2;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                // Without this, Unity destroys the runtime-created texture during
                // scene transitions / asset unloads and the panel loses its background.
                hideFlags = HideFlags.HideAndDontSave
            };
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = x < borderWidth || y < borderWidth || x >= size - borderWidth || y >= size - borderWidth;
                    tex.SetPixel(x, y, isBorder ? border : fill);
                }
            }
            tex.Apply();
            return tex;
        }
    }
}
