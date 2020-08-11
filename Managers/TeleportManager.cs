using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Dalamud.Plugin;
using TeleporterPlugin.Objects;

namespace TeleporterPlugin.Managers {
    public class TeleportManager : IDisposable {
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr GetAvalibleLocationListDelegate(IntPtr locationsPtr, uint arg2);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void SendCommandDelegate(uint cmd, uint aetheryteId, bool useTicket, uint subIndex, uint arg5);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate bool TryTeleportWithTicketDelegate(IntPtr tpStatusPtr, uint aetheryteId, byte subIndex);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int GetItemCountDelegate(IntPtr arg1, uint itemId, uint arg3, uint arg4, uint arg5, uint arg6);

        private GetAvalibleLocationListDelegate _getAvalibleLocationList;
        private SendCommandDelegate _sendCommand;
        private TryTeleportWithTicketDelegate _tryTeleportWithTicket;
        private GetItemCountDelegate _getItemCount;

        private Hook<TryTeleportWithTicketDelegate> _tpTicketHook;

        private readonly TeleporterPlugin _plugin;

        public IntPtr AetheryteListAddress { get; private set; }
        public IntPtr TeleportStatusAddress { get; private set; }
        public IntPtr ItemCountStaticArgAddress { get; private set; }

        public IEnumerable<TeleportLocation> AetheryteList {
            get {
                try {
                    return GetAetheryteList().ToList();
                } catch {
                    _plugin.LogError("Error in GetAetheryteList()");
                    return Enumerable.Empty<TeleportLocation>();
                }
            }
        }

        

        #region Teleport

        private bool TicketTpHook(IntPtr tpStatusPtr, uint aetheryteId, byte subIndex) {
            try {
                if (GetAetheryteTicketCount() <= 0)
                    return _tpTicketHook.Original(tpStatusPtr, aetheryteId, subIndex);

                if (_plugin.Config.UseGilThreshold) {
                    var location = AetheryteList.FirstOrDefault(l => l.AetheryteId == aetheryteId && l.SubIndex == subIndex);
                    if (location != null) {
                        if (location.GilCost < _plugin.Config.GilThreshold) {
                            //_plugin.Log("Price below threshold. Teleporting without ticket.");
                            _sendCommand?.Invoke(0xCA, aetheryteId, false, subIndex, 0);
                            return true;
                        }
                    }
                }

                if (_plugin.Config.SkipTicketPopup) {
                    //_plugin.Log("Skipping Ticket Popup.");
                    _sendCommand?.Invoke(0xCA, aetheryteId, true, subIndex, 0);
                    return true;
                }

                //_plugin.Log("Using Original TP.");
                return _tpTicketHook.Original(tpStatusPtr, aetheryteId, subIndex);
            } catch {
                _plugin.LogError("Error in TicketTpHook Call.");
                return _tpTicketHook.Original(tpStatusPtr, aetheryteId, subIndex);
            }
        }
        
        public void Teleport(string aetheryteName, bool matchPartial = true) {
            try {
                var location = GetLocationByName(aetheryteName, matchPartial);
                if (location == null) {
                    _plugin.LogError($"No attuned Aetheryte found for '{aetheryteName}'.");
                    if (!_plugin.IsInHomeWorld && aetheryteName.Contains('('))
                        _plugin.Log("Note: Estate Teleports not available while visiting other Worlds.");
                    return;
                }

                _plugin.Log($"Teleporting to '{location.Name}'.");
                _sendCommand?.Invoke(0xCA, location.AetheryteId, false, location.SubIndex, 0);
            } catch {
                _plugin.LogError("Error in Teleport(string,bool)");
            }
        }

        public void TeleportTicket(string aetheryteName, bool skipPopup = false, bool matchPartial = true) {
            try {
                var location = GetLocationByName(aetheryteName, matchPartial);
                if (location == null) {
                    _plugin.LogError($"No attuned Aetheryte found for '{aetheryteName}'.");
                    if (!_plugin.IsInHomeWorld && aetheryteName.Contains('('))
                        _plugin.Log("Note: Estate Teleports not available while visiting other Worlds.");
                    return;
                }

                if (skipPopup) {
                    var tickets = GetAetheryteTicketCount();
                    if (tickets > 0) {
                        _plugin.Log($"Teleporting to '{location.Name}'. (Tickets: {tickets})");
                        _sendCommand?.Invoke(0xCA, location.AetheryteId, true, location.SubIndex, 0);
                        return;
                    }
                } else {
                    bool? result = false;
                    if (TeleportStatusAddress != IntPtr.Zero) {
                        if (_tpTicketHook != null)
                            result = _tpTicketHook.Original(TeleportStatusAddress, location.AetheryteId, location.SubIndex);
                        else result = _tryTeleportWithTicket?.Invoke(TeleportStatusAddress, location.AetheryteId, location.SubIndex);
                    }

                    if (result == true) {
                        _plugin.Log($"Teleporting to '{location.Name}'.");
                        return;
                    }
                }

                _plugin.Log($"Teleporting to '{location.Name}'. (Not using Tickets)");
                _sendCommand?.Invoke(0xCA, location.AetheryteId, false, location.SubIndex, 0);
            } catch {
                _plugin.LogError("Error in TeleportTicket(string,bool,bool)");
            }
        }

