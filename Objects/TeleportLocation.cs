using System.Runtime.InteropServices;
using TeleporterPlugin.Managers;

namespace TeleporterPlugin.Objects {
    [StructLayout(LayoutKind.Explicit, Size = 20)]
    public readonly struct TeleportLocation {
        [FieldOffset(0)] public readonly uint AetheryteId;
        [FieldOffset(4)] public readonly uint GilCost;
        [FieldOffset(8)] public readonly ushort ZoneId;
        [FieldOffset(13)] public readonly byte SubIndex;

        public string Name => TeleportManager.GetNameForLocation(this);
    }
}