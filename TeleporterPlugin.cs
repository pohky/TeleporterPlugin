using System;
using Dalamud.Game.Command;
using Dalamud.Plugin;

namespace TeleporterPlugin {
    public class TeleporterPlugin : IDalamudPlugin {
        private const string CommandName = "/tp";
        private const string HelpMessage = "No Help.";
        public string Name => "Teleporter";
        public PluginUi Gui { get; private set; }
        public DalamudPluginInterface Interface { get; private set; }

        public void Initialize(DalamudPluginInterface pluginInterface) {
            Interface = pluginInterface;
            Gui = new PluginUi(this);
            TeleportManager.Init(Interface);
            Interface.CommandManager.AddHandler(CommandName, new CommandInfo(CommandHandler) {HelpMessage = HelpMessage});
        }

        private void CommandHandler(string command, string arguments) {
            var arg = arguments.Trim();
            if (string.IsNullOrEmpty(arg)) {
                return;
            }
            if (arg.Equals("debug", StringComparison.OrdinalIgnoreCase)) {
                Gui.DebugVisible = !Gui.DebugVisible;
                return;
            }
            if (uint.TryParse(arg, out var id)) {
                TeleportManager.Teleport(id);
                return;
            }
            TeleportManager.Teleport(arg);
        }

        public void Dispose() {
            Gui?.Dispose();
            Interface.CommandManager.RemoveHandler(CommandName);
        }
    }
}