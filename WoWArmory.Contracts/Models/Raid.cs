using System.Text.Json.Serialization;

namespace WoWArmory.Contracts.Models;

public class Raid : BaseEntity
{
    public required string Name { get; set; }
    public int MinItemLevel { get; set; } = 0;
    public DayOfWeek RaidDay { get; set; } = DayOfWeek.Friday;
    public TimeOnly RaidTime { get; set; } = new(20, 0, 0);
    public virtual List<Character> Characters { get; set; } = new();
    public virtual List<RaidLog> RaidLogs { get; set; } = new();
    [JsonIgnore] public virtual List<User> Users { get; set; } = [];

    public override string ToString()
    {
        return $"{Name} {Characters.Count} Members.";
    }
}