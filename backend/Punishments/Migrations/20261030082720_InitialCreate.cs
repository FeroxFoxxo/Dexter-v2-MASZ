using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Punishments.Migrations
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
                name: "ModCases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    GuildId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: true),
                    Description = table.Column<string>(type: "longtext", nullable: true),
                    UserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Username = table.Column<string>(type: "longtext", nullable: true),
                    Nickname = table.Column<string>(type: "longtext", nullable: true),
                    ModId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastEditedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastEditedByModId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Labels = table.Column<string>(type: "longtext", nullable: true),
                    Others = table.Column<string>(type: "longtext", nullable: true),
                    Valid = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    CreationType = table.Column<int>(type: "int", nullable: false),
                    PunishmentType = table.Column<int>(type: "int", nullable: false),
                    PunishedUntil = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PunishmentActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AllowComments = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LockedByUserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    MarkedToDeleteAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedByUserId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModCases", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ModCaseTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    TemplateName = table.Column<string>(type: "longtext", nullable: true),
                    CreatedForGuildId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ViewPermission = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CaseTitle = table.Column<string>(type: "longtext", nullable: true),
                    CaseDescription = table.Column<string>(type: "longtext", nullable: true),
                    CaseLabels = table.Column<string>(type: "longtext", nullable: true),
                    CasePunishmentType = table.Column<int>(type: "int", nullable: false),
                    CaseSeverityType = table.Column<int>(type: "int", nullable: false),
                    CasePunishedUntil = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    HandlePunishment = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModCaseTemplates", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PunishmentConfig",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    FinalWarnMuteTime = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    PointMuteTimes = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PunishmentConfig", x => x.GuildId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ModCaseComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ModCaseId = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "longtext", nullable: true),
                    UserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModCaseComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModCaseComments_ModCases_ModCaseId",
                        column: x => x.ModCaseId,
                        principalTable: "ModCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ModCaseComments_ModCaseId",
                table: "ModCaseComments",
                column: "ModCaseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModCaseComments");

            migrationBuilder.DropTable(
                name: "ModCaseTemplates");

            migrationBuilder.DropTable(
                name: "PunishmentConfig");

            migrationBuilder.DropTable(
                name: "ModCases");
        }
    }
}
