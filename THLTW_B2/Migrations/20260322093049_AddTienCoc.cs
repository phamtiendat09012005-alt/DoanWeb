using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace THLTW_B2.Migrations
{
    /// <inheritdoc />
    public partial class AddTienCoc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TienCoc",
                table: "MatchRequests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TienCoc",
                table: "MatchRequests");
        }
    }
}
