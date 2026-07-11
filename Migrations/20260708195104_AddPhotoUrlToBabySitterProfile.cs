using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartBabySitter.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoUrlToBabySitterProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "BabySitterProfiles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "BabySitterProfiles");
        }
    }
}
