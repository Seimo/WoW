using ArgentPonyWarcraftClient;
using Microsoft.EntityFrameworkCore;
using WoWArmory.Context;
using WoWArmory.Contracts.Models;
using WoWArmory.Models;
using Guild = WoWArmory.Contracts.Models.Guild;

namespace WoWArmory.Services;

public class WoWAccountService(
    CharacterService characterService,
    ArmoryDbContext dbContext,
    ArmoryApiClient warcraftClientApi,
    WarcraftLogsApiClient warcraftLogsClientApi) : BaseService(dbContext, warcraftClientApi, warcraftLogsClientApi)
{
    private CharacterService CharacterService { get; } = characterService;

    public IQueryable<Character> GetCharacters(User? user)
    {
        return CharacterService.GetCharacters()
            .Where(c => c.User == user)
            .Include(c => c.Race)
            .Include(c => c.CharacterClass)
            .Include(c => c.Realm);
    }

    public IQueryable<Character> UpdateCharacters(User? user)
    {
        if (user?.WoWAccessToken == null) return GetCharacters(user);

        var summary = GetAccountProfileSummary(user.WoWAccessToken);
        if (summary.Result == null)
        {
            foreach (var character in user.Characters)
            {
                CharacterService.AddCharacterToUpdateQueue(character);
            }
            ExecuteUpdateQueue();
            return GetCharacters(user);
        }

        var profileSummary = summary.Result;

        foreach (var wowAccount in profileSummary.WowAccounts)
        foreach (var wowAccountCharacter in wowAccount.Characters)
        {
            var dbCharacter = CharacterService.GetCharacters()
                .FirstOrDefault(c => c.ArmoryId == wowAccountCharacter.Id);
            if (dbCharacter != null)
            {
                dbCharacter.User ??= user;
            }
            else
            {
                dbCharacter = new Character
                {
                    ArmoryId = wowAccountCharacter.Id,
                    Name = wowAccountCharacter.Name,
                    Realm = GetRealm(wowAccountCharacter.Realm.Slug, wowAccountCharacter.Realm.Name),
                    RealmName = wowAccountCharacter.Realm.Slug,
                    Level = wowAccountCharacter.Level,
                    CharacterClass = GetCharacterClass(wowAccountCharacter.PlayableClass),
                    Faction = wowAccountCharacter.Faction.Name,
                    Gender = wowAccountCharacter.Gender.Name,
                    User = user
                };
                CharacterService.AddCharacter(dbCharacter, false);
            }

            CharacterService.AddCharacterToUpdateQueue(dbCharacter);
        }

        ExecuteUpdateQueue();
        return GetCharacters(user);
    }

    private async Task<AccountProfileSummary?> GetAccountProfileSummary(string accessToken)
    {
        // Retrieve the character profile
        try
        {
            
            
            var result = await WarcraftClient.GetAccountProfileSummaryAsync(accessToken, "profile-eu");
            return !result.Success ? null : result.Value;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return null;
    }

    public override void UpdateCharacter(Character character, bool saveChanges)
    {
        CharacterService.UpdateCharacter(character, saveChanges);
    }

    protected override void UpdateGuild(Guild guild)
    {
    }
}