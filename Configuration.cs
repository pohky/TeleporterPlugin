using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Plugin;
using TeleporterPlugin.Objects;

namespace TeleporterPlugin {
    [Serializable]
    public class Configuration : IPluginConfiguration {
        public int Version { get; set; } = 0;

        public TeleportType DefaultTeleportType { get; set; } = TeleportType.Direct;
        public bool UseTicketWithoutAsking { get; set; }
        public bool UseGilThreshold { get; set; }
        public int GilThreshold { get; set; } = 999;
        public List<string> AliasList { get; set; } = new List<string>();

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