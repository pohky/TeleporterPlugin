using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using TeleporterPlugin.Gui;
using TeleporterPlugin.Managers;

namespace TeleporterPlugin.Plugin {
    public sealed class TeleporterPluginMain : IDalamudPlugin {
        [PluginService] public static DalamudPluginInterface PluginInterface { get; set; } = null!;
        [PluginService] public static IDataManager Data { get; set; } = null!;
        [PluginService] public static IClientState ClientState { get; set; } = null!;
        [PluginService] public static ICommandManager Commands { get; set; } = null!;
        [PluginService] public static IChatGui Chat { get; set; } = null!;
        [PluginService] public static IPluginLog PluginLog { get; set; } = null!;
        public static Configuration Config { get; set; } = new();

        public TeleporterPluginMain() {
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