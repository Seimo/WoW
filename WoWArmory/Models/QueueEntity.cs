using WoWArmory.Contracts.Models;

namespace WoWArmory.Models;

public class QueueEntity : BaseEntity
{
    public enum EntityTypeEnum
    {
        Character = 0,
        Guild = 1
    }
    
    public EntityTypeEnum EntityType { get; set; }

    public bool UpdateReferences { get; set; }
    
    public DateTime QueueStart { get; set; }
}