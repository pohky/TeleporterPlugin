using System;
using Dalamud.Plugin;

namespace TeleporterPlugin {
    public class TeleportAction {
        private readonly Action _action;
        public TeleportLocation Location { get; }

        public static TeleportAction Invalid => new TeleportAction(new TeleportLocation(), () => PluginLog.Log("Invalid Teleport Action"));

        internal TeleportAction(TeleportLocation location, Action teleportAction) {
            _action = teleportAction;
            Location = location;
        }

        public void Execute() {
            _action?.Invoke();
        }
    }
}