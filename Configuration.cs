using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Plugin;
using TeleporterPlugin.Objects;

namespace TeleporterPlugin {
    [Serializable]
    public class Configuration : IPluginConfiguration {
        public int Version { get; set; } = 0;

        public TeleporterLanguage TeleporterLanguage = TeleporterLanguage.Client;
        public bool SkipTicketPopup;
        public bool UseGilThreshold;
        public bool AllowPartialMatch = true;
        public bool ShowTooltips = true;
        public bool UseFloatingWindow;
        public int GilThreshold = 999;
        public List<TeleportAlias> AliasList = new List<TeleportAlias>();
        public List<TeleportButton> TeleportButtons = new List<TeleportButton>();

        #region Init and Save
        
        [NonSerialized] private DalamudPluginInterface _pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface) {
            _pluginInterface = pluginInterface;
        }

        public void Save() {
            _pluginInterface.SavePluginConfig(this);
        }

        #endregion
    }
}