using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;

namespace TeleporterPlugin {
    public class PluginUi : IDisposable {
        private readonly TeleporterPlugin _plugin;
        public bool DebugVisible;

        private string[] _availableLocations = {"Empty List"};
        private int _currentSelection;

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
                
                if (ImGui.Button("GetList")) 
                    _availableLocations = TeleportManager.AvailableLocations.Select(o => o.Name).ToArray();
                ImGui.SameLine();
                if (ImGui.Button("Teleport")) 
                    TeleportManager.Teleport(_availableLocations[_currentSelection]);
                ImGui.SameLine();
                if (ImGui.Button("Teleport (Ticket)"))
                    TeleportManager.TeleportTicket(_availableLocations[_currentSelection]);
                ImGui.SameLine();
                if (ImGui.Button("Teleport (Map)"))
                    TeleportManager.TeleportMap(_availableLocations[_currentSelection]);

                ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 15);
                ImGui.ListBox("", ref _currentSelection, _availableLocations, _availableLocations.Length, 8);
            }
            ImGui.End();
        }
        
        public void Dispose() {
            _plugin.Interface.UiBuilder.OnBuildUi -= Draw;
        }
    }
}