using System;
using System.Collections.Generic;
using Dalamud;
using Dalamud.Configuration;
using Dalamud.Plugin;
using TeleporterPlugin.Objects;

namespace TeleporterPlugin {
    [Serializable]
    public class Configuration : IPluginConfiguration {
        public int Version { get; set; } = 0;

        public ClientLanguage Language = ClientLanguage.English;
        public bool SkipTicketPopup;
        public bool UseGilThreshold;
        public bool AllowPartialMatch = true;
        public bool AllowPartialAlias = false;
        public bool ShowTooltips = true;
        public int GilThreshold = 999;
        public List<TeleportAlias> AliasList = new();

        public bool PrintMessage = true;
        public bool PrintError = true;

        #region Init and Save
        
        [NonSerialized] private DalamudPluginInterface m_PluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface) {
            m_PluginInterface = pluginInterface;
        }

        public void Save() {
            m_PluginInterface.SavePluginConfig(this);
        }

        #endregion
    }
}