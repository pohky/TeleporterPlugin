using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using TeleporterPlugin.Managers;

namespace TeleporterPlugin {
    public class PluginUi : IDisposable {
        private readonly TeleporterPlugin _plugin;
        public bool DebugVisible;
        public bool SettingsVisible;

        public PluginUi(TeleporterPlugin plugin) {
            _plugin = plugin;
            _plugin.Interface.UiBuilder.OnBuildUi += Draw;
        }

        private void Draw() {
            if (DebugVisible) DrawDebug();
            if (SettingsVisible) DrawSettings();
        }

        public void DrawSettings() {
            var windowSize = new Vector2(350, 315);
            ImGui.SetNextWindowSize(windowSize, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(windowSize, new Vector2(float.MaxValue, float.MaxValue));
            if (ImGui.Begin($"{_plugin.Name} Settings", ref SettingsVisible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) {
                
            }
            ImGui.End();
        }

        #region DebugWindow

        private readonly List<string> dbg_locations = new List<string> {"GetList to Update"};
        private readonly List<string> dbg_aetheryteId = new List<string> {"Empty"};
        private readonly List<string> dbg_subIndex = new List<string> {"Empty"};
        private readonly List<string> dbg_zoneId = new List<string> {"Empty"};
        private int dbg_selected;

        public void DrawDebug() {
            var windowSize = new Vector2(350, 315);
            ImGui.SetNextWindowSize(windowSize, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(windowSize, new Vector2(float.MaxValue, float.MaxValue));
            if (ImGui.Begin($"{_plugin.Name} Debug", ref DebugVisible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) {
                ImGui.TextUnformatted($"UiModule: {TeleportManager.UiModuleAddress.ToInt64():X8}");
                ImGui.TextUnformatted($"UiAgentModule: {TeleportManager.UiAgentModuleAddress.ToInt64():X8}");
                ImGui.TextUnformatted($"KnownLocations: {TeleportManager.AvailableLocationsAddress.ToInt64():X8}");
                ImGui.Separator();
                
                if (ImGui.Button("GetList")) {
                    dbg_locations.Clear();
                    dbg_aetheryteId.Clear();
                    dbg_subIndex.Clear();
                    dbg_zoneId.Clear();
                    foreach (var location in TeleportManager.AvailableLocations) {
                        dbg_locations.Add(location.Name);
                        dbg_aetheryteId.Add(location.AetheryteId.ToString());
                        dbg_subIndex.Add(location.SubIndex.ToString());
                        dbg_zoneId.Add(location.ZoneId.ToString());
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("Teleport")) 
                    TeleportManager.Teleport(dbg_locations[dbg_selected]);
                ImGui.SameLine();
                if (ImGui.Button("Teleport (Ticket)"))
                    TeleportManager.TeleportTicket(dbg_locations[dbg_selected]);
                ImGui.SameLine();
                if (ImGui.Button("Teleport (Map)"))
                    TeleportManager.TeleportMap(dbg_locations[dbg_selected]);

                ImGui.BeginChild("##scrollingregion");
                ImGui.Columns(4, "##listbox");
                ImGui.SetColumnWidth(1, 50);
                ImGui.SetColumnWidth(2, 80);
                ImGui.SetColumnWidth(3, 80);
                ImGui.Separator();
                ImGui.Text("Name"); ImGui.NextColumn();
                ImGui.Text("Id"); ImGui.NextColumn();
                ImGui.Text("SubIndex"); ImGui.NextColumn();
                ImGui.Text("ZoneId"); ImGui.NextColumn();

                ImGui.Separator();
                for (var i = 0; i < dbg_locations.Count; i++) {
                    if (ImGui.Selectable($"{dbg_locations[i]}", dbg_selected == i, ImGuiSelectableFlags.SpanAllColumns))
                        dbg_selected = i;
                    ImGui.NextColumn();
                    ImGui.Text(dbg_aetheryteId[i]); ImGui.NextColumn();
                    ImGui.Text(dbg_subIndex[i]); ImGui.NextColumn();
                    ImGui.Text(dbg_zoneId[i]); ImGui.NextColumn();
                }

                ImGui.EndChild();
                ImGui.Columns(1);
                ImGui.Separator();
            }
            ImGui.End();
        }

        #endregion
        
        public void Dispose() {
            _plugin.Interface.UiBuilder.OnBuildUi -= Draw;
        }
    }
}