using System.Runtime.Intrinsics.Arm;
using ArgentPonyWarcraftClient;
using Microsoft.EntityFrameworkCore;
using WoWArmory.Context;
using WoWArmory.Contracts.Models;
using WoWArmory.Models;
using EquippedItem = WoWArmory.Contracts.Models.EquippedItem;
using Guild = WoWArmory.Contracts.Models.Guild;

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

    public Character? GetCharacter(Guid id)
    {
        return GetCharacters()
            .Include(c => c.Realm)
            .FirstOrDefault(c => c.Id == id);
    }

    public async Task<Character?> GetCharacterAsync(Guid id)
    {
        return await DbContext.Characters
            .Include(c => c.Realm)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Character?> GetCharacter(string name)
    {
        return await DbContext.Characters
            .Include(c => c.Realm)
            .FirstOrDefaultAsync(g => g.Name.ToLower() == name.ToLower());
    }

    public Character? GetCharacter(string characterName, string realmName)
    {
        return DbContext.Characters
            .Include(c => c.Realm)
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

        character = await GetCharacterFromArmory(characterName, realmName);
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

    public void AddCharactersToUpdateQueue()
    {
        var characters = DbContext.Characters
            .Include(c => c.Realm)
            .Where(c => c.LastLoginTimestamp > DateTimeOffset.UtcNow.AddDays(-14) &&
                        c.LastModified < DateTime.UtcNow.AddHours(-1)).Take(50).ToList();

        AddCharactersToUpdateQueue(characters);
        ExecuteUpdateQueue();
    }

    public void AddCharactersToUpdateQueue(List<Character> characters)
    {
        foreach (var character in characters)
            AddCharacterToUpdateQueue(character);

        // ExecuteCharacterUpdateQueue();
    }

    public void AddCharacterToUpdateQueue(Guid characterId)
    {
        var character = GetCharacter(characterId);
        if (character == null) return;
        AddCharacterToUpdateQueue(character);
    }

    public void AddCharacterToUpdateQueue(Character character)
    {
        if (!UpdateQueue.Contains(character))
        {
            character.QueueStart = DateTime.UtcNow;
            UpdateQueue.Enqueue(character);
        }
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

        AddCharactersToUpdateQueue(characters);
        ExecuteUpdateQueue();
        var charIds = characters.Select(c2 => c2.Id).ToList();
        var result = await DbContext.Characters.Where(c => charIds.Contains(c.Id))
            .Include(c => c.Realm)
            .Include(c => c.CharacterHistories)
            .ToListAsync();
        return result;
    }

    public override void UpdateCharacter(Character character, bool saveChanges)
    {
        if (character.Realm == null && !string.IsNullOrEmpty(character.RealmName))
            character.Realm = DbContext.Realms.FirstOrDefault(r => r.Name == character.RealmName);

        var newCharacterTask = GetCharacterFromArmory(character.Name,
            character.Realm != null ? character.Realm.Name : character.RealmName);
        if (newCharacterTask == null) return;

        var newCharacter = newCharacterTask.Result;
        if (newCharacter == null) return;
        var oldCharacter = new CharacterHistory(character);

        character.Update(newCharacter);
        var characterChanged = character.HasChanges;

        if (character is { Level: >= MaxCharacterLevel, EquippedItems: not null })
        {
            character.EquippedItems?.Clear();
            foreach (var equippedItem in character.EquippedItems)
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

    protected override void UpdateGuild(Guild guild)
    {
    }

    private async Task DeleteCharacter(string characterName, string realmName)
    {
        var character = GetCharacter(characterName, realmName);
        if (character == null) return;

        DbContext.Characters.Remove(character);
        await SaveChangesAsync();
    }

    public async Task<Character?> GetCharacterFromArmory(string characterName, string realmName)
    {
        // Console.WriteLine($"Fetch Character {characterName}-{realmName}");
        // Retrieve the character profile
        var result = await WarcraftClient.GetCharacterProfileSummaryAsync(realmName.Replace(" ", "-").ToLower(), characterName.ToLower(),
                "profile-eu");
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
            Guild = GetGuild(characterProfileSummary.Guild)
        };

        return character;
    }

    public async Task<int> GetCharacterQueueIndex(Guid id)
    {
        var characterTask = GetCharacterAsync(id);
        var character = await characterTask;
        if (character == null) return 0;
        if (!UpdateQueue.Contains(character)) return 0;

        return 1;
    }
}