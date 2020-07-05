using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using TeleporterPlugin.Managers;
using TeleporterPlugin.Objects;

namespace TeleporterPlugin {
    public class TeleporterPlugin : IDalamudPlugin {
        public string Name => "Teleporter";
        public PluginUi Gui { get; private set; }
        public DalamudPluginInterface Interface { get; private set; }
        public Configuration Config { get; private set; }
        public TeleportManager Manager { get; private set; }

        public ClientLanguage Language {
            get {
                if (Config.TeleporterLanguage == TeleporterLanguage.Client)
                    return Interface.ClientState.ClientLanguage;
                return (ClientLanguage)Config.TeleporterLanguage;
            }
        }

        public void Initialize(DalamudPluginInterface pluginInterface) {
            Interface = pluginInterface;
            Config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Config.Initialize(pluginInterface);
            AetheryteDataManager.Init(pluginInterface);
            Manager = new TeleportManager(this);
            Manager.LogEvent += Log;
            Manager.LogErrorEvent += LogError;
            Interface.CommandManager.AddHandler("/tp", new CommandInfo(CommandHandler) {
                HelpMessage = "/tp <name> - Teleport to <name>"
            });
            Interface.CommandManager.AddHandler("/tpt", new CommandInfo(CommandHandler) {
                HelpMessage = "/tpt <name> - Teleport to <name> using Aetheryte Tickets if possible"
            });
            Gui = new PluginUi(this);
        }

        public void Log(string message) {
            PluginLog.Log(message);
            Interface.Framework.Gui.Chat.Print($"[{Name}] {message}");
        }

        public void LogError(string message) {
            PluginLog.LogError(message);
            Interface.Framework.Gui.Chat.PrintError($"[{Name}] {message}");
        }

        public void CommandHandler(string command, string arguments) {
            var arg = arguments.Trim().Replace("\"", "");
            if (string.IsNullOrEmpty(arg) || arg.Equals("help", StringComparison.OrdinalIgnoreCase)) {
                var helpText =
                    $"{Name} Help:\n" +
                    $"{command} help - Show this Message\n" +
#if DEBUG
                    $"{command} debug - Show Debug Window\n" +
#endif
                    $"{command} config - Show Settings Window\n" +
                    $"{command} quick - Show Quick Teleport Window\n";
                if(command.Equals("/tpt", StringComparison.OrdinalIgnoreCase))
                    helpText += $"{command} <name> - Teleport to <name> using Aetheryte Tickets if possible (e.g. /tpt New Gridania)";
                else helpText += $"{command} <name> - Teleport to <name> (e.g. /tp New Gridania)";
                Interface.Framework.Gui.Chat.Print(helpText);
                return;
            }

            if (arg.Equals("quick", StringComparison.OrdinalIgnoreCase)) {
                Config.UseFloatingWindow = true;
                Gui.FloatingButtonsVisible = true;
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
            if(command.Equals("/tpt", StringComparison.OrdinalIgnoreCase))
                HandleTeleportArguments(SplitArguments(arg), TeleportType.Ticket);
            else HandleTeleportArguments(SplitArguments(arg), TeleportType.Direct);
        }
        
        private void HandleTeleportArguments(List<string> args, TeleportType tpType) {
            string locationString;

            if (!TryGetTeleportType(args, out var type)) {
                type = tpType;
                locationString = string.Join(" ", args);
            } else locationString = string.Join(" ", args.Take(args.Count - 1));
            
            if (TryGetAlias(locationString, out var alias))
                locationString = alias.Aetheryte;

            if (Config.UseGilThreshold) {
                var location = Manager.GetLocationByName(locationString);
                if (location != null) {
                    if (location.GilCost > Config.GilThreshold)
                        type = TeleportType.Ticket;
                }
            }

            switch (type) {
                case TeleportType.Direct:
                    Manager.Teleport(locationString, Config.AllowPartialMatch);
                    break;
                case TeleportType.Ticket:
                    Manager.TeleportTicket(locationString, Config.SkipTicketPopup, Config.AllowPartialMatch);
                    break;
                default:
                    LogError($"Unable to get a valid type for Teleport: '{string.Join(" ", args)}'");
                    break;
            }
        }

        private bool TryGetAlias(string name, out TeleportAlias alias) {
            alias = Config.AliasList.FirstOrDefault(a => name.Equals(a.Alias, StringComparison.OrdinalIgnoreCase));
            return alias != null;
        }

        private static bool TryGetTeleportType(IEnumerable<string> args, out TeleportType type) {
            var last = args.LastOrDefault() ?? string.Empty;
            return Enum.TryParse(last, true, out type);
        }
        
        private static List<string> SplitArguments(string args) {
            if (string.IsNullOrEmpty(args) || string.IsNullOrWhiteSpace(args))
                return new List<string>();
            return new List<string>(args.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries));
        }

        public void Dispose() {
            Manager.LogEvent -= Log;
            Manager.LogErrorEvent -= LogError;
            Interface.CommandManager.RemoveHandler("/tp");
            Interface.CommandManager.RemoveHandler("/tpt");
            Gui?.Dispose();
        }
    }
}