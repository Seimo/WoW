using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WoWArmory.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterClasses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    NameEn = table.Column<string>(type: "text", nullable: false),
                    NameFr = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterClasses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Communities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Communities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    RequiredLevel = table.Column<int>(type: "integer", nullable: false),
                    PurchasePrice = table.Column<long>(type: "bigint", nullable: false),
                    PurchaseQuantity = table.Column<int>(type: "integer", nullable: false),
                    SellPrice = table.Column<long>(type: "bigint", nullable: false),
                    Quality = table.Column<string>(type: "text", nullable: false),
                    MaxCount = table.Column<int>(type: "integer", nullable: false),
                    IsEquippable = table.Column<bool>(type: "boolean", nullable: false),
                    IsStackable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MetaDatas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetaDatas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Races",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NameEn = table.Column<string>(type: "text", nullable: false),
                    NameFr = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Races", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Raids",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    MinItemLevel = table.Column<int>(type: "integer", nullable: false),
                    RaidDay = table.Column<int>(type: "integer", nullable: false),
                    RaidTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Raids", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Realms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    DisplayNameEn = table.Column<string>(type: "text", nullable: false),
                    DisplayNameFr = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Realms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: true),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    WoWAccessToken = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    LastLogin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RaidLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    WarcraftLogsCode = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    RaidId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaidLogs_Raids_RaidId",
                        column: x => x.RaidId,
                        principalTable: "Raids",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Guilds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ArmoryId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Visible = table.Column<bool>(type: "boolean", nullable: false),
                    RealmId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guilds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Guilds_Realms_RealmId",
                        column: x => x.RealmId,
                        principalTable: "Realms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RaidUser",
                columns: table => new
                {
                    RaidsId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsersId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidUser", x => new { x.RaidsId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_RaidUser_Raids_RaidsId",
                        column: x => x.RaidsId,
                        principalTable: "Raids",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RaidUser_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserUserGroup",
                columns: table => new
                {
                    UserGroupsId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsersId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserUserGroup", x => new { x.UserGroupsId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_UserUserGroup_UserGroups_UserGroupsId",
                        column: x => x.UserGroupsId,
                        principalTable: "UserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserUserGroup_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ArmoryId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false, computedColumnSql: "CASE WHEN \"ActiveTitle\" IS NOT NULL THEN regexp_replace(\"ActiveTitle\", '{name}', \"Name\") ELSE \"Name\" END", stored: true),
                    RealmName = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    CharacterClassId = table.Column<int>(type: "integer", nullable: true),
                    Faction = table.Column<string>(type: "text", nullable: false),
                    Gender = table.Column<string>(type: "text", nullable: false),
                    ActiveTitle = table.Column<string>(type: "text", nullable: true),
                    AverageItemLevel = table.Column<int>(type: "integer", nullable: false),
                    EquippedItemLevel = table.Column<int>(type: "integer", nullable: false),
                    AchievementPoints = table.Column<int>(type: "integer", nullable: false),
                    Experience = table.Column<int>(type: "integer", nullable: false),
                    GuildRank = table.Column<int>(type: "integer", nullable: true),
                    RaceId = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastLoginTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: true),
                    RealmId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Characters_CharacterClasses_CharacterClassId",
                        column: x => x.CharacterClassId,
                        principalTable: "CharacterClasses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Characters_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Characters_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Characters_Realms_RealmId",
                        column: x => x.RealmId,
                        principalTable: "Realms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Characters_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CharacterCommunity",
                columns: table => new
                {
                    CharactersId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommunitiesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterCommunity", x => new { x.CharactersId, x.CommunitiesId });
                    table.ForeignKey(
                        name: "FK_CharacterCommunity_Characters_CharactersId",
                        column: x => x.CharactersId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterCommunity_Communities_CommunitiesId",
                        column: x => x.CommunitiesId,
                        principalTable: "Communities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ArmoryId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false, computedColumnSql: "CASE WHEN \"ActiveTitle\" IS NOT NULL THEN regexp_replace(\"ActiveTitle\", '{name}', \"Name\") ELSE \"Name\" END", stored: true),
                    RealmName = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    CharacterClassId = table.Column<int>(type: "integer", nullable: true),
                    Faction = table.Column<string>(type: "text", nullable: false),
                    Gender = table.Column<string>(type: "text", nullable: false),
                    ActiveTitle = table.Column<string>(type: "text", nullable: true),
                    AverageItemLevel = table.Column<int>(type: "integer", nullable: false),
                    EquippedItemLevel = table.Column<int>(type: "integer", nullable: false),
                    AchievementPoints = table.Column<int>(type: "integer", nullable: false),
                    Experience = table.Column<int>(type: "integer", nullable: false),
                    RaceId = table.Column<int>(type: "integer", nullable: true),
                    LastLoginTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterHistories_CharacterClasses_CharacterClassId",
                        column: x => x.CharacterClassId,
                        principalTable: "CharacterClasses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CharacterHistories_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterHistories_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CharacterRaid",
                columns: table => new
                {
                    CharactersId = table.Column<Guid>(type: "uuid", nullable: false),
                    RaidsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterRaid", x => new { x.CharactersId, x.RaidsId });
                    table.ForeignKey(
                        name: "FK_CharacterRaid_Characters_CharactersId",
                        column: x => x.CharactersId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterRaid_Raids_RaidsId",
                        column: x => x.RaidsId,
                        principalTable: "Raids",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EquippedItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Level = table.Column<float>(type: "real", nullable: false),
                    Quality = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Slot = table.Column<string>(type: "text", nullable: false),
                    EnchantmentId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquippedItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquippedItems_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EquippedItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterCommunity_CommunitiesId",
                table: "CharacterCommunity",
                column: "CommunitiesId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterHistories_CharacterClassId",
                table: "CharacterHistories",
                column: "CharacterClassId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterHistories_CharacterId",
                table: "CharacterHistories",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterHistories_RaceId",
                table: "CharacterHistories",
                column: "RaceId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRaid_RaidsId",
                table: "CharacterRaid",
                column: "RaidsId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_CharacterClassId",
                table: "Characters",
                column: "CharacterClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_GuildId",
                table: "Characters",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_RaceId",
                table: "Characters",
                column: "RaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_RealmId",
                table: "Characters",
                column: "RealmId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_UserId",
                table: "Characters",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EquippedItems_CharacterId",
                table: "EquippedItems",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_EquippedItems_ItemId",
                table: "EquippedItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_RealmId",
                table: "Guilds",
                column: "RealmId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidLogs_RaidId",
                table: "RaidLogs",
                column: "RaidId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidUser_UsersId",
                table: "RaidUser",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_UserUserGroup_UsersId",
                table: "UserUserGroup",
                column: "UsersId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterCommunity");

            migrationBuilder.DropTable(
                name: "CharacterHistories");

            migrationBuilder.DropTable(
                name: "CharacterRaid");

            migrationBuilder.DropTable(
                name: "EquippedItems");

            migrationBuilder.DropTable(
                name: "MetaDatas");

            migrationBuilder.DropTable(
                name: "RaidLogs");

            migrationBuilder.DropTable(
                name: "RaidUser");

            migrationBuilder.DropTable(
                name: "UserUserGroup");

            migrationBuilder.DropTable(
                name: "Communities");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Raids");

            migrationBuilder.DropTable(
                name: "UserGroups");

            migrationBuilder.DropTable(
                name: "CharacterClasses");

            migrationBuilder.DropTable(
                name: "Guilds");

            migrationBuilder.DropTable(
                name: "Races");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Realms");
        }
    }
}
