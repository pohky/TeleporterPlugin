using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using TeleporterPlugin.Objects;

namespace TeleporterPlugin.Managers {
    internal static class TeleportManager {
        private delegate IntPtr GetAvalibleLocationListDelegate(IntPtr locationsPtr, uint arg2);
        private delegate void SendCommandDelegate(uint cmd, uint aetheryteId, bool useTicket, uint subIndex, uint arg5);
        private delegate bool TryTeleportWithTicketDelegate(IntPtr tpStatusPtr, uint aetheryteId, byte subIndex);
        private delegate int GetItemCountDelegate(IntPtr arg1, uint itemId, uint arg3, uint arg4, uint arg5, uint arg6);

        private static GetAvalibleLocationListDelegate _getAvalibleLocationList;
        private static SendCommandDelegate _sendCommand;
        private static TryTeleportWithTicketDelegate _tryTeleportWithTicket;
        private static GetItemCountDelegate _getItemCount;

        private static readonly uint[] _privateHouseIds = {59, 60, 61, 97}; //limsa, gridania, uldah, shiro

        public static IntPtr AetheryteListAddress { get; private set; }
        public static IntPtr TeleportStatusAddress { get; private set; }
        public static IntPtr ItemCountStaticArgAddress { get; private set; }

        public static Dictionary<uint, string> AetheryteNames { get; private set; } = new Dictionary<uint, string>();
        public static IEnumerable<TeleportLocation> AetheryteList => GetAetheryteList();
        public static event Action<string> LogEvent;
        public static event Action<string> LogErrorEvent;

        public static ClientLanguage CurrentLanguage = ClientLanguage.English;

        private static readonly Dictionary<ClientLanguage, string> _apartmentNames = new Dictionary<ClientLanguage, string>{
            {ClientLanguage.English, "Apartment"},
            {ClientLanguage.German, "Wohnung"},
            {ClientLanguage.French, "Appartement"},
            {ClientLanguage.Japanese, "アパルトメント"}
        };

        private static readonly Dictionary<ClientLanguage, string> _sharedHouseNames = new Dictionary<ClientLanguage, string> {
            {ClientLanguage.English, "Shared Estate (<number>)"},
            {ClientLanguage.German, "Wohngemeinschaft (<number>)"},
            {ClientLanguage.French, "Maison (<number>)"},
            {ClientLanguage.Japanese, "ハウス（シェア：<number>）"}
        };

        internal static void DebugSetLanguage(ClientLanguage lang, DalamudPluginInterface plugin) {
            CurrentLanguage = lang;
            InitData(plugin);
        }

        #region Teleport

        public static void Teleport(string aetheryteName, bool matchPartial = true) {
            var location = GetLocationByName(aetheryteName, matchPartial);
            if (!location.HasValue) {
                LogErrorEvent?.Invoke($"No attuned Aetheryte found for '{aetheryteName}'.");
                return;
            }
            LogEvent?.Invoke($"Teleporting to '{location.Value.Name}'");
            _sendCommand?.Invoke(0xCA, location.Value.AetheryteId, false, location.Value.SubIndex, 0);
        }

        public static void TeleportTicket(string aetheryteName, bool skipPopup = false, bool matchPartial = true) {
            var location = GetLocationByName(aetheryteName, matchPartial);
            if (!location.HasValue) {
                LogErrorEvent?.Invoke($"No attuned Aetheryte found for '{aetheryteName}'.");
                return;
            }

            if (skipPopup) {
                var tickets = GetAetheryteTicketCount();
                if (tickets > 0) {
                    LogEvent?.Invoke($"Teleporting to '{location.Value.Name}' (Tickets: {tickets})");
                    _sendCommand?.Invoke(0xCA, location.Value.AetheryteId, true, location.Value.SubIndex, 0);
                    return;
                }
            }
            var result = _tryTeleportWithTicket?.Invoke(TeleportStatusAddress, location.Value.AetheryteId, location.Value.SubIndex);
            if (!result.HasValue) {
                LogErrorEvent?.Invoke("Unable to Teleport using Aetheryte Tickets without Popup.");
                return;
            }
            if (result.Value) return;
            LogEvent?.Invoke($"Teleporting to '{location.Value.Name}'");
            _sendCommand?.Invoke(0xCA, location.Value.AetheryteId, false, location.Value.SubIndex, 0);
        }

