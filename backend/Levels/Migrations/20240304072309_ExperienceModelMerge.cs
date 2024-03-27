using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Levels.Migrations;

/// <inheritdoc />
public partial class ExperienceModelMerge : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MaximumTextXpGiven",
            schema: "Levels",
            table: "GuildLevelConfigs");

        migrationBuilder.DropColumn(
            name: "MaximumVoiceXpGiven",
            schema: "Levels",
            table: "GuildLevelConfigs");

        migrationBuilder.DropColumn(
            name: "MinimumTextXpGiven",
            schema: "Levels",
            table: "GuildLevelConfigs");

        migrationBuilder.DropColumn(
            name: "MinimumVoiceXpGiven",
            schema: "Levels",
            table: "GuildLevelConfigs");

        migrationBuilder.AddColumn<string>(
            name: "Experience",
            schema: "Levels",
            table: "GuildLevelConfigs",
            type: "longtext",
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddColumn<string>(
            name: "ExperienceOverrides",
            schema: "Levels",
            table: "GuildLevelConfigs",
            type: "longtext",
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Experience",
            schema: "Levels",
            table: "GuildLevelConfigs");

        migrationBuilder.DropColumn(
            name: "ExperienceOverrides",
            schema: "Levels",
            table: "GuildLevelConfigs");

        migrationBuilder.AddColumn<int>(
            name: "MaximumTextXpGiven",
            schema: "Levels",
            table: "GuildLevelConfigs",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "MaximumVoiceXpGiven",
            schema: "Levels",
            table: "GuildLevelConfigs",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "MinimumTextXpGiven",
            schema: "Levels",
            table: "GuildLevelConfigs",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "MinimumVoiceXpGiven",
            schema: "Levels",
            table: "GuildLevelConfigs",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }
}
