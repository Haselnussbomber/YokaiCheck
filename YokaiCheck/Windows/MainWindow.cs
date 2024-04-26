using System.Numerics;
using Dalamud.Game.Text;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Sheets;
using HaselCommon.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets2;

namespace YokaiCheck.Windows;

public unsafe class MainWindow : Window
{
    public MainWindow() : base(t("Plugin.DisplayName"))
    {
        Namespace = "YokaiCheckMain";

        Size = new Vector2(610, 810);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(570, 200),
            MaximumSize = new Vector2(4069),
        };
    }

    public override void OnClose()
    {
        Service.WindowManager.CloseWindow<MainWindow>();
    }

    public override unsafe void Draw()
    {
        var style = ImGui.GetStyle();
        var itemInnerSpacing = style.ItemInnerSpacing;
        var itemSpacing = style.ItemSpacing;
        var inventoryManager = InventoryManager.Instance();
        var uiState = UIState.Instance();
        var achievementsLoded = uiState->Achievement.IsLoaded();

        if (!achievementsLoded)
            ImGui.TextUnformatted(t("MainWindow.AchievementsNotLoaded"));

        if (ImGui.Button(t("MainWindow.OpenAchievementsButton.Label")))
            GetAgent<AgentInterface>(AgentId.Achievement)->Show();

        ImGui.SameLine();

        if (ImGui.Button(t("MainWindow.OpenYokaiMedalliumButton.Label")))
            GetAgent<AgentInterface>(AgentId.YkwNote)->Show();

        ImGui.SameLine();

        ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - ImGuiUtils.GetIconSize(FontAwesomeIcon.InfoCircle).X);
        ImGuiUtils.Icon(FontAwesomeIcon.InfoCircle, Colors.Grey3);
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(300);
            ImGui.TextUnformatted(t("MainWindow.InfoCircle.Tooltip"));
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }

        var hasAllWeapons = achievementsLoded && uiState->Achievement.IsComplete(Data.WEAPON_ACHIEVEMENT_ALL_17);

        var textHeight = ImGui.GetTextLineHeight();
        var rowHeight = textHeight * 2;

        DrawMinionWeaponTable(itemInnerSpacing, inventoryManager, uiState, achievementsLoded, hasAllWeapons, textHeight, rowHeight);
        DrawPortraitTable(inventoryManager, textHeight, rowHeight);
    }

    private void DrawMinionWeaponTable(Vector2 itemInnerSpacing, InventoryManager* inventoryManager, UIState* uiState, bool achievementsLoded, bool hasAllWeapons, float textHeight, float rowHeight)
    {
        using var table = ImRaii.Table("YKWTable", hasAllWeapons ? 2 : 3);
        if (!table) return;

        ImGui.TableSetupColumn(t("MainWindow.YKWTable.ColumnHeader.Minion"), ImGuiTableColumnFlags.WidthFixed, 180);
        ImGui.TableSetupColumn(t("MainWindow.YKWTable.ColumnHeader.Weapon"), ImGuiTableColumnFlags.WidthFixed, 280);
        if (!hasAllWeapons)
            ImGui.TableSetupColumn(t("MainWindow.YKWTable.ColumnHeader.LegendaryMedals"), ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableHeadersRow();

        foreach (var (minionInfo, weaponInfo) in Data.DataTable)
        {
            ImGui.TableNextRow();

            var medal = GetRow<ExtendedItem>(weaponInfo.Medal)!;
            var companion = GetRow<Companion>(minionInfo.Minion)!;
            var weaponComplete = false;

            // Minion
            ImGui.TableNextColumn();
            var rowPosY = ImGui.GetCursorPosY();
            {
                var minionUnlocked = uiState->IsCompanionUnlocked(minionInfo.Minion);
                DrawCompletionCheckmark(rowPosY, rowHeight, minionUnlocked);

                ImGui.SameLine();
                ImGui.SetCursorPosY(rowPosY);
                DrawItem(medal, rowHeight);

                ImGui.SameLine();
                ImGui.SetCursorPosY(rowPosY + rowHeight / 2f - textHeight / 2f);
                ImGui.TextUnformatted(GetCompanionName(companion.RowId));
                if (minionUnlocked)
                {
                    if (ImGui.IsItemHovered())
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    if (ImGui.IsItemClicked())
                        ActionManager.Instance()->UseAction(ActionType.Companion, minionInfo.Minion);
                }
            }

            // Weapon
            ImGui.TableNextColumn();
            {
                var weapon = GetRow<ExtendedItem>(weaponInfo.Weapon)!;
                var subweapon = GetRow<ExtendedItem>(weaponInfo.Subweapon);
                var hasSubweapon = weaponInfo.Subweapon != 0 && subweapon != null;

                if (achievementsLoded)
                {
                    weaponComplete = uiState->Achievement.IsComplete(weaponInfo.Achievement);
                    DrawCompletionCheckmark(rowPosY, rowHeight, weaponComplete);
                    ImGui.SameLine();
                }

                ImGui.SetCursorPosY(rowPosY);
                DrawItem(weapon, rowHeight);

                if (hasSubweapon)
                {
                    ImGui.SameLine(0, itemInnerSpacing.X);
                    ImGui.SetCursorPosY(rowPosY);
                    DrawItem(subweapon!, rowHeight);
                }

                ImGui.SameLine();

                var weaponName = GetItemName(weapon.RowId);
                var weaponNameSize = ImGui.CalcTextSize(weaponName);

                var textOffset = hasSubweapon
                    ? textHeight
                    : textHeight / 2f;

                var textPosX = ImGui.GetCursorPosX();
                ImGui.SetCursorPos(new(textPosX, rowPosY + rowHeight / 2f - textOffset));
                ImGui.TextUnformatted(weaponName);

                if (hasSubweapon)
                {
                    ImGui.SetCursorPos(new(textPosX, rowPosY + rowHeight / 2f - textOffset));
                    ImGuiUtils.PushCursorY(rowHeight / 2f);
                    ImGui.TextUnformatted(GetItemName(subweapon!.RowId));
                }
            }

            // Legendary Medals
            if (!hasAllWeapons)
            {
                ImGui.TableNextColumn();
                if (achievementsLoded && !weaponComplete)
                {
                    ImGui.SetCursorPosY(rowPosY + rowHeight / 2f - textHeight / 2f);
                    var count = inventoryManager->GetInventoryItemCount(medal.RowId);
                    using var color = ImRaii.PushColor(ImGuiCol.Text, (uint)Colors.Green, count == 10);
                    ImGui.TextUnformatted(t("MainWindow.IncompleteWeaponMedallionCounter", count));
                }
            }
        }
    }

    private void DrawPortraitTable(InventoryManager* inventoryManager, float textHeight, float rowHeight)
    {
        var portraitItem = GetRow<ExtendedItem>(Data.PORTRAIT_ITEM_CATALOG_ID);
        if (portraitItem == null)
            return;

        ImGuiUtils.DrawPaddedSeparator();

        var portraitUnlocked = portraitItem.IsUnlocked;

        using var table = ImRaii.Table("YKWPortraitTable", !portraitUnlocked ? 2 : 1);
        if (!table) return;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        var rowPosY = ImGui.GetCursorPosY();
        DrawCompletionCheckmark(rowPosY, rowHeight, portraitUnlocked);
        ImGui.SameLine();
        ImGui.SetCursorPosY(rowPosY);
        DrawItem(portraitItem, rowHeight);
        ImGui.SameLine();
        ImGui.SetCursorPosY(rowPosY + rowHeight / 2f - textHeight / 2f);
        ImGui.TextUnformatted(GetItemName(portraitItem.RowId));

        if (!portraitUnlocked)
        {
            ImGui.TableNextColumn();
            ImGui.SetCursorPosY(rowPosY + rowHeight / 2f - textHeight / 2f);
            var gilHas = inventoryManager->GetGil();
            var gilNeed = Data.PORTRAIT_NEED_MGP;
            var color = gilHas >= gilNeed ? Colors.Green : Colors.Red;
            ImGuiUtils.TextUnformattedColored(color, $"{gilHas:n0} / {gilNeed:n0} {SeIconChar.Gil.ToIconString()}");
        }
    }

    private void DrawItem(ExtendedItem item, float iconSize = 24, string key = "")
    {
        Service.TextureManager.GetIcon(item.Icon).Draw(iconSize);

        ImGuiContextMenu.Draw($"##{key}_ItemContextMenu{item.RowId}", [
            ImGuiContextMenu.CreateTryOn(item),
            ImGuiContextMenu.CreateItemFinder(item.RowId),
            ImGuiContextMenu.CreateCopyItemName(item.RowId),
            ImGuiContextMenu.CreateItemSearch(item),
            ImGuiContextMenu.CreateOpenOnGarlandTools("item", item.RowId),
        ]);
    }

    private void DrawCompletionCheckmark(float yPos, float rowHeight, bool isComplete)
    {
        var icon = isComplete ? FontAwesomeIcon.Check : FontAwesomeIcon.Times;
        var color = isComplete ? Colors.Green : Colors.Red;
        var tooltipText = isComplete ? t("MainWindow.CompletionCheckmark.Tooltip.Collected") : t("MainWindow.CompletionCheckmark.Tooltip.NotCollected");
        var iconHeight = ImGuiUtils.GetIconSize(icon).Y;
        ImGui.SetCursorPosY(yPos + rowHeight / 2f - iconHeight / 2f);
        ImGuiUtils.Icon(icon, color);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(tooltipText);
    }
}
