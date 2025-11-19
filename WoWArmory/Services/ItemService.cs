using ArgentPonyWarcraftClient;
using WoWArmory.Context;
using WoWArmory.Contracts.Models;
using WoWArmory.Models;
using Guild = WoWArmory.Contracts.Models.Guild;
using Item = WoWArmory.Contracts.Models.Item;

namespace WoWArmory.Services;

public class ItemService(
    ArmoryDbContext dbContext,
    ArmoryApiClient warcraftClientApi,
    WarcraftLogsApiClient warcraftLogsClientApi) : BaseService(dbContext, warcraftClientApi, warcraftLogsClientApi)
{
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

    public override void UpdateCharacter(Character character, bool saveChanges)
    {
        
    }

    protected override void UpdateGuild(Guild guild)
    {
      
    }
}