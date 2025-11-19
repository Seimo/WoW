using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WoWArmory.Contracts.Models;

public class CharacterClass
{
    [Key] public int Id { get; set; }
    public string? Name { get; set; }
    public string NameEn { get; set; } = "";
    public string NameFr { get; set; } = "";

    [JsonIgnore] public virtual List<Character> Characters { get; set; } = [];
}