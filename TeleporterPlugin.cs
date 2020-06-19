using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using TeleporterPlugin.Managers;
using TeleporterPlugin.Objects;

namespace TeleporterPlugin {
    public class TeleporterPlugin : IDalamudPlugin {
        private const string CommandName = "/tp";
        public string Name => "Teleporter";
        public PluginUi Gui { get; private set; }
        public DalamudPluginInterface Interface { get; private set; }
        public Configuration Config { get; private set; }

        public void Initialize(DalamudPluginInterface pluginInterface) {
            Interface = pluginInterface;
            Config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Config.Initialize(pluginInterface);
            TeleportManager.Init(Interface);
            TeleportManager.LogEvent += TeleportManagerOnLogEvent;
            TeleportManager.LogErrorEvent += TeleportManagerOnLogErrorEvent;
            Interface.CommandManager.AddHandler(CommandName, new CommandInfo(CommandHandler) {
                HelpMessage = "/tp <name> <type>. Use '/tp' for more info."
            });
            Gui = new PluginUi(this);
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
            var arg = arguments.Trim().Replace("\"", "");
            if (string.IsNullOrEmpty(arg) || arg.Equals("help", StringComparison.OrdinalIgnoreCase)) {
                PrintHelpText();
                return;
            }

            if (arg.Equals("config", StringComparison.OrdinalIgnoreCase)) {
                Gui.SettingsVisible = !Gui.SettingsVisible;
                return;
            }

            if (arg.Equals("debug", StringComparison.OrdinalIgnoreCase)) {
                Gui.DebugVisible = !Gui.DebugVisible;
                return;
            }

            HandleTeleportArguments(SplitArguments(arg));
        }

        private void HandleTeleportArguments(List<string> args) {
            string locationString;
            
            var type = GetTeleportTypeFromArguments(args);
            if (!type.HasValue) {
                type = Config.DefaultTeleportType;
                locationString = string.Join(" ", args);
            } else locationString = string.Join(" ", args.Take(args.Count - 1));

            if (TryGetAlias(locationString, out var alias))
                locationString = alias.Aetheryte;

            if (Config.UseGilThreshold) {
                var location = TeleportManager.GetLocationByName(locationString);
                if (location.HasValue) {
                    var price = (int)location.Value.GilCost;
                    if (price > Config.GilThreshold)
                        type = TeleportType.Ticket;
                }
            }
            
            switch (type) {
                case TeleportType.Direct:
                    TeleportManager.Teleport(locationString, Config.AllowPartialMatch); break;
                case TeleportType.Ticket:
                    TeleportManager.TeleportTicket(locationString, Config.SkipTicketPopup, Config.AllowPartialMatch); break;
                default:
                    TeleportManagerOnLogErrorEvent($"Unable to get a valid type for Teleport: '{string.Join(" ", args)}'");
                    break;
            }
        }

        private bool TryGetAlias(string name, out TeleportAlias alias) {
            alias = Config.AliasList.FirstOrDefault(a => name.Equals(a.Alias, StringComparison.OrdinalIgnoreCase));
            return alias != null;
        }

        private static TeleportType? GetTeleportTypeFromArguments(IEnumerable<string> args) {
            var last = args.LastOrDefault() ?? string.Empty;
            if (Enum.TryParse<TeleportType>(last, true, out var type))
                return type;
            return null;
        }

        private static List<string> SplitArguments(string args) {
            if (string.IsNullOrEmpty(args) || string.IsNullOrWhiteSpace(args))
                return new List<string>();
            return new List<string>(args.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries));
        }

        private void PrintHelpText() {
            var helpText = $"{Name} Help:\n" +
                           "/tp help - Show this Message\n" +
#if DEBUG
                           "/tp debug - Show Debug Window\n" +
#endif
                           "/tp config - Show Settings Window\n" +
                           "/tp <name> <type>\n" +
                           "name: Aetheryte Name (e.g. New Gridania)\n" +
                           "type: (optional) The type of Teleport to use\n" +
                           "  -> ticket - Teleport using Aetheryte Tickets\n" +
                           "  -> direct - Teleport without asking for anything";
            Interface.Framework.Gui.Chat.Print(helpText);
        }

        public void Dispose() {
            TeleportManager.LogEvent -= TeleportManagerOnLogEvent;
            TeleportManager.LogErrorEvent -= TeleportManagerOnLogErrorEvent;
            Interface.CommandManager.RemoveHandler(CommandName);
            Gui?.Dispose();
        }
    }
}