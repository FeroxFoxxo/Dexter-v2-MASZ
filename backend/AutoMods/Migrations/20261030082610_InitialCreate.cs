using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace AutoMods.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AutoModConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    GuildId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    AutoModType = table.Column<int>(type: "int", nullable: false),
                    AutoModAction = table.Column<int>(type: "int", nullable: false),
                    PunishmentType = table.Column<int>(type: "int", nullable: true),
                    PunishmentDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    IgnoreChannels = table.Column<string>(type: "longtext", nullable: true),
                    IgnoreRoles = table.Column<string>(type: "longtext", nullable: true),
                    TimeLimitMinutes = table.Column<int>(type: "int", nullable: true),
                    Limit = table.Column<int>(type: "int", nullable: true),
                    CustomWordFilter = table.Column<string>(type: "longtext", nullable: true),
                    ChannelNotificationBehavior = table.Column<int>(type: "int", nullable: false),
                    SendDmNotification = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoModConfigs", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AutoModEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    GuildId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    AutoModType = table.Column<int>(type: "int", nullable: false),
                    AutoModAction = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Username = table.Column<string>(type: "longtext", nullable: true),
                    Nickname = table.Column<string>(type: "longtext", nullable: true),
                    MessageId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    MessageContent = table.Column<string>(type: "longtext", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AssociatedCaseId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoModEvents", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutoModConfigs");

            migrationBuilder.DropTable(
                name: "AutoModEvents");
        }
    }
}
