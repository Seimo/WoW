using Microsoft.EntityFrameworkCore;
using WoWArmory.Context;
using WoWArmory.Contracts.Models;
using WoWArmory.Models;

namespace WoWArmory.Services;

public abstract class BaseService : BlizzardApiService
{
    protected BaseService(ArmoryDbContext dbContext, ArmoryApiClient warcraftClientApi,
        WarcraftLogsApiClient warcraftLogsClientApi) : base(warcraftClientApi.ClientId, warcraftClientApi.ClientSecret, ArgentPonyWarcraftClient.Region.Europe)
    {
        DbContext = dbContext;
        WarcraftLogsApi = warcraftLogsClientApi;
        // WarcraftClient = warcraftClient;
        
        WarcraftClient = new ArgentPonyWarcraftClient.WarcraftClient(
            ClientId,
            ClientSecret,
            Region,
            ArgentPonyWarcraftClient.Locale.de_DE);
    }

    protected BaseService(ArmoryDbContext dbContext)
    {
        DbContext = dbContext;
    }
    
    protected static Queue<QueueEntity> UpdateQueue { get; set; } = new();
    // protected static Queue<Character> CharacterUpdateQueue { get; set; } = new();

    private WarcraftLogsApiClient WarcraftLogsApi { get; }
    // protected ArgentPonyWarcraftClient.IWarcraftClient WarcraftClient { get; }
    protected ArgentPonyWarcraftClient.WarcraftClient WarcraftClient { get; }
    protected ArmoryDbContext DbContext { get; }
    
    protected WarcraftLogsClient.WarcraftLogsClient GetWarcraftLogsClient()
    {
        if (WarcraftLogsApi.ClientId == null) throw new NullReferenceException("ClientId not set.");
        if (WarcraftLogsApi.ClientSecret == null) throw new NullReferenceException("ClientSecret not set.");

        return new WarcraftLogsClient.WarcraftLogsClient(WarcraftLogsApi.ClientId, WarcraftLogsApi.ClientSecret);
    }
    
    protected void SaveChanges()
    {
         DbContext.SaveChanges();
    }

    protected async Task SaveChangesAsync()
    {
        await DbContext.SaveChangesAsync();
    }
    
    public void AddToUpdateQueue(Guid id, QueueEntity.EntityTypeEnum type, bool updateReferences)
    {
        if (UpdateQueue.Any(e => e.Id == id)) return;
        
        var entity = new QueueEntity
        {
            Id = id, 
            EntityType = type,
            QueueStart = DateTime.UtcNow,
            UpdateReferences = updateReferences
        };
        UpdateQueue.Enqueue(entity);
    }

    public void ExecuteUpdateQueue()
    {
        while (UpdateQueue.Count > 0)
        {
            var entity = UpdateQueue.Dequeue();
            try
            {
                UpdateEntity(entity, false);
            }
            catch (Exception e)
            {
                if (e.GetBaseException() is InvalidOperationException) continue; 
                // Console.WriteLine(e);
                if (entity.QueueStart > DateTime.UtcNow.AddMinutes(-5))
                    UpdateQueue.Enqueue(entity);
            }
        }
        
        SaveChanges();
    }

    protected abstract void UpdateEntity(QueueEntity entity, bool saveChanges);
    
    #region Realm
    private List<Realm> Realms { get; set; } = new();
    
    protected Realm GetRealm(string name, string displayName)
    {
        SetRealms(false);
        var realm = Realms.FirstOrDefault(r => r.Name == name);
        if (realm != null) return realm;

        realm = new Realm
        {
            Id = new Guid(),
            Name = name,
            DisplayName = displayName
        };
        DbContext.Realms.Add(realm);
        SaveChanges();

        ReloadRealms();
        return realm;
    }
    
    private void SetRealms(bool syncFromArmory)
    {
        if (Realms.Count > 0) return;

        if (syncFromArmory)
        {
            var taskRealms = GetRealms();
            var realms = taskRealms.Result;
            if (realms != null)
            {
                var hasChanges = false;
                foreach (var realm in realms)
                {
                    var dbRealm = DbContext.Realms.FirstOrDefault(r => r.Name == realm.Name);
                    if (dbRealm != null)
                    {
                        if (dbRealm.DisplayName != realm.DisplayName)
                        {
                            dbRealm.DisplayName = realm.DisplayName;
                            hasChanges = true;
                        }

                        if (dbRealm.DisplayNameEn != realm.DisplayNameEn)
                        {
                            dbRealm.DisplayNameEn = realm.DisplayNameEn;
                            hasChanges = true;
                        }

                        if (dbRealm.DisplayNameFr != realm.DisplayNameFr)
                        {
                            dbRealm.DisplayNameFr = realm.DisplayNameFr;
                            hasChanges = true;
                        }
                    }
                    else
                    {
                        DbContext.Realms.Add(realm);
                        hasChanges = true;
                    }
                }

                if (hasChanges) SaveChanges();
            }
        }

        Realms = DbContext.Realms.ToList();
    }

