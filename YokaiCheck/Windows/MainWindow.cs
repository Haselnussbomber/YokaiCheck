using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Graphics;
using HaselCommon.Gui;
using Lumina.Excel.Sheets;

namespace YokaiCheck.Windows;

[RegisterSingleton, AutoConstruct]
public unsafe partial class MainWindow : SimpleWindow
{
    private readonly TextService _textService;
    private readonly ExcelService _excelService;
    private readonly ItemService _itemService;
    private readonly IClientState _clientState;
    private readonly ITextureProvider _textureProvider;
    private readonly MapService _mapService;
    private readonly IServiceProvider _serviceProvider;

    [AutoPostConstruct]
    private void Initialize()
    {
        Size = new Vector2(610, 750);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new()
        {
            MinimumSize = new Vector2(570, 200),
            MaximumSize = new Vector2(4069),
        };
    }

    public override void Draw()
    {
        var style = ImGui.GetStyle();
        var itemInnerSpacing = style.ItemInnerSpacing;
        var itemSpacing = style.ItemSpacing;
        var inventoryManager = InventoryManager.Instance();
        var uiState = UIState.Instance();
        var achievementsLoded = uiState->Achievement.IsLoaded();

        if (!achievementsLoded)
            ImGui.TextUnformatted(_textService.Translate("MainWindow.AchievementsNotLoaded"));

        if (ImGui.Button(_textService.Translate("MainWindow.OpenAchievementsButton.Label")))
            AgentAchievement.Instance()->Show();

        ImGui.SameLine();

        if (ImGui.Button(_textService.Translate("MainWindow.OpenYokaiMedalliumButton.Label")))
            AgentModule.Instance()->GetAgentByInternalId(AgentId.YkwNote)->Show();

        ImGui.SameLine();

        ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - ImGuiUtils.GetIconSize(FontAwesomeIcon.InfoCircle).X);
        ImGuiUtils.Icon(FontAwesomeIcon.InfoCircle, Color.Text600.ToUInt());
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(300);
            ImGui.TextUnformatted(_textService.Translate("MainWindow.InfoCircle.Tooltip"));
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }

        var hasAllWeapons = achievementsLoded && uiState->Achievement.IsComplete(Data.WEAPON_ACHIEVEMENT_ALL_17);

        var textHeight = ImGui.GetTextLineHeight();
        var rowHeight = textHeight * 2;

        DrawMinionWeaponTable(itemInnerSpacing, inventoryManager, uiState, achievementsLoded, hasAllWeapons, textHeight, rowHeight);
        DrawPortraitTable(inventoryManager, textHeight, rowHeight);
        DrawSpecialShopTable(textHeight, rowHeight);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var cursorPos = ImCursor.Position;
        var contentAvail = ImStyle.ContentRegionAvail;

        ImGuiUtils.DrawLink("GitHub", _textService.Translate("GitHubLink.Tooltip"), "https://github.com/Haselnussbomber/YokaiCheck");
        ImGui.SameLine();
        ImGui.Text("•");
        ImGui.SameLine();
        ImGuiUtils.DrawLink("Sponsor", _textService.Translate("SponsorLink.Tooltip"), "https://github.com/sponsors/Haselnussbomber");
        ImGui.SameLine();
        ImGui.Text("•");
        ImGui.SameLine();
        ImGui.Text(_textService.Translate("LicensesLink.Label"));
        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && _serviceProvider.TryGetService<LicensesWindow>(out var licensesWindow))
            {
                Task.Run(licensesWindow.Toggle);
            }
        }

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version != null)
        {
            var versionString = "v" + version.ToString(3);
            ImGui.SameLine();
            ImCursor.X = cursorPos.X + contentAvail.X - ImGui.CalcTextSize(versionString).X;
            ImGui.TextDisabled(versionString);
        }
    }

    private void DrawMinionWeaponTable(Vector2 itemInnerSpacing, InventoryManager* inventoryManager, UIState* uiState, bool achievementsLoded, bool hasAllWeapons, float textHeight, float rowHeight)
    {
        using var table = ImRaii.Table("YKWTable", hasAllWeapons ? 2 : 3);
        if (!table) return;

        ImGui.TableSetupColumn(_textService.Translate("MainWindow.YKWTable.ColumnHeader.Minion"), ImGuiTableColumnFlags.WidthFixed, 180);
        ImGui.TableSetupColumn(_textService.Translate("MainWindow.YKWTable.ColumnHeader.Weapon"), ImGuiTableColumnFlags.WidthFixed, 280);
        if (!hasAllWeapons)
            ImGui.TableSetupColumn(_textService.Translate("MainWindow.YKWTable.ColumnHeader.LegendaryMedals"), ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableHeadersRow();

        var currentMinionId = Plugin.GetCurrentMinionId();

        foreach (var (minionInfo, weaponInfo) in Data.DataTable)
        {
            ImGui.TableNextRow();

            if (!_excelService.TryGetRow<Item>(weaponInfo.Medal, out var medal))
                continue;

            if (!_excelService.TryGetRow<Companion>(minionInfo.Minion, out var companion))
                continue;

            var weaponComplete = false;
            var isMinionActive = currentMinionId == companion.RowId;

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
                using (ImRaii.PushColor(ImGuiCol.Text, Color.Green, isMinionActive))
                    ImGui.TextUnformatted(_textService.GetCompanionName(companion.RowId));

                if (minionUnlocked)
                {
                    if (ImGui.IsItemHovered())
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    if (ImGui.IsItemClicked())
                        ActionManager.Instance()->UseAction(ActionType.Companion, minionInfo.Minion);
                }

                if (isMinionActive && ImGui.IsItemHovered())
                    ImGui.SetTooltip(_textService.GetAddonText(12196));
            }

            // Weapon
            ImGui.TableNextColumn();
            if (_excelService.TryGetRow<Item>(weaponInfo.Weapon, out var weapon))
            {
                var hasSubweapon = _excelService.TryGetRow<Item>(weaponInfo.Subweapon, out var subweapon) && weaponInfo.Subweapon != 0;

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

                var weaponName = _textService.GetItemName(weapon.RowId).ToString();

                var textOffset = hasSubweapon
                    ? textHeight
                    : textHeight / 2f;

                var textPosX = ImGui.GetCursorPosX();
                ImGui.SetCursorPos(new(textPosX, rowPosY + rowHeight / 2f - textOffset));
                ImGui.TextUnformatted(weaponName);

                if (hasSubweapon)
                {
                    ImGui.SetCursorPos(new(textPosX, rowPosY + rowHeight / 2f - textOffset));
                    ImCursor.Y += rowHeight / 2f;
                    ImGui.TextUnformatted(_textService.GetItemName(subweapon!.RowId).ToString());
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

                    using (ImRaii.PushColor(ImGuiCol.Text, Color.Green, count == 10))
                        ImGui.TextUnformatted(_textService.Translate("MainWindow.IncompleteWeaponMedallionCounter", count));

                    if (ImGui.IsItemHovered() && _excelService.TryFindRow<YKW>(row => row.Item.RowId == weaponInfo.Medal, out var row))
                    {
                        ImGui.BeginTooltip();
                        var currentTerritoryId = _clientState.TerritoryType;

                        foreach (var location in row.Location)
                        {
                            if (location.RowId != 0 && location.IsValid)
                            {
                                using (ImRaii.PushColor(ImGuiCol.Text, Color.Green, location.RowId == currentTerritoryId))
                                    ImGui.TextUnformatted("- " + _textService.GetPlaceName(location.Value!.PlaceName.RowId));
                            }
                        }

                        ImGui.EndTooltip();
                    }
                }
            }
        }
    }

    private void DrawPortraitTable(InventoryManager* inventoryManager, float textHeight, float rowHeight)
    {
        if (!_excelService.TryGetRow<Item>(Data.PORTRAIT_ITEM_CATALOG_ID, out var portraitItem))
            return;

        ImGuiUtils.DrawPaddedSeparator();

        var portraitUnlocked = _itemService.IsUnlocked(portraitItem.RowId);

        using var table = ImRaii.Table("YKWPortraitTable", 2);
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
        ImGui.TextUnformatted(_textService.GetItemName(portraitItem.RowId).ToString());

        ImGui.TableNextColumn();
        ImGui.SetCursorPosY(rowPosY + rowHeight / 2f - textHeight / 2f);
        var mgpNeed = Data.PORTRAIT_NEED_MGP;

        if (!portraitUnlocked)
        {
            var mgpHas = inventoryManager->GetGoldSaucerCoin();
            var color = mgpHas >= mgpNeed ? Color.Green : Color.Red;
            ImGui.TextColored(color, $"{mgpHas:n0} / {mgpNeed:n0} MGP");
        }
        else
        {
            ImGui.TextColored(Color.Text600, $"{mgpNeed:n0} MGP");
        }
    }

    private void DrawSpecialShopTable(float textHeight, float rowHeight)
    {
        if (!_excelService.TryGetRow<SpecialShop>(Data.SPECIAL_SHOP_ID, out var specialShop))
            return;

        ImGuiUtils.DrawPaddedSeparator();

        using var table = ImRaii.Table("YKWSpecialShopTable", 2);
        if (!table) return;

        foreach (var item in specialShop.Item)
        {
            if (item.ReceiveItems[0].Item.RowId == 0 || !item.ReceiveItems[0].Item.IsValid)
                continue;

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            var rowPosY = ImGui.GetCursorPosY();
            ImCursor.X -= rowHeight / 4.2f;
            _textureProvider.DrawIcon(60935, rowHeight);
            if (ImGui.IsItemHovered() && _excelService.TryGetRow<Level>(6289315, out var level))
            {
                ImGui.SetTooltip(_textService.Translate("MainWindow.DisplayVendorOnMap.Tooltip", _textService.GetPlaceName(level.Map.Value.PlaceName.RowId), _mapService.GetHumanReadableCoords(level)));
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (ImGui.IsItemClicked())
                    _mapService.OpenMap(level);
            }

            ImGui.SetCursorPosY(rowPosY + rowHeight / 2f - textHeight / 2f);
            ImCursor.X = ImGuiUtils.GetIconSize(FontAwesomeIcon.Check).Y;
            ImGui.SameLine(0, 0);
            ImGui.SetCursorPosY(rowPosY);
            DrawItem(item.ReceiveItems[0].Item.Value, rowHeight);
            ImGui.SameLine();
            ImGui.SetCursorPosY(rowPosY + rowHeight / 2f - textHeight / 2f);
            ImGui.TextUnformatted(_textService.GetItemName(item.ReceiveItems[0].Item.Value.RowId).ToString());

            ImGui.TableNextColumn();
            ImGui.SetCursorPosY(rowPosY + rowHeight / 2f - textHeight / 2f);
            var mgpHas = InventoryManager.Instance()->GetGoldSaucerCoin();
            var mgpNeed = item.ItemCosts[0].CurrencyCost;
            var color = mgpHas >= mgpNeed ? Color.Green : Color.Red;
            ImGui.TextColored(color, $"{mgpHas:n0} / {mgpNeed:n0} MGP");
        }
    }

    private void DrawItem(Item item, float iconSize = 24, string key = "")
    {
        _textureProvider.DrawIcon((uint)item.Icon, iconSize);

        ImGuiContextMenu.Draw($"##{key}_ItemContextMenu{item.RowId}", builder => builder
            .AddTryOn(item.RowId)
            .AddItemFinder(item.RowId)
            .AddCopyItemName(item.RowId)
            .AddItemSearch(item.RowId)
            .AddOpenOnGarlandTools("item", item.RowId));
    }

    private void DrawCompletionCheckmark(float yPos, float rowHeight, bool isComplete)
    {
        var icon = isComplete ? FontAwesomeIcon.Check : FontAwesomeIcon.Times;
        var color = isComplete ? Color.Green : Color.Red;
        var tooltipText = isComplete
            ? _textService.Translate("MainWindow.CompletionCheckmark.Tooltip.Collected")
            : _textService.Translate("MainWindow.CompletionCheckmark.Tooltip.NotCollected");
        var iconHeight = ImGuiUtils.GetIconSize(icon).Y;
        ImGui.SetCursorPosY(yPos + rowHeight / 2f - iconHeight / 2f);
        ImGuiUtils.Icon(icon, color.ToUInt());
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(tooltipText);
    }
}
