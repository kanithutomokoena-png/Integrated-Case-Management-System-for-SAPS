using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAPS_ICMS.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusToCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Cases",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Cases");
        }
    }
}
