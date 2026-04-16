using System.Text;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using Microsoft.Extensions.ObjectPool;
using YokaiCheck.Windows;

namespace YokaiCheck.Services;

[RegisterSingleton, AutoConstruct]
public partial class DtrService : IDisposable
{
    private readonly TextService _textService;
    private readonly ExcelService _excelService;
    private readonly WindowManager _windowManager;
    private readonly IFramework _framework;
    private readonly IDtrBar _dtrBar;

    private ObjectPool<StringBuilder> _stringBuilderPool;
    private IDtrBarEntry _dtrEntry;
    private uint _lastMinionId;
    private bool _lastWeaponUnlockStatus;
    private int _lastMedalCount;

    private void Initialize()
    {
        _stringBuilderPool = new DefaultObjectPool<StringBuilder>(new StringBuilderPooledObjectPolicy()); ;

        _dtrEntry = _dtrBar.Get("Yo-kai Check");
        _dtrEntry.OnClick = (btn) => _windowManager.CreateOrToggle<MainWindow>();
        _dtrEntry.Shown = false;

        _framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        _framework.Update -= OnFrameworkUpdate;
        _dtrEntry.Remove();
        GC.SuppressFinalize(this);
    }

    private unsafe void OnFrameworkUpdate(IFramework framework)
    {
        var minionId = Plugin.GetCurrentMinionId();
        var isWeaponUnlocked = IsWeaponUnlocked(minionId);

        void Reset()
        {
            _dtrEntry.Shown = false;
            _lastMinionId = 0;
            _lastMedalCount = 0;
            _lastWeaponUnlockStatus = false;
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

        var tooltipBuilder = _stringBuilderPool.Get();
        try
        {
            tooltipBuilder.AppendLine(_textService.Translate("Plugin.DisplayName"));
            tooltipBuilder.AppendLine(_textService.GetItemName(weaponInfo.Value.Medal).ExtractText().StripSoftHyphen());

            if (_excelService.TryFindRow<YKW>(row => row.Item.RowId == weaponInfo.Value.Medal, out var row))
            {
                foreach (var location in row.Location)
                {
                    if (location.RowId != 0 && location.IsValid)
                        tooltipBuilder.AppendLine("- " + _textService.GetPlaceName(location.Value!.PlaceName.RowId));
                }
            }

            _dtrEntry.Text = $"{count} / 10";
            _dtrEntry.Tooltip = tooltipBuilder.ToString().TrimEnd();
            _dtrEntry.Shown = true;
        }
        finally
        {
            _stringBuilderPool.Return(tooltipBuilder);
        }
    }

    private bool DidMinionChange(uint minionId)
    {
        if (minionId == _lastMinionId)
            return false;

        _lastMinionId = minionId;
        return true;
    }

    private bool DidMedalCountChange(int count)
    {
        if (count == _lastMedalCount)
            return false;

        _lastMedalCount = count;
        return true;
    }

    private bool DidWeaponUnlockStatusChange(bool isWeaponUnlocked)
    {
        if (_lastWeaponUnlockStatus == isWeaponUnlocked)
            return false;

        _lastWeaponUnlockStatus = isWeaponUnlocked;
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
