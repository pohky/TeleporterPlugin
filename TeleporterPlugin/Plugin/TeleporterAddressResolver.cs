using System.Diagnostics;
using Dalamud.Game;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace TeleporterPlugin.Plugin {
    public class TeleporterAddressResolver : BaseAddressResolver {
        public nint BaseAddress { get; private set; }
        public nint GetInventoryItemCountAddress { get; private set; }
        public nint GrandCompanyAddress { get; private set; }

        public unsafe delegate* unmanaged <InventoryManager*, uint, byte, byte, byte, short, int> GetInventoryItemCount;

        protected override unsafe void Setup64Bit(SigScanner scanner) {
            BaseAddress = Process.GetCurrentProcess().MainModule!.BaseAddress;

            GetInventoryItemCountAddress = scanner.ScanText("E8 ?? ?? ?? ?? 8B 53 F1");
            GetInventoryItemCount = (delegate* unmanaged <InventoryManager*, uint, byte, byte, byte, short, int>)GetInventoryItemCountAddress;

            GrandCompanyAddress = scanner.GetStaticAddressFromSig("48 8B 05 ?? ?? ?? ?? 44 88 64 24");
        }
    }
}