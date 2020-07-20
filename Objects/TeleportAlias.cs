using System;

namespace TeleporterPlugin.Objects {
    public class TeleportAlias {
        public static TeleportAlias Empty => new TeleportAlias(string.Empty, string.Empty);

        [NonSerialized] internal bool GuiSelected;

        public string Alias;
        public string Aetheryte;
        public TeleportType TeleportType;

        public TeleportAlias(string alias, string aetheryte, TeleportType type = TeleportType.Direct) {
            Alias = alias;
            Aetheryte = aetheryte;
            TeleportType = type;
        }
        
        public void Clear() {
            Alias = string.Empty;
            Aetheryte = string.Empty;
        }

        public override string ToString() {
            return $"{Alias} -> {Aetheryte} {TeleportType.ToString().ToLower()}";
        }
    }
}