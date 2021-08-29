using System;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Teleporter.Plugin;

namespace Teleporter.Managers {
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
            if (TeleporterPlugin.Config.UseGilThreshold) {
                for (ulong i = 0; i < suti->Telepo->TeleportList.Size(); i++) {
                    var info = suti->Telepo->TeleportList.Get(i);
                    if (info.AetheryteId == aetheryteId && info.SubIndex == subIndex) {
                        useTicket = info.GilCost > TeleporterPlugin.Config.GilThreshold;
                        break;
                    }
                }
            }

            if (!useTicket) return false;

            if (!TeleporterPlugin.Config.SkipTicketPopup)
                return m_TicketHook.Original(suti, aetheryteId, subIndex);

            TeleporterPlugin.Address.ExecuteCommand(0xCA, aetheryteId, 1, subIndex, 0);
            return true;
        }

        private static int GetAetheryteTicketCount() {
            if (TeleporterPlugin.ClientState.LocalPlayer == null)
                return 0;
            return TeleporterPlugin.Address.GetInventoryItemCount(InventoryManager.Instance(), 7569, 0, 0, 1, 0);
        }

        public static bool Teleport(TeleportInfo info) {
            if (TeleporterPlugin.ClientState.LocalPlayer == null)
                return false;
            return Telepo.Instance()->Teleport(info.AetheryteId, info.SubIndex);
        }

        public static bool Teleport(TeleportAlias alias) {
            if (TeleporterPlugin.ClientState.LocalPlayer == null)
                return false;
            return Telepo.Instance()->Teleport(alias.AetheryteId, alias.SubIndex);
        }
    }
}