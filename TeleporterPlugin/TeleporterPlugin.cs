using System;
using System.Linq;
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

        public TeleportAddressResolver Address { get; private set; }

        public bool IsLoggedIn => LocalPlayer != null;
        public bool IsInHomeWorld => LocalPlayer?.CurrentWorld.Id == LocalPlayer?.HomeWorld.Id;
        
        public void Initialize(DalamudPluginInterface pluginInterface) {
            Interface = pluginInterface;
            Config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

            Config.Initialize(pluginInterface);
            AetheryteDataManager.Init(pluginInterface);
            
            Address = new TeleportAddressResolver();
            Address.Setup(pluginInterface.TargetModuleScanner);

            Manager = new TeleportManager(this);
            Gui = new PluginUi(this);
            
            Interface.CommandManager.AddHandler("/tp", new CommandInfo(CommandHandler) {
                HelpMessage = "/tp <aetheryte> - Teleport to Aetheryte"
            });
            Interface.CommandManager.AddHandler("/tpt", new CommandInfo(CommandHandler) {
                HelpMessage = "/tpt <aetheryte> - Teleport to Aetheryte using Aetheryte Tickets"
            });
            Interface.CommandManager.AddHandler("/tpm", new CommandInfo(CommandHandler) {
                HelpMessage = "/tpm <mapname> - Teleport to first Aetheryte on that Map"
            });
            Interface.CommandManager.AddHandler("/tptm", new CommandInfo(CommandHandler) {
                HelpMessage = "/tptm <mapname> - Teleport to first Aetheryte on that Map using Aetheryte Tickets"
            });
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
            if (string.IsNullOrEmpty(args)) {
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
                var locationName = useMap ? AetheryteDataManager.GetAetheryteLocationsByTerritoryName(locationString, Config.Language, matchPartial).FirstOrDefault()?.Name : locationString;
                var location = Manager.GetLocationByName(locationName);
                if (location != null)
                    if (location.GilCost > Config.GilThreshold)
                        type = TeleportType.Ticket;
            }

            if (type == TeleportType.Ticket) {
                Manager.TeleportTicket(locationString, Config.SkipTicketPopup, matchPartial, useMap);
            } else {
                Manager.Teleport(locationString, matchPartial, useMap);
            }
        }

        private bool TryGetAlias(string name, out TeleportAlias alias, bool matchPartial) {
            if(matchPartial)
                alias = Config.AliasList.FirstOrDefault(a => a.Alias.StartsWith(name, StringComparison.OrdinalIgnoreCase));
            else
                alias = Config.AliasList.FirstOrDefault(a => name.Equals(a.Alias, StringComparison.OrdinalIgnoreCase));
            return alias != null;
        }

        public void Dispose() {
            Interface?.CommandManager.RemoveHandler("/tp");
            Interface?.CommandManager.RemoveHandler("/tpt");
            Interface?.CommandManager.RemoveHandler("/tpm");
            Interface?.CommandManager.RemoveHandler("/tptm");
            Gui?.Dispose();
            Manager?.Dispose();
            Interface?.Dispose();
        }
    }
}