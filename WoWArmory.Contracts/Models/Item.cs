using System.Text.Json.Serialization;

namespace WoWArmory.Contracts.Models;

public class Item : BaseEntity
{
    [JsonIgnore] public virtual List<EquippedItem>? EquippedItems { get; set; }
    public int ItemId { get; set; }

    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public int Level { get; set; }
    public int RequiredLevel { get; set; }
    public long PurchasePrice { get; set; }
    public int PurchaseQuantity { get; set; }
    public long SellPrice { get; set; }
    public required string Quality { get; set; }
    public int MaxCount { get; set; }
    public bool IsEquippable { get; set; }
    public bool IsStackable { get; set; }
}