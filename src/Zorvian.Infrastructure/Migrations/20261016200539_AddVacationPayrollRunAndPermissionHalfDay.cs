using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVacationPayrollRunAndPermissionHalfDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PaidByPayrollRunId",
                table: "VacationRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHalfDay",
                table: "PermissionRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_VacationRequests_PaidByPayrollRunId",
                table: "VacationRequests",
                column: "PaidByPayrollRunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VacationRequests_PaidByPayrollRunId",
                table: "VacationRequests");

            migrationBuilder.DropColumn(
                name: "PaidByPayrollRunId",
                table: "VacationRequests");

            migrationBuilder.DropColumn(
                name: "IsHalfDay",
                table: "PermissionRequests");
        }
    }
}
