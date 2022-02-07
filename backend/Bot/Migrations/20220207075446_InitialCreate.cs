﻿using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bot.Migrations;

public partial class InitialCreate : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.EnsureSchema(
			name: "Bot");

		migrationBuilder.AlterDatabase()
			.Annotation("MySql:CharSet", "utf8mb4");

		migrationBuilder.CreateTable(
			name: "ApiTokens",
			schema: "Bot",
			columns: table => new
			{
				Id = table.Column<int>(type: "int", nullable: false)
					.Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
				Name = table.Column<string>(type: "longtext", nullable: true)
					.Annotation("MySql:CharSet", "utf8mb4"),
				TokenSalt = table.Column<byte[]>(type: "longblob", nullable: true),
				TokenHash = table.Column<byte[]>(type: "longblob", nullable: true),
				CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
				ValidUntil = table.Column<DateTime>(type: "datetime(6)", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_ApiTokens", x => x.Id);
			})
			.Annotation("MySql:CharSet", "utf8mb4");

		migrationBuilder.CreateTable(
			name: "AppSettings",
			schema: "Bot",
			columns: table => new
			{
				ClientId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
					.Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
				DiscordBotToken = table.Column<string>(type: "longtext", nullable: true)
					.Annotation("MySql:CharSet", "utf8mb4"),
				ClientSecret = table.Column<string>(type: "longtext", nullable: true)
					.Annotation("MySql:CharSet", "utf8mb4"),
				AbsolutePathToFileUpload = table.Column<string>(type: "longtext", nullable: true)
					.Annotation("MySql:CharSet", "utf8mb4"),
				ServiceHostName = table.Column<string>(type: "longtext", nullable: true)
					.Annotation("MySql:CharSet", "utf8mb4"),
				ServiceDomain = table.Column<string>(type: "longtext", nullable: true)
					.Annotation("MySql:CharSet", "utf8mb4"),
				ServiceBaseUrl = table.Column<string>(type: "longtext", nullable: true)
					.Annotation("MySql:CharSet", "utf8mb4"),
				SiteAdmins = table.Column<string>(type: "longtext", nullable: true)
					.Annotation("MySql:CharSet", "utf8mb4"),
				AuditLogWebhookUrl = table.Column<string>(type: "longtext", nullable: true)
					.Annotation("MySql:CharSet", "utf8mb4"),
				PublicFileMode = table.Column<bool>(type: "tinyint(1)", nullable: false),
				DemoModeEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
				CorsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
				Lang = table.Column<string>(type: "longtext", nullable: true)
					.Annotation("MySql:CharSet", "utf8mb4"),
				EmbedTitle = table.Column<string>(type: "longtext", nullable: true)
					.Annotation("MySql:CharSet", "utf8mb4"),
				EmbedContent = table.Column<string>(type: "longtext", nullable: true)
					.Annotation("MySql:CharSet", "utf8mb4")
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_AppSettings", x => x.ClientId);
			})
			.Annotation("MySql:CharSet", "utf8mb4");

		migrationBuilder.CreateTable(
			name: "GuildConfigs",
			schema: "Bot",
			columns: table => new
			{
				Id = table.Column<int>(type: "int", nullable: false)
					.Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
				GuildId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
				ModRoles = table.Column<string>(type: "longtext", nullable: true)
					.Annotation("MySql:CharSet", "utf8mb4"),
				AdminRoles = table.Column<string>(type: "longtext", nullable: true)
					.Annotation("MySql:CharSet", "utf8mb4"),
				ModNotificationDm = table.Column<bool>(type: "tinyint(1)", nullable: false),
				ModPublicNotificationWebhook = table.Column<string>(type: "longtext", nullable: true)
					.Annotation("MySql:CharSet", "utf8mb4"),
				ModInternalNotificationWebhook = table.Column<string>(type: "longtext", nullable: true)
					.Annotation("MySql:CharSet", "utf8mb4"),
				StrictModPermissionCheck = table.Column<bool>(type: "tinyint(1)", nullable: false),
				ExecuteWhoIsOnJoin = table.Column<bool>(type: "tinyint(1)", nullable: false),
				PublishModeratorInfo = table.Column<bool>(type: "tinyint(1)", nullable: false),
				PreferredLanguage = table.Column<int>(type: "int", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_GuildConfigs", x => x.Id);
			})
			.Annotation("MySql:CharSet", "utf8mb4");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropTable(
			name: "ApiTokens",
			schema: "Bot");

		migrationBuilder.DropTable(
			name: "AppSettings",
			schema: "Bot");

		migrationBuilder.DropTable(
			name: "GuildConfigs",
			schema: "Bot");
	}
}
