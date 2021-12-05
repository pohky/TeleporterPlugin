using System.Diagnostics;
using Dalamud.Game;

namespace TeleporterPlugin.Plugin {
    public class TeleporterAddressResolver : BaseAddressResolver {
        public nint BaseAddress { get; private set; }
        public nint GrandCompanyAddress { get; private set; }
        
        protected override unsafe void Setup64Bit(SigScanner scanner) {
            BaseAddress = Process.GetCurrentProcess().MainModule!.BaseAddress;
            GrandCompanyAddress = scanner.GetStaticAddressFromSig("48 8B 05 ?? ?? ?? ?? 44 88 64 24");
        }
    }
}