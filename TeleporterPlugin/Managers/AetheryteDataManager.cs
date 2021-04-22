using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using TeleporterPlugin.Objects;

namespace TeleporterPlugin.Managers {
    public static class AetheryteDataManager {
        public static Dictionary<ClientLanguage, Dictionary<uint, string>> AetheryteNames = new();
        public static Dictionary<ClientLanguage, List<AetheryteLocation>> AetheryteLocations = new();

        private static readonly Dictionary<ClientLanguage, string> m_ApartmentNames = new();

        private static readonly Dictionary<ClientLanguage, string> m_SharedHouseNames = new() {
            { ClientLanguage.English, "Shared Estate (<number>)" },
            { ClientLanguage.German, "Wohngemeinschaft (<number>)" },
            { ClientLanguage.French, "Maison (<number>)" },
            { ClientLanguage.Japanese, "ハウス（シェア：<number>）" }
        };

        internal static string GetAetheryteName(TeleportLocationStruct data, ClientLanguage language) {
            if (!AetheryteNames.TryGetValue(language, out var list))
                return string.Empty;
            if (!list.TryGetValue(data.AetheryteId, out var name))
                return string.Empty;

            if (data.IsAppartment)
                return m_ApartmentNames[language];

            if (data.IsShared)
                return m_SharedHouseNames[language].Replace("<number>", $"{data.SubIndex}");

            return name;
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
            m_ApartmentNames.Clear();

            var aetheryteSheet = plugin.Data.GetExcelSheet<Aetheryte>();
            var territorySheet = plugin.Data.GetExcelSheet<TerritoryType>();
            
            foreach (var language in new HashSet<ClientLanguage>{plugin.ClientState.ClientLanguage, ClientLanguage.English}) {
                var addonSheet = plugin.Data.GetExcelSheet<Addon>(language);
                m_ApartmentNames[language] = addonSheet.GetRow(6760).Text.ToString();

                var placeNameSheet = plugin.Data.GetExcelSheet<PlaceName>(language);
                var nameDictionary = new Dictionary<uint, string>();
                var locationList = new List<AetheryteLocation>();

                foreach (var aetheryte in aetheryteSheet) {
                    var id = aetheryte.RowId;
                    if (id <= 0) continue;
                    var name = placeNameSheet.GetRow(aetheryte.PlaceName.Row).Name.ToString();
                    if (string.IsNullOrEmpty(name)) continue;

                    name = plugin.Sanitizer.Sanitize(name, language);
                    
                    if (!nameDictionary.ContainsKey(id))
                        nameDictionary.Add(id, name);

                    if (!aetheryte.IsAetheryte) continue;

                    var territoryName = placeNameSheet.GetRow(territorySheet.GetRow(aetheryte.Territory.Row).PlaceName.Row).Name.ToString();
                    territoryName = plugin.Sanitizer.Sanitize(territoryName, language);

                    locationList.Add(new AetheryteLocation {
                        TerritoryId = aetheryte.Territory.Row,
                        TerritoryName = territoryName,
                        Name = name
                    });
                }

                AetheryteNames[language] = nameDictionary;
                AetheryteLocations[language] = locationList;
            }
        }
    }
}