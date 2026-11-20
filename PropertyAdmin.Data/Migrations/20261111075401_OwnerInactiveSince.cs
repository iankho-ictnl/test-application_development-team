using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyAdmin.Data.Migrations
{
    /// <inheritdoc />
    public partial class OwnerInactiveSince : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "InactiveSince",
                table: "Owners",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InactiveSince",
                table: "Owners");
        }
    }
}
