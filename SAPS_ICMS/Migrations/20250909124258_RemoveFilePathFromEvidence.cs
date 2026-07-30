using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAPS_ICMS.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFilePathFromEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Evidences_Cases_CaseId",
                table: "Evidences");

            migrationBuilder.DropIndex(
                name: "IX_Evidences_CaseId",
                table: "Evidences");

            migrationBuilder.RenameColumn(
                name: "FilePath",
                table: "Evidences",
                newName: "UploadedBy");

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "Evidences",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<byte[]>(
                name: "FileData",
                table: "Evidences",
                type: "longblob",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Evidences",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "UploadDate",
                table: "Evidences",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "Evidences");

            migrationBuilder.DropColumn(
                name: "FileData",
                table: "Evidences");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Evidences");

            migrationBuilder.DropColumn(
                name: "UploadDate",
                table: "Evidences");

            migrationBuilder.RenameColumn(
                name: "UploadedBy",
                table: "Evidences",
                newName: "FilePath");

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
    }
}
