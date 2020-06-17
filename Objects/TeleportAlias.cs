using System;
using System.Text;
using System.Text.RegularExpressions;

namespace TeleporterPlugin.Objects {
    public class TeleportAlias {
        public static TeleportAlias Empty => new TeleportAlias("", "");
        public const char SeperatorChar = ':';
        public uint BufferSize { get; }
        public readonly byte[] AliasBuffer;
        public readonly byte[] ValueBuffer;

        internal bool Selected;

        public string Alias {
            get => Encoding.UTF8.GetString(AliasBuffer).Replace("\0", "");
            set {
                var newValue = value?.Trim() ?? "";
                if (string.IsNullOrEmpty(newValue)) {
                    Array.Clear(AliasBuffer, 0, AliasBuffer.Length);
                    return;
                }
                var bytes = Encoding.UTF8.GetBytes(newValue);
                Array.Clear(AliasBuffer, 0, AliasBuffer.Length);
                if (bytes.Length > 0)
                    Array.Copy(bytes, AliasBuffer, bytes.Length);
            }
        }

        public string Value {
            get => Encoding.UTF8.GetString(ValueBuffer).Replace("\0", "");
            set {
                var newValue = value?.Trim() ?? "";
                if (string.IsNullOrEmpty(newValue)) {
                    Array.Clear(ValueBuffer, 0, ValueBuffer.Length);
                    return;
                }
                var bytes = Encoding.UTF8.GetBytes(newValue);
                Array.Clear(ValueBuffer, 0, ValueBuffer.Length);
                if (bytes.Length > 0)
                    Array.Copy(bytes, ValueBuffer, bytes.Length);
            }
        }

        public TeleportAlias(string alias, string value, uint bufferSize = 160) {
            if (alias == null) alias = "";
            if (value == null) value = "";
            if (alias.Length > bufferSize) bufferSize = (uint)alias.Length;
            if (value.Length > bufferSize) bufferSize = (uint)value.Length;
            BufferSize = bufferSize;
            AliasBuffer = new byte[bufferSize];
            ValueBuffer = new byte[bufferSize];
            Alias = alias.Trim();
            Value = value.Trim();
        }

        public static TeleportAlias FromString(string aliasString) {
            var split = Regex.Split(aliasString, $"{SeperatorChar}{SeperatorChar}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return split.Length != 2 ? null : new TeleportAlias(split[0], split[1]);
        }

        public TeleportAlias RemoveInvalidChars() {
            Alias = Alias.Replace($"{SeperatorChar}", "").Replace("\0", "");
            Value = Value.Replace($"{SeperatorChar}", "").Replace("\0", "");
            return this;
        }

        public void Clear() {
            Alias = null;
            Value = null;
        }

        public override string ToString() {
            return $"{Alias}{SeperatorChar}{SeperatorChar}{Value}";
        }
    }
}