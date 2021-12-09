using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Plugin;
using TeleporterPlugin.Gui;
using TeleporterPlugin.Managers;
using SigScanner = Dalamud.Game.SigScanner;

namespace TeleporterPlugin.Plugin {
    public sealed class TeleporterPluginMain : IDalamudPlugin {
        public string Name => "Teleporter";

        [PluginService] public static DalamudPluginInterface PluginInterface { get; set; } = null!;
        [PluginService] public static DataManager Data { get; set; } = null!;
        [PluginService] public static ClientState ClientState { get; set; } = null!;
        [PluginService] public static SigScanner SigScanner { get; set; } = null!;
        [PluginService] public static Dalamud.Game.Command.CommandManager Commands { get; set; } = null!;
        [PluginService] public static ChatGui Chat { get; set; } = null!;

        public static TeleporterAddressResolver Address { get; set; } = new();
        public static Configuration Config { get; set; } = new();

        public TeleporterPluginMain() {
            Address.Setup(SigScanner);
            AetheryteManager.Load();
            CommandManager.Load();
            
            Config = Configuration.Load();
            if (Config.UseEnglish)
                AetheryteManager.Load();

            IpcManager.Register(PluginInterface);

            PluginInterface.UiBuilder.Draw += OnDraw;
            PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
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
            IpcManager.Unregister();
            CommandManager.UnLoad();
            PluginInterface.UiBuilder.Draw -= OnDraw;
            PluginInterface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;
        }
    }
}