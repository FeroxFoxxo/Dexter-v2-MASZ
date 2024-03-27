using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoleReactions.Migrations;

/// <inheritdoc />
public partial class UserRoleTiedToMenu : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "PK_UserRoles",
            schema: "RoleReactions",
            table: "UserRoles");

        migrationBuilder.AddColumn<ulong>(
            name: "ChannelId",
            schema: "RoleReactions",
            table: "UserRoles",
            type: "bigint unsigned",
            nullable: false,
            defaultValue: 0ul);

        migrationBuilder.AddColumn<int>(
            name: "Id",
            schema: "RoleReactions",
            table: "UserRoles",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddPrimaryKey(
            name: "PK_UserRoles",
            schema: "RoleReactions",
            table: "UserRoles",
            columns: ["GuildId", "ChannelId", "Id", "UserId"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "PK_UserRoles",
            schema: "RoleReactions",
            table: "UserRoles");

        migrationBuilder.DropColumn(
            name: "ChannelId",
            schema: "RoleReactions",
            table: "UserRoles");

        migrationBuilder.DropColumn(
            name: "Id",
            schema: "RoleReactions",
            table: "UserRoles");

        migrationBuilder.AddPrimaryKey(
            name: "PK_UserRoles",
            schema: "RoleReactions",
            table: "UserRoles",
            columns: ["GuildId", "UserId"]);
    }
}
