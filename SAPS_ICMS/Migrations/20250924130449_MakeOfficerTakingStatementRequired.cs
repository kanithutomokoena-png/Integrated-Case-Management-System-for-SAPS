using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAPS_ICMS.Migrations
{
    /// <inheritdoc />
    public partial class MakeOfficerTakingStatementRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Evidences_CaseId",
                table: "Evidences",
                column: "CaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Evidences_Cases_CaseId",
                table: "Evidences",
                column: "CaseId",
                principalTable: "Cases",
                principalColumn: "CaseId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Evidences_Cases_CaseId",
                table: "Evidences");

            migrationBuilder.DropIndex(
                name: "IX_Evidences_CaseId",
                table: "Evidences");
        }
    }
}
