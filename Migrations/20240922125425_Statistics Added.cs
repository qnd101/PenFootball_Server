using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PenFootball_Server.Migrations
{
    /// <inheritdoc />
    public partial class StatisticsAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Loses",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Wins",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RelStats",
                columns: table => new
                {
                    ID1 = table.Column<int>(type: "INTEGER", nullable: false),
                    ID2 = table.Column<int>(type: "INTEGER", nullable: false),
                    Win1 = table.Column<int>(type: "INTEGER", nullable: false),
                    Win2 = table.Column<int>(type: "INTEGER", nullable: false),
                    Recent = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelStats", x => new { x.ID1, x.ID2 });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RelStats");

            migrationBuilder.DropColumn(
                name: "Loses",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Wins",
                table: "Users");
        }
    }
}
