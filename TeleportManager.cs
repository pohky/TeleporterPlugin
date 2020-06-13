using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;

namespace TeleporterPlugin {
    internal static class TeleportManager {
        private delegate IntPtr GetAvalibleLocationListDelegate(IntPtr locationsPtr, uint arg1);
        private delegate void TeleportDirectDelegate(uint cmd, uint aetheryteId, uint arg3, uint subIndex, uint arg5);
        private delegate void TeleportWithTicketDelegate(IntPtr locationsPtr, uint aetheryteId, byte arg2);
        private delegate void TeleportWithMapClickDelegate(IntPtr mapAgent, int arg2, uint aetheryteId, uint arg4);
        private delegate IntPtr GetUiModuleDelegate();

        private static GetAvalibleLocationListDelegate _getAvalibleLocationList;
        private static TeleportDirectDelegate _teleportDirect;
        private static TeleportWithTicketDelegate _teleportWithTicket;
        private static TeleportWithMapClickDelegate _teleportWithMapClick;
        private static GetUiModuleDelegate _getUiModule;

        public static IntPtr AvailableLocationsAddress { get; private set; }
        public static IntPtr UiModuleAddress => _getUiModule?.Invoke() ?? IntPtr.Zero;
        public static IntPtr UiAgentModuleAddress => UiModuleAddress != IntPtr.Zero ? UiModuleAddress + 0xBAB50 : IntPtr.Zero;

        public static Dictionary<uint, string> AetheryteNames { get; private set; } = new Dictionary<uint, string>();

        public static IEnumerable<TeleportLocation> AvailableLocations => GetAvailableLocations();

        public static void Teleport(uint aetheryteId) {
            var location = GetAvailableLocations().FirstOrDefault(o => o.AetheryteId == aetheryteId);
            if (location.AetheryteId <= 0) {
                PluginLog.LogError($"No valid Aetheryte found for ID '{aetheryteId}'.");
                return;
            }
            GetDirectTeleport(location)?.Execute();
        }

        public static void Teleport(string aetheryteName, bool matchPartial = true) {
            var location = GetAvailableLocations().FirstOrDefault(o => 
                    o.Name.Equals(aetheryteName, StringComparison.OrdinalIgnoreCase) ||
                    matchPartial && o.Name.ToUpper().StartsWith(aetheryteName.ToUpper()));
            if (location.AetheryteId <= 0) {
                PluginLog.LogError($"No valid Aetheryte found for '{aetheryteName}'.");
                return;
            }
            GetDirectTeleport(location)?.Execute();
        }

        #region GetTeleport

        public static TeleportAction GetDirectTeleport(TeleportLocation? location) {
            if (!location.HasValue) return null;
            return new TeleportAction(location.Value, () => {
                PluginLog.Log($"Starting Teleport to '{location.Value.Name}'");
                _teleportDirect?.Invoke(0xCA, location.Value.AetheryteId, 0, location.Value.SubIndex, 0);
            });
        }

        public static TeleportAction GetTicketTeleport(TeleportLocation? location) {
            if (!location.HasValue) return null;
            return new TeleportAction(location.Value, () => {
                PluginLog.Log($"Starting Teleport to '{location.Value.Name}'");
                _teleportWithTicket?.Invoke(AvailableLocationsAddress, location.Value.AetheryteId, 0);
            });
        }

        public static TeleportAction GetMapTeleport(TeleportLocation? location) {
            if (!location.HasValue) return null;
            return new TeleportAction(location.Value, () => {
                var agent = GetAgentInterfaceById(0x22) ?? IntPtr.Zero;
                if(agent == IntPtr.Zero) return;
                PluginLog.Log($"Starting Teleport to '{location.Value.Name}'");
                _teleportWithMapClick?.Invoke(agent, 3, location.Value.AetheryteId, 0xFF);
            });
        }

        #endregion

        #region Helpers

