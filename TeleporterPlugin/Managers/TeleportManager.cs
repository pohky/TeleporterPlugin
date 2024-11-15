using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using TeleporterPlugin.Plugin;

namespace TeleporterPlugin.Managers {
    public static unsafe class TeleportManager {
        public static bool Teleport(TeleportInfo info) {
            if (TeleporterPluginMain.ClientState.LocalPlayer == null)
                return false;
            var status = ActionManager.Instance()->GetActionStatus(ActionType.Action, 5);
            if (status != 0) {
                var msg = GetLogMessage(status);
                if (!string.IsNullOrEmpty(msg))
                    TeleporterPluginMain.LogChat(msg, true);
                return false;
            }

            if (TeleporterPluginMain.ClientState.LocalPlayer.CurrentWorld.RowId != TeleporterPluginMain.ClientState.LocalPlayer.HomeWorld.RowId) {
                if (AetheryteManager.IsHousingAetheryte(info.AetheryteId, info.Plot, info.Ward, info.SubIndex)) {
                    TeleporterPluginMain.LogChat($"Unable to Teleport to {AetheryteManager.GetAetheryteName(info)} while visiting other Worlds.", true);
                    return false;
                }
            }

            return Telepo.Instance()->Teleport(info.AetheryteId, info.SubIndex);
        }

        public static bool Teleport(TeleportAlias alias) {
            if (TeleporterPluginMain.ClientState.LocalPlayer == null)
                return false;
            var status = ActionManager.Instance()->GetActionStatus(ActionType.Action, 5);
            if (status != 0) {
                var msg = GetLogMessage(status);
                if(!string.IsNullOrEmpty(msg))
                    TeleporterPluginMain.LogChat(msg, true);
                return false;
            }

            if (TeleporterPluginMain.ClientState.LocalPlayer.CurrentWorld.RowId != TeleporterPluginMain.ClientState.LocalPlayer.HomeWorld.RowId) {
                if (AetheryteManager.IsHousingAetheryte(alias.AetheryteId, alias.Plot, alias.Ward, alias.SubIndex)) {
                    TeleporterPluginMain.LogChat($"Unable to Teleport to {alias.Aetheryte} while visiting other Worlds.", true);
                    return false;
                }
            }
            return Telepo.Instance()->Teleport(alias.AetheryteId, alias.SubIndex);
        }

        private static string GetLogMessage(uint id) {
            var sheet = TeleporterPluginMain.Data.GetExcelSheet<LogMessage>();
            if (sheet == null) return string.Empty;
            var row = sheet.GetRow(id).ToString();
            return row == null ? string.Empty : row;
        }
    }
}