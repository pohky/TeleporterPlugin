using System.Runtime.InteropServices;

namespace TeleporterPlugin.Objects {
    [StructLayout(LayoutKind.Explicit, Size = 0x14)]
    internal struct TeleportLocationStruct {
        [FieldOffset(0x00)] public uint AetheryteId;
        [FieldOffset(0x04)] public uint GilCost;
        [FieldOffset(0x08)] public ushort ZoneId;
        [FieldOffset(0x0B)] public byte Ward;
        [FieldOffset(0x0C)] public byte Plot;
        [FieldOffset(0x0D)] public byte SubIndex;
        [FieldOffset(0x0E)] public byte IsFavourite;

        public bool IsShared => Ward > 0 && Plot > 0;
        public bool IsAppartment => SubIndex == 128 && !IsShared;
    }

    public class TeleportLocation {
        public uint AetheryteId { get; }
        public int GilCost { get; }
        public ushort ZoneId { get; }
        public byte Ward { get; }
        public byte Plot { get; }
        public byte SubIndex { get; }

        public bool IsShared => Ward > 0 && Plot > 0;
        public bool IsAppartment => SubIndex == 128 && !IsShared;

        public string Name { get; }

        internal TeleportLocation(TeleportLocationStruct data, string name) {
            AetheryteId = data.AetheryteId;
            GilCost = (int)data.GilCost;
            ZoneId = data.ZoneId;
            Ward = data.Ward;
            Plot = data.Plot;
            SubIndex = data.SubIndex;
            Name = name;
        }
    }
}