using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud;
using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
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
            TeleportManager.Load();

            if (TryImportOldConfig(PluginInterface, out var configuration))
                Config = configuration;
            else Config = Configuration.Load();
            
            if (Config.UseEnglish)
                AetheryteManager.Load();

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

        private bool TryImportOldConfig(DalamudPluginInterface plugin, out Configuration newConfig) {
            newConfig = new Configuration();
            if (plugin.GetPluginConfig() is Configuration { Version: 0 } old) {
                PluginLog.LogDebug($"Old Config Found: Version={old.Version}");
            } else
                return false;

            var lang = Data.Language;
            if (old.TeleporterLanguage != 4)
                lang = (ClientLanguage)old.TeleporterLanguage;
            var sheet = Data.GetExcelSheet<Aetheryte>(lang);
            if (sheet == null)
                return false;

            newConfig.AllowPartialAlias = old.AllowPartialAlias;
            newConfig.GilThreshold = old.GilThreshold;
            newConfig.UseGilThreshold = old.UseGilThreshold;
            newConfig.SkipTicketPopup = old.SkipTicketPopup;
            newConfig.Version = 2;
            newConfig.AliasList = new List<TeleportAlias>();
            var idFilter = new uint[] { 56, 57, 58, 59, 60, 61, 96, 97 };
            foreach (var aliasOld in old.AliasList) {
                PluginLog.LogDebug($"Trying to Import Alias: {aliasOld.Alias} -> {aliasOld.Aetheryte}");
                var match = false;
                foreach (var aetheryte in sheet) {
                    var name = aetheryte.PlaceName.Value?.Name?.ToString();
                    if (string.IsNullOrEmpty(name)) continue;
                    name = PluginInterface.Sanitizer.Sanitize(name);
                    if (!aliasOld.Aetheryte.Equals(name, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (idFilter.Contains(aetheryte.RowId)) {
                        PluginLog.LogDebug("Estate found. Skipping.");
                        match = false;
                        break;
                    }
                    match = true;
                    PluginLog.LogDebug($"Found Matching Aetheryte: {name}");
                    newConfig.AliasList.Add(new TeleportAlias {
                        Aetheryte = aliasOld.Aetheryte,
                        AetheryteId = aetheryte.RowId,
                        Alias = aliasOld.Alias
                    });
                    break;
                }

                if (match) continue;
                PluginLog.LogDebug("No Matching Aetheryte found. Adding Empty Alias.");
                newConfig.AliasList.Add(new TeleportAlias {
                    Alias = aliasOld.Alias
                });
            }
            
            return true;
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