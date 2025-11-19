using ArgentPonyWarcraftClient;
using Microsoft.EntityFrameworkCore;
using WoWArmory.Context;
using WoWArmory.Contracts.Models;
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

    public async Task<Guild?> GetGuild(Guid id)
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

    public Guild? UpdateGuild(Guid id)
    {
        var guild = DbContext.Guilds
            .Include(g => g.Realm)
            .Include(g => g.Members)
            .ThenInclude(c => c.CharacterHistories)
            .Include(m => m.Realm)
            .FirstOrDefault(g => g.Id == id);

        if (guild == null) return null;

        if (guild.Realm?.Name == null) return guild;
        UpdateGuild(guild);
        ExecuteUpdateQueue();
        return guild;
    }

    private async Task<GuildRoster?> GetGuildRoster(string realmName, string guildName)
    {
        // Retrieve the character profile for Drinian of realm Norgannon.
        var result = await WarcraftClient.GetGuildRosterAsync(realmName.Replace(" ", "-").ToLower(),
            guildName.Replace(" ", "-").ToLower(), "profile-eu");
        return !result.Success ? null : result.Value;
    }

    public override void UpdateCharacter(Character character, bool saveChanges)
    {
        CharacterService.UpdateCharacter(character, saveChanges);
    }

    protected override void UpdateGuild(Guild guild)
    {
        guild.QueueStart = DateTime.UtcNow;
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
                CharacterService.AddCharacterToUpdateQueue(character);
            }
            else
            {
                character = CharacterService.GetCharacter(guildRosterMember.Character.Id);
                if (character != null)
                {
                    CharacterService.AddCharacterToUpdateQueue(character);
                }
                else
                {
                    var characterTask = CharacterService.GetCharacterFromArmory(guildRosterMember.Character.Name,
                        guildRosterMember.Character.Realm.Slug);
                    if (characterTask == null) continue;

                    character = characterTask.Result;
                    if (character == null) continue;
                    CharacterService.AddCharacter(character, false);
                }
            }
        }
    }
}