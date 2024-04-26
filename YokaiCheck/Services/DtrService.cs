using System.Collections.Generic;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Inventory;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Game.Text;
using HaselCommon.Extensions;

namespace YokaiCheck.Services;

public class DtrService : IDisposable
{
    private readonly DtrBarEntry DtrEntry;

    public DtrService()
    {
        Service.GameInventory.InventoryChanged += GameInventory_InventoryChanged;
        Service.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
        DtrEntry = Service.DtrBar.Get("Yo-kai Check");
        DtrEntry.SetVisibility(false);
    }

    public void Dispose()
    {
        Service.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
        Service.GameInventory.InventoryChanged -= GameInventory_InventoryChanged;
        DtrEntry.Dispose();
    }

    private void GameInventory_InventoryChanged(IReadOnlyCollection<InventoryEventArgs> events)
    {
        foreach (var evt in events)
        {
            if (!IsMedal(evt.Item.ItemId))
                continue;

            if (evt.Type is GameInventoryEvent.Added or GameInventoryEvent.Changed)
            {
                DtrEntry.Text = $"{evt.Item.Quantity} / 10";
                DtrEntry.Tooltip = GetItemName(evt.Item.ItemId);
                DtrEntry.SetVisibility(true);
                break;
            }

            if (evt.Type is GameInventoryEvent.Removed)
            {
                DtrEntry.SetVisibility(false);
                break;
            }
        }
    }

    private void ClientState_TerritoryChanged(ushort obj)
    {
        DtrEntry.SetVisibility(false);
    }

    private bool IsMedal(uint itemId)
    {
        foreach (var (_, weaponInfo) in Data.DataTable)
        {
            if (weaponInfo.Medal == itemId)
                return true;
        }

        return false;
    }
}
