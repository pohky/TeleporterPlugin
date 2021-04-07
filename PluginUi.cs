using System;
using TeleporterPlugin.Gui;

namespace TeleporterPlugin {
    public class PluginUi : IDisposable {
        private readonly TeleporterPlugin m_Plugin;
        public ConfigurationWindow ConfigWindow { get; }
        public AetherGateWindow AetherGateWindow { get; }
        public DebugWindow DebugWindow { get; }

        public PluginUi(TeleporterPlugin plugin) {
            ConfigWindow = new ConfigurationWindow(plugin);
            AetherGateWindow = new AetherGateWindow(plugin);
            DebugWindow = new DebugWindow(plugin);

            m_Plugin = plugin;
            m_Plugin.Interface.UiBuilder.OnBuildUi += OnDraw;
            m_Plugin.Interface.UiBuilder.OnOpenConfigUi += OnOpenConfig;
        }

        private void OnOpenConfig(object sender, EventArgs eventArgs) {
            ConfigWindow.Visible = true;
        }

        private void OnDraw() {
            ConfigWindow.Draw();
            AetherGateWindow.Draw();
            DebugWindow.Draw();
        }

        public void Dispose() {
            m_Plugin.Interface.UiBuilder.OnBuildUi -= OnDraw;
            m_Plugin.Interface.UiBuilder.OnOpenConfigUi -= OnOpenConfig;
        }
    }
}