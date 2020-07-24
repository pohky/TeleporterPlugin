using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using TeleporterPlugin.Objects;

namespace TeleporterPlugin.Gui {
    public class AetherGateWindow : Window<TeleporterPlugin> {
        private string _buttonTextBuffer = string.Empty;
        private string _buttonAetheryteBuffer = string.Empty;
        private bool _buttonUseTickets = true;
        private DateTime _lastAetheryteListUpdate = DateTime.MinValue;
        private string[] _aetheryteList = new string[0];

        public Configuration Config => Plugin.Config;
        public override bool Visible {
            get => Config.UseFloatingWindow;
            set => Config.UseFloatingWindow = value;
        }

        public AetherGateWindow(TeleporterPlugin plugin) : base(plugin) { }

        protected override void DrawUi() {
            if (!Plugin.IsLoggedIn) return;
            ImGui.SetNextWindowSizeConstraints(new Vector2(150, 65), new Vector2(float.MaxValue, float.MaxValue));
            if (!ImGui.Begin("AetherGate", ref Config.UseFloatingWindow, ImGuiWindowFlags.NoScrollWithMouse)) return;

            if (ImGui.BeginChild("##floatyButtons")) {
                if (ImGui.BeginPopupContextWindow("##addButton", 1, false)) {
                    ImGui.TextUnformatted("Add New Button");
                    ImGui.TextUnformatted("Name:");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(200);
                    ImGui.InputText("##hideBtnAddText", ref _buttonTextBuffer, 256);
                    ImGui.TextUnformatted("Aetheryte:");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(180);
                    ImGui.InputText("##hideBtnAddAetheryte", ref _buttonAetheryteBuffer, 256);
                    ImGui.SameLine();
                    UpdateAetheryteList();
                    if (ImGui.BeginCombo("##hideAddSelectAetheryte", "", ImGuiComboFlags.NoPreview)) {
                        for (var i = 0; i < _aetheryteList.Length; i++)
                            if (ImGui.Selectable(_aetheryteList[i]))
                                _buttonAetheryteBuffer = _aetheryteList[i];
                        ImGui.EndCombo();
                    }

                    ImGui.Checkbox("Use Tickets", ref _buttonUseTickets);
                    if (ImGui.Button("Add")) {
                        if (!string.IsNullOrEmpty(_buttonTextBuffer) && !string.IsNullOrEmpty(_buttonAetheryteBuffer)) {
                            Config.TeleportButtons.Add(new TeleportButton(_buttonTextBuffer, _buttonAetheryteBuffer, _buttonUseTickets));
                            Config.Save();
                        }

                        ImGui.CloseCurrentPopup();
                        _buttonTextBuffer = string.Empty;
                        _buttonAetheryteBuffer = string.Empty;
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("Cancel")) {
                        ImGui.CloseCurrentPopup();
                        _buttonTextBuffer = string.Empty;
                        _buttonAetheryteBuffer = string.Empty;
                    }

                    ImGui.EndPopup();
                }

                var windowVisibleX = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
                var lastButtonX = 0f;
                var style = ImGui.GetStyle();
                for (var i = 0; i < Config.TeleportButtons.Count; i++) {
                    var button = Config.TeleportButtons[i];
                    var buttonSizeX = ImGui.CalcTextSize(button.Text).X + 2 * style.FramePadding.X;
                    var nextButtonX = lastButtonX + style.ItemSpacing.X + buttonSizeX;
                    if (nextButtonX < windowVisibleX)
                        ImGui.SameLine(0, i == 0 ? 0 : style.ItemSpacing.X);
                    if (button.Draw() && !string.IsNullOrEmpty(button.Aetheryte)) {
                        var cmd = button.UseTickets ? "/tpt" : "/tp";
                        Plugin.CommandHandler(cmd, button.Aetheryte);
                    }

                    lastButtonX = ImGui.GetItemRectMax().X;

                    if (ImGui.BeginPopup($"##editButton{i}")) {
                        ImGui.TextUnformatted("Edit Button");
                        ImGui.TextUnformatted("Name:");
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(200);
                        if (ImGui.InputText($"##hideBtnEditText{i}", ref button.Text, 256))
                            Config.Save();
                        ImGui.TextUnformatted("Aetheryte:");
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(180);
                        if (ImGui.InputText($"##hideBtnEditAetheryte{i}", ref button.Aetheryte, 256))
                            Config.Save();
                        ImGui.SameLine();
                        UpdateAetheryteList();
                        if (ImGui.BeginCombo($"##hideEditSelectAetheryte{i}", "", ImGuiComboFlags.NoPreview)) {
                            for (var o = 0; o < _aetheryteList.Length; o++)
                                if (ImGui.Selectable(_aetheryteList[o])) {
                                    button.Aetheryte = _aetheryteList[o];
                                    Config.Save();
                                }

                            ImGui.EndCombo();
                        }

                        if (ImGui.Checkbox("Use Tickets", ref button.UseTickets))
                            Config.Save();
                        if (ImGui.Button("Close"))
                            ImGui.CloseCurrentPopup();
                        ImGui.SameLine(ImGui.GetWindowContentRegionWidth() - ImGui.CalcTextSize("Delete").X - 1);
                        if (ImGui.SmallButton("Delete")) {
                            Config.TeleportButtons.Remove(button);
                            Config.Save();
                        }

                        ImGui.EndPopup();
                    }

                    ImGui.OpenPopupOnItemClick($"##editButton{i}", 1);
                }

                var buttSizeX = ImGui.CalcTextSize("+").X + 2 * style.FramePadding.X;
                var nextButtX = lastButtonX + style.ItemSpacing.X + buttSizeX;
                if (nextButtX < windowVisibleX)
                    ImGui.SameLine(0, Config.TeleportButtons.Count <= 0 ? 0 : style.ItemSpacing.X);
                if (ImGui.Button("+"))
                    ImGui.OpenPopup("##addButton");
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Add new Button.\nRightclick any Button to change them.");

                ImGui.EndChild();
            }

            ImGui.End();
        }

        private void UpdateAetheryteList() {
            if (DateTime.UtcNow.Subtract(_lastAetheryteListUpdate).TotalMilliseconds < 1000)
                return;
            _aetheryteList = Plugin.Manager.AetheryteList.Select(a => a.Name).ToArray();
            _lastAetheryteListUpdate = DateTime.UtcNow;
        }
    }
}