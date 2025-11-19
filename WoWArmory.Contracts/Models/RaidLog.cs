using System.Text.Json.Serialization;

namespace WoWArmory.Contracts.Models;

public class RaidLog : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string WarcraftLogsCode { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public Uri Url { get; set; }
    [JsonIgnore] public virtual Raid Raid { get; set; }
}