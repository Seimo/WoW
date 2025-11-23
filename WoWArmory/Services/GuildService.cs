using ArgentPonyWarcraftClient;
using Microsoft.EntityFrameworkCore;
using WoWArmory.Context;
using WoWArmory.Models;
using Guild = WoWArmory.Contracts.Models.Guild;

namespace WoWArmory.Services;

public class GuildService(
    CharacterService characterService,
    ArmoryDbContext dbContext,
    ArmoryApiClient warcraftClientApi,
    WarcraftLogsApiClient warcraftLogsClientApi) : BaseService(dbContext, warcraftClientApi, warcraftLogsClientApi)
{
    private CharacterService CharacterService { get; } = characterService;

    public IQueryable<Guild> GetGuilds()
    {
        return DbContext.Guilds;
    }
    
    public Guild? GetGuild(Guid id)
    {
        return GetGuilds()
            .Include(c => c.Realm)
            .FirstOrDefault(c => c.Id == id);
    }

    public async Task<Guild?> GetGuildAsync(Guid id)
    {
        return await DbContext.Guilds
            .Include(g => g.Realm)
            .Include(g => g.Members)
            .ThenInclude(c => c.CharacterHistories)
            .Include(m => m.Realm)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public Guild? GetGuild(string name)
    {
        return GetGuilds().FirstOrDefault(g => g.Name.ToLower() == name.ToLower());
    }

    private async Task<GuildRoster?> GetGuildRoster(string realmName, string guildName)
    {
        // Retrieve the character profile for Drinian of realm Norgannon.
        var result = await WarcraftClient.GetGuildRosterAsync(realmName.Replace(" ", "-").ToLower(),
            guildName.Replace(" ", "-").ToLower(), ProfileNamespace);
        return !result.Success ? null : result.Value;
    }

    private void UpdateGuildFromArmory(Guild guild, bool saveChanges)
    {
        var guildRosterTask = GetGuildRoster(guild.Realm?.Name, guild.Name);
        if (guildRosterTask == null) return;

        var guildRoster = guildRosterTask.Result;
        if (guildRoster == null) return;

        var characterToRemove = guild.Members
            .Where(c => guildRoster.Members.All(m => m.Character.Id != c.ArmoryId))
            .ToList();
        foreach (var character in characterToRemove) guild.Members.Remove(character);

        foreach (var guildRosterMember in guildRoster.Members)
        {
            var character = guild.Members.FirstOrDefault(c => c.ArmoryId == guildRosterMember.Character.Id);
            if (character == null)
                character = guild.Members.FirstOrDefault(c =>
                    c.Name == guildRosterMember.Character.Name &&
                    c.RealmName == guildRosterMember.Character.Realm.Slug);

            if (character != null)
            {
                CharacterService.AddCharacterToUpdateQueue(character, false);
            }
            else
            {
                character = CharacterService.GetCharacter(guildRosterMember.Character.Id);
                if (character != null)
                {
                    CharacterService.AddCharacterToUpdateQueue(character, false);
                }
                else
                {
                    var characterTask = CharacterService.GetCharacterFromArmory(guildRosterMember.Character.Name, guildRosterMember.Character.Realm.Slug, false);
                    if (characterTask == null) continue;

                    character = characterTask.Result;
                    if (character == null) continue;
                    CharacterService.AddCharacter(character, false);
                }
            }
        }
    }

    protected override void UpdateEntity(QueueEntity entity, bool saveChanges)
    {
        switch (entity.EntityType)
        {
            case QueueEntity.EntityTypeEnum.Character:
                CharacterService.UpdateCharacter(entity.Id, entity.UpdateReferences, false);
                break;
            case QueueEntity.EntityTypeEnum.Guild:
            {
                var guild = GetGuild(entity.Id);
                if (guild == null) return;
        
                UpdateGuildFromArmory(guild, false);
                break;
            }
        }

        if (saveChanges)
            SaveChanges();
    }
}