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

        internal static List<AetheryteLocation> GetAetheryteLocationsByTerritory(uint territory, ClientLanguage language) {
            var list = AetheryteLocations[language];
            return list.Where(l => l.TerritoryId == territory).ToList();
        }

        public static void Init(DalamudPluginInterface plugin) {
            AetheryteNames = new Dictionary<ClientLanguage, Dictionary<uint, string>>();
            var languageCount = Enum.GetNames(typeof(ClientLanguage)).Length;

            AetheryteLocations = new Dictionary<ClientLanguage, List<AetheryteLocation>>();
            var mapMarkers = plugin.Data.GetExcelSheet<MapMarker>().Where(m => m.DataType == 3).ToList();

            for (var i = 0; i < languageCount; i++) {
                var language = (ClientLanguage)i;
                var aetheryteList = plugin.Data.GetExcelSheet<Aetheryte>(language);
                var nameList = new Dictionary<uint, string>();
                var locationList = new List<AetheryteLocation>();
                foreach (var aetheryte in aetheryteList) {
                    var id = aetheryte.RowId;
                    if (id <= 0) continue;
                    var name = aetheryte.PlaceName.Value.Name;
                    if (string.IsNullOrEmpty(name)) continue;
                    if (language == ClientLanguage.German)
                        name = Regex.Replace(name, "[^\u0020-\u00FF]+", string.Empty, RegexOptions.Compiled);
                    if (!nameList.ContainsKey(id))
                        nameList.Add(id, name);

                    if (!aetheryte.IsAetheryte) continue;
                    var marker = mapMarkers.FirstOrDefault(m => m.DataKey == aetheryte.RowId);
                    if (marker == null) continue;
                    var scale = aetheryte.Map.Value.SizeFactor;
                    var objX = MapMarkerToMapPos(marker.X, scale);
                    var objY = MapMarkerToMapPos(marker.Y, scale);
                    locationList.Add(new AetheryteLocation {
                        AetheryteId = aetheryte.RowId,
                        Location = new Vector2(objX, objY),
                        TerritoryId = aetheryte.Territory.Value.RowId,
                        //TerritoryId = 0, 
                        //Name = aetheryte.PlaceName.Value.Name
                        Name = name
                    });
                }

                AetheryteNames[language] = nameList;
                AetheryteLocations[language] = locationList;
            }

            //for (var i = 0; i < languageCount; i++) {
            //    var language = (ClientLanguage)i;
            //    var aetheryteList = plugin.Data.GetExcelSheet<Aetheryte>(language);
            //    var locationList = new List<AetheryteLocation>();
            //    foreach (var aetheryte in aetheryteList.Where(a => a.IsAetheryte)) {
            //        var marker = mapMarkers.FirstOrDefault(m => m.DataType == 3 && m.DataKey == aetheryte.RowId);
            //        if (marker == null) continue;
            //        var scale = aetheryte.Map.Value.SizeFactor;
            //        var objX = MapMarkerToMapPos(marker.X, scale);
            //        var objY = MapMarkerToMapPos(marker.Y, scale);
            //        locationList.Add(new AetheryteLocation {
            //            AetheryteId = aetheryte.RowId,
            //            Location = new Vector2(objX, objY),
            //            TerritoryId = aetheryte.Territory.Value.RowId,
            //            Name = aetheryte.PlaceName.Value.Name
            //        });
            //    }
            //    AetheryteLocations.Add(language, locationList);
            //}
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