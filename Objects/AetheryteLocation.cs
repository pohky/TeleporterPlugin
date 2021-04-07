namespace TeleporterPlugin.Objects {
    public class AetheryteLocation {
        public uint TerritoryId { get; set; }
        public string TerritoryName { get; set; }
        public string Name { get; set; }

        public override string ToString() {
            return $"{Name}";
        }
    }
}