    private void ReloadRealms()
    {
        Realms.Clear();
        SetRealms(false);
    }
    
    private async Task<List<Realm>> GetRealms()
    {
        var realms = new List<Realm>();

        // Retrieve the realms de
        var result = await WarcraftClient.GetRealmsIndexAsync(DynamicNamespace, ArgentPonyWarcraftClient.Region.Europe, ArgentPonyWarcraftClient.Locale.de_DE);
        if (result.Success)
            realms.AddRange(result.Value.Realms.Select(r => new Realm { Name = r.Slug, DisplayName = r.Name }));

        // Retrieve the realms en
        result = await WarcraftClient.GetRealmsIndexAsync(DynamicNamespace, ArgentPonyWarcraftClient.Region.Europe, ArgentPonyWarcraftClient.Locale.en_GB);
        if (result.Success)
            foreach (var realmReference in result.Value.Realms)
            {
                var realm = realms.FirstOrDefault(r => r.Name == realmReference.Slug);
                if (realm != null) realm.DisplayNameEn = realmReference.Name;
            }

        // Retrieve the realms fr
        result = await WarcraftClient.GetRealmsIndexAsync(DynamicNamespace, ArgentPonyWarcraftClient.Region.Europe, ArgentPonyWarcraftClient.Locale.fr_FR);
        if (result.Success)
            foreach (var realmReference in result.Value.Realms)
            {
                var realm = realms.FirstOrDefault(r => r.Name == realmReference.Slug);
                if (realm != null) realm.DisplayNameFr = realmReference.Name;
            }

        return realms;
    }
    
    #endregion
    
    #region CharacterClass
    
    private List<CharacterClass> CharacterClasses { get; set; } = new();
    
    protected CharacterClass GetCharacterClass(ArgentPonyWarcraftClient.PlayableClassReference classReferance)
    {
        SetCharacterClasses(false);
        var cClass = CharacterClasses.FirstOrDefault(r => r.Id == classReferance.Id);
        if (cClass != null) return cClass;

        cClass = new CharacterClass
        {
            Id = classReferance.Id,
            Name = classReferance.Name
        };
        DbContext.CharacterClasses.Add(cClass);
        SaveChanges();

        ReloadCharacterClasses();
        return cClass;
    }
    
    private void SetCharacterClasses(bool syncFromArmory)
    {
        if (CharacterClasses.Count > 0) return;

        if (syncFromArmory)
        {
            var task = GetCharacterClasses();
            var characterClasses = task.Result;
            if (characterClasses != null)
            {
                var hasChanges = false;
                foreach (var characterClass in characterClasses)
                {
                    var dbCharacterClass =
                        DbContext.CharacterClasses.FirstOrDefault(r => r.Id == characterClass.Id);
                    if (dbCharacterClass != null)
                    {
                        if (dbCharacterClass.Name != characterClass.Name)
                        {
                            dbCharacterClass.Name = characterClass.Name;
                            hasChanges = true;
                        }

                        if (dbCharacterClass.NameEn != characterClass.NameEn)
                        {
                            dbCharacterClass.NameEn = characterClass.NameEn;
                            hasChanges = true;
                        }

                        if (dbCharacterClass.NameFr != characterClass.NameFr)
                        {
                            dbCharacterClass.NameFr = characterClass.NameFr;
                            hasChanges = true;
                        }
                    }
                    else
                    {
                        DbContext.CharacterClasses.Add(characterClass);
                        hasChanges = true;
                    }
                }

                if (hasChanges) SaveChanges();
            }
        }

        CharacterClasses = DbContext.CharacterClasses.ToList();
    }

    private void ReloadCharacterClasses()
    {
        CharacterClasses.Clear();
        SetCharacterClasses(false);
    }
    
