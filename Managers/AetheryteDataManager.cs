using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using TeleporterPlugin.Objects;

namespace TeleporterPlugin.Managers {
    public static class AetheryteDataManager {
        public static Dictionary<ClientLanguage, Dictionary<uint, string>> AetheryteNames = new Dictionary<ClientLanguage, Dictionary<uint, string>>();
        public static Dictionary<ClientLanguage, List<AetheryteLocation>> AetheryteLocations = new Dictionary<ClientLanguage, List<AetheryteLocation>>();

        private static readonly Dictionary<ClientLanguage, string> _apartmentNames = new Dictionary<ClientLanguage, string> {
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

        private static readonly uint[] _privateHouseIds = {59, 60, 61, 97}; //limsa, gridania, uldah, kugane

        internal static string GetAetheryteName(uint id, byte subIndex, ClientLanguage language) {
            if (!AetheryteNames.TryGetValue(language, out var list))
                return string.Empty;
            if (!list.TryGetValue(id, out var name))
                return string.Empty;
            if (!_privateHouseIds.Contains(id))
                return name;

            switch (subIndex) {
                case 0: //use default name
                    return name;
                case 128:
                    return _apartmentNames[language];
                case var n when n >= 1 && n <= 127:
                    return _sharedHouseNames[language].Replace("<number>", $"{subIndex}");
                default:
                    return $"Unknown Estate ({id}, {subIndex})";
            }
        }

        internal static List<AetheryteLocation> GetAetheryteLocationsByTerritoryId(uint territory, ClientLanguage language) {
            var list = AetheryteLocations[language];
            return list.Where(l => l.TerritoryId == territory).ToList();
        }

        internal static List<AetheryteLocation> GetAetheryteLocationsByTerritoryName(string territory, ClientLanguage language, bool matchContains) {
            var list = AetheryteLocations[language];
            if(matchContains)
                return list.Where(l => l.TerritoryName.ToUpperInvariant().Contains(territory.ToUpperInvariant())).ToList();
            return list.Where(l => l.TerritoryName.Equals(territory, StringComparison.InvariantCultureIgnoreCase)).ToList();
        }

        public static void Init(DalamudPluginInterface plugin) {
            AetheryteNames = new Dictionary<ClientLanguage, Dictionary<uint, string>>();
            AetheryteLocations = new Dictionary<ClientLanguage, List<AetheryteLocation>>();

            var aetheryteSheet = plugin.Data.GetExcelSheet<Aetheryte>();
            var mapSheet = plugin.Data.GetExcelSheet<Map>();
            var territorySheet = plugin.Data.GetExcelSheet<TerritoryType>();
            var mapMarkerList = plugin.Data.GetExcelSheet<MapMarker>().Where(m => m.DataType == 3).ToList();
            
            foreach (ClientLanguage language in Enum.GetValues(typeof(ClientLanguage))) {
                var placeNameSheet = plugin.Data.GetExcelSheet<PlaceName>(language);
                var nameDictionary = new Dictionary<uint, string>();
                var locationList = new List<AetheryteLocation>();

                foreach (var aetheryte in aetheryteSheet) {
                    var id = aetheryte.RowId;
                    if (id <= 0) continue;
                    string name = placeNameSheet.GetRow(aetheryte.PlaceName.Row).Name;
                    if (string.IsNullOrEmpty(name)) continue;
                    if (language == ClientLanguage.German)
                        name = Regex.Replace(name, "[^\u0020-\u00FF]+", string.Empty, RegexOptions.Compiled);
                    if (!nameDictionary.ContainsKey(id))
                        nameDictionary.Add(id, name);
                    
                    if (!aetheryte.IsAetheryte) continue;
                    var marker = mapMarkerList.FirstOrDefault(m => m.DataKey == aetheryte.RowId);
                    if (marker == null) continue;
                    var scale = mapSheet.GetRow(aetheryte.Map.Row).SizeFactor;
                    var markerPos = new Vector2(MapMarkerToMapPos(marker.X, scale), MapMarkerToMapPos(marker.Y, scale));
                    locationList.Add(new AetheryteLocation {
                        AetheryteId = aetheryte.RowId,
                        Location = markerPos,
                        TerritoryId = aetheryte.Territory.Row,
                        TerritoryName = placeNameSheet.GetRow(territorySheet.GetRow(aetheryte.Territory.Row).PlaceName.Row).Name,
                        Name = name
                    });
                }

                AetheryteNames[language] = nameDictionary;
                AetheryteLocations[language] = locationList;
            }
        }

        public static int MapPosToRawPos(float pos, float scale) {
            var num = scale / 100f;
            return (int)((float)((pos - 1.0) * num / 41.0 * 2048.0 - 1024.0) / num * 1000f);
        }

        public static float RawPosToMapPos(int pos, float scale) {
            var num = scale / 100f;
            return (float)((pos / 1000f * num + 1024.0) / 2048.0 * 41.0 / num + 1.0);
        }

        public static float MapMarkerToMapPos(int pos, float scale) {
            var num = scale / 100f;
            var rawPosition = (int)((float)(pos - 1024.0) / num * 1000f);
            return RawPosToMapPos(rawPosition, scale);
        }
    }
}