        #endregion

        #region Helpers

        internal static string GetNameForLocation(TeleportLocation location) {
            if (!AetheryteNames.TryGetValue(location.AetheryteId, out var name))
                return string.Empty;

            if(!_privateHouseIds.Contains(location.AetheryteId))
                return CurrentLanguage == ClientLanguage.German ? name.Replace("", "") : name;
            
            switch (location.SubIndex) {
                case 0: break; // use default name
                case 128:
                    name = _apartmentNames[CurrentLanguage];
                    break;
                case var n when n >= 1 && n <= 127: 
                    name = _sharedHouseNames[CurrentLanguage].Replace("<number>", $"{location.SubIndex}");
#if DEBUG
                    if (location.ZoneId == 420) name = $"Debug {name}";
#endif
                    break;
                default:
                    name = $"Unknown Estate ({location.AetheryteId}, {location.SubIndex})";
                    break;
            }
            return name;
        }

        public static TeleportLocation? GetLocationByName(string aetheryteName, bool matchPartial = true) {
            var location = GetAetheryteList().FirstOrDefault(o =>
                o.Name.Equals(aetheryteName, StringComparison.OrdinalIgnoreCase) ||
                matchPartial && o.Name.ToUpper().StartsWith(aetheryteName.ToUpper()));
            return location.AetheryteId > 0 ? (TeleportLocation?)location : null;
        }

        private static IEnumerable<TeleportLocation> GetAetheryteList() {
            var ptr = _getAvalibleLocationList?.Invoke(AetheryteListAddress, 0) ?? IntPtr.Zero;
            if (ptr == IntPtr.Zero) yield break;
#if DEBUG
            yield return new TeleportLocation {
                AetheryteId = 97,
                GilCost = 6996,
                SubIndex = 69,
                ZoneId = 420
            };
#endif
            var start = Marshal.ReadIntPtr(ptr, 0);
            var end = Marshal.ReadIntPtr(ptr, 8);
            var size = Marshal.SizeOf<TeleportLocation>();
            var count = (int)((end.ToInt64() - start.ToInt64()) / size);
            for (var i = 0; i < count; i++)
                yield return Marshal.PtrToStructure<TeleportLocation>(start + i * size);
        }

        private static int GetAetheryteTicketCount() {
            //aetheryte ticket id = 0x1D91
            if (ItemCountStaticArgAddress == IntPtr.Zero)
                return 0;
            var count = _getItemCount?.Invoke(ItemCountStaticArgAddress, 0x1D91, 0, 0, 1, 0);
            return count ?? 0;
        }

        #endregion

        #region Init

        public static void Init(DalamudPluginInterface plugin) {
            CurrentLanguage = plugin.ClientState.ClientLanguage;
            InitData(plugin);
            InitDelegates(plugin);
            InitAddresses(plugin);
        }

        private static void InitData(DalamudPluginInterface plugin) {
            var aetherytes = plugin.Data.GetExcelSheet<Aetheryte>(CurrentLanguage);
            var placeNames = plugin.Data.GetExcelSheet<PlaceName>(CurrentLanguage);
            AetheryteNames = new Dictionary<uint, string>();
            aetherytes.GetRows().ForEach(data => {
                var id = data.RowId;
                var place = data.PlaceName?.Row;
                if(id <= 0 || !place.HasValue) return;
                var name = placeNames.GetRow(place.Value).Name;
                if(string.IsNullOrEmpty(name)) return;
                if(!AetheryteNames.ContainsKey(id))
                    AetheryteNames.Add(id, name);
            });
        }

        private static void InitDelegates(DalamudPluginInterface plugin) {
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

        private static void InitAddresses(DalamudPluginInterface plugin) {
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