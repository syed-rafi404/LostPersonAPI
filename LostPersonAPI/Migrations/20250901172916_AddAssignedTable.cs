using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LostPersonAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AssignedVolunteers",
                table: "AssignedVolunteers");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "AssignedVolunteers",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AssignedVolunteers",
                table: "AssignedVolunteers",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AssignedVolunteers",
                table: "AssignedVolunteers");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "AssignedVolunteers");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AssignedVolunteers",
                table: "AssignedVolunteers",
                columns: new[] { "ReportID", "VolunteerID" });
        }
    }
}
