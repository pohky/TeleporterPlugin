using System.Linq;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace TeleporterPlugin.Managers {
    public static unsafe class IpcManager {
        private static ICallGateProvider<uint, byte, bool>? m_CallGate;

        public static void Register(DalamudPluginInterface pluginInterface) {
            Unregister();
            m_CallGate = pluginInterface.GetIpcProvider<uint, byte, bool>("Teleporter");
            m_CallGate.RegisterFunc(IpcTeleport);
        }

        private static bool IpcTeleport(uint aetheryteId, byte subIndex) {
            if (!AetheryteManager.UpdateAvailableAetherytes())
                return false;
            if (!AetheryteManager.AvailableAetherytes.Any(tp => tp.AetheryteId == aetheryteId && tp.SubIndex == subIndex))
                return false;
            if (ActionManager.Instance()->GetActionStatus(ActionType.Spell, 5) != 0)
                return false;
            return Telepo.Instance()->Teleport(aetheryteId, subIndex);
        }

        public static void Unregister() {
            m_CallGate?.UnregisterFunc();
            m_CallGate = null;
        }
    }
}