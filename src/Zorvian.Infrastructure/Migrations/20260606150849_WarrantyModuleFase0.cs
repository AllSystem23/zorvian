using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WarrantyModuleFase0 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "WarrantyClaims",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Warranties",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<Guid>(
                name: "BrandId",
                table: "Warranties",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Warranties",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Imei",
                table: "Warranties",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LotNumber",
                table: "Warranties",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "Warranties",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "ApiKeys",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warranties_BrandId",
                table: "Warranties",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Warranties_CategoryId",
                table: "Warranties",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Warranties_Brands_BrandId",
                table: "Warranties",
                column: "BrandId",
                principalTable: "Brands",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Warranties_Categories_CategoryId",
                table: "Warranties",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Migrar datos existentes: string → enum (PascalCase)
            migrationBuilder.Sql("UPDATE \"Warranties\" SET \"Status\" = 'Registered' WHERE \"Status\" = 'active'");
            migrationBuilder.Sql("UPDATE \"Warranties\" SET \"Status\" = 'PendingReview' WHERE \"Status\" = 'claimed'");
            migrationBuilder.Sql("UPDATE \"WarrantyClaims\" SET \"Status\" = 'Registered' WHERE \"Status\" = 'pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Warranties_Brands_BrandId",
                table: "Warranties");

            migrationBuilder.DropForeignKey(
                name: "FK_Warranties_Categories_CategoryId",
                table: "Warranties");

            migrationBuilder.DropIndex(
                name: "IX_Warranties_BrandId",
                table: "Warranties");

            migrationBuilder.DropIndex(
                name: "IX_Warranties_CategoryId",
                table: "Warranties");

            migrationBuilder.DropColumn(
                name: "BrandId",
                table: "Warranties");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Warranties");

            migrationBuilder.DropColumn(
                name: "Imei",
                table: "Warranties");

            migrationBuilder.DropColumn(
                name: "LotNumber",
                table: "Warranties");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "Warranties");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ApiKeys");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "WarrantyClaims",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Warranties",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);
        }
    }
}
