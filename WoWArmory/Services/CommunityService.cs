using ArgentPonyWarcraftClient;
using Microsoft.EntityFrameworkCore;
using WoWArmory.Context;
using WoWArmory.Contracts.Models;
using WoWArmory.Models;
using Guild = WoWArmory.Contracts.Models.Guild;

namespace WoWArmory.Services;

public class CommunityService(
    CharacterService characterService,
    ArmoryDbContext dbContext,
    ArmoryApiClient warcraftClientApi,
    WarcraftLogsApiClient warcraftLogsClientApi) : BaseService(dbContext, warcraftClientApi, warcraftLogsClientApi)
{
    
    private CharacterService CharacterService { get; } = characterService;
    public IQueryable<Community> GetCommunities()
    {
        return DbContext.Communities
            .Include(c => c.Characters)
            .ThenInclude(cc => cc.Race)
            .Include(c => c.Characters)
            .ThenInclude(cc => cc.CharacterClass)
            .Include(c => c.Characters)
            .ThenInclude(cc => cc.Realm);
        //.Include(c => c.Characters)
        //.ThenInclude(cc => cc.CharacterHistories);
    }

    public async Task<Community?> GetCommunity(Guid id)
    {
        return await GetCommunities()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task DeleteCommunity(Guid id)
    {
        var community = await DbContext.Communities.FindAsync(id);
        if (community == null) return;

        DbContext.Communities.Remove(community);
        await SaveChangesAsync();
    }

    public async Task<Community?> UpdateCommunity(Guid id)
    {
        var community = await GetCommunity(id);
        if (community == null) return null;

        CharacterService.AddCharactersToUpdateQueue(community.Characters);
        ExecuteUpdateQueue();
        return community;
    }

    public async Task<Character?> AddCharacter(Guid communityId, Guid characterId)
    {
        var community = await GetCommunity(communityId);
        if (community == null) return null;
        
        if (community.Characters.Any(c => c.Id == characterId)) return null;
        
        var character = await CharacterService.GetCharacterAsync(characterId);
        if (character == null) return null;

        community.Characters.Add(character);

        await SaveChangesAsync();
        return character;
    }
    
    public async Task<bool> RemoveCharacter(Guid communityId, Guid characterId)
    {
        var community = await GetCommunity(communityId);
        if (community == null) return true;

        var character = community.Characters.FirstOrDefault(c => c.Id == characterId);
        if (character == null) return false;

        community.Characters.Remove(character);

        await SaveChangesAsync();
        return true;
    }

    public override void UpdateCharacter(Character character, bool saveChanges)
    {
        CharacterService.UpdateCharacter(character, saveChanges);
    }

    protected override void UpdateGuild(Guild guild)
    {
       
    }
}