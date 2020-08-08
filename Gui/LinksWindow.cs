using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Chat;
using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Dalamud.Plugin;
using ImGuiNET;
using TeleporterPlugin.Objects;

namespace TeleporterPlugin.Gui {
    public class LinksWindow : Window<TeleporterPlugin> {
        private int _selectedLink;
        public HashSet<MapLink> MapLinks { get; } = new HashSet<MapLink>();
        
        public LinksWindow(TeleporterPlugin plugin) : base(plugin) {
            WindowVisible = true;
        }

        public void ChatOnChatMessage(XivChatType type, uint senderid, ref SeString sender, ref SeString message, ref bool ishandled) {
            for (var i = 0; i < message.Payloads.Count; i++) {
                if (!(message.Payloads[i] is MapLinkPayload mapLink)) continue;
                PluginLog.Log($"PlaceName: {mapLink.PlaceName}, PlaceNameRegion: {mapLink.PlaceNameRegion}");
                var link = new MapLink(Plugin, mapLink, sender.TextValue, message.TextValue);
                if (link.HasAetheryte)
                    MapLinks.Add(link);
            }
        }

        protected override void DrawUi() {
            ImGui.SetNextWindowSize(new Vector2(300, 150), ImGuiCond.FirstUseEver);
            if (!ImGui.Begin("TP Link Tracker", ref WindowVisible, ImGuiWindowFlags.NoScrollWithMouse)) {
                ImGui.End();
                return;
            }
            if(ImGui.Button("Clear"))
                MapLinks.Clear();
            ImGui.BeginChild("##scrollingregion");
            ImGui.Columns(2, "##listbox");
            ImGui.Separator();
            ImGui.Text("Link"); ImGui.NextColumn();
            ImGui.Text("Aetheryte"); ImGui.NextColumn();
            ImGui.Separator();

            var mapLinks = MapLinks.ToArray();
            for (var i = 0; i < mapLinks.Length; i++) {
                var link = mapLinks[i];
                if (ImGui.Selectable($"{link.SenderName}: {link}", _selectedLink == i, ImGuiSelectableFlags.SpanAllColumns))
                    _selectedLink = i;
                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0)) {
                    if(link.HasAetheryte)
                        Plugin.CommandHandler("/tp", link.Aetheryte.Name);
                    else Plugin.LogError($"No Aetheryte found for: {link}");
                }
                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(1))
                    MapLinks.Remove(link);
                ImGui.NextColumn();
                ImGui.Text($"{(link.HasAetheryte ? link.Aetheryte.ToString() : "No Aetheryte")}");
                ImGui.NextColumn();
            }
            ImGui.EndChild();
            ImGui.Columns(1);
            ImGui.End();
        }
    }
}