using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bot.Migrations;

public partial class AddHTTPSetting : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ServiceBaseUrl",
            schema: "Bot",
            table: "AppSettings");

        migrationBuilder.AddColumn<int>(
            name: "EncryptionType",
            schema: "Bot",
            table: "AppSettings",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EncryptionType",
            schema: "Bot",
            table: "AppSettings");

        migrationBuilder.AddColumn<string>(
            name: "ServiceBaseUrl",
            schema: "Bot",
            table: "AppSettings",
            type: "longtext",
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");
    }
}
