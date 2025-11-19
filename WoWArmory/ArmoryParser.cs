using ArgentPonyWarcraftClient;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WoWArmory.Context;
using WoWArmory.Contracts.Models;
using WoWArmory.Models;
using WoWArmory.Services;
using EquippedItem = WoWArmory.Contracts.Models.EquippedItem;
using Guild = WoWArmory.Contracts.Models.Guild;
using Item = WoWArmory.Contracts.Models.Item;
using Realm = WoWArmory.Contracts.Models.Realm;

namespace WoWArmory;

public class ArmoryParser
{
    private const int MaxCharacterLevel = 80;

    public ArmoryParser(ApiClient warcraftClientApi, ApiClient warcraftLogsClientApi,
        CharacterService characterService, ArmoryDbContext context)
    {
        if (warcraftClientApi == null) throw new ArgumentNullException(nameof(warcraftClientApi));
        if (warcraftLogsClientApi == null) throw new ArgumentNullException(nameof(warcraftLogsClientApi));

        WarcraftClientApi = warcraftClientApi;
        WarcraftLogsApi = warcraftLogsClientApi;
        ArmoryDbContext = context;
        CharacterService = characterService;
    }

    private ApiClient WarcraftClientApi { get; }
    private ApiClient WarcraftLogsApi { get; }

    private ArmoryDbContext ArmoryDbContext { get; }
    private CharacterService CharacterService { get; }
    private List<CharacterClass> CharacterClasses { get; set; } = new();
    private List<Race> Races { get; set; } = new();
    private List<Realm> Realms { get; set; } = new();
    public event EventHandler StatusEvent;

    public void InitDatabase()
    {
        //Postgres            
        var connString = ArmoryDbContext.Database.GetConnectionString();
        var connBuilder = new NpgsqlConnectionStringBuilder(connString);
        StatusEvent(this,
            new StatusEventArg { Text = $"Connect Database {connBuilder.Database} on {connBuilder.Host} ..." });

        var migrationsCount = ArmoryDbContext.Database.GetPendingMigrations().Count();
        if (migrationsCount > 0)
        {
            StatusEvent(this, new StatusEventArg { Text = $"{migrationsCount} migrations pending ..." });
            ArmoryDbContext.Database.Migrate();
        }

        var syncFromArmory = GetMetaDataBool(MetaData.KeyEnums.SyncFromArmory, true);
        SetRaces(syncFromArmory);
        SetCharacterClasses(syncFromArmory);
        SetRealms(syncFromArmory);

        if (syncFromArmory) SetMetaDataBool(MetaData.KeyEnums.SyncFromArmory, false);
    }

    private bool GetMetaDataBool(MetaData.KeyEnums key, bool defaultValue = false)
    {
        var metaData = ArmoryDbContext.MetaDatas.FirstOrDefault(m => m.Key == key);
        if (metaData == null) return defaultValue;

        return bool.TryParse(metaData.Value, out var value) ? value : defaultValue;
    }

    private void SetMetaDataBool(MetaData.KeyEnums key, bool value)
    {
        var metaData = ArmoryDbContext.MetaDatas.FirstOrDefault(m => m.Key == key);
        if (metaData == null)
            ArmoryDbContext.MetaDatas.Add(new MetaData { Key = key, Value = value.ToString() });
        else
            metaData.Value = value.ToString();

        SaveChanges();
    }

    private void CheckDefaultGuilds()
    {
        if (ArmoryDbContext.Guilds.Any()) return;

        CheckDefaultGuild(new Guild { Name = "Die Dunklen Templer", Visible = true });
        CheckDefaultGuild(new Guild { Name = "Fyrd Of Hellebrynne", Visible = true });
        CheckDefaultGuild(new Guild { Name = "Bündnis des Mondes", Visible = true });
    }

    private void CheckDefaultGuild(Guild guild)
    {
        if (ArmoryDbContext.Guilds.Any(g => g.Name == guild.Name)) return;

        ArmoryDbContext.Guilds.Add(guild);
        SaveChanges();
    }

