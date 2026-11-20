using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAdmin.Data.Migrations
{
    /// <inheritdoc />
    public partial class AssetAddressFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Assets",
                newName: "Street");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Assets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HouseNumber",
                table: "Assets",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Country",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "HouseNumber",
                table: "Assets");

            migrationBuilder.RenameColumn(
                name: "Street",
                table: "Assets",
                newName: "Address");
        }
    }
}
