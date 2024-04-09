using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Messaging.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "Messaging");

        migrationBuilder.AlterDatabase()
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "ScheduledMessages",
            schema: "Messaging",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Name = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Content = table.Column<string>(type: "longtext", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                ScheduledFor = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                GuildId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                ChannelId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                CreatorId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                LastEditedById = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                LastEditedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                FailureReason = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_ScheduledMessages", x => x.Id))
            .Annotation("MySql:CharSet", "utf8mb4");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(
            name: "ScheduledMessages",
            schema: "Messaging");
}
