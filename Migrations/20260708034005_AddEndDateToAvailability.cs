using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartBabySitter.Migrations
{
    /// <inheritdoc />
    public partial class AddEndDateToAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Availabilities",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Availabilities");
        }
    }
}
