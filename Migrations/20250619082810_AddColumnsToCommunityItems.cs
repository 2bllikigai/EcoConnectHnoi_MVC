using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoConnect_Hanoi.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnsToCommunityItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ItemCondtion",
                table: "CommunityItems",
                newName: "ItemCondition");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "CommunityItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "PreferredLocation",
                table: "CommunityItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CommunityItems");

            migrationBuilder.DropColumn(
                name: "PreferredLocation",
                table: "CommunityItems");

            migrationBuilder.RenameColumn(
                name: "ItemCondition",
                table: "CommunityItems",
                newName: "ItemCondtion");
        }
    }
}
