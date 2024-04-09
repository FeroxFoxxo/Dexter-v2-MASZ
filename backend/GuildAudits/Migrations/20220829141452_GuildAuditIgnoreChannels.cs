using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuildAudits.Migrations;

public partial class GuildAuditIgnoreChannels : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "GuildAuditEvent",
            schema: "GuildAudits",
            table: "GuildAuditConfigs",
            newName: "GuildAuditLogEvent");

        migrationBuilder.AddColumn<string>(
            name: "IgnoreChannels",
            schema: "GuildAudits",
            table: "GuildAuditConfigs",
            type: "longtext",
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddColumn<string>(
            name: "IgnoreRoles",
            schema: "GuildAudits",
            table: "GuildAuditConfigs",
            type: "longtext",
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IgnoreChannels",
            schema: "GuildAudits",
            table: "GuildAuditConfigs");

        migrationBuilder.DropColumn(
            name: "IgnoreRoles",
            schema: "GuildAudits",
            table: "GuildAuditConfigs");

        migrationBuilder.RenameColumn(
            name: "GuildAuditLogEvent",
            schema: "GuildAudits",
            table: "GuildAuditConfigs",
            newName: "GuildAuditEvent");
    }
}
