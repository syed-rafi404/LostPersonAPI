using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LostPersonAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoUrlToReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "MissingPersonReports",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "MissingPersonReports");
        }
    }
}
