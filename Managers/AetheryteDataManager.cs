using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;

namespace TeleporterPlugin.Managers {
    public static class AetheryteDataManager {
        public static Dictionary<ClientLanguage, Dictionary<uint, string>> AetheryteNames = new Dictionary<ClientLanguage, Dictionary<uint, string>>();

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

        private static readonly uint[] _privateHouseIds = {59, 60, 61, 97}; //limsa, gridania, uldah, shiro

        internal static string GetAetheryteName(uint id, byte subIndex, ClientLanguage language) {
            if (!AetheryteNames.TryGetValue(language, out var list))
                return string.Empty;
            if(!list.TryGetValue(id, out var name))
                return string.Empty;
            if (!_privateHouseIds.Contains(id))
                return name;

            switch (subIndex) {
                case 0: break; // use default name
                case 128:
                    name = _apartmentNames[language];
                    break;
                case var n when n >= 1 && n <= 127:
                    name = _sharedHouseNames[language].Replace("<number>", $"{subIndex}");
                    break;
                default:
                    name = $"Unknown Estate ({id}, {subIndex})";
                    break;
            }
            return name;
        }

        #region Init
        
        public static void Init(DalamudPluginInterface plugin) {
            InitNames(plugin);
        }

        private static void InitNames(DalamudPluginInterface plugin) {
            AetheryteNames = new Dictionary<ClientLanguage, Dictionary<uint, string>>();
            var languageCount = Enum.GetNames(typeof(ClientLanguage)).Length;
            for (var i = 0; i < languageCount; i++) {
                var language = (ClientLanguage)i;
                var aetherytes = plugin.Data.GetExcelSheet<Aetheryte>(language);
                var placeNames = plugin.Data.GetExcelSheet<PlaceName>(language);
                var nameList = new Dictionary<uint, string>();
                foreach (var data in aetherytes.GetRows()) {
                    var id = data.RowId;
                    var place = data.PlaceName?.Row;
                    if (id <= 0 || !place.HasValue || place.Value == 0) continue;
                    var name = placeNames.GetRow(place.Value).Name;
                    if (string.IsNullOrEmpty(name)) continue;
                    if (language == ClientLanguage.German)
                        name = name.Replace("", "");
                    if (!nameList.ContainsKey(id))
                        nameList.Add(id, name);
                }
                AetheryteNames[language] = nameList;
            }
        }

        #endregion
    }
}