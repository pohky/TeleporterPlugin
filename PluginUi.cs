using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using TeleporterPlugin.Objects;

namespace TeleporterPlugin {
    public class PluginUi : IDisposable {
        private readonly TeleporterPlugin _plugin;
        public bool DebugVisible;
        public bool FloatingButtonsVisible = true;
        private bool _settingsVisible;
        public bool SettingsVisible {
            get => _settingsVisible;
            set {
                if (value) LoadSettings();
                _settingsVisible = value;
            }
        }

        public PluginUi(TeleporterPlugin plugin) {
            _plugin = plugin;
            _plugin.Interface.UiBuilder.OnBuildUi += Draw;
            _plugin.Interface.UiBuilder.OnOpenConfigUi += (sender, args) => SettingsVisible = true;
            LoadSettings();
        }

        private void Draw() {
            DrawDebug();
            DrawSettings();
            DrawFloatingWindow();
        }
        
        private void LoadSettings() {
            cfg_inputGilThreshold = _plugin.Config.GilThreshold;
            cfg_useGilThreshold = _plugin.Config.UseGilThreshold;
            cfg_skipTicketPopup = _plugin.Config.SkipTicketPopup;
            cfg_allowPartialMatch = _plugin.Config.AllowPartialMatch;
            cfg_aliasList = _plugin.Config.AliasList;
            cfg_useFloatingWindow = _plugin.Config.UseFloatingWindow;
            cfg_teleportButtons = _plugin.Config.TeleportButtons;
            cfg_showTooltips = _plugin.Config.ShowTooltips;
            cfg_selectedLanguage = (int)_plugin.Config.TeleporterLanguage;
        }

        private void SaveSettings() {
            _plugin.Config.GilThreshold = cfg_inputGilThreshold;
            _plugin.Config.AllowPartialMatch = cfg_allowPartialMatch;
            _plugin.Config.SkipTicketPopup = cfg_skipTicketPopup;
            _plugin.Config.UseGilThreshold = cfg_useGilThreshold;
            _plugin.Config.AliasList = cfg_aliasList;
            _plugin.Config.UseFloatingWindow = cfg_useFloatingWindow;
            _plugin.Config.TeleportButtons = cfg_teleportButtons;
            _plugin.Config.ShowTooltips = cfg_showTooltips;
            _plugin.Config.TeleporterLanguage = (TeleporterLanguage)cfg_selectedLanguage;
            _plugin.Config.Save();
        }

        #region Floating Window

        private const uint ButtonTextBufferLength = 256;
        private static string _buttonTextBuffer = string.Empty;
        private string _buttonAetheryteBuffer = string.Empty;
        private bool _buttonUseTickets;

