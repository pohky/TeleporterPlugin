using System.Diagnostics;
using Dalamud.Game;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace TeleporterPlugin.Plugin {
    public class TeleporterAddressResolver : BaseAddressResolver {
        public nint BaseAddress { get; private set; }
        public nint ExecuteCommandAddress { get; private set; }
        public nint GetInventoryItemCountAddress { get; private set; }

        public unsafe delegate* unmanaged <InventoryManager*, uint, byte, byte, byte, short, int> GetInventoryItemCount;
        public unsafe delegate* unmanaged <uint, uint, uint, uint, uint, void> ExecuteCommand;

        protected override unsafe void Setup64Bit(SigScanner scanner) {
            BaseAddress = Process.GetCurrentProcess().MainModule!.BaseAddress;
            ExecuteCommandAddress = scanner.ScanText("E8 ?? ?? ?? ?? 8D 43 0A");
            ExecuteCommand = (delegate* unmanaged <uint, uint, uint, uint, uint, void>)ExecuteCommandAddress;

            GetInventoryItemCountAddress = scanner.ScanText("E8 ?? ?? ?? ?? 8B 53 F1");
            GetInventoryItemCount = (delegate* unmanaged <InventoryManager*, uint, byte, byte, byte, short, int>)GetInventoryItemCountAddress;
        }
    }
}