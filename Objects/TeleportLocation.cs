using System.Runtime.InteropServices;
using TeleporterPlugin.Managers;

namespace TeleporterPlugin.Objects {
    [StructLayout(LayoutKind.Explicit, Size = 20)]
    public struct TeleportLocation {
        [FieldOffset(0)] public uint AetheryteId;
        [FieldOffset(4)] public uint GilCost;
        [FieldOffset(8)] public ushort ZoneId;
        [FieldOffset(13)] public byte SubIndex;

        public string Name => TeleportManager.GetNameForLocation(this);
    }
}