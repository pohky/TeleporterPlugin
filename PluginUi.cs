using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace TeleporterPlugin {
    public class PluginUi : IDisposable {
        private readonly TeleporterPlugin _plugin;
        public bool DebugVisible;

        private List<string> _locations = new List<string>{"GetList to Update"};
        private List<string> _aetheryteId = new List<string> {"Empty"};
        private List<string> _subIndex = new List<string> {"Empty"};
        private List<string> _zoneId = new List<string> {"Empty"};
        private int _selected;

        public PluginUi(TeleporterPlugin plugin) {
            _plugin = plugin;
            _plugin.Interface.UiBuilder.OnBuildUi += Draw;
        }

        private void Draw() {
            if (DebugVisible) DrawDebug();
        }

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
                    _locations.Clear();
                    _aetheryteId.Clear();
                    _subIndex.Clear();
                    _zoneId.Clear();
                    foreach (var location in TeleportManager.AvailableLocations) {
                        _locations.Add(location.Name);
                        _aetheryteId.Add(location.AetheryteId.ToString());
                        _subIndex.Add(location.SubIndex.ToString());
                        _zoneId.Add(location.ZoneId.ToString());
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("Teleport")) 
                    TeleportManager.Teleport(_locations[_selected]);
                ImGui.SameLine();
                if (ImGui.Button("Teleport (Ticket)"))
                    TeleportManager.TeleportTicket(_locations[_selected]);
                ImGui.SameLine();
                if (ImGui.Button("Teleport (Map)"))
                    TeleportManager.TeleportMap(_locations[_selected]);

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
                for (var i = 0; i < _locations.Count; i++) {
                    if (ImGui.Selectable($"{_locations[i]}", _selected == i, ImGuiSelectableFlags.SpanAllColumns))
                        _selected = i;
                    ImGui.NextColumn();
                    ImGui.Text(_aetheryteId[i]); ImGui.NextColumn();
                    ImGui.Text(_subIndex[i]); ImGui.NextColumn();
                    ImGui.Text(_zoneId[i]); ImGui.NextColumn();
                }

                ImGui.EndChild();
                ImGui.Columns(1);
                ImGui.Separator();
            }
            ImGui.End();
        }
        
        public void Dispose() {
            _plugin.Interface.UiBuilder.OnBuildUi -= Draw;
        }
    }
}