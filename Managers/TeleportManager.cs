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

        private GetAvalibleLocationListDelegate m_GetAvalibleLocationList;
        private SendCommandDelegate m_SendCommand;
        private TryTeleportWithTicketDelegate m_TryTeleportWithTicket;
        private GetItemCountDelegate m_GetItemCount;

        private Hook<TryTeleportWithTicketDelegate> m_TpTicketHook;

        private readonly TeleporterPlugin m_Plugin;

        public IntPtr AetheryteListAddress { get; private set; }
        public IntPtr TeleportStatusAddress { get; private set; }
        public IntPtr ItemCountStaticArgAddress { get; private set; }

        public IEnumerable<TeleportLocation> AetheryteList {
            get {
                try {
                    return GetAetheryteList().ToList();
                } catch {
                    m_Plugin.LogError("Error in GetAetheryteList()");
                    return Enumerable.Empty<TeleportLocation>();
                }
            }
        }

        #region Teleport

        private bool TicketTpHook(IntPtr tpStatusPtr, uint aetheryteId, byte subIndex) {
            try {
                if (GetAetheryteTicketCount() <= 0)
                    return m_TpTicketHook.Original(tpStatusPtr, aetheryteId, subIndex);

                if (m_Plugin.Config.UseGilThreshold) {
                    var location = AetheryteList.FirstOrDefault(l => l.AetheryteId == aetheryteId && l.SubIndex == subIndex);
                    if (location != null) {
                        if (location.GilCost < m_Plugin.Config.GilThreshold) {
                            m_SendCommand?.Invoke(0xCA, aetheryteId, false, subIndex, 0);
                            return true;
                        }
                    }
                }

                if (m_Plugin.Config.SkipTicketPopup) {
                    m_SendCommand?.Invoke(0xCA, aetheryteId, true, subIndex, 0);
                    return true;
                }

                return m_TpTicketHook.Original(tpStatusPtr, aetheryteId, subIndex);
            } catch {
                m_Plugin.LogError("Error in TicketTpHook Call.");
                return m_TpTicketHook.Original(tpStatusPtr, aetheryteId, subIndex);
            }
        }
        
        public void Teleport(string aetheryteName, bool matchPartial, bool useMapName = false) {
            try {
                var mapName = "";
                if (useMapName) {
                    var aetheryteLocation = AetheryteDataManager.GetAetheryteLocationsByTerritoryName(aetheryteName, m_Plugin.Language, matchPartial).FirstOrDefault();
                    var name = aetheryteLocation?.Name;
                    mapName = aetheryteLocation?.TerritoryName;
                    if (name == null) {
                        m_Plugin.LogError($"No Aetheryte found for Map '{aetheryteName}'.");
                        return;
                    }

                    aetheryteName = name;
                }
                var location = GetLocationByName(aetheryteName, matchPartial);
                if (location == null) {
                    m_Plugin.LogError($"No attuned Aetheryte found for '{aetheryteName}'.");
                    if (!m_Plugin.IsInHomeWorld && aetheryteName.Contains('('))
                        m_Plugin.Log("Note: Estate Teleports not available while visiting other Worlds.");
                    return;
                }

                m_Plugin.Log($"Teleporting to '{location.Name}'" + (useMapName ? $" in '{mapName}'" : "") + ".");
                m_SendCommand?.Invoke(0xCA, location.AetheryteId, false, location.SubIndex, 0);
            } catch {
                m_Plugin.LogError("Error in Teleport(string,bool)");
            }
        }

        public void TeleportTicket(string aetheryteName, bool skipPopup, bool matchPartial, bool useMapName = false) {
            try {
                m_TpTicketHook?.Disable();
                var mapName = "";
                if (useMapName) {
                    var aetheryteLocation = AetheryteDataManager.GetAetheryteLocationsByTerritoryName(aetheryteName, m_Plugin.Language, matchPartial).FirstOrDefault();
                    var name = aetheryteLocation?.Name;
                    mapName = aetheryteLocation?.TerritoryName;
                    if (name == null) {
                        m_Plugin.LogError($"No Aetheryte found for Map '{aetheryteName}'.");
                        return;
                    }

                    aetheryteName = name;
                }

                var location = GetLocationByName(aetheryteName, matchPartial);
                if (location == null) {
                    m_Plugin.LogError($"No attuned Aetheryte found for '{aetheryteName}'.");
                    if (!m_Plugin.IsInHomeWorld && aetheryteName.Contains('('))
                        m_Plugin.Log("Note: Estate Teleports not available while visiting other Worlds.");
                    return;
                }

                if (skipPopup) {
                    var tickets = GetAetheryteTicketCount();
                    if (tickets > 0) {
                        m_Plugin.Log($"Teleporting to '{location.Name}'" + (useMapName ? $" in '{mapName}'" : "") + $". (Tickets: {tickets})");
                        m_SendCommand?.Invoke(0xCA, location.AetheryteId, true, location.SubIndex, 0);
                        return;
                    }
                } else {
                    bool? result = false;
                    if (TeleportStatusAddress != IntPtr.Zero)
                        result = m_TryTeleportWithTicket?.Invoke(TeleportStatusAddress, location.AetheryteId, location.SubIndex);
                    if (result == true) {
                        m_Plugin.Log($"Teleporting to '{location.Name}'" + (useMapName ? $" in '{mapName}'" : "") + ".");
                        return;
                    }
                }

                m_Plugin.Log($"Teleporting to '{location.Name}'" + (useMapName ? $" in '{mapName}'" : "") + ". (Not using Tickets)");
                m_SendCommand?.Invoke(0xCA, location.AetheryteId, false, location.SubIndex, 0);
            } catch {
                m_Plugin.LogError("Error in TeleportTicket(string,bool,bool)");
            } finally {
                m_TpTicketHook?.Enable();
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
            if (AetheryteListAddress == IntPtr.Zero || !m_Plugin.IsLoggedIn)
                yield break;
            var ptr = m_GetAvalibleLocationList?.Invoke(AetheryteListAddress, 0) ?? IntPtr.Zero;
            if (ptr == IntPtr.Zero) yield break;
            var start = Marshal.ReadIntPtr(ptr, 0);
            var end = Marshal.ReadIntPtr(ptr, 8);
            var size = Marshal.SizeOf<TeleportLocationStruct>();
            var count = (int)((end.ToInt64() - start.ToInt64()) / size);
            var language = m_Plugin.Language;
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
                var count = m_GetItemCount?.Invoke(ItemCountStaticArgAddress, 0x1D91, 0, 0, 1, 0);
                return count ?? 0;
            } catch {
                m_Plugin.LogError("Error in GetAetheryteTicketCount()");
                return 0;
            }
        }

        #endregion

        #region Load/Unload

        public TeleportManager(TeleporterPlugin plugin) {
            m_Plugin = plugin;
            try {
                InitDelegates(plugin.Interface);
                InitAddresses(plugin.Interface);
            } catch(Exception ex) {
                m_Plugin.LogError($"TeleportManager Init Error.\n{ex.Message}");
            }
        }

        private void InitDelegates(DalamudPluginInterface plugin) {
            var scanner = plugin.TargetModuleScanner;
            var sendCmdAddr = scanner.ScanText("48895C24??48896C24??48897424??574881EC????????488B05????????4833C448898424????????8BE9418BD9488B0D????????418BF88BF2");
            if(sendCmdAddr != IntPtr.Zero)
                m_SendCommand = Marshal.GetDelegateForFunctionPointer<SendCommandDelegate>(sendCmdAddr);
            else m_Plugin.LogError("sendCmdAddr is null.");

            var getLocationsAddr = scanner.ScanText("48895C24??5557415441554156488DAC24????????4881EC");
            if (getLocationsAddr != IntPtr.Zero)
                m_GetAvalibleLocationList = Marshal.GetDelegateForFunctionPointer<GetAvalibleLocationListDelegate>(getLocationsAddr);
            else m_Plugin.LogError("getLocationsAddr is null.");

            var tryTicketTpAddr = scanner.ScanText("48895C24??48897424??574883EC??8079??00410FB6F88BF2");
            if (tryTicketTpAddr != IntPtr.Zero) {
                m_TryTeleportWithTicket = Marshal.GetDelegateForFunctionPointer<TryTeleportWithTicketDelegate>(tryTicketTpAddr);
                m_TpTicketHook = new Hook<TryTeleportWithTicketDelegate>(tryTicketTpAddr, new TryTeleportWithTicketDelegate(TicketTpHook));
                m_TpTicketHook.Enable();
            } else m_Plugin.LogError("tryTicketTpAddr is null.");

            var getItemCountAddr = scanner.ScanText("48895C24??48896C24??48897424??48897C24??4154415641574883EC??33??8D");
            if (getItemCountAddr != IntPtr.Zero)
                m_GetItemCount = Marshal.GetDelegateForFunctionPointer<GetItemCountDelegate>(getItemCountAddr);
            else m_Plugin.LogError("getItemCountAddr is null.");
        }

        private void InitAddresses(DalamudPluginInterface plugin) {
            var scanner = plugin.TargetModuleScanner;
            var aetheryteListAob = scanner.ScanText("33D2488D0D????????E8????????49894424");
            if (aetheryteListAob == IntPtr.Zero) {
                m_Plugin.LogError("aetheryteListAob is null.");
                return;
            }
            var aetheryteListOffset = Marshal.ReadInt32(aetheryteListAob, 5);
            AetheryteListAddress = scanner.ResolveRelativeAddress(aetheryteListAob + 9, aetheryteListOffset);
            TeleportStatusAddress = AetheryteListAddress == IntPtr.Zero ? IntPtr.Zero : AetheryteListAddress + 0x28;
            if (TeleportStatusAddress == IntPtr.Zero) {
                m_Plugin.LogError("TeleportStatusAddress is null.");
                return;
            }
            var itemCountArgAob = scanner.ScanText("488D0D????????66894424??4533C94533C0");
            if (itemCountArgAob == IntPtr.Zero) {
                m_Plugin.LogError("itemCountArgAob is null.");
                return;
            }
            var itemCountArgOffset = Marshal.ReadInt32(itemCountArgAob, 3);
            ItemCountStaticArgAddress = scanner.ResolveRelativeAddress(itemCountArgAob + 7, itemCountArgOffset);
            if (ItemCountStaticArgAddress == IntPtr.Zero)
                m_Plugin.LogError("ItemCountStaticArgAddress is null.");
        }

        public void Dispose() {
            m_TpTicketHook?.Disable();
            m_TpTicketHook?.Dispose();
            GC.SuppressFinalize(this);
        }

        ~TeleportManager() {
            Dispose();
        }

        #endregion
    }
}