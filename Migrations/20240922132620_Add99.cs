using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PenFootball_Server.Migrations
{
    /// <inheritdoc />
    public partial class Add99 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Loses99",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Wins99",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Loses99",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Wins99",
                table: "Users");
        }
    }
}
