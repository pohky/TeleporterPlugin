using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using TeleporterPlugin.Objects;

namespace TeleporterPlugin.Gui {
    public class DebugWindow : Window {
        private int dbg_selected;
        private readonly List<TeleportLocation> dbg_locationList = new();
        
        public DebugWindow(TeleporterPlugin plugin) : base(plugin) { }

        protected override void DrawUi() {
            var windowSize = new Vector2(450, 315);
            ImGui.SetNextWindowSize(windowSize, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(windowSize, new Vector2(float.MaxValue, float.MaxValue));
            if (ImGui.Begin($"{Plugin.Name} Debug", ref WindowVisible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) {
                ImGui.TextUnformatted($"AetheryteList: {Plugin.Manager.AetheryteListAddress.ToInt64():X8}");
                ImGui.TextUnformatted($"TeleportStatus: {Plugin.Manager.TeleportStatusAddress.ToInt64():X8}");
                ImGui.TextUnformatted($"ItemCountStaticArg: {Plugin.Manager.ItemCountStaticArgAddress.ToInt64():X8}");

                ImGui.Separator();
                if (ImGui.Button("GetList")) {
                    dbg_locationList.Clear();
                    dbg_locationList.AddRange(Plugin.Manager.AetheryteList);
                }

                ImGui.SameLine();
                if (ImGui.Button("Teleport") && dbg_selected < dbg_locationList.Count)
                    Plugin.Manager.Teleport(dbg_locationList[dbg_selected].Name, true);
                ImGui.SameLine();
                if (ImGui.Button("Teleport (Ticket)") && dbg_selected < dbg_locationList.Count)
                    Plugin.Manager.TeleportTicket(dbg_locationList[dbg_selected].Name, true, true);
                ImGui.SameLine();
                if (ImGui.Button("Teleport (Ticket + Popup)") && dbg_selected < dbg_locationList.Count)
                    Plugin.Manager.TeleportTicket(dbg_locationList[dbg_selected].Name, false, true);

                ImGui.BeginChild("##scrollingregion");
                ImGui.Columns(7, "##listbox");
                ImGui.SetColumnWidth(1, 50);
                ImGui.SetColumnWidth(2, 80);
                ImGui.SetColumnWidth(3, 50);
                ImGui.SetColumnWidth(4, 50);
                ImGui.SetColumnWidth(5, 80);
                ImGui.SetColumnWidth(6, 80);
                ImGui.Separator();
                ImGui.Text("Name"); ImGui.NextColumn();
                ImGui.Text("Id"); ImGui.NextColumn();
                ImGui.Text("SubIndex"); ImGui.NextColumn();
                ImGui.Text("Ward"); ImGui.NextColumn();
                ImGui.Text("Plot"); ImGui.NextColumn();
                ImGui.Text("ZoneId"); ImGui.NextColumn();
                ImGui.Text("Price"); ImGui.NextColumn();

                ImGui.Separator();
                if (dbg_locationList.Count == 0) {
                    ImGui.TextUnformatted("GetList to Update");
                } else {
                    for (var i = 0; i < dbg_locationList.Count; i++) {
                        var location = dbg_locationList[i];
                        if (ImGui.Selectable($"{location.Name}", dbg_selected == i, ImGuiSelectableFlags.SpanAllColumns))
                            dbg_selected = i;
                        ImGui.NextColumn();
                        ImGui.TextUnformatted($"{location.AetheryteId}");
                        ImGui.NextColumn();
                        ImGui.TextUnformatted($"{location.SubIndex}");
                        ImGui.NextColumn();
                        ImGui.TextUnformatted($"{location.Ward}");
                        ImGui.NextColumn();
                        ImGui.TextUnformatted($"{location.Plot}");
                        ImGui.NextColumn();
                        ImGui.TextUnformatted($"{location.ZoneId}");
                        ImGui.NextColumn();
                        ImGui.TextUnformatted($"{location.GilCost}");
                        ImGui.NextColumn();
                    }
                }

                ImGui.EndChild();
                ImGui.Columns(1);
                ImGui.Separator();
            }

            ImGui.End();
        }
    }
}