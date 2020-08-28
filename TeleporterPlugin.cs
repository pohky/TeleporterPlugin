using System;
using System.Linq;
using System.Text.RegularExpressions;
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
        public bool IsInHomeWorld => LocalPlayer?.CurrentWorld == LocalPlayer?.HomeWorld;

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
                HelpMessage = "<name> - Teleport to <name>"
            });
            Interface.CommandManager.AddHandler("/tpt", new CommandInfo(CommandHandler) {
                HelpMessage = "<name> - Teleport to <name> using Aetheryte Tickets if possible"
            });
            Interface.CommandManager.AddHandler("/tpm", new CommandInfo(CommandHandler) {
                HelpMessage = "<mapname> - Teleport to <mapname>"
            });
            Interface.CommandManager.AddHandler("/tptm", new CommandInfo(CommandHandler) {
                HelpMessage = "<mapname> - Teleport to <mapname> using Aetheryte Tickets if possible"
            });
            Gui = new PluginUi(this);
            Interface.Framework.Gui.Chat.OnChatMessage += Gui.LinksWindow.ChatOnChatMessage;
        }
        
        public void Log(string message) {
            if (!Config.PrintMessage) return;
            var msg = $"[{Name}] {message}";
            Interface.Framework.Gui.Chat.Print(msg);
        }

        public void LogError(string message) {
            var msg = $"[{Name}] {message}";
            PluginLog.LogError(msg);
            if (!Config.PrintMessage) return;
            Interface.Framework.Gui.Chat.PrintError(msg);
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

            if (args.Equals("links", StringComparison.OrdinalIgnoreCase)) {
                Gui.LinksWindow.Visible = !Gui.LinksWindow.Visible;
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
            var locationString = args.Replace("\xe040", "").Replace("\xe041", "").Trim();
            
            var type = command.Equals("/tpt", StringComparison.OrdinalIgnoreCase) || command.Equals("/tptm", StringComparison.OrdinalIgnoreCase)
                ? TeleportType.Ticket 
                : TeleportType.Direct;

            var useMap = command.EndsWith("m", StringComparison.OrdinalIgnoreCase);
            var matchPartial = Config.AllowPartialMatch;

            if (TryGetAlias(locationString, out var alias, Config.AllowPartialAlias)) {
                locationString = alias.Aetheryte;
                matchPartial = false;
            }

            if (Config.UseGilThreshold) {
                var location = Manager.GetLocationByName(locationString);
                if (location != null)
                    if (location.GilCost > Config.GilThreshold)
                        type = TeleportType.Ticket;
            }
            
            switch (type) {
                case TeleportType.Direct:
                    Manager.Teleport(locationString, matchPartial, useMap);
                    break;
                case TeleportType.Ticket:
                    Manager.TeleportTicket(locationString, Config.SkipTicketPopup, matchPartial, useMap);
                    break;
                default:
                    LogError($"Unable to get a valid type for Teleport: '{string.Join(" ", args)}'");
                    break;
            }
        }

        private bool TryGetAlias(string name, out TeleportAlias alias, bool matchPartial) {
            if(matchPartial)
                alias = Config.AliasList.FirstOrDefault(a => a.Alias.StartsWith(name, StringComparison.OrdinalIgnoreCase));
            else
                alias = Config.AliasList.FirstOrDefault(a => name.Equals(a.Alias, StringComparison.OrdinalIgnoreCase));
            return alias != null;
        }

        private void PrintHelpMessage(string command) {
            var helpText =
                $"{Name} Help:\n" +
#if DEBUG
                $"{command} debug - Show Debug Window\n" +
#endif
                $"{command} config - Show Settings Window\n" +
                $"{command} links - Show Maplink Tracker\n" +
                $"{command} quick - Show AetherGate Window\n" +
                "/tp <name> - Teleport to <name> (/tp New Gridania)\n" +
                "/tpt <name> - Teleport using Aetheryte tickets if possible\n" +
                "/tpm <mapname> - Teleport to <mapname> (/tpm The Peaks)\n" +
                "/tptm <mapname> - Teleport using Aetheryte tickets if possible";
            Interface.Framework.Gui.Chat.Print($"{helpText}\0");
        }

        public void Dispose() {
            if(Gui != null)
                Interface.Framework.Gui.Chat.OnChatMessage -= Gui.LinksWindow.ChatOnChatMessage;
            Interface.CommandManager.RemoveHandler("/tp");
            Interface.CommandManager.RemoveHandler("/tpt");
            Interface.CommandManager.RemoveHandler("/tpm");
            Interface.CommandManager.RemoveHandler("/tptm");
            Gui?.Dispose();
            Manager?.Dispose();
            Interface?.Dispose();
        }
    }
}