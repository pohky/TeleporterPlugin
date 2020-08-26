using System;
using System.Numerics;

namespace TeleporterPlugin.Objects {
    public class AetheryteLocation {
        public Vector2 Location { get; set; }
        public uint AetheryteId { get; set; }
        public uint TerritoryId { get; set; }
        public string TerritoryName { get; set; }
        public string Name { get; set; }

        public float Distance2D(Vector2 other) {
            var diffX = Location.X - other.X;
            var diffY = Location.Y - other.Y;
            return (float)Math.Sqrt(diffX * diffX + diffY * diffY);
        }

        public override string ToString() {
            return $"{Name}";
        }
    }
}