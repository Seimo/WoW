using Microsoft.EntityFrameworkCore;
using WoWArmory.Context;
using WoWArmory.Contracts.Models;
using WoWArmory.Models;
using EquippedItem = WoWArmory.Contracts.Models.EquippedItem;

namespace WoWArmory.Services;

public class CharacterService(
    ItemService itemService,
    ArmoryDbContext dbContext,
    ArmoryApiClient warcraftClientApi,
    WarcraftLogsApiClient warcraftLogsClientApi) : BaseService(dbContext, warcraftClientApi, warcraftLogsClientApi)
{
    private const int MaxCharacterLevel = 80;
    public int QueueCount => UpdateQueue.Count;

    private ItemService ItemService { get; } = itemService;

    public IQueryable<Character> GetCharacters()
    {
        return DbContext.Characters;
    }
    
    private IQueryable<Character> GetCharacterQuery()
    {
        return GetCharacters()
            .Include(c => c.Realm)
            .Include(c => c.Guild)
            .Include(c => c.EquippedItems);
    }

    public Character? GetCharacter(Guid id)
    {
        return GetCharacterQuery()
            .FirstOrDefault(c => c.Id == id);
    }

    public async Task<Character?> GetCharacterAsync(Guid id)
    {
        return await GetCharacterQuery()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Character?> GetCharacter(string name)
    {
        return await GetCharacterQuery()
            .FirstOrDefaultAsync(g => g.Name.ToLower() == name.ToLower());
    }

    public Character? GetCharacter(string characterName, string realmName)
    {
        return GetCharacterQuery()
            .FirstOrDefault(c =>
                c.Realm != null && c.Realm.Name.ToLower() == realmName.ToLower() &&
                c.Name.ToLower() == characterName.ToLower());
    }

    public Character? GetCharacter(int armoryId)
    {
        return GetCharacters()
            .FirstOrDefault(c => c.ArmoryId == armoryId);
    }

    public void AddCharacter(Character character, bool save)
    {
        DbContext.Characters.Add(character);
        if (save)
            SaveChanges();
    }

    public async Task<Character?> GetOrCreateCharacter(string characterName, string realmName)
    {
        var character = GetCharacter(characterName, realmName);
        if (character != null) return character;

        character = await GetCharacterFromArmory(characterName, realmName, false);
        if (character == null) return null;

        AddCharacter(character, true);
        return character;
    }

    public IQueryable<Character> GetCharactersMaxLevel()
    {
        return DbContext.Characters.Where(c =>
            c.LastLoginTimestamp > DateTimeOffset.UtcNow.AddMonths(-1) &&
            c.Level == MaxCharacterLevel);
    }

    public bool CharacterExists(Guid id)
    {
        return DbContext.Characters.Any(e => e.Id == id);
    }

    // public void AddCharactersToUpdateQueue()
    // {
    //     var maxCount = 50;
    //     var characters = DbContext.Characters
    //         .Include(c => c.Realm)
    //         .Where(c => c.LastLoginTimestamp > DateTimeOffset.UtcNow.AddDays(-14) &&
    //                     c.LastModified < DateTime.UtcNow.AddHours(-1)).Take(maxCount).ToList();
    //
    //     AddCharactersToUpdateQueue(characters, maxCount <= 1);
    //     ExecuteUpdateQueue();
    // }

    public void AddCharactersToUpdateQueue(List<Character> characters, bool updateReferences)
    {
        foreach (var character in characters)
            AddCharacterToUpdateQueue(character, updateReferences);

        // ExecuteCharacterUpdateQueue();
    }

    public void AddCharacterToUpdateQueue(Character character, bool withDetails)
    {
        AddToUpdateQueue(character.Id, QueueEntity.EntityTypeEnum.Character, withDetails);
    }

    public async Task<IEnumerable<Character>> UpdateCharacters(int maxCount = 50)
    {
        var characters = await GetCharacters()
            .Where(c =>
                c.LastLoginTimestamp > DateTimeOffset.UtcNow.AddDays(-14) &&
                c.LastModified < DateTime.UtcNow.AddHours(-1))
            .OrderByDescending(c => c.Level)
            .ThenByDescending(c => c.EquippedItemLevel)
            .ThenBy(c => c.LastModified)
            .ThenBy(c => c.Name)
            .Take(maxCount)
            .ToListAsync();

        AddCharactersToUpdateQueue(characters, maxCount <= 1);
        ExecuteUpdateQueue();
        var charIds = characters.Select(c2 => c2.Id).ToList();
        var result = await DbContext.Characters.Where(c => charIds.Contains(c.Id))
            .Include(c => c.Realm)
            .Include(c => c.CharacterHistories)
            .ToListAsync();
        return result;
    }

    protected override void UpdateEntity(QueueEntity entity, bool saveChanges)
    {
        UpdateCharacter(entity.Id, entity.UpdateReferences, saveChanges);
    }
    
    public void UpdateCharacter(Guid id, bool withEquipment, bool saveChanges)
    {
        var character = GetCharacter(id);
        if (character == null) return;
        
        if (character.Realm == null && !string.IsNullOrEmpty(character.RealmName))
            character.Realm = DbContext.Realms.FirstOrDefault(r => r.Name == character.RealmName);

        var newCharacterTask = GetCharacterFromArmory(character.Name, character.Realm != null ? character.Realm.Name : character.RealmName, withEquipment);
        if (newCharacterTask == null) return;

        var newCharacter = newCharacterTask.Result;
        if (newCharacter == null) return;
        var oldCharacter = new CharacterHistory(character);

        character.Update(newCharacter);
        var characterChanged = character.HasChanges;

        if (newCharacter is { Level: >= MaxCharacterLevel, EquippedItems: not null })
        {
            character.EquippedItems?.Clear();
            foreach (var equippedItem in newCharacter.EquippedItems)
            {
                var dbEquippedItem = new EquippedItem
                {
                    Character = character,
                    Item = ItemService.GetItem(equippedItem.Item.ItemId, equippedItem.Name, equippedItem.Description,
                        equippedItem.Quality, equippedItem.Slot),
                    Name = equippedItem.Name,
                    Description = equippedItem.Description,
                    Level = equippedItem.Level,
                    Quality = equippedItem.Quality,
                    Quantity = equippedItem.Quantity,
                    Slot = equippedItem.Slot,
                    EnchantmentId = equippedItem.EnchantmentId
                };
                DbContext.EquippedItems.Add(dbEquippedItem);
            }

            characterChanged = true;
        }

        if (characterChanged)
        {
            if (character.EquippedItemLevel != oldCharacter.EquippedItemLevel ||
                character.AverageItemLevel != oldCharacter.AverageItemLevel ||
                character.Level != oldCharacter.Level) DbContext.CharacterHistories.Add(oldCharacter);

            if (saveChanges)
                SaveChanges();
        }
    }

    private async Task DeleteCharacter(string characterName, string realmName)
    {
        var character = GetCharacter(characterName, realmName);
        if (character == null) return;

        DbContext.Characters.Remove(character);
        await SaveChangesAsync();
    }

    public async Task<Character?> GetCharacterFromArmory(string characterName, string realmName, bool withEquipment)
    {
        var realmSlug = realmName.Replace(" ", "-").ToLower();

        // Console.WriteLine($"Fetch Character {characterName}-{realmName}");
        // Retrieve the character profile
        var result = await WarcraftClient.GetCharacterProfileSummaryAsync(realmSlug, characterName.ToLower(), ProfileNamespace);
        if (!result.Success)
        {
            if (result.Error.Code == 404)
            {
                await DeleteCharacter(characterName, realmName);
            }

            return null;
        }

        var characterProfileSummary = result.Value;
        if (characterProfileSummary == null) return null;

        var character = new Character
        {
            ArmoryId = characterProfileSummary.Id,
            Name = characterProfileSummary.Name,
            Realm = GetRealm(characterProfileSummary.Realm.Slug, characterProfileSummary.Realm.Name),
            RealmName = characterProfileSummary.Realm.Slug,
            Level = characterProfileSummary.Level,
            CharacterClass = GetCharacterClass(characterProfileSummary.CharacterClass),
            Faction = characterProfileSummary.Faction.Name,
            Gender = characterProfileSummary.Gender.Name,
            ActiveTitle = characterProfileSummary.ActiveTitle?.DisplayString,
            AverageItemLevel = characterProfileSummary.AverageItemLevel,
            AchievementPoints = characterProfileSummary.AchievementPoints,
            Experience = characterProfileSummary.Experience,
            EquippedItemLevel = characterProfileSummary.EquippedItemLevel,
            LastLoginTimestamp = characterProfileSummary.LastLoginTimestamp,
            Race = GetRace(characterProfileSummary.Race),
            Guild = GetGuild(characterProfileSummary.Guild),
            EquippedItems = []
        };

        if (withEquipment)
        {
            var equippedItems = await GetCharacterEquipmentFromArmory(characterName, realmSlug);
            if (equippedItems != null)
            {
                foreach (var equippedItem in equippedItems)
                {
                    character.EquippedItems.Add(equippedItem);
                }
            }
        }

        return character;
    }

    private async Task<List<EquippedItem>?> GetCharacterEquipmentFromArmory(string characterName, string realmSlug)
    {
        var result = await WarcraftClient.GetCharacterEquipmentSummaryAsync(realmSlug, characterName.ToLower(), ProfileNamespace);
        if (!result.Success) return null;

        var characterEquipmentSummary = result.Value;
        if (characterEquipmentSummary == null) return null;

        var items = new List<EquippedItem>();
        foreach (var equippedItem in characterEquipmentSummary.EquippedItems)
        {
            items.Add(new EquippedItem
            {
                Name = equippedItem.Name,
                Item = ItemService.GetItem(equippedItem.Item.Id, equippedItem.Name, equippedItem.Description,
                    equippedItem.Quality.Type, equippedItem.Slot.Type),
                Description = equippedItem.Description,
                Quality = equippedItem.Quality.Type,
                Slot = equippedItem.Slot.Type,
                Level = equippedItem.Level.Value,
                EnchantmentId = equippedItem.Enchantments != null ? equippedItem.Enchantments.FirstOrDefault().EnchantmentId : 0,
            });
        }

        return items;
    }

    public async Task<int> GetCharacterQueueIndex(Guid id)
    {
        var entity = new QueueEntity { Id = id };
        if (!UpdateQueue.Contains(entity)) return 0;

        return 1;
    }
}