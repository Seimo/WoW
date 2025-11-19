using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WoWArmory.Migrations
{
    /// <inheritdoc />
    public partial class User_AddMains : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MainCharacterId",
                table: "Users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "MainGuildId",
                table: "Users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "MainRaidId",
                table: "Users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MainCharacterId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MainGuildId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MainRaidId",
                table: "Users");
        }
    }
}
