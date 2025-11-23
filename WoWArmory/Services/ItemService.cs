using ArgentPonyWarcraftClient;
using Microsoft.EntityFrameworkCore;
using WoWArmory.Context;
using WoWArmory.Models;
using Item = WoWArmory.Contracts.Models.Item;

namespace WoWArmory.Services;

public class ItemService(
    ArmoryDbContext dbContext,
    ArmoryApiClient warcraftClientApi,
    WarcraftLogsApiClient warcraftLogsClientApi) : BaseService(dbContext, warcraftClientApi, warcraftLogsClientApi)
{

    public async Task<Item?> GetItemAsync(int itemId)
    {
        return await DbContext.Items
            .FirstOrDefaultAsync(c => c.ItemId == itemId);
    }
    
    public Item GetItem(int itemId, string name, string description, string quality, string type)
    {
        var item = DbContext.Items.FirstOrDefault(i => i.ItemId == itemId);
        if (item != null) return item;

        item = new Item
        {
            ItemId = itemId,
            Name = name,
            Description = description,
            Quality = quality,
            Type = type
        };
        DbContext.Items.Add(item);

        return item;
    }

    public async Task<Item?> GetItemFromArmory(int itemId)
    {
        var result = await WarcraftClient.GetItemAsync(itemId, ProfileNamespace, Region, Locale.de_DE);
        if (!result.Success)
            return null;    
        
        var itemSummary = result.Value;
        if (itemSummary == null) return null;

        var item = new Item
        {
            ItemId = itemId,
            Name = itemSummary.Name,
            Description = itemSummary.Name,
            Quality = itemSummary.Quality.Type,
            Type = itemSummary.InventoryType.Type,
            Level = itemSummary.Level,
            RequiredLevel = itemSummary.RequiredLevel,
            PurchasePrice = itemSummary.PurchasePrice,
            PurchaseQuantity = itemSummary.PurchaseQuantity,
            SellPrice = itemSummary.SellPrice,
            MaxCount = itemSummary.MaxCount,
            IsEquippable = itemSummary.IsEquippable,
            IsStackable = itemSummary.IsStackable
        };
        DbContext.Items.Add(item);

        return item;
    }

    protected override void UpdateEntity(QueueEntity entity, bool saveChanges)
    {
        
    }
}