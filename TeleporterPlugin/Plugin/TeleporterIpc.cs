using System;
using Dalamud.Logging;
using Dalamud.Plugin.Ipc;
using TeleporterPlugin.Managers;

namespace TeleporterPlugin.Plugin
{
    public class TeleporterIpc : IDisposable
    {
        public const int Version = 1;

        public const string VersionCallGateName = "Teleporter.Version";
        public const string TpCallGateName      = "Teleporter.Tp";
        public const string TpIdCallGateName    = "Teleporter.TpId";
        public const string TpmCallGateName     = "Teleporter.Tpm";
        public const string TpmIdCallGateName   = "Teleporter.TpmId";

        private readonly ICallGateProvider<int>?          m_VersionCallGate;
        private readonly ICallGateProvider<string, bool>? m_TpCallGate;
        private readonly ICallGateProvider<uint,   bool>? m_TpIdCallGate;
        private readonly ICallGateProvider<string, bool>? m_TpmCallGate;
        private readonly ICallGateProvider<uint,   bool>? m_TpmIdCallGate;

        private static int GetVersion()
            => Version;

        public TeleporterIpc() {
            var pi = TeleporterPluginMain.PluginInterface;
            try {
                m_VersionCallGate = pi.GetIpcProvider<int>(VersionCallGateName);
                m_VersionCallGate.RegisterFunc(GetVersion);
            }
            catch (Exception e) {
                PluginLog.Error($"Could not obtain {VersionCallGateName} callgate:\n{e}");
            }

            try {
                m_TpCallGate = pi.GetIpcProvider<string, bool>(TpCallGateName);
                m_TpCallGate.RegisterFunc(CommandManager.TeleportByName);
            }
            catch (Exception e) {
                PluginLog.Error($"Could not obtain {TpCallGateName} callgate:\n{e}");
            }

            try {
                m_TpIdCallGate = pi.GetIpcProvider<uint, bool>(TpIdCallGateName);
                m_TpIdCallGate.RegisterFunc(CommandManager.TeleportByAetheryteId);
            }
            catch (Exception e) {
                PluginLog.Error($"Could not obtain {TpIdCallGateName} callgate:\n{e}");
            }

            try {
                m_TpmCallGate = pi.GetIpcProvider<string, bool>(TpmCallGateName);
                m_TpmCallGate.RegisterFunc(CommandManager.TeleportByMapName);
            }
            catch (Exception e) {
                PluginLog.Error($"Could not obtain {TpmCallGateName} callgate:\n{e}");
            }

            try {
                m_TpmIdCallGate = pi.GetIpcProvider<uint, bool>(TpmIdCallGateName);
                m_TpmIdCallGate.RegisterFunc(CommandManager.TeleportByTerritoryId);
            }
            catch (Exception e) {
                PluginLog.Error($"Could not obtain {TpmIdCallGateName} callgate:\n{e}");
            }
        }

        public void Dispose()
        {
            m_TpmIdCallGate?.UnregisterFunc();
            m_TpmCallGate?.UnregisterFunc();
            m_TpIdCallGate?.UnregisterFunc();
            m_TpCallGate?.UnregisterFunc();
            m_VersionCallGate?.UnregisterFunc();
        }
    }
}
