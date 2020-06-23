using System.Runtime.InteropServices;

namespace TeleporterPlugin.Objects {
    [StructLayout(LayoutKind.Explicit, Size = 20)]
    internal struct TeleportLocationStruct {
        [FieldOffset(0)] public uint AetheryteId;
        [FieldOffset(4)] public uint GilCost;
        [FieldOffset(8)] public ushort ZoneId;
        [FieldOffset(13)] public byte SubIndex;
    }

    public class TeleportLocation {
        public uint AetheryteId { get; }
        public int GilCost { get; }
        public ushort ZoneId { get; }
        public byte SubIndex { get; }

        public string Name { get; }

        internal TeleportLocation(TeleportLocationStruct data, string name) {
            AetheryteId = data.AetheryteId;
            GilCost = (int)data.GilCost;
            ZoneId = data.ZoneId;
            SubIndex = data.SubIndex;
            Name = name;
        }
    }
}