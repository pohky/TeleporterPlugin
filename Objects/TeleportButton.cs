using System;
using ImGuiNET;

namespace TeleporterPlugin.Objects {
    [Serializable]
    public class TeleportButton : IEquatable<TeleportButton> {
        private static readonly Random Rng = new Random();
        public string Text;
        public string Aetheryte;
        public bool UseTickets;

        public TeleportButton(string text, string aetheryte, bool useTickets) {
            Text = text ?? $"Button{Rng.Next(0, 9999)}";
            Aetheryte = aetheryte ?? string.Empty;
            UseTickets = useTickets;
        }

        public bool Draw() {
            if (!ImGui.Button(Text ?? "Invalid"))
                return false;
            return true;
        }

        #region IEquatable

        public bool Equals(TeleportButton other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Text, other.Text, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj) {
            return ReferenceEquals(this, obj) || obj is TeleportButton other && Equals(other);
        }

        public override int GetHashCode() {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(Text);
        }

        public static bool operator ==(TeleportButton left, TeleportButton right) {
            return Equals(left, right);
        }

        public static bool operator !=(TeleportButton left, TeleportButton right) {
            return !Equals(left, right);
        }

        #endregion
    }
}