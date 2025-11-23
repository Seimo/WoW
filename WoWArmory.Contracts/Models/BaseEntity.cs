using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WoWArmory.Contracts.Models;

public class BaseEntity
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();

    [NotMapped] [JsonIgnore] public bool HasChanges { get; set; }
}