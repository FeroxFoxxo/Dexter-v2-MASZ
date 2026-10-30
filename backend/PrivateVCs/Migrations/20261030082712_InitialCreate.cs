using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace PrivateVcs.Migrations
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
                name: "PrivateVcConfigs",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    WaitingVcName = table.Column<string>(type: "longtext", nullable: true),
                    PrivateCategoryId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    AllowedRoles = table.Column<string>(type: "longtext", nullable: true),
                    CreatorRoles = table.Column<string>(type: "longtext", nullable: true),
                    ChannelFilterRegex = table.Column<string>(type: "longtext", nullable: true),
                    DeleteWaitingRoom = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivateVcConfigs", x => x.GuildId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrivateVcConfigs");
        }
    }
}
