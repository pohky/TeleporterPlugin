using System;

namespace TeleporterPlugin {
    public class TeleportAction {
        private readonly Action _action;
        public TeleportLocation Location { get; }

        internal TeleportAction(TeleportLocation location, Action teleportAction) {
            _action = teleportAction;
            Location = location;
        }

        public void Execute() {
            _action?.Invoke();
        }
    }
}