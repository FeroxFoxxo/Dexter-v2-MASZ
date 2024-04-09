using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoMods.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "AutoMods");

        migrationBuilder.AlterDatabase()
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "AutoModConfigs",
            schema: "AutoMods",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                GuildId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                AutoModType = table.Column<int>(type: "int", nullable: false),
                AutoModAction = table.Column<int>(type: "int", nullable: false),
                PunishmentType = table.Column<int>(type: "int", nullable: true),
                PunishmentDurationMinutes = table.Column<int>(type: "int", nullable: true),
                IgnoreChannels = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                IgnoreRoles = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                TimeLimitMinutes = table.Column<int>(type: "int", nullable: true),
                Limit = table.Column<int>(type: "int", nullable: true),
                CustomWordFilter = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                ChannelNotificationBehavior = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AutoModConfigs", x => x.Id))
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "AutoModEvents",
            schema: "AutoMods",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                GuildId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                AutoModType = table.Column<int>(type: "int", nullable: false),
                AutoModAction = table.Column<int>(type: "int", nullable: false),
                UserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                Username = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Nickname = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Discriminator = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                MessageId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                MessageContent = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                AssociatedCaseId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_AutoModEvents", x => x.Id))
            .Annotation("MySql:CharSet", "utf8mb4");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AutoModConfigs",
            schema: "AutoMods");

        migrationBuilder.DropTable(
            name: "AutoModEvents",
            schema: "AutoMods");
    }
}
