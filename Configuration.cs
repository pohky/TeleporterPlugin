using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace TeleporterPlugin {
    [Serializable]
    public class Configuration : IPluginConfiguration {
        public int Version { get; set; } = 0;

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