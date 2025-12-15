using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sho2on.Database.Migrations
{
    /// <inheritdoc />
    public partial class EmployeeDocumetnDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeDocuments_CompanyDocuments_DocumentId",
                table: "EmployeeDocuments");

            migrationBuilder.RenameColumn(
                name: "SignedFileName",
                table: "EmployeeDocuments",
                newName: "Description");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SignedDate",
                table: "EmployeeDocuments",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "DocumentId",
                table: "EmployeeDocuments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "DocumentType",
                table: "EmployeeDocuments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "EmployeeDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "EmployeeDocuments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "EmployeeDocuments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "FileType",
                table: "EmployeeDocuments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "EmployeeDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "EmployeeDocuments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UploadDate",
                table: "EmployeeDocuments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "UploadedBy",
                table: "EmployeeDocuments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocuments_UploadedBy",
                table: "EmployeeDocuments",
                column: "UploadedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeDocuments_CompanyDocuments_DocumentId",
                table: "EmployeeDocuments",
                column: "DocumentId",
                principalTable: "CompanyDocuments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeDocuments_Users_UploadedBy",
                table: "EmployeeDocuments",
                column: "UploadedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeDocuments_CompanyDocuments_DocumentId",
                table: "EmployeeDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeDocuments_Users_UploadedBy",
                table: "EmployeeDocuments");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeDocuments_UploadedBy",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "FileType",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "UploadDate",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "UploadedBy",
                table: "EmployeeDocuments");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "EmployeeDocuments",
                newName: "SignedFileName");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SignedDate",
                table: "EmployeeDocuments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DocumentId",
                table: "EmployeeDocuments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeDocuments_CompanyDocuments_DocumentId",
                table: "EmployeeDocuments",
                column: "DocumentId",
                principalTable: "CompanyDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
