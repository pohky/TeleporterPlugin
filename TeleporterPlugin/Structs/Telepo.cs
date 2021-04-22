using System.Runtime.InteropServices;

namespace TeleporterPlugin.Structs {
    [StructLayout(LayoutKind.Explicit, Size = 0x58)]
    public struct Telepo {
        [FieldOffset(0x10)] public Vector<TeleportInfo> AetheryteVector;
    }
}