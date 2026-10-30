using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Bot.Migrations
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
                name: "AppSettings",
                columns: table => new
                {
                    ClientId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    DiscordBotToken = table.Column<string>(type: "longtext", nullable: true),
                    ClientSecret = table.Column<string>(type: "longtext", nullable: true),
                    AbsolutePathToFileUpload = table.Column<string>(type: "longtext", nullable: true),
                    ServiceDomain = table.Column<string>(type: "longtext", nullable: true),
                    SiteAdmins = table.Column<string>(type: "longtext", nullable: true),
                    AuditLogWebhookUrl = table.Column<string>(type: "longtext", nullable: true),
                    CorsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EncryptionType = table.Column<int>(type: "int", nullable: false),
                    Lang = table.Column<string>(type: "longtext", nullable: true),
                    EmbedTitle = table.Column<string>(type: "longtext", nullable: true),
                    EmbedContent = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.ClientId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GuildConfigs",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ModRoles = table.Column<string>(type: "longtext", nullable: true),
                    AdminRoles = table.Column<string>(type: "longtext", nullable: true),
                    StaffChannels = table.Column<string>(type: "longtext", nullable: true),
                    BotChannels = table.Column<string>(type: "longtext", nullable: true),
                    ModNotificationDm = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    StaffLogs = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    StaffAnnouncements = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    StrictModPermissionCheck = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ExecuteWhoIsOnJoin = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PublishModeratorInfo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PreferredLanguage = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildConfigs", x => x.GuildId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LeftUsers",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    IsBot = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Username = table.Column<string>(type: "longtext", nullable: true),
                    DiscriminatorValue = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    AvatarId = table.Column<string>(type: "longtext", nullable: true),
                    PublicFlags = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeftUsers", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "GuildConfigs");

            migrationBuilder.DropTable(
                name: "LeftUsers");
        }
    }
}
