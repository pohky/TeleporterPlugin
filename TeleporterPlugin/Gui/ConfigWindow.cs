using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using TeleporterPlugin.Managers;
using TeleporterPlugin.Plugin;

namespace TeleporterPlugin.Gui {
    public static class ConfigWindow {
        private static bool m_Visible;
        public static bool Enabled {
            get => m_Visible;
            set {
                if (value) AetheryteManager.UpdateAvailableAetherytes();
                m_AetheryteFilter = string.Empty;
                m_Visible = value;
            }
        }

        private static TeleportAlias m_DummyAlias = new();
        private static string m_AetheryteFilter = string.Empty;

        public static void Draw() {
            if(!m_Visible) return;
            try {
                if (!ImGui.Begin("Teleporter Config", ref m_Visible)) return;

                if(ImGui.BeginTabBar("##tpConfigTabs")) {
                    if (ImGui.BeginTabItem("General")) {
                        DrawGeneralTab();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Alias")) {
                        DrawAliasTab();
                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }
            } finally {
                ImGui.End();
            }
        }

        private static void DrawGeneralTab() {
            if (ImGui.Checkbox("Show Chat Messages", ref TeleporterPluginMain.Config.ChatMessage))
                TeleporterPluginMain.Config.Save();
            ImGui.SameLine();
            if (ImGui.Checkbox("Show Error Messages", ref TeleporterPluginMain.Config.ChatError)) 
                TeleporterPluginMain.Config.Save();
            if (ImGui.Checkbox("Use English Aetheryte Names", ref TeleporterPluginMain.Config.UseEnglish)) {
                AetheryteManager.Load();
            }
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Allow Partial Match:");
            ImGui.SameLine();
            if (ImGui.Checkbox("Aetheryte", ref TeleporterPluginMain.Config.AllowPartialName))
                TeleporterPluginMain.Config.Save();
            ImGui.SameLine();
            if (ImGui.Checkbox("Alias", ref TeleporterPluginMain.Config.AllowPartialAlias))
                TeleporterPluginMain.Config.Save();

            if (ImGui.Checkbox("*Hide Ticket Popup", ref TeleporterPluginMain.Config.SkipTicketPopup))
                TeleporterPluginMain.Config.Save();

            if (ImGui.Checkbox("*Only use Tickets if Cost is above", ref TeleporterPluginMain.Config.UseGilThreshold))
                TeleporterPluginMain.Config.Save();
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
            if (ImGui.InputInt("Gil", ref TeleporterPluginMain.Config.GilThreshold))
                TeleporterPluginMain.Config.Save();

            ImGui.TextDisabled("*This also applies when using the Teleport Window and Map (not the Friendlist)");
            ImGui.Separator();
            if(ImGui.Checkbox("Grand Company Ticket Teleport", ref TeleporterPluginMain.Config.EnableGrandCompany))
                TeleporterPluginMain.Config.Save();
            if (TeleporterPluginMain.Config.EnableGrandCompany) {
                ImGui.SameLine();
                ImGui.TextUnformatted(" /tp");
                ImGui.SetNextItemWidth(80);
                ImGui.SameLine();
                ImGui.InputText("##GcAlias", ref TeleporterPluginMain.Config.GrandCompanyAlias, 512);
            }

            if (ImGui.Checkbox("Eternity Ring Teleport", ref TeleporterPluginMain.Config.EnableEternityRing))
                TeleporterPluginMain.Config.Save();
            if (TeleporterPluginMain.Config.EnableEternityRing) {
                ImGui.SameLine();
                ImGui.TextUnformatted(" /tp");
                ImGui.SetNextItemWidth(80);
                ImGui.SameLine();
                ImGui.InputText("##RingAlias", ref TeleporterPluginMain.Config.EternityRingAlias, 512);
            }
        }
        
        private static void DrawAliasTab() {
            if (!ImGui.BeginTable("##tpAliasTable", 3, ImGuiTableFlags.ScrollY | ImGuiTableFlags.PadOuterX)) return;
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn("Alias", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Aetheryte", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("##aliasBtns", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableHeadersRow();

            var list = TeleporterPluginMain.Config.AliasList.ToList();
            for (var i = -1; i < list.Count; i++) {
                var alias = i < 0 ? m_DummyAlias : list[i];
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                ImGui.SetCursorPosX(0);
                if (ImGui.InputText($"##alias{i}Input", ref alias.Alias, 512, ImGuiInputTextFlags.EnterReturnsTrue)) {
                    if (i == -1) {
                        TeleporterPluginMain.Config.AliasList.Insert(0, alias);
                        m_DummyAlias = new TeleportAlias();
                    }
                    TeleporterPluginMain.Config.Save();
                }
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(alias.Aetheryte);
                ImGui.TableNextColumn();
                if (ImGui.BeginCombo($"##alias{i}AetheryteCombo", string.Empty, ImGuiComboFlags.NoPreview)) {
                    if (AetheryteManager.AvailableAetherytes.Count == 0) {
                        ImGui.TextUnformatted("No Aetherytes Available");
                        if (ImGui.Selectable("Click here to Update"))
                            AetheryteManager.UpdateAvailableAetherytes();
                    } else {
                        ImGui.InputText("Search##aetheryteFilter", ref m_AetheryteFilter, 512);
                        foreach (var info in AetheryteManager.AvailableAetherytes) {
                            var name = AetheryteManager.GetAetheryteName(info);
                            if (!string.IsNullOrEmpty(m_AetheryteFilter) && !name.Contains(m_AetheryteFilter, StringComparison.OrdinalIgnoreCase))
                                continue;

                            var selected = alias.Aetheryte.Equals(name, StringComparison.OrdinalIgnoreCase);
                            if (ImGui.Selectable(name, selected)) {
                                alias.Update(info);
                                if (i == -1) {
                                    TeleporterPluginMain.Config.AliasList.Insert(0, alias);
                                    m_DummyAlias = new TeleportAlias();
                                }

                                TeleporterPluginMain.Config.Save();
                            }

                            if (selected) ImGui.SetItemDefaultFocus();
                        }
                    }

                    ImGui.EndCombo();
                }

                ImGui.SameLine();
                if (i != -1 && ImGui.Button($" X ##alias{i}delete")) {
                    TeleporterPluginMain.Config.AliasList.Remove(alias);
                    TeleporterPluginMain.Config.Save();
                }
            }
            ImGui.EndTable();
        }
    }
}