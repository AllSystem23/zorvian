using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanModuleEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BonusRecords_Employees_EmployeeId",
                table: "BonusRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_BonusRecords_PayrollPeriods_PayrollPeriodId",
                table: "BonusRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_OvertimeRecords_Employees_EmployeeId",
                table: "OvertimeRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_OvertimeRecords_PayrollPeriods_PayrollPeriodId",
                table: "OvertimeRecords");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "OvertimeRecords",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "OvertimeType",
                table: "OvertimeRecords",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "BonusRecords",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "BonusRecords",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "BonusType",
                table: "BonusRecords",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "PlanModules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Module = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanModules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanModules_PlanId_Module",
                table: "PlanModules",
                columns: new[] { "PlanId", "Module" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BonusRecords_Employees_EmployeeId",
                table: "BonusRecords",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BonusRecords_PayrollPeriods_PayrollPeriodId",
                table: "BonusRecords",
                column: "PayrollPeriodId",
                principalTable: "PayrollPeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OvertimeRecords_Employees_EmployeeId",
                table: "OvertimeRecords",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OvertimeRecords_PayrollPeriods_PayrollPeriodId",
                table: "OvertimeRecords",
                column: "PayrollPeriodId",
                principalTable: "PayrollPeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BonusRecords_Employees_EmployeeId",
                table: "BonusRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_BonusRecords_PayrollPeriods_PayrollPeriodId",
                table: "BonusRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_OvertimeRecords_Employees_EmployeeId",
                table: "OvertimeRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_OvertimeRecords_PayrollPeriods_PayrollPeriodId",
                table: "OvertimeRecords");

            migrationBuilder.DropTable(
                name: "PlanModules");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "OvertimeRecords",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "OvertimeType",
                table: "OvertimeRecords",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "BonusRecords",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "BonusRecords",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "BonusType",
                table: "BonusRecords",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddForeignKey(
                name: "FK_BonusRecords_Employees_EmployeeId",
                table: "BonusRecords",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BonusRecords_PayrollPeriods_PayrollPeriodId",
                table: "BonusRecords",
                column: "PayrollPeriodId",
                principalTable: "PayrollPeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OvertimeRecords_Employees_EmployeeId",
                table: "OvertimeRecords",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OvertimeRecords_PayrollPeriods_PayrollPeriodId",
                table: "OvertimeRecords",
                column: "PayrollPeriodId",
                principalTable: "PayrollPeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
