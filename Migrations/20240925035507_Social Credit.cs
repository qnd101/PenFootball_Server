using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PenFootball_Server.Migrations
{
    /// <inheritdoc />
    public partial class SocialCredit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SocialCredit",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SocialCredit",
                table: "Users");
        }
    }
}
