using System;
using ImGuiNET;

namespace TeleporterPlugin.Objects {
    [Serializable]
    public class TeleportButton : IEquatable<TeleportButton> {
        private static int _buttonCounter;
        [NonSerialized] public readonly int Id;
        public string Text;
        public string Aetheryte;
        public bool UseTickets;

        public TeleportButton(string text, string aetheryte, bool useTickets) {
            Id = _buttonCounter++;
            Text = text ?? $"Button{Id}";
            Aetheryte = aetheryte ?? string.Empty;
            UseTickets = useTickets;
        }

        public bool Draw() {
            if (!ImGui.Button(Text ?? "Invalid"))
                return false;
            return true;
        }

        public bool Equals(TeleportButton other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        public override bool Equals(object obj) {
            return ReferenceEquals(this, obj) || obj is TeleportButton other && Equals(other);
        }

        public override int GetHashCode() {
            return Id;
        }

        public static bool operator ==(TeleportButton left, TeleportButton right) {
            return Equals(left, right);
        }

        public static bool operator !=(TeleportButton left, TeleportButton right) {
            return !Equals(left, right);
        }
    }
}