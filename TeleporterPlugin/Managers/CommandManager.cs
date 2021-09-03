using System;
using System.Linq;
using Dalamud.Game.Command;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using TeleporterPlugin.Plugin;

namespace TeleporterPlugin.Managers {
    public static class CommandManager {
        public static void Load() {
            Plugin.TeleporterPluginMain.Commands.RemoveHandler("/tp");
            Plugin.TeleporterPluginMain.Commands.AddHandler("/tp", new CommandInfo(CommandHandler) {
                ShowInHelp = true,
                HelpMessage = "/tp <aetheryte name> - Teleport to aetheryte"
            });
            Plugin.TeleporterPluginMain.Commands.AddHandler("/tpm", new CommandInfo(CommandHandlerMaps) {
                ShowInHelp = true,
                HelpMessage = "/tpm <map name> - Teleport to map"
            });
        }

        public static void UnLoad() {
            Plugin.TeleporterPluginMain.Commands.RemoveHandler("/tp");
            Plugin.TeleporterPluginMain.Commands.RemoveHandler("/tpm");
        }

        internal static bool TeleportByAetheryteId(uint id) {
            if (AetheryteManager.AvailableAetherytes.Count == 0
             && !AetheryteManager.UpdateAvailableAetherytes())
                return false;

            var info = AetheryteManager.AvailableAetherytes.Cast<TeleportInfo?>()
               .First(a => a!.Value.AetheryteId == id);
            if (info == null)
                return false;

            return TeleportManager.Teleport(info.Value);
        }

        internal static bool TeleportByTerritoryId(uint id) {
            if (AetheryteManager.AvailableAetherytes.Count == 0
              && !AetheryteManager.UpdateAvailableAetherytes())
                return false;

            var info = AetheryteManager.AvailableAetherytes.Cast<TeleportInfo?>()
               .First(a => a!.Value.TerritoryId == id);
            if (info == null)
                return false;

            return TeleportManager.Teleport(info.Value);
        }

        internal static bool TeleportByName(string name) {
            if (string.IsNullOrEmpty(name)) {
                Plugin.TeleporterPluginMain.OnOpenConfigUi();
                return false;
            }

            if (AetheryteManager.AvailableAetherytes.Count == 0)
                AetheryteManager.UpdateAvailableAetherytes();

            name = CleanArgument(name);

            if (!AetheryteManager.TryFindAetheryteByMapName(name, Plugin.TeleporterPluginMain.Config.AllowPartialName, out var info)) {
                Plugin.TeleporterPluginMain.LogChat($"No attuned Aetheryte found for '{name}'.", true);
                return false;
            }

            Plugin.TeleporterPluginMain.LogChat($"Teleporting to {AetheryteManager.GetAetheryteName(info)}.");
            TeleportManager.Teleport(info);
            return true;
        }

        internal static bool TeleportByMapName(string name) {
            if (string.IsNullOrEmpty(name)) {
                Plugin.TeleporterPluginMain.OnOpenConfigUi();
                return false;
            }

            if (AetheryteManager.AvailableAetherytes.Count == 0)
                AetheryteManager.UpdateAvailableAetherytes();

            name = CleanArgument(name);

            if (TryFindAliasByName(name, Plugin.TeleporterPluginMain.Config.AllowPartialName, out var alias)) {
                Plugin.TeleporterPluginMain.LogChat($"Teleporting to {AetheryteManager.GetAetheryteName(alias)}.");
                TeleportManager.Teleport(alias);
                return false;
            }

            if (!AetheryteManager.TryFindAetheryteByName(name, Plugin.TeleporterPluginMain.Config.AllowPartialName, out var info)) {
                Plugin.TeleporterPluginMain.LogChat($"No attuned Aetheryte found for '{name}'.", true);
                return false;
            }

            Plugin.TeleporterPluginMain.LogChat($"Teleporting to {AetheryteManager.GetAetheryteName(info)}.");
            TeleportManager.Teleport(info);
            return true;
        }

        private static void CommandHandlerMaps(string cmd, string arg)
            => TeleportByMapName(arg);

        private static void CommandHandler(string cmd, string arg)
            => TeleportByName(arg);

        private static string CleanArgument(string arg) {
            //remove autotranslate arrows and double spaces
            arg = arg.Replace("\xe040", "").Replace("\xe041", "");
            arg = arg.Replace("  ", " ");
            return arg.Trim();
        }

        private static bool TryFindAliasByName(string name, bool matchPartial, out TeleportAlias alias) {
            //TODO Support multiple matches, maybe by checking which of the matches can be used and only return that
            alias = new TeleportAlias();
            foreach (var teleportAlias in Plugin.TeleporterPluginMain.Config.AliasList) {
                var result = matchPartial && teleportAlias.Alias.Contains(name, StringComparison.OrdinalIgnoreCase);
                if (!result && !teleportAlias.Alias.Equals(name, StringComparison.OrdinalIgnoreCase))
                    continue;
                alias = teleportAlias;
                return true;
            }
            return false;
        }
    }
}