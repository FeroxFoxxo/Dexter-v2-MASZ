using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoleReactions.Migrations;

/// <inheritdoc />
public partial class MaximumRoles : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<int>(
            name: "MaximumRoles",
            schema: "RoleReactions",
            table: "RoleReactionsMenu",
            type: "int",
            nullable: false,
            defaultValue: 0);

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(
            name: "MaximumRoles",
            schema: "RoleReactions",
            table: "RoleReactionsMenu");
}
