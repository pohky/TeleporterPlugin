using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Plugin;
using TeleporterPlugin.Objects;

namespace TeleporterPlugin.Managers {
    public class TeleportManager {
        private delegate IntPtr GetAvalibleLocationListDelegate(IntPtr locationsPtr, uint arg2);
        private delegate void SendCommandDelegate(uint cmd, uint aetheryteId, bool useTicket, uint subIndex, uint arg5);
        private delegate bool TryTeleportWithTicketDelegate(IntPtr tpStatusPtr, uint aetheryteId, byte subIndex);
        private delegate int GetItemCountDelegate(IntPtr arg1, uint itemId, uint arg3, uint arg4, uint arg5, uint arg6);

        private GetAvalibleLocationListDelegate _getAvalibleLocationList;
        private SendCommandDelegate _sendCommand;
        private TryTeleportWithTicketDelegate _tryTeleportWithTicket;
        private GetItemCountDelegate _getItemCount;

        private readonly TeleporterPlugin _plugin;

        public IntPtr AetheryteListAddress { get; private set; }
        public IntPtr TeleportStatusAddress { get; private set; }
        public IntPtr ItemCountStaticArgAddress { get; private set; }

        public IEnumerable<TeleportLocation> AetheryteList => GetAetheryteList();
        public event Action<string> LogEvent;
        public event Action<string> LogErrorEvent;

        #region Teleport

        public void Teleport(string aetheryteName, bool matchPartial = true) {
            var location = GetLocationByName(aetheryteName, matchPartial);
            if (location == null) {
                LogErrorEvent?.Invoke($"No attuned Aetheryte found for '{aetheryteName}'.");
                return;
            }
            LogEvent?.Invoke($"Teleporting to '{location.Name}'.");
            _sendCommand?.Invoke(0xCA, location.AetheryteId, false, location.SubIndex, 0);
        }

        public void TeleportTicket(string aetheryteName, bool skipPopup = false, bool matchPartial = true) {
            var location = GetLocationByName(aetheryteName, matchPartial);
            if (location == null) {
                LogErrorEvent?.Invoke($"No attuned Aetheryte found for '{aetheryteName}'.");
                return;
            }

            if (skipPopup) {
                var tickets = GetAetheryteTicketCount();
                if (tickets > 0) {
                    LogEvent?.Invoke($"Teleporting to '{location.Name}'. (Tickets: {tickets})");
                    _sendCommand?.Invoke(0xCA, location.AetheryteId, true, location.SubIndex, 0);
                    return;
                }
            } else {
                bool? result = false;
                if (TeleportStatusAddress != IntPtr.Zero)
                    result = _tryTeleportWithTicket?.Invoke(TeleportStatusAddress, location.AetheryteId, location.SubIndex);
                if (result == true) {
                    LogEvent?.Invoke($"Teleporting to '{location.Name}'.");
                    return;
                }
            }
            LogEvent?.Invoke($"Teleporting to '{location.Name}'. (Not using Tickets)");
            _sendCommand?.Invoke(0xCA, location.AetheryteId, false, location.SubIndex, 0);
        }

        #endregion

        #region Helpers

        public TeleportLocation GetLocationByName(string aetheryteName, bool matchPartial = true) {
            var location = GetAetheryteList().FirstOrDefault(o =>
                o.Name.Equals(aetheryteName, StringComparison.OrdinalIgnoreCase) ||
                matchPartial && o.Name.ToUpper().Contains(aetheryteName.ToUpper()));
            return location;
        }

        private IEnumerable<TeleportLocation> GetAetheryteList() {
            if (AetheryteListAddress == IntPtr.Zero || _plugin.Interface.ClientState.LocalPlayer == null)
                yield break;
            var ptr = _getAvalibleLocationList?.Invoke(AetheryteListAddress, 0) ?? IntPtr.Zero;
            if (ptr == IntPtr.Zero) yield break;
            var start = Marshal.ReadIntPtr(ptr, 0);
            var end = Marshal.ReadIntPtr(ptr, 8);
            var size = Marshal.SizeOf<TeleportLocationStruct>();
            var count = (int)((end.ToInt64() - start.ToInt64()) / size);
            var language = _plugin.Language;
            for (var i = 0; i < count; i++) {
                var data = Marshal.PtrToStructure<TeleportLocationStruct>(start + i * size);
                var name = AetheryteDataManager.GetAetheryteName(data.AetheryteId, data.SubIndex, language);
                yield return new TeleportLocation(data, name);
            }
        }

        private int GetAetheryteTicketCount() {
            //aetheryte ticket id = 0x1D91
            if (ItemCountStaticArgAddress == IntPtr.Zero)
                return 0;
            var count = _getItemCount?.Invoke(ItemCountStaticArgAddress, 0x1D91, 0, 0, 1, 0);
            return count ?? 0;
        }

        #endregion

        #region Init

        public TeleportManager(TeleporterPlugin plugin) {
            _plugin = plugin;
            InitDelegates(plugin.Interface);
            InitAddresses(plugin.Interface);
        }
        
        private void InitDelegates(DalamudPluginInterface plugin) {
            var scanner = plugin.TargetModuleScanner;
            var sendCmdAddr = scanner.ScanText("48895C24??48896C24??48897424??574881EC????????488B05????????4833C448898424????????8BE9418BD9488B0D????????418BF88BF2");
            if(sendCmdAddr != IntPtr.Zero)
                _sendCommand = Marshal.GetDelegateForFunctionPointer<SendCommandDelegate>(sendCmdAddr);

            var getLocationsAddr = scanner.ScanText("48895C24??5557415441554156488DAC24????????4881EC");
            if (getLocationsAddr != IntPtr.Zero)
                _getAvalibleLocationList = Marshal.GetDelegateForFunctionPointer<GetAvalibleLocationListDelegate>(getLocationsAddr);

            var tryTicketTpAddr = scanner.ScanText("48895C24??48897424??574883EC??8079??00410FB6F88BF2");
            if (tryTicketTpAddr != IntPtr.Zero)
                _tryTeleportWithTicket = Marshal.GetDelegateForFunctionPointer<TryTeleportWithTicketDelegate>(tryTicketTpAddr);

            var getItemCountAddr = scanner.ScanText("48895C24??48896C24??48897424??48897C24??4154415641574883EC??33DB8D");
            if (getItemCountAddr != IntPtr.Zero)
                _getItemCount = Marshal.GetDelegateForFunctionPointer<GetItemCountDelegate>(getItemCountAddr);
        }

        private void InitAddresses(DalamudPluginInterface plugin) {
            var scanner = plugin.TargetModuleScanner;
            var locationAob = scanner.ScanText("33D2488D0D????????E8????????49894424");
            if (locationAob == IntPtr.Zero) return;
            var locationOffset = Marshal.ReadInt32(locationAob, 5);
            AetheryteListAddress = scanner.ResolveRelativeAddress(locationAob + 9, locationOffset);
            TeleportStatusAddress = AetheryteListAddress == IntPtr.Zero ? IntPtr.Zero : AetheryteListAddress + 0x20;

            var itemCountArgAob = scanner.ScanText("488D0D????????66894424??4533C94533C0");
            if(itemCountArgAob == IntPtr.Zero) return;
            var itemCountArgOffset = Marshal.ReadInt32(itemCountArgAob, 3);
            ItemCountStaticArgAddress = scanner.ResolveRelativeAddress(itemCountArgAob + 7, itemCountArgOffset);
        }

        #endregion
    }
}