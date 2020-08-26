using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Chat;
using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using TeleporterPlugin.Managers;

namespace TeleporterPlugin.Objects {
    public class MapLink : IEquatable<MapLink> {
        private readonly TeleporterPlugin _plugin;
        private string _typeString;
        public Vector2 Location { get; }
        public string PlaceName { get; }
        public uint TerritoryId { get; }
        public bool HasAetheryte => Aetheryte != null;
        public AetheryteLocation Aetheryte { get; }
        public string SenderName { get; }
        public string Message { get; }
        public byte[] Data { get; }
        public XivChatType ChatType { get; }

        public MapLink(TeleporterPlugin plugin, XivChatType type, MapLinkPayload payload, string senderName, ref SeString message) {
            _plugin = plugin;
            ChatType = type;
            Location = new Vector2(payload.XCoord, payload.YCoord);
            PlaceName = payload.PlaceName;
            TerritoryId = payload.TerritoryType.RowId;
            SenderName = senderName;
            Message = message.TextValue;
            Data = message.Encode();
            Aetheryte = GetClosestAetheryte();
        }

        private AetheryteLocation GetClosestAetheryte() {
            var aetherytes = AetheryteDataManager.GetAetheryteLocationsByTerritoryId(TerritoryId, _plugin.Language);
            if (aetherytes.Count <= 0) return null;
            return aetherytes.Aggregate((curMin, x) => curMin == null || x.Distance2D(Location) < curMin.Distance2D(Location) ? x : curMin);
        }

        public string GetTypeString() {
            return _typeString ?? (_typeString = GetEnumDescription(ChatType));
        }

        public static string GetEnumDescription(Enum value) {
            var fi = value.GetType().GetField(value.ToString());
            if (fi.GetCustomAttributes(typeof(XivChatTypeInfoAttribute), false) is XivChatTypeInfoAttribute[] attributes && attributes.Any()) {
                return attributes.First().FancyName;
            }
            return value.ToString();
        }

        public override string ToString() {
            return $"{PlaceName} ({Math.Round(Location.X, 2):N1} {Math.Round(Location.Y, 2):N1})";
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