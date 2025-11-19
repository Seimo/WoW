using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WoWArmory.Contracts.Models;

public class Race
{
    [Key] public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameFr { get; set; } = string.Empty;

    [JsonIgnore] public virtual List<Character> Characters { get; set; } = [];
}