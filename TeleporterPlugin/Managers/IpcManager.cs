using System.Linq;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using TeleporterPlugin.Plugin;

namespace TeleporterPlugin.Managers {
    public static unsafe class IpcManager {
        private static ICallGateProvider<uint, byte, bool>? m_CallGateTp;
        private static ICallGateProvider<bool>? m_CallGateTpMessage;
        
        public static void Register(IDalamudPluginInterface pluginInterface) {
            Unregister();
            
            m_CallGateTp = pluginInterface.GetIpcProvider<uint, byte, bool>("Teleport");
            m_CallGateTp.RegisterFunc(IpcTeleport);

            m_CallGateTpMessage = pluginInterface.GetIpcProvider<bool>("Teleport.ChatMessage");
            m_CallGateTpMessage.RegisterFunc(IpcChatMessageSetting);
        }

        private static bool IpcChatMessageSetting() {
            return TeleporterPluginMain.Config.ChatMessage;
        }

        private static bool IpcTeleport(uint aetheryteId, byte subIndex) {
            if (!AetheryteManager.UpdateAvailableAetherytes())
                return false;
            if (!AetheryteManager.AvailableAetherytes.Any(tp => tp.AetheryteId == aetheryteId && tp.SubIndex == subIndex))
                return false;
            if (ActionManager.Instance()->GetActionStatus(ActionType.Action, 5) != 0)
                return false;
            return Telepo.Instance()->Teleport(aetheryteId, subIndex);
        }

        public static void Unregister() {
            m_CallGateTp?.UnregisterFunc();
            m_CallGateTp = null;

            m_CallGateTpMessage?.UnregisterFunc();
            m_CallGateTpMessage = null;
        }
    }
}