using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace THLTW_B2.Migrations
{
    /// <inheritdoc />
    public partial class AddSoccerFieldsTable2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "SoccerFields",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "SoccerFields");
        }
    }
}
