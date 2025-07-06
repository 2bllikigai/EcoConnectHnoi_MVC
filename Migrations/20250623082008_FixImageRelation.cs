using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoConnect_Hanoi.Migrations
{
    /// <inheritdoc />
    public partial class FixImageRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemImages_CommunityItems_ItemID",
                table: "ItemImages");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "ItemImages");

            migrationBuilder.RenameColumn(
                name: "ItemID",
                table: "ItemImages",
                newName: "ItemId");

            migrationBuilder.RenameIndex(
                name: "IX_ItemImages_ItemID",
                table: "ItemImages",
                newName: "IX_ItemImages_ItemId");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "ItemImages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemImages_CommunityItems_ItemId",
                table: "ItemImages",
                column: "ItemId",
                principalTable: "CommunityItems",
                principalColumn: "ItemId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemImages_CommunityItems_ItemId",
                table: "ItemImages");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "ItemImages");

            migrationBuilder.RenameColumn(
                name: "ItemId",
                table: "ItemImages",
                newName: "ItemID");

            migrationBuilder.RenameIndex(
                name: "IX_ItemImages_ItemId",
                table: "ItemImages",
                newName: "IX_ItemImages_ItemID");

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "ItemImages",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemImages_CommunityItems_ItemID",
                table: "ItemImages",
                column: "ItemID",
                principalTable: "CommunityItems",
                principalColumn: "ItemId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
