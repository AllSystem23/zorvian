using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoalAssignments_Employees_EmployeeId1",
                table: "GoalAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_IncentivePayments_Employees_EmployeeId1",
                table: "IncentivePayments");

            migrationBuilder.DropIndex(
                name: "IX_IncentivePayments_EmployeeId1",
                table: "IncentivePayments");

            migrationBuilder.DropIndex(
                name: "IX_GoalAssignments_EmployeeId1",
                table: "GoalAssignments");

            migrationBuilder.DropColumn(
                name: "EmployeeId1",
                table: "IncentivePayments");

            migrationBuilder.DropColumn(
                name: "EmployeeId1",
                table: "GoalAssignments");

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    Period = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MaxEmployees = table.Column<int>(type: "integer", nullable: false),
                    IsPopular = table.Column<bool>(type: "boolean", nullable: false),
                    ShortDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                    table.UniqueConstraint("AK_SubscriptionPlans_PlanId", x => x.PlanId);
                });

            migrationBuilder.CreateTable(
                name: "CompanyPlanPricings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CustomPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    CustomPeriod = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CustomMaxEmployees = table.Column<int>(type: "integer", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyPlanPricings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyPlanPricings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyPlanPricings_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "PlanId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyPlanPricings_CompanyId_PlanId_IsActive",
                table: "CompanyPlanPricings",
                columns: new[] { "CompanyId", "PlanId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyPlanPricings_PlanId",
                table: "CompanyPlanPricings",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_PlanId",
                table: "SubscriptionPlans",
                column: "PlanId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyPlanPricings");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.AddColumn<Guid>(
                name: "EmployeeId1",
                table: "IncentivePayments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EmployeeId1",
                table: "GoalAssignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncentivePayments_EmployeeId1",
                table: "IncentivePayments",
                column: "EmployeeId1");

            migrationBuilder.CreateIndex(
                name: "IX_GoalAssignments_EmployeeId1",
                table: "GoalAssignments",
                column: "EmployeeId1");

            migrationBuilder.AddForeignKey(
                name: "FK_GoalAssignments_Employees_EmployeeId1",
                table: "GoalAssignments",
                column: "EmployeeId1",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_IncentivePayments_Employees_EmployeeId1",
                table: "IncentivePayments",
                column: "EmployeeId1",
                principalTable: "Employees",
                principalColumn: "Id");
        }
    }
}
