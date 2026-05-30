using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Sprint8LeaveTypeCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "LeaveTypes");

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "LeaveTypes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypes_CompanyId",
                table: "LeaveTypes",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveTypes_Companies_CompanyId",
                table: "LeaveTypes",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveTypes_Companies_CompanyId",
                table: "LeaveTypes");

            migrationBuilder.DropIndex(
                name: "IX_LeaveTypes_CompanyId",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "LeaveTypes");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "LeaveTypes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
