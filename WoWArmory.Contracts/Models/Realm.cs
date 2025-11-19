using System.Text.Json.Serialization;

namespace WoWArmory.Contracts.Models;

public class Realm : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DisplayNameEn { get; set; } = string.Empty;
    public string DisplayNameFr { get; set; } = string.Empty;
    [JsonIgnore] public virtual List<Character> Characters { get; set; } = new();
}