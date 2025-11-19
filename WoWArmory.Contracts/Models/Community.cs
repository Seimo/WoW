namespace WoWArmory.Contracts.Models;

public class Community : BaseEntity
{
    public required string Name { get; set; }
    public virtual List<Character> Characters { get; set; } = new();

    public override string ToString()
    {
        return $"{Name} {Characters.Count} Members.";
    }
}