using System;
using System.Linq;
using Dalamud;
using Dalamud.Game.ClientState.Actors.Types;
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
        public PlayerCharacter LocalPlayer => Interface.ClientState.LocalPlayer;
        public bool IsLoggedIn => LocalPlayer != null;
        public bool IsInHomeWorld => LocalPlayer.CurrentWorld == LocalPlayer.HomeWorld;
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
            Interface.CommandManager.AddHandler("/tp", new CommandInfo(CommandHandler) {
                HelpMessage = "/tp <name> - Teleport to <name>"
            });
            Interface.CommandManager.AddHandler("/tpt", new CommandInfo(CommandHandler) {
                HelpMessage = "/tpt <name> - Teleport to <name> using Aetheryte Tickets if possible"
            });
            Gui = new PluginUi(this);
        }
        
        public void Log(string message) {
            Interface.Framework.Gui.Chat.Print($"[{Name}] {message}\0");
        }

        public void LogError(string message) {
            Interface.Framework.Gui.Chat.PrintError($"[{Name}] {message}\0");
        }

        public void CommandHandler(string command, string arguments) {
            var args = arguments.Trim().Replace("\"", string.Empty);
            if (string.IsNullOrEmpty(args) || args.Equals("help", StringComparison.OrdinalIgnoreCase)) {
                PrintHelpMessage(command);
                return;
            }

            if (args.Equals("quick", StringComparison.OrdinalIgnoreCase)) {
                Gui.AetherGateWindow.Visible = !Gui.AetherGateWindow.Visible;
                return;
            }

            if (args.Equals("config", StringComparison.OrdinalIgnoreCase)) {
                Gui.ConfigWindow.Visible = !Gui.ConfigWindow.Visible;
                return;
            }

            if (args.Equals("debug", StringComparison.OrdinalIgnoreCase)) {
                Gui.DebugWindow.Visible = !Gui.DebugWindow.Visible;
                return;
            }

            HandleTeleportCommand(command, args);
        }
        
        private void HandleTeleportCommand(string command, string args) {
            var locationString = args;
            var type = command.Equals("/tpt", StringComparison.OrdinalIgnoreCase) ? TeleportType.Ticket : TeleportType.Direct;

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

        private void PrintHelpMessage(string command) {
            var helpText =
                $"{Name} Help:\n" +
                $"{command} help - Show this Message\n" +
#if DEBUG
                $"{command} debug - Show Debug Window\n" +
#endif
                $"{command} config - Show Settings Window\n" +
                $"{command} quick - Show Quick Teleport Window\n";
            if (command.Equals("/tpt", StringComparison.OrdinalIgnoreCase))
                helpText += $"{command} <name> - Teleport to <name> using Aetheryte Tickets if possible (e.g. /tpt New Gridania)";
            else helpText += $"{command} <name> - Teleport to <name> (e.g. /tp New Gridania)";
            Interface.Framework.Gui.Chat.Print($"{helpText}\0");
        }

        public void Dispose() {
            Interface.CommandManager.RemoveHandler("/tp");
            Interface.CommandManager.RemoveHandler("/tpt");
            Gui?.Dispose();
            Interface?.Dispose();
        }
    }
}