using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Invites.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "Invites");

        migrationBuilder.AlterDatabase()
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "UserInvites",
            schema: "Invites",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                GuildId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                TargetChannelId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                JoinedUserId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                UsedInvite = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                InviteIssuerId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                JoinedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                InviteCreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_UserInvites", x => x.Id))
            .Annotation("MySql:CharSet", "utf8mb4");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(
            name: "UserInvites",
            schema: "Invites");
}
