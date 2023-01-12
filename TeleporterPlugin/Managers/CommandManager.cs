using System;
using System.Linq;
using Dalamud.Game.Command;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.GeneratedSheets;
using TeleporterPlugin.Plugin;

namespace TeleporterPlugin.Managers {
    public static class CommandManager {
        public static void Load() {
            TeleporterPluginMain.Commands.RemoveHandler("/tp");
            TeleporterPluginMain.Commands.AddHandler("/tp", new CommandInfo(CommandHandler) {
                ShowInHelp = true,
                HelpMessage = "/tp <aetheryte name> - Teleport to aetheryte (use just '/tp' to open the Config)"
            });
            TeleporterPluginMain.Commands.RemoveHandler("/tpm");
            TeleporterPluginMain.Commands.AddHandler("/tpm", new CommandInfo(CommandHandlerMaps) {
                ShowInHelp = true,
                HelpMessage = "/tpm <map name> - Teleport to map"
            });
        }

        public static void UnLoad() {
            TeleporterPluginMain.Commands.RemoveHandler("/tp");
            TeleporterPluginMain.Commands.RemoveHandler("/tpm");
        }

        private static void CommandHandlerMaps(string cmd, string arg) {
            if (string.IsNullOrEmpty(arg)) {
                TeleporterPluginMain.OnOpenConfigUi();
                return;
            }

            if (TeleporterPluginMain.ClientState.LocalPlayer == null)
                return;

            AetheryteManager.UpdateAvailableAetherytes();

            arg = CleanArgument(arg);

            if (!AetheryteManager.TryFindAetheryteByMapName(arg, TeleporterPluginMain.Config.AllowPartialName, out var info)) {
                TeleporterPluginMain.LogChat($"No attuned Aetheryte found for '{arg}'.", true);
                return;
            }

            TeleporterPluginMain.LogChat($"Teleporting to {AetheryteManager.GetAetheryteName(info)}.");
            TeleportManager.Teleport(info);
        }

        private static void CommandHandler(string cmd, string arg) {
            arg = CleanArgument(arg);

            if (string.IsNullOrEmpty(arg)) {
                TeleporterPluginMain.OnOpenConfigUi();
                return;
            }

            if(TeleporterPluginMain.ClientState.LocalPlayer == null)
                return;

            AetheryteManager.UpdateAvailableAetherytes();
            
            if (TeleporterPluginMain.Config.EnableGrandCompany &&
                arg.Equals(TeleporterPluginMain.Config.GrandCompanyAlias, StringComparison.OrdinalIgnoreCase)) {
                unsafe {
	                var gc = PlayerState.Instance()->GrandCompany;
                    if (gc == 0) return;
                    uint gcTicket = gc switch {
                        1 => 21069, //Maelstrom
                        2 => 21070, //Order of the Twin Adder
                        3 => 21071, //Immortal Flames
                        _ => 0
                    };
                    if (gcTicket == 0)
                        return;

                    var cnt = InventoryManager.Instance()->GetInventoryItemCount(gcTicket);
                    if (cnt < 1) {
                        TeleporterPluginMain.LogChat("You do not have the required GC Tickets.", true);
                        return;
                    }

                    var gcName = TryGetGrandCompanyName(gc, out var name) ? name : $"GrandCompany{gc}";
                    TeleporterPluginMain.LogChat($"Teleporting to {gcName}.");
                    ActionManager.Instance()->UseAction(ActionType.Item, gcTicket, 0xE000_0000, 65535);
                    return;
                }
            }

            if (TeleporterPluginMain.Config.EnableEternityRing 
                && arg.Equals(TeleporterPluginMain.Config.EternityRingAlias, StringComparison.OrdinalIgnoreCase)) {
                unsafe {
                    var cnt = InventoryManager.Instance()->GetInventoryItemCount(8575);
                    if (cnt < 1) {
                        TeleporterPluginMain.LogChat("You do not have the Eternity Ring.", true);
                        return;
                    }
                    TeleporterPluginMain.LogChat("Teleporting to Partner.");
                    ActionManager.Instance()->UseAction(ActionType.Item, 8575, 0xE000_0000, ushort.MaxValue);
                    return;
                }
            }

            if (TryFindAliasByName(arg, TeleporterPluginMain.Config.AllowPartialAlias, out var alias)) {
                if (alias.Aetheryte.Equals("NO_DATA", StringComparison.OrdinalIgnoreCase)) {
                    TeleporterPluginMain.LogChat($"Invalid Alias: {alias.Alias} -> {alias.Aetheryte}", true);
                    return;
                }

                TeleporterPluginMain.LogChat($"Teleporting to {AetheryteManager.GetAetheryteName(alias)}.");
                TeleportManager.Teleport(alias);
                return;
            }

            if (!AetheryteManager.TryFindAetheryteByName(arg, TeleporterPluginMain.Config.AllowPartialName, out var info)) {
                TeleporterPluginMain.LogChat($"No attuned Aetheryte found for '{arg}'.", true);
                return;
            }

            TeleporterPluginMain.LogChat($"Teleporting to {AetheryteManager.GetAetheryteName(info)}.");
            TeleportManager.Teleport(info);
        }

        private static string CleanArgument(string arg) {
            //remove autotranslate arrows and double spaces
            arg = arg.Replace("\xe040", "").Replace("\xe041", "");
            arg = arg.Replace("  ", " ");
            return arg.Trim();
        }

        private static bool TryGetGrandCompanyName(byte id, out string name) {
            name = string.Empty;
            if (id == 0) return false;
            var sheet = TeleporterPluginMain.Data.GetExcelSheet<GrandCompany>();
            var row = sheet?.GetRow(id);
            if (row == null) return false;
            name = row.Name.ToString();
            return !string.IsNullOrEmpty(name);
        }

        private static bool TryFindAliasByName(string name, bool matchPartial, out TeleportAlias alias) {
            //TODO Support multiple matches, maybe by checking which of the matches can be used and only return that
            alias = new TeleportAlias();
            foreach (var teleportAlias in TeleporterPluginMain.Config.AliasList) {
                var result = matchPartial && teleportAlias.Alias.Contains(name, StringComparison.OrdinalIgnoreCase);
                if (!result && !teleportAlias.Alias.Equals(name, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!AetheryteManager.AvailableAetherytes.Any(i => teleportAlias.Equals(i)))
                    continue;
                alias = teleportAlias;
                return true;
            }
            return false;
        }
    }
}