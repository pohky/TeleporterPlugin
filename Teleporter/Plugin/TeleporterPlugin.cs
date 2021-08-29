using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Plugin;
using Teleporter.Gui;
using Teleporter.Managers;
using SigScanner = Dalamud.Game.SigScanner;

namespace Teleporter.Plugin {
    public sealed class TeleporterPlugin : IDalamudPlugin {
        public string Name => "Teleporter";

        [PluginService] public static DalamudPluginInterface PluginInterface { get; set; } = null!;
        [PluginService] public static DataManager Data { get; set; } = null!;
        [PluginService] public static ClientState ClientState { get; set; } = null!;
        [PluginService] public static SigScanner SigScanner { get; set; } = null!;
        [PluginService] public static Dalamud.Game.Command.CommandManager Commands { get; set; } = null!;
        [PluginService] public static ChatGui Chat { get; set; } = null!;

        public static TeleporterAddressResolver Address { get; set; } = new();
        public static Configuration Config { get; set; } = new();

        public TeleporterPlugin() {
            Address.Setup(SigScanner);
            AetheryteManager.Load();
            CommandManager.Load();
            TeleportManager.Load();
            Config = Configuration.Load();
            PluginInterface.UiBuilder.Draw += OnDraw;
            PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
            ConfigWindow.Enabled = true;
        }
        
        public static void LogChat(string message, bool error = false) {
            switch (error) {
                case true when Config.ChatError:
                    Chat.PrintError(message);
                    break;
                case false when Config.ChatMessage:
                    Chat.Print(message);
                    break;
            }
        }

        private static void OnDraw() {
            ConfigWindow.Draw();
        }

        public static void OnOpenConfigUi() {
            ConfigWindow.Enabled = !ConfigWindow.Enabled;
        }
        
        public void Dispose() {
            Config.Save();
            TeleportManager.UnLoad();
            CommandManager.UnLoad();
            PluginInterface.UiBuilder.Draw -= OnDraw;
            PluginInterface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;
        }
    }
}