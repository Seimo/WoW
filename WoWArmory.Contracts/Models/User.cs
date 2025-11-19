using System.Text.Json.Serialization;

namespace WoWArmory.Contracts.Models;

public class User : BaseEntity
{
    public required string Name { get; set; }

    public required string Password { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? WoWAccessToken { get; set; }
    public string? Email { get; set; }
    public DateTime? LastLogin { get; set; }

    public virtual List<Character> Characters { get; set; } = [];

    [JsonIgnore] public virtual List<UserGroup> UserGroups { get; set; } = [];
    [JsonIgnore] public virtual List<Raid> Raids { get; set; } = [];
    public Guid MainCharacterId { get; set; }
    public Guid MainGuildId { get; set; }
    public Guid MainRaidId { get; set; }
}