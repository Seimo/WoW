namespace WoWArmory.Contracts.Models;

public class MetaData : BaseEntity
{
    public enum KeyEnums
    {
        SyncFromArmory
    }

    public KeyEnums Key { get; set; }
    public string Value { get; set; } = "";
}