using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WoWArmory.Contracts.Models;

public class Character : BaseEntity
{
    public Character()
    {
        DisplayName = "";
    }

    public int ArmoryId { get; set; }
    public required string Name { get; set; } = string.Empty;

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public string DisplayName { get; }

    public required string RealmName { get; set; } = string.Empty;
    public int Level { get; set; }
    public virtual CharacterClass? CharacterClass { get; set; }
    public required string Faction { get; set; } = string.Empty;
    public required string Gender { get; set; } = string.Empty;
    public string? ActiveTitle { get; set; }
    public int AverageItemLevel { get; set; }
    public int EquippedItemLevel { get; set; }
    public int AchievementPoints { get; set; }
    public int Experience { get; set; }
    public int? GuildRank { get; set; }
    public virtual Race? Race { get; set; }
    [JsonIgnore] public virtual User? User { get; set; }

    public DateTimeOffset LastLoginTimestamp { get; set; }

    public DateTime? LastModified { get; set; }

    public int AverageItemLevelDifference
    {
        get
        {
            var charHist = GetLastRaidCharacterHistory(Raids?.FirstOrDefault());
            if (charHist != null)
                return AverageItemLevel - charHist.AverageItemLevel;
            return 0;
        }
    }

    public int EquippedItemLevelDifference
    {
        get
        {
            if (LastLoginTimestamp < DateTimeOffset.UtcNow.AddDays(-10)) return 0;
            var charHist = GetLastRaidCharacterHistory(Raids?.FirstOrDefault());
            if (charHist != null)
                return EquippedItemLevel - charHist.EquippedItemLevel;
            return 0;
        }
    }

    [JsonIgnore] public virtual Guild? Guild { get; set; }

    [JsonIgnore] public virtual List<Raid>? Raids { get; set; }

    [JsonIgnore] public virtual List<Community>? Communities { get; set; }
    [JsonIgnore] public virtual List<EquippedItem>? EquippedItems { get; set; }

    public virtual Realm? Realm { get; set; }

    [JsonIgnore] public virtual List<CharacterHistory>? CharacterHistories { get; set; }

    private CharacterHistory? GetLastRaidCharacterHistory(Raid? raid)
    {
        var lastRaidDay = GetLastRaidDay(raid);
        return CharacterHistories != null
            ? CharacterHistories.Where(c => c.LastLoginTimestamp < lastRaidDay)
                .OrderByDescending(c => c.LastLoginTimestamp).FirstOrDefault()
            : null;
    }

    private DateTime GetLastRaidDay(Raid? raid)
    {
        var raidTime = new TimeOnly(DateTime.Now.Hour, DateTime.Now.Month, 0);
        var lastRaidDay = DateTime.Now.AddDays(-7);

        if (raid != null)
        {
            var raidDay = raid.RaidDay;
            raidTime = raid.RaidTime;
            lastRaidDay = DateTime.Now.AddDays(-1);
            while (lastRaidDay.DayOfWeek != raidDay)
                lastRaidDay = lastRaidDay.AddDays(-1);
        }

        return new DateTime(lastRaidDay.Year, lastRaidDay.Month, lastRaidDay.Day, raidTime.Hour, raidTime.Minute,
            raidTime.Second);
    }

    public void Update(Character character)
    {
        if (ArmoryId != character.ArmoryId)
        {
            ArmoryId = character.ArmoryId;
            HasChanges = true;
        }

        if (AchievementPoints != character.AchievementPoints)
        {
            AchievementPoints = character.AchievementPoints;
            HasChanges = true;
        }

        if (ActiveTitle != character.ActiveTitle)
        {
            ActiveTitle = character.ActiveTitle;
            HasChanges = true;
        }

        if (AverageItemLevel != character.AverageItemLevel)
        {
            AverageItemLevel = character.AverageItemLevel;
            HasChanges = true;
        }

        if (CharacterClass != character.CharacterClass)
        {
            CharacterClass = character.CharacterClass;
            HasChanges = true;
        }

        if (EquippedItemLevel != character.EquippedItemLevel)
        {
            EquippedItemLevel = character.EquippedItemLevel;
            HasChanges = true;
        }

        if (Experience != character.Experience)
        {
            Experience = character.Experience;
            HasChanges = true;
        }

        if (Faction != character.Faction)
        {
            Faction = character.Faction;
            HasChanges = true;
        }

        if (Gender != character.Gender)
        {
            Gender = character.Gender;
            HasChanges = true;
        }

        if (character.GuildRank != null && GuildRank != character.GuildRank)
        {
            GuildRank = character.GuildRank;
            HasChanges = true;
        }

        if (Guild != character.Guild)
        {
            Guild = character.Guild;
            HasChanges = true;
        }

        if (LastLoginTimestamp != character.LastLoginTimestamp)
        {
            LastLoginTimestamp = character.LastLoginTimestamp;
            HasChanges = true;
        }

        if (Level != character.Level)
        {
            Level = character.Level;
            HasChanges = true;
        }

        if (Race != character.Race)
        {
            Race = character.Race;
            HasChanges = true;
        }

        if (Realm != character.Realm)
        {
            Realm = character.Realm;
            HasChanges = true;
        }

        LastModified = DateTime.UtcNow;
    }

    public override string ToString()
    {
        return $"{Name}-{Realm?.DisplayName} [{Level}] GS {EquippedItemLevel} AchievementPoints {AchievementPoints}";
    }
}