        private void DrawFloatingWindow() {
            if (!cfg_useFloatingWindow || !FloatingButtonsVisible) return;
            ImGui.SetNextWindowSizeConstraints(new Vector2(150, 65), new Vector2(float.MaxValue, float.MaxValue));
            if (!ImGui.Begin("Quick Teleport", ref FloatingButtonsVisible, ImGuiWindowFlags.NoScrollWithMouse)) return;

            if (ImGui.BeginChild("##floatyButtons")) {
                if (ImGui.BeginPopupContextWindow("##addButton", 1, false)) {
                    ImGui.TextUnformatted("Add New Button");
                    ImGui.TextUnformatted("Name:");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(200);
                    ImGui.InputText("##hideBtnAddText", ref _buttonTextBuffer, ButtonTextBufferLength);
                    ImGui.TextUnformatted("Aetheryte:");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(180);
                    ImGui.InputText("##hideBtnAddAetheryte", ref _buttonAetheryteBuffer, ButtonTextBufferLength);
                    ImGui.SameLine();
                    UpdateAetheryteList();
                    if (ImGui.BeginCombo("##hideAddSelectAetheryte", "", ImGuiComboFlags.NoPreview)) {
                        for (var i = 0; i < cfg_aetheryteList.Length; i++) {
                            if (ImGui.Selectable(cfg_aetheryteList[i]))
                                _buttonAetheryteBuffer = cfg_aetheryteList[i];
                        }
                        ImGui.EndCombo();
                    }
                    ImGui.Checkbox("Use Tickets", ref _buttonUseTickets);
                    if (ImGui.Button("Add")) {
                        if (!string.IsNullOrEmpty(_buttonTextBuffer) && !string.IsNullOrEmpty(_buttonAetheryteBuffer)) {
                            cfg_teleportButtons.Add(new TeleportButton(_buttonTextBuffer, _buttonAetheryteBuffer, _buttonUseTickets));
                            SaveSettings();
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
                for (var i = 0; i < cfg_teleportButtons.Count; i++) {
                    var button = cfg_teleportButtons[i];
                    var buttonSizeX = ImGui.CalcTextSize(button.Text).X + 2 * style.FramePadding.X;
                    var nextButtonX = lastButtonX + style.ItemSpacing.X + buttonSizeX;
                    if (nextButtonX < windowVisibleX)
                        ImGui.SameLine(0, i == 0 ? 0 : style.ItemSpacing.X);
                    if (button.Draw() && !string.IsNullOrEmpty(button.Aetheryte)) {
                        var cmd = button.UseTickets ? "/tpt" : "/tp";
                        _plugin.CommandHandler(cmd, button.Aetheryte);
                    }
                    lastButtonX = ImGui.GetItemRectMax().X;
                    
                    if (ImGui.BeginPopup($"##editButton{i}")) {
                        ImGui.TextUnformatted("Edit Button");
                        ImGui.TextUnformatted("Name:");
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(200);
                        if(ImGui.InputText($"##hideBtnEditText{i}", ref button.Text, ButtonTextBufferLength))
                            SaveSettings();
                        ImGui.TextUnformatted("Aetheryte:");
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(180);
                        if(ImGui.InputText($"##hideBtnEditAetheryte{i}", ref button.Aetheryte, ButtonTextBufferLength))
                            SaveSettings();
                        ImGui.SameLine();
                        UpdateAetheryteList();
                        if (ImGui.BeginCombo($"##hideEditSelectAetheryte{i}", "", ImGuiComboFlags.NoPreview)) {
                            for (var o = 0; o < cfg_aetheryteList.Length; o++) {
                                if (ImGui.Selectable(cfg_aetheryteList[o])) {
                                    button.Aetheryte = cfg_aetheryteList[o];
                                    SaveSettings();
                                }
                            }
                            ImGui.EndCombo();
                        }
                        if(ImGui.Checkbox("Use Tickets", ref button.UseTickets))
                            SaveSettings();
                        if (ImGui.Button("Close"))
                            ImGui.CloseCurrentPopup();
                        ImGui.SameLine(ImGui.GetWindowContentRegionWidth() - ImGui.CalcTextSize("Delete").X - 1);
                        //if (ImGui.Button("Delete")) {
                        if (ImGui.SmallButton("Delete")) {
                            cfg_teleportButtons.Remove(button);
                            SaveSettings();
                        }
                        ImGui.EndPopup();
                    }
                    ImGui.OpenPopupOnItemClick($"##editButton{i}", 1);
                }

                var buttSizeX = ImGui.CalcTextSize("+").X + 2 * style.FramePadding.X;
                var nextButtX = lastButtonX + style.ItemSpacing.X + buttSizeX;
                if (nextButtX < windowVisibleX)
                    ImGui.SameLine(0, cfg_teleportButtons.Count <= 0 ? 0 : style.ItemSpacing.X);
                if (ImGui.Button("+"))
                    ImGui.OpenPopup("##addButton");
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("Add new Button.\nRightclick any Button to change them.");

                ImGui.EndChild();
            }
            ImGui.End();
        }

        #endregion

        #region Settings Window

        private static readonly Vector4 ColorRed = new Vector4(255, 0, 0, 255);

        private bool cfg_useGilThreshold;
        private int cfg_inputGilThreshold;
        private bool cfg_skipTicketPopup;
        private bool cfg_allowPartialMatch;
        private bool cfg_showTooltips;
        private List<TeleportAlias> cfg_aliasList;
        private string[] cfg_aetheryteList;
        private DateTime cfg_lastAetheryteListUpdate = DateTime.MinValue;
        private readonly string[] cfg_languageList = Enum.GetNames(typeof(TeleporterLanguage));
        private int cfg_selectedLanguage;
        private bool cfg_useFloatingWindow;
        private List<TeleportButton> cfg_teleportButtons = new List<TeleportButton>();

        public void DrawSettings() {
            if(!SettingsVisible) return;
            ImGui.SetNextWindowSize(new Vector2(530, 450), ImGuiCond.Appearing);
            ImGui.SetNextWindowSizeConstraints(new Vector2(300, 300), new Vector2(float.MaxValue, float.MaxValue));
            if (!ImGui.Begin($"{_plugin.Name} Config", ref _settingsVisible, ImGuiWindowFlags.NoScrollWithMouse))
                return;
            
            if (ImGui.BeginChild("##scrollingregionSettings")) {
                if (ImGui.CollapsingHeader("General Settings", ImGuiTreeNodeFlags.DefaultOpen))
                    DrawGeneralSettings();
                
                ImGui.Spacing();
                if (ImGui.CollapsingHeader("Alias Settings", ImGuiTreeNodeFlags.DefaultOpen))
                    DrawAliasSettings();

                ImGui.EndChild();
            }
            ImGui.End();
        }

        private void DrawAliasSettings() {
            var newAliasAdded = false;
            if (ImGui.Button("New Alias")) {
                cfg_aliasList.Insert(0, TeleportAlias.Empty);
                newAliasAdded = true;
            }
            ImGui.SameLine();
            ImGui.Spacing();
            ImGui.SameLine();
            if (ImGui.Button("Delete")) {
                if (cfg_aliasList.Count > 0)
                    cfg_aliasList.RemoveAt(0);
                SaveSettings();
            }
            var deleteAliasHovered = ImGui.IsItemHovered();
            
            ImGui.SameLine();
            if (ImGui.Button("Delete Selected")) {
                cfg_aliasList.RemoveAll(a => a.GuiSelected);
                SaveSettings();
            }

            ImGui.SameLine();
            if (ImGui.Button("Delete All"))
                ImGui.OpenPopupOnItemClick("deleteallpopup", 0);

            if (ImGui.BeginPopup("deleteallpopup")) {
                ImGui.TextColored(ColorRed, "Are you sure you want to delete ALL aliases?");
                if (ImGui.Button("No", new Vector2(80, ImGui.GetTextLineHeightWithSpacing()))) {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine(ImGui.GetWindowWidth() - 90);
                if (ImGui.Button("Yes", new Vector2(80, ImGui.GetTextLineHeightWithSpacing()))) {
                    cfg_aliasList.Clear();
                    SaveSettings();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
            
            ImGui.Separator();
            if (!ImGui.BeginChild("##scrollingregionAlias", Vector2.Zero)) 
                return;
            if (newAliasAdded) ImGui.SetScrollHereY();
            ImGui.Columns(2);
            ImGui.TextUnformatted("Alias"); ImGui.NextColumn();
            ImGui.TextUnformatted("Target Aetheryte"); ImGui.NextColumn();
            ImGui.Separator();

            UpdateAetheryteList();
            for (var i = 0; i < cfg_aliasList.Count; i++) {
                var alias = cfg_aliasList[i];
                if (deleteAliasHovered && i == 0) ImGui.ArrowButton("delete_indicator", ImGuiDir.Right);
                else ImGui.Checkbox($"##hidelabelAliasSelected{i}", ref alias.GuiSelected);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(ImGui.GetColumnWidth() - 45);
                if (deleteAliasHovered && i == 0) ImGui.TextColored(ColorRed, alias.Alias);
                else if(ImGui.InputText($"##hidelabelAliasKey{i}", alias.AliasBuffer, TeleportAlias.BufferSize, ImGuiInputTextFlags.CharsNoBlank))
                    SaveSettings();
                ImGui.NextColumn();
                if (deleteAliasHovered && i == 0) ImGui.TextColored(ColorRed, alias.Aetheryte);
                else {
                    ImGui.SetNextItemWidth(ImGui.GetColumnWidth() - 45);
                    if(ImGui.InputText($"##hidelabelAliasValue{i}", alias.AetheryteBuffer, TeleportAlias.BufferSize))
                        SaveSettings();
                    ImGui.SameLine();
                    if (ImGui.BeginCombo($"##hidelabelAliasSelect{i}", "", ImGuiComboFlags.NoPreview)) {
                        for (var a = 0; a < cfg_aetheryteList.Length; a++) {
                            var selected = alias.GuiSelectedIndex == a;
                            if (ImGui.Selectable(cfg_aetheryteList[a], selected)) {
                                alias.GuiSelectedIndex = a;
                                alias.Aetheryte = cfg_aetheryteList[a];
                            }
                            if(selected) ImGui.SetItemDefaultFocus();
                        }
                        ImGui.EndCombo();
                    }
                }
                ImGui.NextColumn();
            }
            
            ImGui.EndChild();
            ImGui.Columns(1);
        }

        private void DrawGeneralSettings() {
            if(ImGui.Checkbox("Allow partial Name matching", ref cfg_allowPartialMatch)) SaveSettings();
            if (cfg_showTooltips && ImGui.IsItemHovered())
                ImGui.SetTooltip("Matches the first Aetheryte found that contains ...\n" +
                                 "e.g.: 'kug' matches 'Kugane' or 'gridania' matches 'New Gridania'");

            ImGui.SameLine(ImGui.GetColumnWidth() - 80);
            ImGui.TextUnformatted("Tooltips");
            ImGui.AlignTextToFramePadding();
            ImGui.SameLine();
            if(ImGui.Checkbox("##hidelabelTooltips", ref cfg_showTooltips))
                SaveSettings();

            if(ImGui.Checkbox("Skip Ticket Popup", ref cfg_skipTicketPopup)) SaveSettings();
            if (cfg_showTooltips && ImGui.IsItemHovered())
                ImGui.SetTooltip("Removes the 'Use an aetheryte ticket to teleport...' popup when using Teleporter commands");

            if(ImGui.Checkbox("Use Tickets if Gil Price is above:", ref cfg_useGilThreshold)) SaveSettings();
            if (cfg_showTooltips && ImGui.IsItemHovered()) 
                ImGui.SetTooltip("Attempt to use Aetheryte Tickets if the Teleport cost is greater than this value.\n" +
                                 "Applies to '/tp'.\n" +
                                 "e.g.: '/tp Kugane' will attempt to use a ticket if the price is above the set value as if '/tpt Kugane' was used");
            
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);
            if (ImGui.InputInt("##hidelabelInputGil", ref cfg_inputGilThreshold, 1, 100)) {
                if(cfg_inputGilThreshold < 0)
                    cfg_inputGilThreshold = 0;
                SaveSettings();
            }

            ImGui.TextUnformatted("Language:");
            if (cfg_showTooltips && ImGui.IsItemHovered())
                ImGui.SetTooltip("Change the Language used for Aetheryte Names.\n" +
                                 $"(default) Client = Game Language [{_plugin.Interface.ClientState.ClientLanguage}]");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            if (ImGui.Combo("##hidelabelLangSetting", ref cfg_selectedLanguage, cfg_languageList, cfg_languageList.Length))
                SaveSettings();

            if (ImGui.Checkbox("Show Quick Teleport Window", ref cfg_useFloatingWindow)) {
                FloatingButtonsVisible = cfg_useFloatingWindow;
                SaveSettings();
            }

            if (cfg_showTooltips && ImGui.IsItemHovered())
                ImGui.SetTooltip("Show a Window with customizable Buttons to quickly Teleport around.\n" +
                                 "Rightclick on the Window or clicking the + Button will let you add a new Button\n" +
                                 "Rightlick on any Button will let you Edit or Delete the Button");
        }
        
        private void UpdateAetheryteList() {
            if(DateTime.UtcNow.Subtract(cfg_lastAetheryteListUpdate).TotalMilliseconds < 1000)
                return;
            cfg_aetheryteList = _plugin.Manager.AetheryteList.Select(a => a.Name).ToArray();
            cfg_lastAetheryteListUpdate = DateTime.UtcNow;
        }

        #endregion

        #region Debug Window

        private readonly List<string> dbg_locations = new List<string> {"GetList to Update"};
        private readonly List<string> dbg_aetheryteId = new List<string> {"Empty"};
        private readonly List<string> dbg_subIndex = new List<string> {"Empty"};
        private readonly List<string> dbg_zoneId = new List<string> {"Empty"};
        private int dbg_selected;

        public void DrawDebug() {
            if(!DebugVisible) return;
            var windowSize = new Vector2(350, 315);
            ImGui.SetNextWindowSize(windowSize, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(windowSize, new Vector2(float.MaxValue, float.MaxValue));
            if (ImGui.Begin($"{_plugin.Name} Debug", ref DebugVisible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) {
                ImGui.TextUnformatted($"AetheryteList: {_plugin.Manager.AetheryteListAddress.ToInt64():X8}");
                ImGui.TextUnformatted($"TeleportStatus: {_plugin.Manager.TeleportStatusAddress.ToInt64():X8}");
                ImGui.TextUnformatted($"ItemCountStaticArg: {_plugin.Manager.ItemCountStaticArgAddress.ToInt64():X8}");
                ImGui.Separator();
                
                if (ImGui.Button("GetList")) {
                    dbg_locations.Clear();
                    dbg_aetheryteId.Clear();
                    dbg_subIndex.Clear();
                    dbg_zoneId.Clear();
                    foreach (var location in _plugin.Manager.AetheryteList) {
                        dbg_locations.Add(location.Name);
                        dbg_aetheryteId.Add(location.AetheryteId.ToString());
                        dbg_subIndex.Add(location.SubIndex.ToString());
                        dbg_zoneId.Add(location.ZoneId.ToString());
                    }
                }

                ImGui.SameLine();
                if (ImGui.Button("Teleport")) 
                    _plugin.Manager.Teleport(dbg_locations[dbg_selected]);
                ImGui.SameLine();
                if (ImGui.Button("Teleport (Ticket)"))
                    _plugin.Manager.TeleportTicket(dbg_locations[dbg_selected], true);
                ImGui.SameLine();
                if (ImGui.Button("Teleport (Ticket + Popup)"))
                    _plugin.Manager.TeleportTicket(dbg_locations[dbg_selected]);

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
            _plugin.Interface.UiBuilder.OnOpenConfigUi = null;
        }
    }
}