        private static IEnumerable<TeleportLocation> GetAvailableLocations() {
            var ptr = _getAvalibleLocationList?.Invoke(AvailableLocationsAddress, 0) ?? IntPtr.Zero;
            if (ptr == IntPtr.Zero) yield break;

            var start = Marshal.ReadIntPtr(ptr, 0);
            var end = Marshal.ReadIntPtr(ptr, 8);
            var count = (int)((end.ToInt64() - start.ToInt64()) / 20);
            for (var i = 0; i < count; i++)
                yield return Marshal.PtrToStructure<TeleportLocation>(start + i * 20);
        }

        private static IntPtr? GetAgentInterfaceById(int id) {
            var module = UiAgentModuleAddress;
            if (module == IntPtr.Zero) return null;
            var listStart = module + 0x20;
            var agent = Marshal.ReadIntPtr(listStart, id * 8);
            if (agent == IntPtr.Zero) return null;
            return agent;
        }

        #endregion

        #region Init

        public static void Init(DalamudPluginInterface plugin) {
            InitData(plugin);
            InitDelegates(plugin);
            InitAddresses(plugin);
        }

        private static void InitData(DalamudPluginInterface plugin) {
            var aetherytes = plugin.Data.GetExcelSheet<Aetheryte>();
            AetheryteNames = new Dictionary<uint, string>();
            aetherytes.GetRows().ForEach(data => {
                var name = data.PlaceName?.Value.Name;
                if(string.IsNullOrEmpty(name) || data.RowId <= 0) return;
                if(!AetheryteNames.ContainsKey(data.RowId))
                    AetheryteNames.Add(data.RowId, name);
            });
        }

        private static void InitDelegates(DalamudPluginInterface plugin) {
            var directAddr = plugin.TargetModuleScanner.ScanText("48895C24??48896C24??48897424??574881EC????????488B05????????4833C448898424????????8BE9418BD9488B0D????????418BF88BF2");
            if(directAddr != IntPtr.Zero)
                _teleportDirect = Marshal.GetDelegateForFunctionPointer<TeleportDirectDelegate>(directAddr);

            var ticketAddr = plugin.TargetModuleScanner.ScanText("48895C24??48897424??574883EC??488BF9410FB6F0488B0D");
            if(ticketAddr != IntPtr.Zero)
                _teleportWithTicket = Marshal.GetDelegateForFunctionPointer<TeleportWithTicketDelegate>(ticketAddr);

            var mapClickAddr = plugin.TargetModuleScanner.ScanText("48895C24??48897424??574883EC??418BF9418BF0488BD983EA01");
            if(mapClickAddr != IntPtr.Zero)
                _teleportWithMapClick = Marshal.GetDelegateForFunctionPointer<TeleportWithMapClickDelegate>(mapClickAddr);

            var getLocationsAddr = plugin.TargetModuleScanner.ScanText("48895C24??5557415441554156488DAC24????????4881EC");
            if(getLocationsAddr != IntPtr.Zero)
                _getAvalibleLocationList = Marshal.GetDelegateForFunctionPointer<GetAvalibleLocationListDelegate>(getLocationsAddr);
            
            var getUimoduleAddr = plugin.TargetModuleScanner.ScanText("E8????????488BC8488B10FF52408088????????01E9");
            if(getUimoduleAddr != IntPtr.Zero)
                _getUiModule = Marshal.GetDelegateForFunctionPointer<GetUiModuleDelegate>(getUimoduleAddr);
        }

        private static void InitAddresses(DalamudPluginInterface plugin) {
            var locationAob = plugin.TargetModuleScanner.ScanText("33D2488D0D????????E8????????49894424");
            if (locationAob == IntPtr.Zero) return;
            var locationOffset = Marshal.ReadInt32(locationAob, 5);
            AvailableLocationsAddress = plugin.TargetModuleScanner.ResolveRelativeAddress(locationAob + 9, locationOffset);
        }

        #endregion
    }
}