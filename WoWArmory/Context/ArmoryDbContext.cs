using Microsoft.EntityFrameworkCore;
using WoWArmory.Contracts.Models;

namespace WoWArmory.Context;

public class ArmoryDbContext : DbContext
{
    public ArmoryDbContext()
    {
    }

    public ArmoryDbContext(DbContextOptions<ArmoryDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserGroup> UserGroups { get; set; }
    public DbSet<Guild> Guilds { get; set; }
    public DbSet<Raid> Raids { get; set; }
    public DbSet<RaidLog> RaidLogs { get; set; }
    public DbSet<Community> Communities { get; set; }
    public DbSet<Character> Characters { get; set; }
    public DbSet<Race> Races { get; set; }
    public DbSet<CharacterClass> CharacterClasses { get; set; }
    public DbSet<CharacterHistory> CharacterHistories { get; set; }
    public DbSet<Realm> Realms { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<EquippedItem> EquippedItems { get; set; }
    public DbSet<MetaData> MetaDatas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Character>()
            .Property(b => b.DisplayName)
            .HasComputedColumnSql(
                "CASE WHEN \"ActiveTitle\" IS NOT NULL THEN regexp_replace(\"ActiveTitle\", '{name}', \"Name\") ELSE \"Name\" END",
                true);

        modelBuilder.Entity<CharacterHistory>()
            .Property(b => b.DisplayName)
            .HasComputedColumnSql(
                "CASE WHEN \"ActiveTitle\" IS NOT NULL THEN regexp_replace(\"ActiveTitle\", '{name}', \"Name\") ELSE \"Name\" END",
                true);
    }
}