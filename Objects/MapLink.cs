using System;
using System.Linq;
using System.Numerics;
using Dalamud;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using TeleporterPlugin.Managers;

namespace TeleporterPlugin.Objects {
    public class MapLink : IEquatable<MapLink> {
        private readonly TeleporterPlugin _plugin;
        public Vector2 Location { get; }
        public string PlaceName { get; }
        public uint TerritoryId { get; }
        public bool HasAetheryte => Aetheryte != null;
        public AetheryteLocation Aetheryte { get; }
        public string SenderName { get; }
        public string Message { get; }

        public MapLink(TeleporterPlugin plugin, MapLinkPayload payload, string senderName, string message) {
            _plugin = plugin;
            Location = new Vector2(payload.XCoord, payload.YCoord);
            var territory = plugin.Interface.Data.GetExcelSheet<TerritoryType>(ClientLanguage.English).GetRow(payload.TerritoryType.RowId);
            //territory = AetheryteDataManager.Territories[plugin.Language].GetRow(payload.TerritoryType.RowId);
            plugin.Log($"Lang: {plugin.Language} Name: {territory.PlaceName.Value.Name} Map: {territory.Map.Value.PlaceName.Value.Name}");
            PlaceName = territory.PlaceName.Value.Name;
            TerritoryId = payload.TerritoryType.RowId;
            SenderName = senderName;
            Message = message;
            Aetheryte = GetClosestAetheryte();
        }

        private AetheryteLocation GetClosestAetheryte() {
            var aetherytes = AetheryteDataManager.GetAetheryteLocationsByTerritory(TerritoryId, _plugin.Language);
            if (aetherytes.Count <= 0) return null;
            return aetherytes.Aggregate((curMin, x) => curMin == null || x.Distance2D(Location) < curMin.Distance2D(Location) ? x : curMin);
        }

        public override string ToString() {
            return $"{PlaceName} ({Math.Round(Location.X, 2):N1}, {Math.Round(Location.Y, 2):N1})";
        }

        public bool Equals(MapLink other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Location.Equals(other.Location) && TerritoryId == other.TerritoryId;
        }

        public override bool Equals(object obj) {
            return ReferenceEquals(this, obj) || obj is MapLink other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                return (Location.GetHashCode() * 397) ^ (int)TerritoryId;
            }
        }

        public static bool operator ==(MapLink left, MapLink right) {
            return Equals(left, right);
        }

        public static bool operator !=(MapLink left, MapLink right) {
            return !Equals(left, right);
        }
    }
}