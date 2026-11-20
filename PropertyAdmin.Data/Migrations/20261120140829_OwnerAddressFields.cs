using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAdmin.Data.Migrations
{
    /// <inheritdoc />
    public partial class OwnerAddressFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // D-041: rename Address to Street so the data stays; nulls become '' before NOT NULL.
            migrationBuilder.Sql("""UPDATE "Owners" SET "Address" = COALESCE("Address", ''), "PostalCode" = COALESCE("PostalCode", ''), "City" = COALESCE("City", '');""");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Owners",
                newName: "Street");

            migrationBuilder.AlterColumn<string>(
                name: "Street",
                table: "Owners",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PostalCode",
                table: "Owners",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "Owners",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Owners",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HouseNumber",
                table: "Owners",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "KvkNumber",
                table: "Owners",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VatNumber",
                table: "Owners",
                type: "character varying(14)",
                maxLength: 14,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Country",
                table: "Owners");

            migrationBuilder.DropColumn(
                name: "HouseNumber",
                table: "Owners");

            migrationBuilder.DropColumn(
                name: "KvkNumber",
                table: "Owners");

            migrationBuilder.DropColumn(
                name: "VatNumber",
                table: "Owners");

            migrationBuilder.AlterColumn<string>(
                name: "Street",
                table: "Owners",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldDefaultValue: "");

            migrationBuilder.RenameColumn(
                name: "Street",
                table: "Owners",
                newName: "Address");

            migrationBuilder.AlterColumn<string>(
                name: "PostalCode",
                table: "Owners",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "Owners",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldDefaultValue: "");
        }
    }
}
