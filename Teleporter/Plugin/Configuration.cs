using System.Collections.Generic;
using Dalamud.Configuration;
using Teleporter.Managers;

namespace Teleporter.Plugin {
    public class Configuration : IPluginConfiguration {
        public int Version { get; set; } = 2;

        public bool AllowPartialName = true;
        public bool AllowPartialAlias = false;

        public bool SkipTicketPopup = false;
        public bool ChatMessage = true;
        public bool ChatError = true;

        public bool UseGilThreshold = false;
        public int GilThreshold = 999;

        public List<TeleportAlias> AliasList = new();

        #region Helper
        
        public void Save() {
            TeleporterPlugin.PluginInterface?.SavePluginConfig(this);
        }

        public static Configuration Load() {
            var config = TeleporterPlugin.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            foreach (var alias in config.AliasList)
                alias.Aetheryte = AetheryteManager.GetAetheryteName(alias);
            return config;
        }
        
        #endregion
    }
}