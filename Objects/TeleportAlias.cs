using System;
using System.Text;

namespace TeleporterPlugin.Objects {
    public class TeleportAlias {
        public static TeleportAlias Empty => new TeleportAlias(null, null);

        [NonSerialized] internal const uint BufferSize = 160;
        [NonSerialized] internal readonly byte[] AliasBuffer;
        [NonSerialized] internal readonly byte[] AetheryteBuffer;
        [NonSerialized] internal bool GuiSelected;
        [NonSerialized] internal int GuiSelectedIndex;

        public string Alias {
            get => GetBufferValue(AliasBuffer);
            set => SetBufferValue(value, AliasBuffer);
        }

        public string Aetheryte {
            get => GetBufferValue(AetheryteBuffer);
            set => SetBufferValue(value, AetheryteBuffer);
        }

        public TeleportType TeleportType { get; set; }

        public TeleportAlias(string alias, string aetheryte, TeleportType type = TeleportType.Direct) {
            AliasBuffer = new byte[BufferSize];
            AetheryteBuffer = new byte[BufferSize];
            Alias = alias;
            Aetheryte = aetheryte;
            TeleportType = type;
        }

        private string GetBufferValue(byte[] buffer) {
            var len = GetZeroIndexOrLength(buffer);
            return len == 0 ? string.Empty : Encoding.UTF8.GetString(buffer, 0, len);
        }

        private void SetBufferValue(string newValue, byte[] buffer) {
            var value = newValue?.Trim();
            if (string.IsNullOrEmpty(value)) {
                Array.Clear(buffer, 0, buffer.Length);
                return;
            }
            var bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length <= 0) return;
            Array.Clear(buffer, 0, buffer.Length);
            Array.Copy(bytes, buffer, bytes.Length > buffer.Length ? buffer.Length : bytes.Length);
        }

        private int GetZeroIndexOrLength(byte[] buffer) {
            for(var i = 0; i < buffer.Length; i++)
                if (buffer[i] == 0) return i;
            return buffer.Length;
        }

        public void Clear() {
            Alias = null;
            Aetheryte = null;
        }

        public override string ToString() {
            return $"{Alias} | {Aetheryte} {TeleportType.ToString().ToLower()}";
        }
    }
}