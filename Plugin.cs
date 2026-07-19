using BepInEx;
using SineusModding.Api;
using UnityEngine;

namespace LiveStatsOverlay
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.community.sineusarena.livestatsoverlay";
        public const string PluginName = "Live Stats Overlay";
        public const string PluginVersion = "3.0.1";

        private const string GithubOwner = "maanu113";
        private const string GithubRepo = "LiveStatsOverlay";

        internal static OverlaySettings Settings;
        private OverlayWindows windows;
        private UpdateNotice updateNotice;

        private void Awake()
        {
            Settings = new OverlaySettings(Config);
            windows = new OverlayWindows(Settings);
            updateNotice = new UpdateNotice(PluginName);
            Logger.LogInfo($"{PluginName} v{PluginVersion} loaded. Press {Settings.ToggleOverlayKey.Value} to show/hide, {Settings.ToggleSettingsKey.Value} for settings.");

            UpdateChecker.CheckAsync(this, GithubOwner, GithubRepo, PluginVersion, result =>
            {
                if (result.Status == UpdateCheckStatus.UpdateAvailable)
                {
                    updateNotice.Show(result.LatestVersion, result.ReleaseUrl);
                }
            });
        }

        private void Update()
        {
            if (Input.GetKeyDown(Settings.ToggleOverlayKey.Value))
            {
                Settings.OverlayVisible.Value = !Settings.OverlayVisible.Value;
            }

            if (Input.GetKeyDown(Settings.ToggleSettingsKey.Value))
            {
                windows.ToggleSettingsWindow();
            }

            windows.Update();
        }

        private void OnGUI()
        {
            windows.Draw();
            updateNotice.Draw();
        }
    }
}
