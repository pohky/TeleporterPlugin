using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using TeleporterPlugin.Objects;

namespace TeleporterPlugin.Gui {
    public class AetherGateWindow : Window<TeleporterPlugin> {
        private string m_ButtonTextBuffer = string.Empty;
        private string m_ButtonAetheryteBuffer = string.Empty;
        private bool m_ButtonUseTickets = true;
        private DateTime m_LastAetheryteListUpdate = DateTime.MinValue;
        private string[] m_AetheryteList = new string[0];

        public Configuration Config => Plugin.Config;
        public override bool Visible {
            get => Config.UseFloatingWindow;
            set => Config.UseFloatingWindow = value;
        }

        public AetherGateWindow(TeleporterPlugin plugin) : base(plugin) { }

        protected override void DrawUi() {
            if (!Plugin.IsLoggedIn) return;
            ImGui.SetNextWindowSizeConstraints(new Vector2(150, 65), new Vector2(float.MaxValue, float.MaxValue));
            if (!ImGui.Begin("AetherGate", ref Config.UseFloatingWindow, ImGuiWindowFlags.NoScrollWithMouse)) {
                ImGui.End();
                return;
            }
            
            if (ImGui.BeginChild("##floatyButtons")) {
                if (ImGui.BeginPopupContextWindow("##addButton", ImGuiPopupFlags.MouseButtonRight | ImGuiPopupFlags.NoOpenOverItems)) {
                    ImGui.TextUnformatted("Add New Button");
                    ImGui.TextUnformatted("Name:");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(200);
                    ImGui.InputText("##hideBtnAddText", ref m_ButtonTextBuffer, 256);
                    ImGui.TextUnformatted("Aetheryte:");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(180);
                    ImGui.InputText("##hideBtnAddAetheryte", ref m_ButtonAetheryteBuffer, 256);
                    ImGui.SameLine();
                    UpdateAetheryteList();
                    if (ImGui.BeginCombo("##hideAddSelectAetheryte", "", ImGuiComboFlags.NoPreview)) {
                        for (var i = 0; i < m_AetheryteList.Length; i++)
                            if (ImGui.Selectable(m_AetheryteList[i]))
                                m_ButtonAetheryteBuffer = m_AetheryteList[i];
                        ImGui.EndCombo();
                    }

                    ImGui.Checkbox("Use Tickets", ref m_ButtonUseTickets);
                    if (ImGui.Button("Add")) {
                        if (!string.IsNullOrEmpty(m_ButtonTextBuffer) && !string.IsNullOrEmpty(m_ButtonAetheryteBuffer)) {
                            Config.TeleportButtons.Add(new TeleportButton(m_ButtonTextBuffer, m_ButtonAetheryteBuffer, m_ButtonUseTickets));
                            Config.Save();
                        }

                        ImGui.CloseCurrentPopup();
                        m_ButtonTextBuffer = string.Empty;
                        m_ButtonAetheryteBuffer = string.Empty;
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("Cancel")) {
                        ImGui.CloseCurrentPopup();
                        m_ButtonTextBuffer = string.Empty;
                        m_ButtonAetheryteBuffer = string.Empty;
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
                            for (var o = 0; o < m_AetheryteList.Length; o++)
                                if (ImGui.Selectable(m_AetheryteList[o])) {
                                    button.Aetheryte = m_AetheryteList[o];
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

                    ImGui.OpenPopupOnItemClick($"##editButton{i}", ImGuiPopupFlags.MouseButtonRight);
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
            if (DateTime.UtcNow.Subtract(m_LastAetheryteListUpdate).TotalMilliseconds < 1000)
                return;
            m_AetheryteList = Plugin.Manager.AetheryteList.Select(a => a.Name).ToArray();
            m_LastAetheryteListUpdate = DateTime.UtcNow;
        }
    }
}