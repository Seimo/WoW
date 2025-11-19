using System.Text.Json.Serialization;

namespace WoWArmory.Contracts.Models;

public class EquippedItem : BaseEntity
{
    public virtual Item Item { get; set; }
    [JsonIgnore] public virtual Character Character { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public float Level { get; set; }
    public required string Quality { get; set; }
    public int Quantity { get; set; }
    public required string Slot { get; set; }
    public int EnchantmentId { get; set; }
}