    public void Start()
    {
        CheckDefaultGuilds();

        foreach (var guild in ArmoryDbContext.Guilds.ToList()) UpdateGuild(guild);
        //Console.WriteLine(guild.ToString());
        //foreach (var guildMember in guild.Members)
        //{
        //    Console.WriteLine(guildMember?.ToString());
        //}
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
                        ArmoryDbContext.CharacterClasses.FirstOrDefault(r => r.Id == characterClass.Id);
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
                        ArmoryDbContext.CharacterClasses.Add(characterClass);
                        hasChanges = true;
                    }
                }

                if (hasChanges) SaveChanges();
            }
        }

        CharacterClasses = ArmoryDbContext.CharacterClasses.ToList();
    }

    private void ReloadCharacterClasses()
    {
        CharacterClasses.Clear();
        SetCharacterClasses(false);
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
                    var dbRace = ArmoryDbContext.Races.FirstOrDefault(r => r.Id == race.Id);
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
                        ArmoryDbContext.Races.Add(race);
                        hasChanges = true;
                    }
                }

                if (hasChanges) SaveChanges();
            }
        }

        Races = ArmoryDbContext.Races.ToList();
    }


    private void ReloadRaces()
    {
        Races.Clear();
        SetRaces(false);
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
                    var dbRealm = ArmoryDbContext.Realms.FirstOrDefault(r => r.Name == realm.Name);
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
                        ArmoryDbContext.Realms.Add(realm);
                        hasChanges = true;
                    }
                }

                if (hasChanges) SaveChanges();
            }
        }

        Realms = ArmoryDbContext.Realms.ToList();
    }

    private void ReloadRealms()
    {
        Realms.Clear();
        SetRealms(false);
    }

    private Character? GetCharacter(string realmName, string characterName)
    {
        try
        {
            var summary = GetCharacterProfileSummary(realmName, characterName);
            if (summary == null) return null;

            var characterProfileSummary = summary.Result;
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

            var summary2 = GetCharacterEquipmentSummary(realmName, characterName);
            if (summary2 != null)
            {
                var characterEquipmentSummary = summary2.Result;
                if (characterEquipmentSummary != null)
                {
                    character.EquippedItems = new List<EquippedItem>();
                    foreach (var equippedItem in characterEquipmentSummary.EquippedItems)
                    {
                        var dbEquippedItem = new EquippedItem
                        {
                            Item = GetItem(equippedItem.Item.Id, equippedItem.Name, equippedItem.Description,
                                equippedItem.Quality.Type, equippedItem.Slot.Type),
                            Name = equippedItem.Name,
                            Description = equippedItem.Description,
                            Quality = equippedItem.Quality.Type,
                            EnchantmentId = equippedItem.Enchantments != null
                                ? equippedItem.Enchantments[0].EnchantmentId
                                : 0,
                            Level = equippedItem.Level.Value,
                            Quantity = equippedItem.Quantity,
                            Slot = equippedItem.Slot.Type
                        };

                        character.EquippedItems.Add(dbEquippedItem);
                    }
                }
            }

            return character;
        }
        catch (Exception ex)
        {
            Console.WriteLine(new Exceptions.Character(characterName, realmName, ex));
        }

        return null;
    }

    private Guild? GetGuild(GuildReference? guildReference)
    {
        if (guildReference == null) return null;

        var guild = ArmoryDbContext.Guilds.Include(guild => guild.Realm)
            .FirstOrDefault(g => g.ArmoryId == guildReference.Id);
        if (guild != null) return guild;

        guild = new Guild
        {
            ArmoryId = guildReference.Id,
            Name = guildReference.Name,
            Realm = GetRealm(guildReference.Realm.Slug, guildReference.Realm.Name)
        };
        ArmoryDbContext.Guilds.Add(guild);

        return guild;
    }

    private Item GetItem(int itemId, string name, string description, string quality, string type)
    {
        var item = ArmoryDbContext.Items.FirstOrDefault(i => i.ItemId == itemId);
        if (item != null) return item;

        item = new Item
        {
            ItemId = itemId,
            Name = name,
            Description = description,
            Quality = quality,
            Type = type
        };
        ArmoryDbContext.Items.Add(item);

        return item;
    }

    private CharacterClass GetCharacterClass(PlayableClassReference classReferance)
    {
        SetCharacterClasses(false);
        var cClass = CharacterClasses.FirstOrDefault(r => r.Id == classReferance.Id);
        if (cClass != null) return cClass;

        cClass = new CharacterClass
        {
            Id = classReferance.Id,
            Name = classReferance.Name
        };
        ArmoryDbContext.CharacterClasses.Add(cClass);
        SaveChanges();

        ReloadCharacterClasses();
        return cClass;
    }

    private Race GetRace(PlayableRaceReference raceReferance)
    {
        SetRaces(false);
        var race = Races.FirstOrDefault(r => r.Id == raceReferance.Id);
        if (race != null) return race;

        race = new Race
        {
            Id = raceReferance.Id,
            Name = raceReferance.Name
        };
        ArmoryDbContext.Races.Add(race);
        SaveChanges();

        ReloadRaces();
        return race;
    }

    private Realm GetRealm(string name, string displayName)
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
        ArmoryDbContext.Realms.Add(realm);
        SaveChanges();

        ReloadRealms();
        return realm;
    }

    private async Task<List<CharacterClass>> GetCharacterClasses()
    {
        var charClasses = new List<CharacterClass>();

        var warcraftClient = GetWarcraftClient();
        // Retrieve the races de
        var result = await warcraftClient.GetPlayableClassesIndexAsync("static-eu",
            Region.Europe, Locale.de_DE);
        if (result.Success)
            charClasses.AddRange(result.Value.Classes.Select(playableClassReference => new CharacterClass
                { Id = playableClassReference.Id, Name = playableClassReference.Name }));

        // Retrieve the races en
        result = await warcraftClient.GetPlayableClassesIndexAsync("static-eu",
            Region.Europe, Locale.en_GB);
        if (result.Success)
            foreach (var playableClassReference in result.Value.Classes)
            {
                var characterClass = charClasses.FirstOrDefault(r => r.Id == playableClassReference.Id);
                if (characterClass != null) characterClass.NameEn = playableClassReference.Name;
            }

        // Retrieve the races fr
        result = await warcraftClient.GetPlayableClassesIndexAsync("static-eu",
            Region.Europe, Locale.fr_FR);
        if (result.Success)
            foreach (var playableClassReference in result.Value.Classes)
            {
                var characterClass = charClasses.FirstOrDefault(r => r.Id == playableClassReference.Id);
                if (characterClass != null) characterClass.NameFr = playableClassReference.Name;
            }

        return charClasses;
    }

    private async Task<List<Race>> GetRaces()
    {
        var races = new List<Race>();

        var warcraftClient = GetWarcraftClient();
        // Retrieve the races de
        var result = await warcraftClient.GetPlayableRacesIndexAsync("static-eu",
            Region.Europe, Locale.de_DE);
        if (result.Success)
            races.AddRange(result.Value.Races.Select(playableRaceReference =>
                new Race { Id = playableRaceReference.Id, Name = playableRaceReference.Name }));

        // Retrieve the races en
        result = await warcraftClient.GetPlayableRacesIndexAsync("static-eu",
            Region.Europe, Locale.en_GB);
        if (result.Success)
            foreach (var playableRaceReference in result.Value.Races)
            {
                var race = races.FirstOrDefault(r => r.Id == playableRaceReference.Id);
                if (race != null) race.NameEn = playableRaceReference.Name;
            }

        // Retrieve the races fr
        result = await warcraftClient.GetPlayableRacesIndexAsync("static-eu",
            Region.Europe, Locale.fr_FR);
        if (result.Success)
            foreach (var playableRaceReference in result.Value.Races)
            {
                var race = races.FirstOrDefault(r => r.Id == playableRaceReference.Id);
                if (race != null) race.NameFr = playableRaceReference.Name;
            }

        return races;
    }

    private async Task<List<Realm>> GetRealms()
    {
        var realms = new List<Realm>();

        var warcraftClient = GetWarcraftClient();
        // Retrieve the realms de
        var result = await warcraftClient.GetRealmsIndexAsync("dynamic-eu", Region.Europe,
            Locale.de_DE);
        if (result.Success)
            realms.AddRange(result.Value.Realms.Select(r => new Realm { Name = r.Slug, DisplayName = r.Name }));

        // Retrieve the realms en
        result = await warcraftClient.GetRealmsIndexAsync("dynamic-eu", Region.Europe,
            Locale.en_GB);
        if (result.Success)
            foreach (var realmReference in result.Value.Realms)
            {
                var realm = realms.FirstOrDefault(r => r.Name == realmReference.Slug);
                if (realm != null) realm.DisplayNameEn = realmReference.Name;
            }

        // Retrieve the realms fr
        result = await warcraftClient.GetRealmsIndexAsync("dynamic-eu", Region.Europe,
            Locale.fr_FR);
        if (result.Success)
            foreach (var realmReference in result.Value.Realms)
            {
                var realm = realms.FirstOrDefault(r => r.Name == realmReference.Slug);
                if (realm != null) realm.DisplayNameFr = realmReference.Name;
            }

        return realms;
    }

    private async Task<CharacterProfileSummary?> GetCharacterProfileSummary(
        string realmName, string characterName)
    {
        var warcraftClient = GetWarcraftClient();

        // Retrieve the character profile
        var result = await warcraftClient.GetCharacterProfileSummaryAsync(realmName.Replace(" ", "-").ToLower(),
            characterName.ToLower(), "profile-eu");
        return !result.Success ? null : result.Value;
    }

    private async Task<ArgentPonyWarcraftClient.Item?> GetArmoryItem(int itemId, string @namespace = "static-eu")
    {
        var warcraftClient = GetWarcraftClient();

        var result = await warcraftClient.GetItemAsync(itemId, @namespace);
        return !result.Success ? null : result.Value;
    }

    private async Task<CharacterEquipmentSummary?> GetCharacterEquipmentSummary(
        string realmName,
        string characterName)
    {
        var warcraftClient = GetWarcraftClient();

        var result = await warcraftClient.GetCharacterEquipmentSummaryAsync(realmName.Replace(" ", "-").ToLower(),
            characterName.ToLower(), "profile-eu");
        return !result.Success ? null : result.Value;
    }

    private WarcraftClient GetWarcraftClient()
    {
        if (WarcraftClientApi.ClientId == null) throw new NullReferenceException("ClientId not set.");
        if (WarcraftClientApi.ClientSecret == null) throw new NullReferenceException("ClientSecret not set.");

        return new WarcraftClient(
            WarcraftClientApi.ClientId,
            WarcraftClientApi.ClientSecret,
            Region.Europe,
            Locale.de_DE);
    }

    private bool UpdateDbCharacter(Character dbCharacter, Character character)
    {
        dbCharacter.Update(character);
        var characterChanged = dbCharacter.HasChanges;

        if (character is { Level: >= MaxCharacterLevel, EquippedItems: not null })
        {
            dbCharacter.EquippedItems?.Clear();
            foreach (var equippedItem in character.EquippedItems)
            {
                /*var dbCharacterEquippedItem = dbCharacter.EquippedItems?.FirstOrDefault(i => i.Slot == equippedItem.Slot);
                if (dbCharacterEquippedItem != null)
                {
                    if (dbCharacterEquippedItem.Name != equippedItem.Name)
                    {
                        dbCharacterEquippedItem.Name = equippedItem.Name;
                        dbCharacterEquippedItem.Description = equippedItem.Description;
                        dbCharacterEquippedItem.Item = GetItem(equippedItem.Item.ItemId, equippedItem.Name, equippedItem.Description);
                        characterChanged = true;
                    }

                    if (dbCharacterEquippedItem.Level != equippedItem.Level)
                    {
                        dbCharacterEquippedItem.Level = equippedItem.Level;
                        characterChanged = true;
                    }

                    if (dbCharacterEquippedItem.Quantity != equippedItem.Quantity)
                    {
                        dbCharacterEquippedItem.Quantity = equippedItem.Quantity;
                        characterChanged = true;
                    }
                }
                else
                {*/
                var dbEquippedItem = new EquippedItem
                {
                    Character = dbCharacter,
                    Item = GetItem(equippedItem.Item.ItemId, equippedItem.Name, equippedItem.Description,
                        equippedItem.Quality, equippedItem.Slot),
                    Name = equippedItem.Name,
                    Description = equippedItem.Description,
                    Level = equippedItem.Level,
                    Quality = equippedItem.Quality,
                    Quantity = equippedItem.Quantity,
                    Slot = equippedItem.Slot,
                    EnchantmentId = equippedItem.EnchantmentId
                };
                ArmoryDbContext.EquippedItems.Add(dbEquippedItem);
                //characterChanged = true;
                //}
            }

            characterChanged = true;
        }

        return characterChanged;
    }

    public Character? AddCharacter(string realmName, string characterName)
    {
        var dbCharacter = CharacterService.GetCharacter(characterName, realmName);
        if (dbCharacter != null) return dbCharacter;

        var character = GetCharacter(realmName, characterName);
        if (character == null) return null;

        dbCharacter = CharacterService.GetCharacter(character.Id);
        if (dbCharacter != null) return dbCharacter;

        //character.ArmoryId = guildRosterMember.Character.Id;
        //character.GuildRank = guildRosterMember.Rank;
        CharacterService.AddCharacter(character, false);
        return character;
    }

    public void UpdateGuild(Guild guild)
    {
        if (guild.Realm?.Name == null) return;

        var guildRosterTask = GetGuildRoster(guild.Realm?.Name, guild.Name);
        if (guildRosterTask == null) return;

        var guildRoster = guildRosterTask.Result;
        if (guildRoster == null) return;

        var characterToRemove = guild.Members.Where(c => guildRoster.Members.All(m => m.Character.Id != c.ArmoryId))
            .ToList();
        foreach (var character in characterToRemove) guild.Members.Remove(character);

        foreach (var guildRosterMember in guildRoster.Members)
        {
            var character = GetCharacter(guildRosterMember.Character.Realm.Slug, guildRosterMember.Character.Name);
            if (character == null) continue;
            character.ArmoryId = guildRosterMember.Character.Id;
            character.GuildRank = guildRosterMember.Rank;
            var dbCharacter = guild.Members.FirstOrDefault(c =>
                c.Name == character.Name && c.Realm != null && c.Realm.Name == character.Realm.Name);
            if (dbCharacter == null)
            {
                character.Guild = guild;
                CharacterService.AddCharacter(character, false);
            }
            else
            {
                UpdateDbCharacter(dbCharacter, character);
            }
        }

        SaveChanges();
    }

    private async Task<GuildRoster?> GetGuildRoster(string realmName, string guildName)
    {
        var warcraftClient = GetWarcraftClient();

        // Retrieve the character profile for Drinian of realm Norgannon.
        var result = await warcraftClient.GetGuildRosterAsync(realmName.Replace(" ", "-").ToLower(),
            guildName.Replace(" ", "-").ToLower(), "profile-eu");
        return !result.Success ? null : result.Value;
    }

    public Community? AddCommunity(string name)
    {
        var dbCommunity = ArmoryDbContext.Communities.FirstOrDefault(c => c.Name == name);
        if (dbCommunity != null) return dbCommunity;

        var community = new Community { Name = name };
        ArmoryDbContext.Communities.Add(community);
        SaveChanges();
        return ArmoryDbContext.Communities.FirstOrDefault(c => c.Id == community.Id);
    }

    public Item? AddItem(int itemId)
    {
        var dbItem = ArmoryDbContext.Items.FirstOrDefault(i => i.ItemId == itemId);
        if (dbItem != null) return dbItem;

        var itemTask = GetArmoryItem(itemId);
        if (itemTask == null) return null;

        var item = itemTask.Result;
        if (item == null) return null;

        var newItem = new Item
        {
            ItemId = itemId,
            Name = item.Name,
            Type = item.InventoryType.Type,
            Level = item.Level,
            Quality = item.Quality.Type,
            MaxCount = item.MaxCount,
            IsEquippable = item.IsEquippable,
            IsStackable = item.IsStackable,
            RequiredLevel = item.RequiredLevel,
            PurchasePrice = item.PurchasePrice,
            PurchaseQuantity = item.PurchaseQuantity,
            SellPrice = item.SellPrice
        };

        ArmoryDbContext.Items.Add(newItem);
        SaveChanges();
        return ArmoryDbContext.Items.FirstOrDefault(i => i.ItemId == itemId);
    }

    public void UpdateItems()
    {
        foreach (var dbItem in ArmoryDbContext.Items.Where(i => i.Type == null)) UpdateDbItem(dbItem);

        SaveChanges();
    }

    public void UpdateItem(Item item)
    {
        var dbItem = ArmoryDbContext.Items.FirstOrDefault(i => i.ItemId == item.ItemId);
        if (dbItem == null) return;

        if (UpdateDbItem(dbItem))
            SaveChanges();
    }

    private bool UpdateDbItem(Item dbItem)
    {
        var itemTask = GetArmoryItem(dbItem.ItemId);
        if (itemTask == null) return false;

        var armoryItem = itemTask.Result;
        if (armoryItem == null) return false;

        var hasChanged = false;
        if (dbItem.Name != armoryItem.Name)
        {
            dbItem.Name = armoryItem.Name;
            hasChanged = true;
        }

        if (dbItem.Type != armoryItem.InventoryType.Type)
        {
            dbItem.Type = armoryItem.InventoryType.Type;
            hasChanged = true;
        }

        if (dbItem.Quality != armoryItem.Quality.Type)
        {
            dbItem.Quality = armoryItem.Quality.Type;
            hasChanged = true;
        }

        if (dbItem.MaxCount != armoryItem.MaxCount)
        {
            dbItem.MaxCount = armoryItem.MaxCount;
            hasChanged = true;
        }

        if (dbItem.Level != armoryItem.Level)
        {
            dbItem.Level = armoryItem.Level;
            hasChanged = true;
        }

        if (dbItem.IsEquippable != armoryItem.IsEquippable)
        {
            dbItem.IsEquippable = armoryItem.IsEquippable;
            hasChanged = true;
        }

        if (dbItem.IsStackable != armoryItem.IsStackable)
        {
            dbItem.IsStackable = armoryItem.IsStackable;
            hasChanged = true;
        }

        if (dbItem.RequiredLevel != armoryItem.RequiredLevel)
        {
            dbItem.RequiredLevel = armoryItem.RequiredLevel;
            hasChanged = true;
        }

        if (dbItem.PurchasePrice != armoryItem.PurchasePrice)
        {
            dbItem.PurchasePrice = armoryItem.PurchasePrice;
            hasChanged = true;
        }

        if (dbItem.PurchaseQuantity != armoryItem.PurchaseQuantity)
        {
            dbItem.PurchaseQuantity = armoryItem.PurchaseQuantity;
            hasChanged = true;
        }

        if (dbItem.SellPrice != armoryItem.SellPrice)
        {
            dbItem.SellPrice = armoryItem.SellPrice;
            hasChanged = true;
        }

        return hasChanged;
    }

    private void LogChangedEntries(EntityState entityState)
    {
        var count = ArmoryDbContext.ChangeTracker.Entries().Count(e => e.State == entityState);
        switch (count)
        {
            case 1:
                Console.WriteLine($"{count} record {entityState}.");
                break;
            case > 1:
                Console.WriteLine($"{count} records {entityState}.");
                break;
        }
    }

    private void SaveChanges()
    {
        if (!ArmoryDbContext.ChangeTracker.HasChanges()) return;

        LogChangedEntries(EntityState.Added);
        LogChangedEntries(EntityState.Modified);
        LogChangedEntries(EntityState.Deleted);

        var count = ArmoryDbContext.SaveChanges();
        switch (count)
        {
            case 1:
                Console.WriteLine($"{count} record saved.");
                break;
            case > 1:
                Console.WriteLine($"{count} records saved.");
                break;
        }
    }

    public class StatusEventArg : EventArgs
    {
        public required string Text { get; set; }
        public Exception? Exception { get; set; }
        public int ProgressPercentage { get; set; }
    }
}