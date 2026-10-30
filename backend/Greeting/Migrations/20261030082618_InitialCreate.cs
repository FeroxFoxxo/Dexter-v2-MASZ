using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Greeting.Migrations
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
                name: "GreeterConfigs",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    AllowedGreetChannels = table.Column<string>(type: "longtext", nullable: true),
                    AllowedGreetRoles = table.Column<string>(type: "longtext", nullable: true),
                    DisallowedMuteRoles = table.Column<string>(type: "longtext", nullable: true),
                    DisallowedMuteExistence = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    PunishmentTime = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    LoggingChannel = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GreeterConfigs", x => x.GuildId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GreeterConfigs");
        }
    }
}
