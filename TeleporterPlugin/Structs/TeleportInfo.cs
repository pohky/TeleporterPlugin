using System.Runtime.InteropServices;

namespace TeleporterPlugin.Structs {
    [StructLayout(LayoutKind.Explicit, Size = 0x14)]
    public struct TeleportInfo {
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
}