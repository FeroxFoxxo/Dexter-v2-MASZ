using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrivateVcs.Migrations;

/// <inheritdoc />
public partial class DeleteWaitingRoom : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<bool>(
            name: "DeleteWaitingRoom",
            schema: "PrivateVcs",
            table: "PrivateVcConfigs",
            type: "tinyint(1)",
            nullable: false,
            defaultValue: false);

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(
            name: "DeleteWaitingRoom",
            schema: "PrivateVcs",
            table: "PrivateVcConfigs");
}
