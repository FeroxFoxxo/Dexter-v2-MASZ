using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Punishments.Migrations;

public partial class TemplateSeverityType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "HighSeverity",
            schema: "Punishments",
            table: "ModCaseTemplates");

        migrationBuilder.AddColumn<int>(
            name: "CaseSeverityType",
            schema: "Punishments",
            table: "ModCaseTemplates",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CaseSeverityType",
            schema: "Punishments",
            table: "ModCaseTemplates");

        migrationBuilder.AddColumn<bool>(
            name: "HighSeverity",
            schema: "Punishments",
            table: "ModCaseTemplates",
            type: "tinyint(1)",
            nullable: true);
    }
}
