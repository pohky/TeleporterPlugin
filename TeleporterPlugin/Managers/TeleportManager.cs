﻿using System;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.GeneratedSheets;
using TeleporterPlugin.Plugin;

namespace TeleporterPlugin.Managers {
    public static unsafe class TeleportManager {
        private delegate bool TeleportWithTicketDelegate(SelectUseTicketInvoker* suti, uint aetheryteId, byte subIndex);
        private static Hook<TeleportWithTicketDelegate> m_TicketHook = null!;

        public static void Load() {
            m_TicketHook = new Hook<TeleportWithTicketDelegate>((IntPtr)SelectUseTicketInvoker.fpTeleportWithTickets, TicketDetour);
            m_TicketHook.Enable();
        }

        public static void UnLoad() {
            m_TicketHook.Dispose();
        }

        private static bool TicketDetour(SelectUseTicketInvoker* suti, uint aetheryteId, byte subIndex) {
            if (GetAetheryteTicketCount() == 0)
                return false;

            var useTicket = true;
            if (TeleporterPluginMain.Config.UseGilThreshold) {
                if (AetheryteManager.AvailableAetherytes.Count == 0)
                    AetheryteManager.UpdateAvailableAetherytes();
                for (ulong i = 0; i < suti->Telepo->TeleportList.Size(); i++) {
                    var info = suti->Telepo->TeleportList.Get(i);
                    if (info.AetheryteId == aetheryteId && info.SubIndex == subIndex) {
                        useTicket = info.GilCost > TeleporterPluginMain.Config.GilThreshold;
                        break;
                    }
                }
            }

            if (!useTicket) return false;

            if (!TeleporterPluginMain.Config.SkipTicketPopup)
                return m_TicketHook.Original(suti, aetheryteId, subIndex);

            TeleporterPluginMain.Address.ExecuteCommand(0xCA, aetheryteId, 1, subIndex, 0);
            return true;
        }

        private static int GetAetheryteTicketCount() {
            if (TeleporterPluginMain.ClientState.LocalPlayer == null)
                return 0;
            return TeleporterPluginMain.Address.GetInventoryItemCount(InventoryManager.Instance(), 7569, 0, 0, 1, 0);
        }

        public static bool Teleport(TeleportInfo info) {
            if (TeleporterPluginMain.ClientState.LocalPlayer == null)
                return false;
            var status = ActionManager.Instance()->GetActionStatus(ActionType.Spell, 5);
            if (status != 0) {
                var msg = GetLogMessage(status);
                if (!string.IsNullOrEmpty(msg))
                    TeleporterPluginMain.LogChat(msg, true);
                return false;
            }

            if (TeleporterPluginMain.ClientState.LocalPlayer.CurrentWorld.Id != TeleporterPluginMain.ClientState.LocalPlayer.HomeWorld.Id) {
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
            var status = ActionManager.Instance()->GetActionStatus(ActionType.Spell, 5);
            if (status != 0) {
                var msg = GetLogMessage(status);
                if(!string.IsNullOrEmpty(msg))
                    TeleporterPluginMain.LogChat(msg, true);
                return false;
            }

            if (TeleporterPluginMain.ClientState.LocalPlayer.CurrentWorld.Id != TeleporterPluginMain.ClientState.LocalPlayer.HomeWorld.Id) {
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
            var row = sheet.GetRow(id);
            return row == null ? string.Empty : row.Text.ToString();
        }
    }
}