        #endregion

        #region Helpers

        public TeleportLocation GetLocationByName(string aetheryteName, bool matchPartial = true) {
            if (string.IsNullOrEmpty(aetheryteName)) return null;
            var location = AetheryteList.FirstOrDefault(o =>
                o.Name.Equals(aetheryteName, StringComparison.OrdinalIgnoreCase) ||
                matchPartial && o.Name.ToUpper().Contains(aetheryteName.ToUpper()));
            return location;
        }

        private IEnumerable<TeleportLocation> GetAetheryteList() {
            if (AetheryteListAddress == IntPtr.Zero || !_plugin.IsLoggedIn)
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
            if (ItemCountStaticArgAddress == IntPtr.Zero)
                return 0;
            try {
                //aetheryte ticket id = 0x1D91
                var count = _getItemCount?.Invoke(ItemCountStaticArgAddress, 0x1D91, 0, 0, 1, 0);
                return count ?? 0;
            } catch {
                _plugin.LogError("Error in GetAetheryteTicketCount()");
                return 0;
            }
        }

        #endregion

        #region Load/Unload

        public TeleportManager(TeleporterPlugin plugin) {
            _plugin = plugin;
            try {
                InitDelegates(plugin.Interface);
                InitAddresses(plugin.Interface);
            } catch(Exception ex) {
                _plugin.LogError($"TeleportManager Init Error.\n{ex.Message}");
            }
        }

        private void InitDelegates(DalamudPluginInterface plugin) {
            var scanner = plugin.TargetModuleScanner;
            var sendCmdAddr = scanner.ScanText("48895C24??48896C24??48897424??574881EC????????488B05????????4833C448898424????????8BE9418BD9488B0D????????418BF88BF2");
            if(sendCmdAddr != IntPtr.Zero)
                _sendCommand = Marshal.GetDelegateForFunctionPointer<SendCommandDelegate>(sendCmdAddr);
            else _plugin.LogError("sendCmdAddr is null.");

            var getLocationsAddr = scanner.ScanText("48895C24??5557415441554156488DAC24????????4881EC");
            if (getLocationsAddr != IntPtr.Zero)
                _getAvalibleLocationList = Marshal.GetDelegateForFunctionPointer<GetAvalibleLocationListDelegate>(getLocationsAddr);
            else _plugin.LogError("getLocationsAddr is null.");

            var tryTicketTpAddr = scanner.ScanText("48895C24??48897424??574883EC??8079??00410FB6F88BF2");
            if (tryTicketTpAddr != IntPtr.Zero) {
                _tryTeleportWithTicket = Marshal.GetDelegateForFunctionPointer<TryTeleportWithTicketDelegate>(tryTicketTpAddr);
                _tpTicketHook = new Hook<TryTeleportWithTicketDelegate>(tryTicketTpAddr, new TryTeleportWithTicketDelegate(TicketTpHook));
                _tpTicketHook.Enable();
            } else _plugin.LogError("tryTicketTpAddr is null.");

            var getItemCountAddr = scanner.ScanText("48895C24??48896C24??48897424??48897C24??4154415641574883EC??33??8D");
            if (getItemCountAddr != IntPtr.Zero)
                _getItemCount = Marshal.GetDelegateForFunctionPointer<GetItemCountDelegate>(getItemCountAddr);
            else _plugin.LogError("getItemCountAddr is null.");
        }

        private void InitAddresses(DalamudPluginInterface plugin) {
            var scanner = plugin.TargetModuleScanner;
            var aetheryteListAob = scanner.ScanText("33D2488D0D????????E8????????49894424");
            if (aetheryteListAob == IntPtr.Zero) {
                _plugin.LogError("aetheryteListAob is null.");
                return;
            }
            var aetheryteListOffset = Marshal.ReadInt32(aetheryteListAob, 5);
            AetheryteListAddress = scanner.ResolveRelativeAddress(aetheryteListAob + 9, aetheryteListOffset);
            TeleportStatusAddress = AetheryteListAddress == IntPtr.Zero ? IntPtr.Zero : AetheryteListAddress + 0x20;
            if (TeleportStatusAddress == IntPtr.Zero) {
                _plugin.LogError("TeleportStatusAddress is null.");
                return;
            }
            var itemCountArgAob = scanner.ScanText("488D0D????????66894424??4533C94533C0");
            if (itemCountArgAob == IntPtr.Zero) {
                _plugin.LogError("itemCountArgAob is null.");
                return;
            }
            var itemCountArgOffset = Marshal.ReadInt32(itemCountArgAob, 3);
            ItemCountStaticArgAddress = scanner.ResolveRelativeAddress(itemCountArgAob + 7, itemCountArgOffset);
            if (ItemCountStaticArgAddress == IntPtr.Zero)
                _plugin.LogError("ItemCountStaticArgAddress is null.");
        }

        public void Dispose() {
            _tpTicketHook?.Disable();
            _tpTicketHook?.Dispose();
            GC.SuppressFinalize(this);
        }

        ~TeleportManager() {
            Dispose();
        }

        #endregion
    }
}