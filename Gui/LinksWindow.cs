using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.Chat;
using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using ImGuiNET;
using TeleporterPlugin.Objects;

namespace TeleporterPlugin.Gui {
    public class LinksWindow : Window<TeleporterPlugin> {
        private int _selectedLink;
        public HashSet<MapLink> MapLinks { get; } = new HashSet<MapLink>();
        private Stack<MapLink> UndoLinkStack { get; } = new Stack<MapLink>();

        public LinksWindow(TeleporterPlugin plugin) : base(plugin) { }

        public void ChatOnChatMessage(XivChatType type, uint senderid, ref SeString sender, ref SeString message, ref bool ishandled) {
            if (!Visible && !Plugin.Config.LinkTrackerAlwaysActive) return;
            foreach (var payload in message.Payloads) {
                if (!(payload is MapLinkPayload mapLink)) continue;
                var link = new MapLink(Plugin, type, mapLink, sender.TextValue, message);
                if (link.HasAetheryte) {
                    MapLinks.Add(link);
                    if (Plugin.Config.LinkTrackerAutoPop)
                        Visible = true;
                }
            }
        }

        protected override void DrawUi() {
            ImGui.SetNextWindowSize(new Vector2(350, 150), ImGuiCond.FirstUseEver);
            if (!ImGui.Begin("TP Link Tracker", ref WindowVisible, ImGuiWindowFlags.NoScrollWithMouse)) {
                ImGui.End();
                return;
            }

            if(ImGui.CollapsingHeader("Link Tracker Settings")) 
                DrawTrackerSettings();
            DrawLinkList();
            ImGui.End();
        }

        private void DrawTrackerSettings() {
            if(ImGui.Checkbox("Always Active", ref Plugin.Config.LinkTrackerAlwaysActive))
                Plugin.Config.Save();
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Also collect Links while this window is closed.");
            if (Plugin.Config.LinkTrackerAlwaysActive) {
                ImGui.SameLine();
                if(ImGui.Checkbox("Open on new Link", ref Plugin.Config.LinkTrackerAutoPop))
                    Plugin.Config.Save();
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Open this window if a Link is posted in chat.");
            }
            if (ImGui.Checkbox("Prefer Ticket Teleport", ref Plugin.Config.LinkTrackerUseTickets))
                Plugin.Config.Save();
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Try using Tickets when teleporting to closest Aetheryte");
            ImGui.Separator();
        }

        private void DrawLinkList() {
            if (ImGui.Button("Clear"))
                MapLinks.Clear();
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Remove ALL links from the list.");
            ImGui.SameLine();
            if (ImGui.Button("Restore"))
                if (UndoLinkStack.Count > 0)
                    MapLinks.Add(UndoLinkStack.Pop());
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Restore deleted links. (Does not undo a Clear)");
            ImGui.SameLine();
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Double Click = Teleport to closest Aetheryte\n" +
                                 "Shift + Left Click = Set a flag on the Map.\n" +
                                 "Shift + Right Click = Delete Link from the list");
            }

            ImGui.BeginChild("##scrollingregion");
            ImGui.Columns(3, "##listbox");
            ImGui.Separator();
            ImGui.Text("Sender"); ImGui.NextColumn();
            ImGui.Text("Location"); ImGui.NextColumn();
            ImGui.Text("Chat"); ImGui.NextColumn();
            ImGui.Separator();

            var mapLinks = MapLinks.Reverse().ToArray();
            for (var i = 0; i < mapLinks.Length; i++) {
                var link = mapLinks[i];
                if (ImGui.Selectable($"{link.SenderName}", _selectedLink == i, ImGuiSelectableFlags.SpanAllColumns))
                    _selectedLink = i;
                if (ImGui.IsItemHovered()) {
                    if (ImGui.IsMouseDoubleClicked(0)) {
                        if (link.HasAetheryte) {
                            if(Plugin.Config.LinkTrackerUseTickets)
                                Plugin.CommandHandler("/tpt", link.Aetheryte.Name);
                            else Plugin.CommandHandler("/tp", link.Aetheryte.Name);
                        }
                        else Plugin.LogError($"No Aetheryte found for: {link}");
                    } else if (ImGui.IsMouseClicked(0) && IsShiftKeyPressed()) {
                        link.OpenOnMap();
                    } else if (ImGui.IsMouseClicked(1) && IsShiftKeyPressed()) {
                        UndoLinkStack.Push(link);
                        MapLinks.Remove(link);
                    }
                    ImGui.SetTooltip(link.Message);
                }

                ImGui.NextColumn();
                ImGui.Text(link.ToString());
                ImGui.NextColumn();
                ImGui.Text($"{link.GetTypeString()}");
                ImGui.NextColumn();
            }

            ImGui.EndChild();
            ImGui.Columns(1);
        }

        [DllImport("user32")]
        private static extern short GetAsyncKeyState(int vKey);

        private static bool IsShiftKeyPressed() {
            var state = GetAsyncKeyState(0x10);
            return (state & 0x8000) != 0;
        }
    }
}