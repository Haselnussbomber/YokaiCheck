using System.Text;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Extensions;
using Lumina.Excel.GeneratedSheets;
using YokaiCheck.Windows;

namespace YokaiCheck.Services;

public class DtrService : IDisposable
{
    private readonly DtrBarEntry DtrEntry;
    private uint LastMinionId;
    private bool LastWeaponUnlockStatus;
    private int LastMedalCount;

    public DtrService()
    {
        Service.Framework.Update += Framework_Update;

        DtrEntry = Service.DtrBar.Get("Yo-kai Check");
        DtrEntry.OnClick = Service.WindowManager.ToggleWindow<MainWindow>;
        DtrEntry.SetVisibility(false);
    }

    public void Dispose()
    {
        Service.Framework.Update -= Framework_Update;
        DtrEntry.Dispose();
    }

    private unsafe void Framework_Update(IFramework framework)
    {
        var minionId = Plugin.GetCurrentMinionId();
        var isWeaponUnlocked = IsWeaponUnlocked(minionId);

        void Reset()
        {
            DtrEntry.SetVisibility(false);
            LastMinionId = 0;
            LastMedalCount = 0;
            LastWeaponUnlockStatus = false;
        }

        if (minionId == 0 || isWeaponUnlocked)
        {
            Reset();
            return;
        }

        var weaponInfo = Data.GetWeaponInfoByMinionId(minionId);
        if (weaponInfo == null)
        {
            Reset();
            return;
        }

        var count = InventoryManager.Instance()->GetInventoryItemCount(weaponInfo.Value.Medal);

        if (!(DidMinionChange(minionId) || DidWeaponUnlockStatusChange(isWeaponUnlocked) || DidMedalCountChange(count)))
            return;

        var tooltipBuilder = new StringBuilder();
        tooltipBuilder.AppendLine(t("Plugin.DisplayName"));
        tooltipBuilder.AppendLine(GetItemName(weaponInfo.Value.Medal));

        var row = FindRow<YKW>(row => row?.Item.Row == weaponInfo.Value.Medal);
        if (row != null)
        {
            foreach (var location in row.Location)
            {
                if (location.Row != 0 && location.Value != null)
                    tooltipBuilder.AppendLine("- " + GetSheetText<PlaceName>(location.Value!.PlaceName.Row, "Name"));
            }
        }

        DtrEntry.SetText($"{count} / 10");
        DtrEntry.Tooltip = tooltipBuilder.ToString().TrimEnd();
        DtrEntry.SetVisibility(true);
    }

    private bool DidMinionChange(uint minionId)
    {
        if (minionId == LastMinionId)
            return false;

        LastMinionId = minionId;
        return true;
    }

    private bool DidMedalCountChange(int count)
    {
        if (count == LastMedalCount)
            return false;

        LastMedalCount = count;
        return true;
    }

    private unsafe bool DidWeaponUnlockStatusChange(bool isWeaponUnlocked)
    {
        if (LastWeaponUnlockStatus == isWeaponUnlocked)
            return false;

        LastWeaponUnlockStatus = isWeaponUnlocked;
        return true;
    }

    private unsafe bool IsWeaponUnlocked(uint minionId)
    {
        if (minionId == 0)
            return false;

        var weaponInfo = Data.GetWeaponInfoByMinionId(minionId);
        if (weaponInfo == null)
            return false;

        ref var achievement = ref UIState.Instance()->Achievement;
        if (!achievement.IsLoaded())
            return false;

        return achievement.IsComplete(weaponInfo.Value.Achievement);
    }
}
