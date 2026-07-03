using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PendingModelChanges_Jul2026 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NetSettlement",
                table: "TerminationRecords",
                newName: "YearsWorked");

            migrationBuilder.RenameColumn(
                name: "AccruedVacationPay",
                table: "TerminationRecords",
                newName: "VacationPay");

            migrationBuilder.RenameColumn(
                name: "AccruedAguinaldoPay",
                table: "TerminationRecords",
                newName: "VacationDaysToPay");

            migrationBuilder.AddColumn<decimal>(
                name: "AguinaldoPay",
                table: "TerminationRecords",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "TerminationRecords",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CountryName",
                table: "TerminationRecords",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "TerminationRecords",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "DailySalary",
                table: "TerminationRecords",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DaysAccrued25PerMonth",
                table: "TerminationRecords",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DaysWorked",
                table: "TerminationRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "HireDate",
                table: "TerminationRecords",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<decimal>(
                name: "InatecAmount",
                table: "TerminationRecords",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "InssEmployeeRateDisplay",
                table: "TerminationRecords",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "InssEmployerRateDisplay",
                table: "TerminationRecords",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "InssLaboralAmount",
                table: "TerminationRecords",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "InssPatronalAmount",
                table: "TerminationRecords",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "IrSalaryAmount",
                table: "TerminationRecords",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "IrTotalAmount",
                table: "TerminationRecords",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsTrustPosition",
                table: "TerminationRecords",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlySalary",
                table: "TerminationRecords",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthsWorked",
                table: "TerminationRecords",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "OtherEmployerName",
                table: "TerminationRecords",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "OtherEmployerRateDisplay",
                table: "TerminationRecords",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OvertimeHours",
                table: "TerminationRecords",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OvertimePay",
                table: "TerminationRecords",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PendingSalaryDays",
                table: "TerminationRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PendingSalaryPay",
                table: "TerminationRecords",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SeverancePay",
                table: "TerminationRecords",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalDeductions",
                table: "TerminationRecords",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TrustPositionPay",
                table: "TerminationRecords",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VacationDaysAccrued",
                table: "TerminationRecords",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VacationDaysTaken",
                table: "TerminationRecords",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "DeductAguinaldo",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DeductInss",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DeductIr",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDomesticWorkerWithBoard",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTrustPosition",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AguinaldoPeriodStartDay",
                table: "CountryTaxConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AguinaldoPeriodStartMonth",
                table: "CountryTaxConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DefaultFiscalStartMonth",
                table: "CountryTaxConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IndemnityTiersJson",
                table: "CountryTaxConfigs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "InssIntegralEmployeeRate",
                table: "CountryTaxConfigs",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "InssIntegralEmployerRate",
                table: "CountryTaxConfigs",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "InssIntegralEmployerRateSmall",
                table: "CountryTaxConfigs",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "InssIvmEmployeeRate",
                table: "CountryTaxConfigs",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "InssIvmEmployerRate",
                table: "CountryTaxConfigs",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "InssIvmEmployerRateSmall",
                table: "CountryTaxConfigs",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "InssSmallEmployerThreshold",
                table: "CountryTaxConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxIndemnityDays",
                table: "CountryTaxConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FiscalYearStartMonth",
                table: "CompanySettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "InssRegime",
                table: "CompanySettings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CloseNotes",
                table: "AccountingPeriods",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosedBy",
                table: "AccountingPeriods",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FiscalYearId",
                table: "AccountingPeriods",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReopenReason",
                table: "AccountingPeriods",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReopenedAt",
                table: "AccountingPeriods",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReopenedBy",
                table: "AccountingPeriods",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FiscalYears",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AuditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AuditedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalYears", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPeriods_FiscalYearId",
                table: "AccountingPeriods",
                column: "FiscalYearId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPeriods_Status",
                table: "AccountingPeriods",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYears_Year_CompanyId",
                table: "FiscalYears",
                columns: new[] { "Year", "CompanyId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AccountingPeriods_FiscalYears_FiscalYearId",
                table: "AccountingPeriods",
                column: "FiscalYearId",
                principalTable: "FiscalYears",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountingPeriods_FiscalYears_FiscalYearId",
                table: "AccountingPeriods");

            migrationBuilder.DropTable(
                name: "FiscalYears");

            migrationBuilder.DropIndex(
                name: "IX_AccountingPeriods_FiscalYearId",
                table: "AccountingPeriods");

            migrationBuilder.DropIndex(
                name: "IX_AccountingPeriods_Status",
                table: "AccountingPeriods");

            migrationBuilder.DropColumn(
                name: "AguinaldoPay",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "CountryName",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "DailySalary",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "DaysAccrued25PerMonth",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "DaysWorked",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "HireDate",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "InatecAmount",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "InssEmployeeRateDisplay",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "InssEmployerRateDisplay",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "InssLaboralAmount",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "InssPatronalAmount",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "IrSalaryAmount",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "IrTotalAmount",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "IsTrustPosition",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "MonthlySalary",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "MonthsWorked",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "OtherEmployerName",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "OtherEmployerRateDisplay",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "OvertimeHours",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "OvertimePay",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "PendingSalaryDays",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "PendingSalaryPay",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "SeverancePay",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "TotalDeductions",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "TrustPositionPay",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "VacationDaysAccrued",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "VacationDaysTaken",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "DeductAguinaldo",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DeductInss",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DeductIr",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IsDomesticWorkerWithBoard",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IsTrustPosition",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AguinaldoPeriodStartDay",
                table: "CountryTaxConfigs");

            migrationBuilder.DropColumn(
                name: "AguinaldoPeriodStartMonth",
                table: "CountryTaxConfigs");

            migrationBuilder.DropColumn(
                name: "DefaultFiscalStartMonth",
                table: "CountryTaxConfigs");

            migrationBuilder.DropColumn(
                name: "IndemnityTiersJson",
                table: "CountryTaxConfigs");

            migrationBuilder.DropColumn(
                name: "InssIntegralEmployeeRate",
                table: "CountryTaxConfigs");

            migrationBuilder.DropColumn(
                name: "InssIntegralEmployerRate",
                table: "CountryTaxConfigs");

            migrationBuilder.DropColumn(
                name: "InssIntegralEmployerRateSmall",
                table: "CountryTaxConfigs");

            migrationBuilder.DropColumn(
                name: "InssIvmEmployeeRate",
                table: "CountryTaxConfigs");

            migrationBuilder.DropColumn(
                name: "InssIvmEmployerRate",
                table: "CountryTaxConfigs");

            migrationBuilder.DropColumn(
                name: "InssIvmEmployerRateSmall",
                table: "CountryTaxConfigs");

            migrationBuilder.DropColumn(
                name: "InssSmallEmployerThreshold",
                table: "CountryTaxConfigs");

            migrationBuilder.DropColumn(
                name: "MaxIndemnityDays",
                table: "CountryTaxConfigs");

            migrationBuilder.DropColumn(
                name: "FiscalYearStartMonth",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "InssRegime",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "CloseNotes",
                table: "AccountingPeriods");

            migrationBuilder.DropColumn(
                name: "ClosedBy",
                table: "AccountingPeriods");

            migrationBuilder.DropColumn(
                name: "FiscalYearId",
                table: "AccountingPeriods");

            migrationBuilder.DropColumn(
                name: "ReopenReason",
                table: "AccountingPeriods");

            migrationBuilder.DropColumn(
                name: "ReopenedAt",
                table: "AccountingPeriods");

            migrationBuilder.DropColumn(
                name: "ReopenedBy",
                table: "AccountingPeriods");

            migrationBuilder.RenameColumn(
                name: "YearsWorked",
                table: "TerminationRecords",
                newName: "NetSettlement");

            migrationBuilder.RenameColumn(
                name: "VacationPay",
                table: "TerminationRecords",
                newName: "AccruedVacationPay");

            migrationBuilder.RenameColumn(
                name: "VacationDaysToPay",
                table: "TerminationRecords",
                newName: "AccruedAguinaldoPay");
        }
    }
}
