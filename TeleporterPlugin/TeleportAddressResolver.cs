using System;
using System.Collections.Generic;
using Dalamud.Game;
using Dalamud.Game.Internal;
using TeleporterPlugin.Structs;

namespace TeleporterPlugin {
    public class TeleportAddressResolver : BaseAddressResolver {
        public unsafe Telepo* TelepoInstance => (Telepo*)Telepo;

        // Statics
        public IntPtr Telepo { get; private set; }
        public IntPtr InventoryManager { get; private set; }
        
        // Functions
        public IntPtr ExecuteCommand { get; private set; }
        public IntPtr InventoryManager_GetInventoryItemCount { get; private set; }
        public IntPtr Telepo_TeleportWithTicket { get; private set; }
        public IntPtr Telepo_Teleport { get; private set; }
        public IntPtr Telepo_UpdatePlayerAetheryteList { get; private set; }

        protected override void Setup64Bit(SigScanner sig) {
            Telepo = sig.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? 48 8B 12");
            InventoryManager = sig.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? 44 8B 08 44 0F B7 44 24");

            ExecuteCommand = sig.ScanText("E8 ?? ?? ?? ?? 8D 43 0A");
            InventoryManager_GetInventoryItemCount = sig.ScanText("E8 ?? ?? ?? ?? 85 C0 74 C7");
            Telepo_TeleportWithTicket = sig.ScanText("E8 ?? ?? ?? ?? 84 C0 75 1B 44 0F B6 CE");
            Telepo_Teleport = sig.ScanText("E8 ?? ?? ?? ?? 48 8B 4B 10 84 C0 48 8B 01 74 2C");
            Telepo_UpdatePlayerAetheryteList = sig.ScanText("E8 ?? ?? ?? ?? 48 8B 48 08 48 2B 08");
        }

        public unsafe List<TeleportInfo> GetTeleportLocations() {
            var count = TelepoInstance->AetheryteVector.Size();
            var list = new List<TeleportInfo>((int)count);
            for (var i = 0; i < count; i++)
                list.Add(TelepoInstance->AetheryteVector.Get(i));
            return list;
        }

        public unsafe void TeleportCommand(uint aetheryteId, byte subIndex, bool useTicket) {
            var ticket = (uint)(useTicket ? 1 : 0);
            var f = (delegate*<uint, uint, uint, uint, uint, void>)ExecuteCommand;
            f(0xCA, aetheryteId, ticket, subIndex, 0);
        }
    }
}