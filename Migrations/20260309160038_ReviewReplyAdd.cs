using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartBabySitter.Migrations
{
    /// <inheritdoc />
    public partial class ReviewReplyAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SitterReply",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SitterReplyAt",
                table: "Reviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SitterReplyByUserId",
                table: "Reviews",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SitterReplyUpdatedAt",
                table: "Reviews",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SitterReply",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "SitterReplyAt",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "SitterReplyByUserId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "SitterReplyUpdatedAt",
                table: "Reviews");
        }
    }
}
