using System;
using Dalamud.Game.Command;
using Teleporter.Plugin;

namespace Teleporter.Managers {
    public static class CommandManager {
        public static void Load() {
            TeleporterPlugin.Commands.RemoveHandler("/tp");
            TeleporterPlugin.Commands.AddHandler("/tp", new CommandInfo(CommandHandler) {
                ShowInHelp = true,
                HelpMessage = "/tp <aetheryte name> - Teleport to aetheryte"
            });
            TeleporterPlugin.Commands.AddHandler("/tpm", new CommandInfo(CommandHandlerMaps) {
                ShowInHelp = true,
                HelpMessage = "/tpm <map name> - Teleport to map"
            });
        }

        public static void UnLoad() {
            TeleporterPlugin.Commands.RemoveHandler("/tp");
            TeleporterPlugin.Commands.RemoveHandler("/tpm");
        }

        private static void CommandHandlerMaps(string cmd, string arg) {
            if (string.IsNullOrEmpty(arg)) {
                TeleporterPlugin.OnOpenConfigUi();
                return;
            }
            arg = TeleporterPlugin.PluginInterface.Sanitizer.Sanitize(arg);
            if (!AetheryteManager.TryFindAetheryteByMapName(arg, TeleporterPlugin.Config.AllowPartialName, out var info)) {
                TeleporterPlugin.LogChat($"No attuned Aetheryte found for '{arg}'.", true);
                return;
            }

            TeleporterPlugin.LogChat($"Teleporting to {AetheryteManager.GetAetheryteName(info)}.");
            TeleportManager.Teleport(info);
        }

        private static void CommandHandler(string cmd, string arg) {
            if (string.IsNullOrEmpty(arg)) {
                TeleporterPlugin.OnOpenConfigUi();
                return;
            }
            arg = TeleporterPlugin.PluginInterface.Sanitizer.Sanitize(arg);
            
            if (TryFindAliasByName(arg, TeleporterPlugin.Config.AllowPartialName, out var alias)) {
                TeleporterPlugin.LogChat($"Teleporting to {AetheryteManager.GetAetheryteName(alias)}.");
                TeleportManager.Teleport(alias);
                return;
            }

            if (!AetheryteManager.TryFindAetheryteByName(arg, TeleporterPlugin.Config.AllowPartialName, out var info)) {
                TeleporterPlugin.LogChat($"No attuned Aetheryte found for '{arg}'.", true);
                return;
            }

            TeleporterPlugin.LogChat($"Teleporting to {AetheryteManager.GetAetheryteName(info)}.");
            TeleportManager.Teleport(info);
        }
        
        private static bool TryFindAliasByName(string name, bool matchPartial, out TeleportAlias alias) {
            alias = new TeleportAlias();
            foreach (var teleportAlias in TeleporterPlugin.Config.AliasList) {
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