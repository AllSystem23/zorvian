using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSignatureEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Comments",
                table: "DocumentSignatures",
                newName: "SignatureData");

            migrationBuilder.AlterColumn<string>(
                name: "SignerId",
                table: "DocumentSignatures",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "SignatureToken",
                table: "DocumentSignatures",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "DocumentSignatures",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "DocumentSignatures");

            migrationBuilder.RenameColumn(
                name: "SignatureData",
                table: "DocumentSignatures",
                newName: "Comments");

            migrationBuilder.AlterColumn<Guid>(
                name: "SignerId",
                table: "DocumentSignatures",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "SignatureToken",
                table: "DocumentSignatures",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);
        }
    }
}
