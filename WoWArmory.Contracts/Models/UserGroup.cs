namespace WoWArmory.Contracts.Models;

public class UserGroup : BaseEntity
{
    public required string Name { get; set; }

    public virtual List<User> Users { get; set; } = [];
}