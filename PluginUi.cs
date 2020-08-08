using System;
using TeleporterPlugin.Gui;

namespace TeleporterPlugin {
    public class PluginUi : IDisposable {
        private readonly TeleporterPlugin _plugin;
        public ConfigurationWindow ConfigWindow { get; }
        public AetherGateWindow AetherGateWindow { get; }
        public DebugWindow DebugWindow { get; }
        public LinksWindow LinksWindow { get; }

        public PluginUi(TeleporterPlugin plugin) {
            ConfigWindow = new ConfigurationWindow(plugin);
            AetherGateWindow = new AetherGateWindow(plugin);
            DebugWindow = new DebugWindow(plugin);
            LinksWindow = new LinksWindow(plugin);

            _plugin = plugin;
            _plugin.Interface.UiBuilder.OnBuildUi += Draw;
            _plugin.Interface.UiBuilder.OnOpenConfigUi += (sender, args) => ConfigWindow.Visible = true;
        }

        private void Draw() {
            ConfigWindow.Draw();
            AetherGateWindow.Draw();
            DebugWindow.Draw();
            LinksWindow.Draw();
        }

        public void Dispose() {
            _plugin.Interface.UiBuilder.OnBuildUi -= Draw;
            _plugin.Interface.UiBuilder.OnOpenConfigUi = null;
        }
    }
}