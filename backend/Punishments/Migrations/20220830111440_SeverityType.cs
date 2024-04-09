using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Punishments.Migrations;

public partial class SeverityType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "HighSeverity",
            schema: "Punishments",
            table: "ModCases");

        migrationBuilder.AddColumn<int>(
            name: "Severity",
            schema: "Punishments",
            table: "ModCases",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Severity",
            schema: "Punishments",
            table: "ModCases");

        migrationBuilder.AddColumn<bool>(
            name: "HighSeverity",
            schema: "Punishments",
            table: "ModCases",
            type: "tinyint(1)",
            nullable: true);
    }
}
