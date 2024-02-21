using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoleReactions.Migrations;

/// <inheritdoc />
public partial class PrerequesiteRoles : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<string>(
            name: "RoleToPrerequesite",
            schema: "RoleReactions",
            table: "RoleReactionsMenu",
            type: "longtext",
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(
            name: "RoleToPrerequesite",
            schema: "RoleReactions",
            table: "RoleReactionsMenu");
}
