using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WoWArmory.Contracts.Models;

public sealed class CharacterHistory : BaseEntity
{
    public CharacterHistory()
    {
        Character = new Character
        {
            Name = Name,
            RealmName = RealmName,
            Faction = Faction,
            Gender = Gender
        };
    }

    public CharacterHistory(Character character)
    {
        Character = character;
        AchievementPoints = character.AchievementPoints;
        ActiveTitle = character.ActiveTitle;
        ArmoryId = character.ArmoryId;
        AverageItemLevel = character.AverageItemLevel;
        CharacterClass = character.CharacterClass;
        EquippedItemLevel = character.EquippedItemLevel;
        Experience = character.Experience;
        Faction = character.Faction;
        Gender = character.Gender;
        //GuildRank = character.GuildRank;
        LastLoginTimestamp = character.LastLoginTimestamp;
        Level = character.Level;
        Name = character.Name;
        Race = character.Race;
        RealmName = character.Realm != null ? character.Realm.Name : "";
        LastModified = DateTime.UtcNow;
    }

    public int ArmoryId { get; set; }
    public string Name { get; set; } = string.Empty;

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public string DisplayName { get; } = string.Empty;

    public string RealmName { get; set; } = string.Empty;
    public int Level { get; set; }
    public CharacterClass? CharacterClass { get; set; }
    public string Faction { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string? ActiveTitle { get; set; }
    public int AverageItemLevel { get; set; }
    public int EquippedItemLevel { get; set; }
    public int AchievementPoints { get; set; }
    public int Experience { get; set; }
    public Race? Race { get; set; }

    public DateTimeOffset LastLoginTimestamp { get; set; }

    public DateTime? LastModified { get; set; }

    [JsonIgnore] public Character Character { get; set; }

    public override string ToString()
    {
        return $"{Name}-{RealmName} [{Level}] GS {EquippedItemLevel} AchievementPoints {AchievementPoints}";
    }
}