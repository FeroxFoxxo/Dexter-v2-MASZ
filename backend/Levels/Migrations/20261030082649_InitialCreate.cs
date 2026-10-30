using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Levels.Migrations
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
                name: "GuildLevelConfigs",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Experience = table.Column<string>(type: "longtext", nullable: true),
                    Coefficients = table.Column<string>(type: "longtext", nullable: true),
                    XpInterval = table.Column<int>(type: "int", nullable: false),
                    LevelUpTemplate = table.Column<string>(type: "longtext", nullable: true),
                    VoiceLevelUpChannel = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    TextLevelUpChannel = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    DisabledXpChannels = table.Column<string>(type: "longtext", nullable: true),
                    HandleRoles = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SendTextLevelUps = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SendVoiceLevelUps = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    VoiceXpCountMutedMembers = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    VoiceXpRequiredMembers = table.Column<int>(type: "int", nullable: false),
                    NicknameDisabledRole = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    NicknameDisabledReplacement = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Levels = table.Column<string>(type: "longtext", nullable: true),
                    LevelUpMessageOverrides = table.Column<string>(type: "longtext", nullable: true),
                    ExperienceOverrides = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildLevelConfigs", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GuildUserLevels",
                columns: table => new
                {
                    UserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    GuildId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    TextXp = table.Column<long>(type: "bigint", nullable: false),
                    VoiceXp = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildUserLevels", x => new { x.GuildId, x.UserId });
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserRankcardConfigs",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    XpColor = table.Column<uint>(type: "int unsigned", nullable: false),
                    OffColor = table.Column<uint>(type: "int unsigned", nullable: false),
                    LevelBgColor = table.Column<uint>(type: "int unsigned", nullable: false),
                    TitleBgColor = table.Column<uint>(type: "int unsigned", nullable: false),
                    Background = table.Column<string>(type: "longtext", nullable: true),
                    TitleOffsetX = table.Column<int>(type: "int", nullable: false),
                    TitleOffsetY = table.Column<int>(type: "int", nullable: false),
                    LevelOffsetX = table.Column<int>(type: "int", nullable: false),
                    LevelOffsetY = table.Column<int>(type: "int", nullable: false),
                    PfpOffsetX = table.Column<int>(type: "int", nullable: false),
                    PfpOffsetY = table.Column<int>(type: "int", nullable: false),
                    PfpRadiusFactor = table.Column<float>(type: "float", nullable: false),
                    RankcardFlags = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRankcardConfigs", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildLevelConfigs");

            migrationBuilder.DropTable(
                name: "GuildUserLevels");

            migrationBuilder.DropTable(
                name: "UserRankcardConfigs");
        }
    }
}
