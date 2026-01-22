using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoConnect_Hanoi.Migrations
{
    /// <inheritdoc />
    public partial class AddDisplayNameToItemCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "ItemCategories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "ItemCategories");
        }
    }
}
