using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Game.Command;
using Dalamud.Plugin;

namespace TeleporterPlugin {
    public class TeleporterPlugin : IDalamudPlugin {
        private const string CommandName = "/tp";
        private const string HelpMessage = "/tp <name> <type>. Use '/tp help' for more info.";
        public string Name => "Teleporter";
        public PluginUi Gui { get; private set; }
        public DalamudPluginInterface Interface { get; private set; }

        public void Initialize(DalamudPluginInterface pluginInterface) {
            Interface = pluginInterface;
            Gui = new PluginUi(this);
            TeleportManager.Init(Interface);
            TeleportManager.LogEvent += TeleportManagerOnLogEvent;
            TeleportManager.LogErrorEvent += TeleportManagerOnLogErrorEvent;
            Interface.CommandManager.AddHandler(CommandName, new CommandInfo(CommandHandler) {HelpMessage = HelpMessage});
        }

        private void TeleportManagerOnLogEvent(string message) {
            PluginLog.Log(message);
            Interface.Framework.Gui.Chat.Print($"[{Name}] {message}");
        }

        private void TeleportManagerOnLogErrorEvent(string message) {
            PluginLog.LogError(message);
            Interface.Framework.Gui.Chat.PrintError($"[{Name}] {message}");
        }

        private void CommandHandler(string command, string arguments) {
            var arg = arguments.Trim();
            if (arg.Equals("help") || string.IsNullOrEmpty(arg) || string.IsNullOrWhiteSpace(arg)) {
                var helpText = $"{Name} Help:\n" +
#if DEBUG
                               "/tp debug - Open the Debug Window\n" +
#endif
                               "/tp <name> <type>\n" +
                               "name: Aetheryte Name (e.g. New Gridania)\n" +
                               "type: (optional) The type of Teleport to use\n" +
                               "  -> map    - Teleport as if using the Worldmap\n" +
                               "  -> ticket - Teleport as if using the Teleport Window\n" +
                               "  -> direct - Teleport without asking for anything (default)";
                Interface.Framework.Gui.Chat.Print(helpText);
                return;
            }

            if (arg.Equals("debug", StringComparison.OrdinalIgnoreCase)) {
                Gui.DebugVisible = !Gui.DebugVisible;
                return;
            }

            var list = ParseArguments(arg);
            var type = list.Last() ?? "direct";
            var location = string.Join(" ", list.Take(list.Count - 1));
            if (type.Equals("direct", StringComparison.OrdinalIgnoreCase)) {
                if (uint.TryParse(location, out var id)) TeleportManager.Teleport(id);
                else TeleportManager.Teleport(location);
            } else if (type.Equals("map", StringComparison.OrdinalIgnoreCase)) {
                if (uint.TryParse(location, out var id)) TeleportManager.TeleportMap(id);
                else TeleportManager.TeleportMap(location);
            } else if (type.Equals("ticket", StringComparison.OrdinalIgnoreCase)) {
                if (uint.TryParse(location, out var id)) TeleportManager.TeleportTicket(id);
                else TeleportManager.TeleportTicket(location);
            } else {
                if (uint.TryParse(arg, out var id))
                    TeleportManager.Teleport(id);
                else TeleportManager.Teleport(arg);
            }
        }

        private static List<string> ParseArguments(string args) {
            var list = new List<string>();
            if (string.IsNullOrEmpty(args) || string.IsNullOrWhiteSpace(args))
                return list;
            var matches = Regex.Matches(args, "(?<=\").*(?=\")|\\w{3,}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (matches.Count == 0) return list;
            for (var i = 0; i < matches.Count; i++)
                list.Add(matches[i].Value);
            return list;
        }

        public void Dispose() {
            TeleportManager.LogEvent -= TeleportManagerOnLogEvent;
            TeleportManager.LogErrorEvent -= TeleportManagerOnLogErrorEvent;
            Interface.CommandManager.RemoveHandler(CommandName);
            Gui?.Dispose();
        }
    }
}