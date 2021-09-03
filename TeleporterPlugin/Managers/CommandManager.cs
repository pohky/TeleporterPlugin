using System;
using Dalamud.Game.Command;
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

        private static void CommandHandlerMaps(string cmd, string arg) {
            if (string.IsNullOrEmpty(arg)) {
                Plugin.TeleporterPluginMain.OnOpenConfigUi();
                return;
            }

            if (AetheryteManager.AvailableAetherytes.Count == 0)
                AetheryteManager.UpdateAvailableAetherytes();

            arg = CleanArgument(arg);

            if (!AetheryteManager.TryFindAetheryteByMapName(arg, Plugin.TeleporterPluginMain.Config.AllowPartialName, out var info)) {
                Plugin.TeleporterPluginMain.LogChat($"No attuned Aetheryte found for '{arg}'.", true);
                return;
            }

            Plugin.TeleporterPluginMain.LogChat($"Teleporting to {AetheryteManager.GetAetheryteName(info)}.");
            TeleportManager.Teleport(info);
        }

        private static void CommandHandler(string cmd, string arg) {
            if (string.IsNullOrEmpty(arg)) {
                Plugin.TeleporterPluginMain.OnOpenConfigUi();
                return;
            }

            if (AetheryteManager.AvailableAetherytes.Count == 0)
                AetheryteManager.UpdateAvailableAetherytes();

            if (TryFindAliasByName(arg, Plugin.TeleporterPluginMain.Config.AllowPartialName, out var alias)) {
                Plugin.TeleporterPluginMain.LogChat($"Teleporting to {AetheryteManager.GetAetheryteName(alias)}.");
                TeleportManager.Teleport(alias);
                return;
            }

            arg = CleanArgument(arg);

            if (!AetheryteManager.TryFindAetheryteByName(arg, Plugin.TeleporterPluginMain.Config.AllowPartialName, out var info)) {
                Plugin.TeleporterPluginMain.LogChat($"No attuned Aetheryte found for '{arg}'.", true);
                return;
            }

            Plugin.TeleporterPluginMain.LogChat($"Teleporting to {AetheryteManager.GetAetheryteName(info)}.");
            TeleportManager.Teleport(info);
        }

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