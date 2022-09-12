using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Games.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Games");

            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GameProfiles",
                schema: "Games",
                columns: table => new
                {
                    UserId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameProfiles", x => x.UserId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GameStates",
                schema: "Games",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    GameType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RoomName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Password = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AllowGuests = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MasterId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    MaxPlayers = table.Column<int>(type: "int", nullable: false),
                    TimeCreated = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    TimeUpdated = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    Data = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameStates", x => x.GameId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GameRatings",
                schema: "Games",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    GameId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Elo = table.Column<float>(type: "float", nullable: false),
                    PlacementFactor = table.Column<float>(type: "float", nullable: false),
                    GameCount = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: true),
                    Draws = table.Column<int>(type: "int", nullable: true),
                    Losses = table.Column<int>(type: "int", nullable: true),
                    GameProfileUserId = table.Column<ulong>(type: "bigint unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameRatings_GameProfiles_GameProfileUserId",
                        column: x => x.GameProfileUserId,
                        principalSchema: "Games",
                        principalTable: "GameProfiles",
                        principalColumn: "UserId");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GameConnections",
                schema: "Games",
                columns: table => new
                {
                    UserId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ConnectionId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsGuest = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    GameId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameConnections", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_GameConnections_GameStates_GameId",
                        column: x => x.GameId,
                        principalSchema: "Games",
                        principalTable: "GameStates",
                        principalColumn: "GameId");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_GameConnections_GameId",
                schema: "Games",
                table: "GameConnections",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GameRatings_GameProfileUserId",
                schema: "Games",
                table: "GameRatings",
                column: "GameProfileUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameConnections",
                schema: "Games");

            migrationBuilder.DropTable(
                name: "GameRatings",
                schema: "Games");

            migrationBuilder.DropTable(
                name: "GameStates",
                schema: "Games");

            migrationBuilder.DropTable(
                name: "GameProfiles",
                schema: "Games");
        }
    }
}
