using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixLeaveTypeIndexAndQueryFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoalAssignments_Employees_EmployeeId",
                table: "GoalAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_IncentivePayments_Employees_EmployeeId",
                table: "IncentivePayments");

            migrationBuilder.DropForeignKey(
                name: "FK_IncentivePayments_Incentives_IncentiveId",
                table: "IncentivePayments");

            migrationBuilder.DropForeignKey(
                name: "FK_Incentives_GoalDefinitions_GoalDefinitionId",
                table: "Incentives");

            migrationBuilder.DropIndex(
                name: "IX_LeaveTypes_Code",
                table: "LeaveTypes");

            migrationBuilder.DropIndex(
                name: "IX_IncentivePayments_GoalAssignmentId",
                table: "IncentivePayments");

            migrationBuilder.DropIndex(
                name: "IX_GoalProgressEntries_GoalAssignmentId",
                table: "GoalProgressEntries");

            migrationBuilder.DropIndex(
                name: "IX_GoalAssignments_GoalDefinitionId",
                table: "GoalAssignments");

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Purchases",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseOrderId",
                table: "Purchases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Incentives",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentTrigger",
                table: "Incentives",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Incentives",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "IncentiveType",
                table: "Incentives",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Incentives",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "IncentivePayments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "EmployeeId1",
                table: "IncentivePayments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PeriodKey",
                table: "GoalProgressEntries",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "GoalAssignments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "EmployeeId1",
                table: "GoalAssignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpectedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Tax = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Discount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ExchangeRateToReporting = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    CountryCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PurchaseId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityOrdered = table.Column<int>(type: "integer", nullable: false),
                    QuantityReceived = table.Column<int>(type: "integer", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Discount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_PurchaseOrderDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderDetails_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_PurchaseOrderId",
                table: "Purchases",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypes_Code_TenantId",
                table: "LeaveTypes",
                columns: new[] { "Code", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncentivePayments_EmployeeId1",
                table: "IncentivePayments",
                column: "EmployeeId1");

            migrationBuilder.CreateIndex(
                name: "IX_IncentivePayments_GoalAssignmentId_EmployeeId",
                table: "IncentivePayments",
                columns: new[] { "GoalAssignmentId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_GoalProgressEntries_GoalAssignmentId_EvaluationDate",
                table: "GoalProgressEntries",
                columns: new[] { "GoalAssignmentId", "EvaluationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_GoalAssignments_EmployeeId1",
                table: "GoalAssignments",
                column: "EmployeeId1");

            migrationBuilder.CreateIndex(
                name: "IX_GoalAssignments_GoalDefinitionId_EmployeeId",
                table: "GoalAssignments",
                columns: new[] { "GoalDefinitionId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderDetails_ProductId",
                table: "PurchaseOrderDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderDetails_PurchaseOrderId",
                table: "PurchaseOrderDetails",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrderNumber",
                table: "PurchaseOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_GoalAssignments_Employees_EmployeeId",
                table: "GoalAssignments",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_GoalAssignments_Employees_EmployeeId1",
                table: "GoalAssignments",
                column: "EmployeeId1",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_IncentivePayments_Employees_EmployeeId",
                table: "IncentivePayments",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IncentivePayments_Employees_EmployeeId1",
                table: "IncentivePayments",
                column: "EmployeeId1",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_IncentivePayments_Incentives_IncentiveId",
                table: "IncentivePayments",
                column: "IncentiveId",
                principalTable: "Incentives",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Incentives_GoalDefinitions_GoalDefinitionId",
                table: "Incentives",
                column: "GoalDefinitionId",
                principalTable: "GoalDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_PurchaseOrders_PurchaseOrderId",
                table: "Purchases",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoalAssignments_Employees_EmployeeId",
                table: "GoalAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_GoalAssignments_Employees_EmployeeId1",
                table: "GoalAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_IncentivePayments_Employees_EmployeeId",
                table: "IncentivePayments");

            migrationBuilder.DropForeignKey(
                name: "FK_IncentivePayments_Employees_EmployeeId1",
                table: "IncentivePayments");

            migrationBuilder.DropForeignKey(
                name: "FK_IncentivePayments_Incentives_IncentiveId",
                table: "IncentivePayments");

            migrationBuilder.DropForeignKey(
                name: "FK_Incentives_GoalDefinitions_GoalDefinitionId",
                table: "Incentives");

            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_PurchaseOrders_PurchaseOrderId",
                table: "Purchases");

            migrationBuilder.DropTable(
                name: "PurchaseOrderDetails");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_PurchaseOrderId",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_LeaveTypes_Code_TenantId",
                table: "LeaveTypes");

            migrationBuilder.DropIndex(
                name: "IX_IncentivePayments_EmployeeId1",
                table: "IncentivePayments");

            migrationBuilder.DropIndex(
                name: "IX_IncentivePayments_GoalAssignmentId_EmployeeId",
                table: "IncentivePayments");

            migrationBuilder.DropIndex(
                name: "IX_GoalProgressEntries_GoalAssignmentId_EvaluationDate",
                table: "GoalProgressEntries");

            migrationBuilder.DropIndex(
                name: "IX_GoalAssignments_EmployeeId1",
                table: "GoalAssignments");

            migrationBuilder.DropIndex(
                name: "IX_GoalAssignments_GoalDefinitionId_EmployeeId",
                table: "GoalAssignments");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderId",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "EmployeeId1",
                table: "IncentivePayments");

            migrationBuilder.DropColumn(
                name: "EmployeeId1",
                table: "GoalAssignments");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Incentives",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentTrigger",
                table: "Incentives",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Incentives",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "IncentiveType",
                table: "Incentives",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Incentives",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "IncentivePayments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "PeriodKey",
                table: "GoalProgressEntries",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "GoalAssignments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypes_Code",
                table: "LeaveTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncentivePayments_GoalAssignmentId",
                table: "IncentivePayments",
                column: "GoalAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_GoalProgressEntries_GoalAssignmentId",
                table: "GoalProgressEntries",
                column: "GoalAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_GoalAssignments_GoalDefinitionId",
                table: "GoalAssignments",
                column: "GoalDefinitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_GoalAssignments_Employees_EmployeeId",
                table: "GoalAssignments",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_IncentivePayments_Employees_EmployeeId",
                table: "IncentivePayments",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IncentivePayments_Incentives_IncentiveId",
                table: "IncentivePayments",
                column: "IncentiveId",
                principalTable: "Incentives",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Incentives_GoalDefinitions_GoalDefinitionId",
                table: "Incentives",
                column: "GoalDefinitionId",
                principalTable: "GoalDefinitions",
                principalColumn: "Id");
        }
    }
}
