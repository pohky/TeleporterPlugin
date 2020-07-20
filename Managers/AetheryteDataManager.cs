using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

        private static readonly uint[] _privateHouseIds = {59, 60, 61, 97}; //limsa, gridania, uldah, kugane

        internal static string GetAetheryteName(uint id, byte subIndex, ClientLanguage language) {
            if (!AetheryteNames.TryGetValue(language, out var list))
                return string.Empty;
            if(!list.TryGetValue(id, out var name))
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
        
        public static void Init(DalamudPluginInterface plugin) {
            AetheryteNames = new Dictionary<ClientLanguage, Dictionary<uint, string>>();
            var languageCount = Enum.GetNames(typeof(ClientLanguage)).Length;
            for (var i = 0; i < languageCount; i++) {
                var language = (ClientLanguage)i;
                var aetherytes = plugin.Data.GetExcelSheet<Aetheryte>(language);
                var nameList = new Dictionary<uint, string>();
                foreach (var data in aetherytes) {
                    var id = data.RowId;
                    if (id <= 0) continue;
                    var name = data.PlaceName.Value.Name;
                    if (string.IsNullOrEmpty(name)) continue;
                    if (language == ClientLanguage.German)
                        name = Regex.Replace(name, "[^\u0020-\u00FF]+", string.Empty, RegexOptions.Compiled);
                    if (!nameList.ContainsKey(id))
                        nameList.Add(id, name);
                }

                AetheryteNames[language] = nameList;
            }
        }
    }
}