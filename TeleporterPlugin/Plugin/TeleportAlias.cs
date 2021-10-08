using System;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using TeleporterPlugin.Managers;

namespace TeleporterPlugin.Plugin {
    public class TeleportAlias : IEquatable<TeleportAlias>, IEquatable<TeleportInfo> {
        public string Alias = string.Empty;
        public string Aetheryte = string.Empty;

        public uint AetheryteId;
        public byte SubIndex;
        public byte Ward;
        public byte Plot;

        public void Update(TeleportInfo info) {
            AetheryteId = info.AetheryteId;
            SubIndex = info.SubIndex;
            Ward = info.Ward;
            Plot = info.Plot;
            Aetheryte = AetheryteManager.GetAetheryteName(info);
        }

        #region Equality members

        public bool Equals(TeleportAlias? other) {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return AetheryteId == other.AetheryteId && SubIndex == other.SubIndex && Ward == other.Ward && Plot == other.Plot;
        }

        public override bool Equals(object? obj) {
            return ReferenceEquals(this, obj) || obj is TeleportAlias other && Equals(other);
        }

        public override int GetHashCode() {
            var hashCode = new HashCode();
            hashCode.Add(AetheryteId);
            hashCode.Add(SubIndex);
            hashCode.Add(Ward);
            hashCode.Add(Plot);
            return hashCode.ToHashCode();
        }

        public static bool operator ==(TeleportAlias? left, TeleportAlias? right) {
            return Equals(left, right);
        }

        public static bool operator !=(TeleportAlias? left, TeleportAlias? right) {
            return !Equals(left, right);
        }

        #endregion

        #region Equality members

        public bool Equals(TeleportInfo other) {
            return AetheryteId == other.AetheryteId && SubIndex == other.SubIndex && Ward == other.Ward && Plot == other.Plot;
        }

        #endregion
    }
}