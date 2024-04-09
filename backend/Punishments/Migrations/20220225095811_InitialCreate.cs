using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Punishments.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "Punishments");

        migrationBuilder.AlterDatabase()
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "ModCases",
            schema: "Punishments",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                CaseId = table.Column<int>(type: "int", nullable: false),
                GuildId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                Title = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Description = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                UserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                Username = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Discriminator = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Nickname = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                ModId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                HighSeverity = table.Column<bool>(type: "tinyint(1)", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                OccurredAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                LastEditedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                LastEditedByModId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                Labels = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Others = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
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
            constraints: table => table.PrimaryKey("PK_ModCases", x => x.Id))
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "ModCaseTemplates",
            schema: "Punishments",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                UserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                TemplateName = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                CreatedForGuildId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                ViewPermission = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                CaseTitle = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                CaseDescription = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                CaseLabels = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                CasePunishmentType = table.Column<int>(type: "int", nullable: false),
                CasePunishedUntil = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                HandlePunishment = table.Column<bool>(type: "tinyint(1)", nullable: false),
                HighSeverity = table.Column<bool>(type: "tinyint(1)", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_ModCaseTemplates", x => x.Id))
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "ModCaseComments",
            schema: "Punishments",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                ModCaseId = table.Column<int>(type: "int", nullable: false),
                Message = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                UserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ModCaseComments", x => x.Id);
                table.ForeignKey(
                    name: "FK_ModCaseComments_ModCases_ModCaseId",
                    column: x => x.ModCaseId,
                    principalSchema: "Punishments",
                    principalTable: "ModCases",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_ModCaseComments_ModCaseId",
            schema: "Punishments",
            table: "ModCaseComments",
            column: "ModCaseId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ModCaseComments",
            schema: "Punishments");

        migrationBuilder.DropTable(
            name: "ModCaseTemplates",
            schema: "Punishments");

        migrationBuilder.DropTable(
            name: "ModCases",
            schema: "Punishments");
    }
}
