using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace THLTW_B2.Migrations
{
    /// <inheritdoc />
    public partial class AddOpponentPhone_Fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OpponentPhone",
                table: "MatchRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OpponentPhone",
                table: "MatchRequests");
        }
    }
}
