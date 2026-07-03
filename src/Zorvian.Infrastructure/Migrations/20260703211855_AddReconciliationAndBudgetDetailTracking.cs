using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReconciliationAndBudgetDetailTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "CompanyPlanPricings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CompanyPlanPricings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CompanyPlanPricings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "CompanyPlanPricings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "CompanyPlanPricings",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "CompanyPlanPricings");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CompanyPlanPricings");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CompanyPlanPricings");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CompanyPlanPricings");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "CompanyPlanPricings");
        }
    }
}
