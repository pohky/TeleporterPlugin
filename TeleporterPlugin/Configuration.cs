using System.Collections.Generic;
using Dalamud.Configuration;
using TeleporterPlugin.Managers;
using TeleporterPlugin.Plugin;

namespace TeleporterPlugin {
    public class Configuration : IPluginConfiguration {
        public int Version { get; set; } = 2;

        public bool UseEnglish = false;

        public bool AllowPartialName = true;
        public bool AllowPartialAlias = false;

        public bool SkipTicketPopup = false;
        public bool ChatMessage = true;
        public bool ChatError = true;

        public bool UseGilThreshold = false;
        public int GilThreshold = 999;

        public bool EnableGrandCompany = false;
        public string GrandCompanyAlias = "gc";

        public bool EnableEternityRing = false;
        public string EternityRingAlias = "ring";
        
        public List<TeleportAlias> AliasList = new();

        #region OldConfig

        public int TeleporterLanguage = 4;

        #endregion

        #region Helper

        public void Save() {
            TeleporterPluginMain.PluginInterface.SavePluginConfig(this);
        }

        public static Configuration Load() {
            var config = TeleporterPluginMain.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            foreach (var alias in config.AliasList)
                alias.Aetheryte = AetheryteManager.GetAetheryteName(alias);
            return config;
        }
        
        #endregion
    }
}