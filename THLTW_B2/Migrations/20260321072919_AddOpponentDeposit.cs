    using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace THLTW_B2.Migrations
{
    /// <inheritdoc />
    public partial class AddOpponentDeposit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOpponentDeposited",
                table: "MatchRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOpponentDeposited",
                table: "MatchRequests");
        }
    }
}
