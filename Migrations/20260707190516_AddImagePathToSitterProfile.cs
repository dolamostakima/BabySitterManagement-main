using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartBabySitter.Migrations
{
    /// <inheritdoc />
    public partial class AddImagePathToSitterProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "SitterProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "SitterProfiles");
        }
    }
}
