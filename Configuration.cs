using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Plugin;
using TeleporterPlugin.Objects;

namespace TeleporterPlugin {
    [Serializable]
    public class Configuration : IPluginConfiguration {
        public int Version { get; set; } = 0;

        public TeleporterLanguage TeleporterLanguage { get; set; } = TeleporterLanguage.Client;
        public bool SkipTicketPopup { get; set; }
        public bool UseGilThreshold { get; set; }
        public bool AllowPartialMatch { get; set; } = true;
        public bool ShowTooltips { get; set; } = true;
        public bool UseFloatingWindow { get; set; }
        public int GilThreshold { get; set; } = 999;
        public List<TeleportAlias> AliasList { get; set; } = new List<TeleportAlias>();
        public List<TeleportButton> TeleportButtons { get; set; } = new List<TeleportButton>();

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