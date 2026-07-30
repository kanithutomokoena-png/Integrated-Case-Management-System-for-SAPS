using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAPS_ICMS.Migrations
{
    /// <inheritdoc />
    public partial class ExpandCaseModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Evidence",
                table: "Evidence");

            migrationBuilder.RenameTable(
                name: "Evidence",
                newName: "Evidences");

            migrationBuilder.AlterColumn<string>(
                name: "Complainant",
                table: "Cases",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "CaseInformation",
                table: "Cases",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ComplainantSignature",
                table: "Cases",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ComplainantSignatureDate",
                table: "Cases",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ComplainantSignatureTime",
                table: "Cases",
                type: "time(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "Cases",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeAddress",
                table: "Cases",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LocationTaken",
                table: "Cases",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Occupation",
                table: "Cases",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OfficerSignature",
                table: "Cases",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "OfficerSignatureDate",
                table: "Cases",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficerTakingStatement",
                table: "Cases",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Cases",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "RelationshipToIncident",
                table: "Cases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CaseId",
                table: "Evidences",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "Evidences",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Evidences",
                table: "Evidences",
                column: "EvidenceId");

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

            migrationBuilder.DropPrimaryKey(
                name: "PK_Evidences",
                table: "Evidences");

            migrationBuilder.DropIndex(
                name: "IX_Evidences_CaseId",
                table: "Evidences");

            migrationBuilder.DropColumn(
                name: "ComplainantSignature",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "ComplainantSignatureDate",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "ComplainantSignatureTime",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "HomeAddress",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "LocationTaken",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "Occupation",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "OfficerSignature",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "OfficerSignatureDate",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "OfficerTakingStatement",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "RelationshipToIncident",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "CaseId",
                table: "Evidences");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "Evidences");

            migrationBuilder.RenameTable(
                name: "Evidences",
                newName: "Evidence");

            migrationBuilder.UpdateData(
                table: "Cases",
                keyColumn: "Complainant",
                keyValue: null,
                column: "Complainant",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Complainant",
                table: "Cases",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Cases",
                keyColumn: "CaseInformation",
                keyValue: null,
                column: "CaseInformation",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "CaseInformation",
                table: "Cases",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Evidence",
                table: "Evidence",
                column: "EvidenceId");
        }
    }
}
