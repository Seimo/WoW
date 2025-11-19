using Microsoft.EntityFrameworkCore;
using WoWArmory.Context;
using WoWArmory.Contracts.Models;
using WoWArmory.Models;
using Guild = WoWArmory.Contracts.Models.Guild;

namespace WoWArmory.Services;

public class RaidService(
    CharacterService characterService,
    ArmoryDbContext dbContext,
    ArmoryApiClient warcraftClientApi,
    WarcraftLogsApiClient warcraftLogsClientApi)
    : BaseService(dbContext, warcraftClientApi, warcraftLogsClientApi)
{
    private CharacterService CharacterService { get; } = characterService;

    public IQueryable<Raid> GetRaids()
    {
        return DbContext.Raids
            .Include(r => r.Characters)
            .ThenInclude(c => c.CharacterHistories)
            .Include(r => r.Characters)
            .ThenInclude(c => c.CharacterClass)
            .Include(r => r.Characters)
            .ThenInclude(c => c.Realm)
            .Include(r => r.RaidLogs);
    }

    public Raid? GetRaid(Guid id)
    {
        return GetRaids().FirstOrDefault(r => r.Id == id);
    }

    public async Task<Raid?> GetRaidAsync(Guid id)
    {
        return await GetRaids().FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Raid> CreateRaid(string name)
    {
        var dbRaid = GetRaids().FirstOrDefault(c => c.Name == name);
        if (dbRaid != null) return dbRaid;

        var raid = new Raid { Name = name };
        DbContext.Raids.Add(raid);
        await SaveChangesAsync();
        return raid;
    }

    public async Task<Raid?> UpdateRaid(Guid id)
    {
        var raid = await GetRaidAsync(id);
        if (raid == null) return null;

        CharacterService.AddCharactersToUpdateQueue(raid.Characters);
        ExecuteUpdateQueue();
        return raid;
    }

    public async Task<Raid?> AddRaidCharacter(Guid raidId, Guid characterId)
    {
        var raid = await GetRaidAsync(raidId);
        if (raid == null) return null;

        var dbCharacter = raid.Characters.FirstOrDefault(c => c.Id == characterId);
        if (dbCharacter != null) return raid;

        dbCharacter = CharacterService.GetCharacter(characterId);
        if (dbCharacter == null) return raid;

        raid.Characters.Add(dbCharacter);
        await SaveChangesAsync();

        return raid;
    }

    public async Task<Raid?> RemoveRaidCharacter(Guid raidId, Guid characterId)
    {
        var raid = await GetRaidAsync(raidId);
        if (raid == null) return null;

        var dbCharacter = raid.Characters.FirstOrDefault(c => c.Id == characterId);
        if (dbCharacter == null) return raid;

        raid.Characters.Remove(dbCharacter);
        await SaveChangesAsync();

        return raid;
    }

    public override void UpdateCharacter(Character character, bool saveChanges)
    {
       CharacterService.UpdateCharacter(character, saveChanges);
    }

    protected override void UpdateGuild(Guild guild)
    {
        
    }
    
}