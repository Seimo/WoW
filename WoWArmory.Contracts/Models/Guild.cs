namespace WoWArmory.Contracts.Models;

public class Guild : BaseEntity
{
    public int ArmoryId { get; set; }
    public required string Name { get; set; }
    public bool Visible { get; set; }
    public virtual Realm? Realm { get; set; }
    public virtual List<Character> Members { get; set; } = [];

    public override string ToString()
    {
        return $"{Name} {Members.Count} Members.";
    }
}