    private async Task<List<CharacterClass>> GetCharacterClasses()
    {
        var charClasses = new List<CharacterClass>();

        // Retrieve the races de
        var result = await WarcraftClient.GetPlayableClassesIndexAsync(StaticNamespace, ArgentPonyWarcraftClient.Region.Europe, ArgentPonyWarcraftClient.Locale.de_DE);
        if (result.Success)
            charClasses.AddRange(result.Value.Classes.Select(playableClassReference => new CharacterClass
                { Id = playableClassReference.Id, Name = playableClassReference.Name }));

        // Retrieve the races en
        result = await WarcraftClient.GetPlayableClassesIndexAsync(StaticNamespace, ArgentPonyWarcraftClient.Region.Europe, ArgentPonyWarcraftClient.Locale.en_GB);
        if (result.Success)
            foreach (var playableClassReference in result.Value.Classes)
            {
                var characterClass = charClasses.FirstOrDefault(r => r.Id == playableClassReference.Id);
                if (characterClass != null) characterClass.NameEn = playableClassReference.Name;
            }

        // Retrieve the races fr
        result = await WarcraftClient.GetPlayableClassesIndexAsync(StaticNamespace, ArgentPonyWarcraftClient.Region.Europe, ArgentPonyWarcraftClient.Locale.fr_FR);
        if (result.Success)
            foreach (var playableClassReference in result.Value.Classes)
            {
                var characterClass = charClasses.FirstOrDefault(r => r.Id == playableClassReference.Id);
                if (characterClass != null) characterClass.NameFr = playableClassReference.Name;
            }

        return charClasses;
    }
    
    #endregion
    
    #region Race
    
    private List<Race> Races { get; set; } = new();

    protected Race GetRace(ArgentPonyWarcraftClient.PlayableRaceReference raceReferance)
    {
        SetRaces(false);
        var race = Races.FirstOrDefault(r => r.Id == raceReferance.Id);
        if (race != null) return race;

        race = new Race
        {
            Id = raceReferance.Id,
            Name = raceReferance.Name
        };
        DbContext.Races.Add(race);
        SaveChanges();

        ReloadRaces();
        return race;
    }
    
    private void SetRaces(bool syncFromArmory)
    {
        if (Races.Count > 0) return;

        if (syncFromArmory)
        {
            var taskRaces = GetRaces();
            var races = taskRaces.Result;
            if (races != null)
            {
                var hasChanges = false;
                foreach (var race in races)
                {
                    var dbRace = DbContext.Races.FirstOrDefault(r => r.Id == race.Id);
                    if (dbRace != null)
                    {
                        if (dbRace.Name != race.Name)
                        {
                            dbRace.Name = race.Name;
                            hasChanges = true;
                        }

                        if (dbRace.NameEn != race.NameEn)
                        {
                            dbRace.NameEn = race.NameEn;
                            hasChanges = true;
                        }

                        if (dbRace.NameFr != race.NameFr)
                        {
                            dbRace.NameFr = race.NameFr;
                            hasChanges = true;
                        }
                    }
                    else
                    {
                        DbContext.Races.Add(race);
                        hasChanges = true;
                    }
                }

                if (hasChanges) SaveChanges();
            }
        }

        Races = DbContext.Races.ToList();
    }


    private void ReloadRaces()
    {
        Races.Clear();
        SetRaces(false);
    }

    private async Task<List<Race>> GetRaces()
    {
        var races = new List<Race>();

        // Retrieve the races de
        var result = await WarcraftClient.GetPlayableRacesIndexAsync(StaticNamespace, ArgentPonyWarcraftClient.Region.Europe, ArgentPonyWarcraftClient.Locale.de_DE);
        if (result.Success)
            races.AddRange(result.Value.Races.Select(playableRaceReference =>
                new Race { Id = playableRaceReference.Id, Name = playableRaceReference.Name }));

        // Retrieve the races en
        result = await WarcraftClient.GetPlayableRacesIndexAsync(StaticNamespace, ArgentPonyWarcraftClient.Region.Europe, ArgentPonyWarcraftClient.Locale.en_GB);
        if (result.Success)
            foreach (var playableRaceReference in result.Value.Races)
            {
                var race = races.FirstOrDefault(r => r.Id == playableRaceReference.Id);
                if (race != null) race.NameEn = playableRaceReference.Name;
            }

        // Retrieve the races fr
        result = await WarcraftClient.GetPlayableRacesIndexAsync(StaticNamespace, ArgentPonyWarcraftClient.Region.Europe, ArgentPonyWarcraftClient.Locale.fr_FR);
        if (result.Success)
            foreach (var playableRaceReference in result.Value.Races)
            {
                var race = races.FirstOrDefault(r => r.Id == playableRaceReference.Id);
                if (race != null) race.NameFr = playableRaceReference.Name;
            }

        return races;
    }
    
    #endregion
    
    #region Guild
    
    protected Guild? GetGuild(ArgentPonyWarcraftClient.GuildReference? guildReference)
    {
        if (guildReference == null) return null;
        
        var guild = DbContext.Guilds.Include(guild => guild.Realm)
            .FirstOrDefault(g => g.ArmoryId == guildReference.Id);
        if (guild != null) return guild;

        guild = new Guild
        {
            ArmoryId = guildReference.Id,
            Name = guildReference.Name,
            Realm = GetRealm(guildReference.Realm.Slug, guildReference.Realm.Name)
        };
        DbContext.Guilds.Add(guild);

        return guild;
    }
    
    #endregion
    
}