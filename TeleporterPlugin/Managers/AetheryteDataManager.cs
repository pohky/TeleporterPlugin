using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using TeleporterPlugin.Objects;

namespace TeleporterPlugin.Managers {
    public static class AetheryteDataManager {
        public static Dictionary<ClientLanguage, Dictionary<uint, string>> AetheryteNames = new();
        public static Dictionary<ClientLanguage, List<AetheryteLocation>> AetheryteLocations = new();

        private static readonly Dictionary<ClientLanguage, string> _apartmentNames = new() {
            {ClientLanguage.English, "Apartment"},
            {ClientLanguage.German, "Wohnung"},
            {ClientLanguage.French, "Appartement"},
            {ClientLanguage.Japanese, "アパルトメント"}
        };

        private static readonly Dictionary<ClientLanguage, string> _sharedHouseNames = new() {
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

            return subIndex switch {
                0 => name, //use default name
                128 => _apartmentNames[language],
                var n when n >= 1 && n <= 127 => _sharedHouseNames[language].Replace("<number>", $"{subIndex}"),
                _ => $"Unknown Estate ({id}, {subIndex})"
            };
        }

        internal static List<AetheryteLocation> GetAetheryteLocationsByTerritoryId(uint territory, ClientLanguage language) {
            var list = AetheryteLocations[language];
            return list.Where(l => l.TerritoryId == territory).ToList();
        }

        internal static List<AetheryteLocation> GetAetheryteLocationsByTerritoryName(string territory, ClientLanguage language, bool matchContains) {
            if (string.IsNullOrEmpty(territory))
                return new List<AetheryteLocation>();
            var list = AetheryteLocations[language];
            if(matchContains)
                return list.Where(l => l.TerritoryName.ToUpperInvariant().Contains(territory.ToUpperInvariant())).ToList();
            return list.Where(l => l.TerritoryName.Equals(territory, StringComparison.InvariantCultureIgnoreCase)).ToList();
        }

        public static void Init(DalamudPluginInterface plugin) {
            AetheryteNames = new Dictionary<ClientLanguage, Dictionary<uint, string>>();
            AetheryteLocations = new Dictionary<ClientLanguage, List<AetheryteLocation>>();

            var aetheryteSheet = plugin.Data.GetExcelSheet<Aetheryte>();
            var territorySheet = plugin.Data.GetExcelSheet<TerritoryType>();
            
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
                    locationList.Add(new AetheryteLocation {
                        TerritoryId = aetheryte.Territory.Row,
                        TerritoryName = placeNameSheet.GetRow(territorySheet.GetRow(aetheryte.Territory.Row).PlaceName.Row).Name,
                        Name = name
                    });
                }

                AetheryteNames[language] = nameDictionary;
                AetheryteLocations[language] = locationList;
            }
        }
    }
}