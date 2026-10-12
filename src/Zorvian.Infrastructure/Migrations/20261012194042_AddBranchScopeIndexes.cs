using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchScopeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Warranties_TenantId_BranchId",
                table: "Warranties",
                columns: new[] { "TenantId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId_BranchId",
                table: "Sales",
                columns: new[] { "TenantId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_TenantId_BranchId",
                table: "Quotes",
                columns: new[] { "TenantId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_TenantId_BranchId",
                table: "Purchases",
                columns: new[] { "TenantId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TenantId_BranchId",
                table: "PurchaseOrders",
                columns: new[] { "TenantId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_BranchId",
                table: "Products",
                columns: new[] { "TenantId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_TenantId_BranchId",
                table: "InventoryMovements",
                columns: new[] { "TenantId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_Credits_TenantId_BranchId",
                table: "Credits",
                columns: new[] { "TenantId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_TenantId_BranchId",
                table: "Clients",
                columns: new[] { "TenantId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisters_TenantId_BranchId",
                table: "CashRegisters",
                columns: new[] { "TenantId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingEntries_TenantId_BranchId",
                table: "AccountingEntries",
                columns: new[] { "TenantId", "BranchId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Warranties_TenantId_BranchId",
                table: "Warranties");

            migrationBuilder.DropIndex(
                name: "IX_Sales_TenantId_BranchId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_TenantId_BranchId",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_TenantId_BranchId",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_TenantId_BranchId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_BranchId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_TenantId_BranchId",
                table: "InventoryMovements");

            migrationBuilder.DropIndex(
                name: "IX_Credits_TenantId_BranchId",
                table: "Credits");

            migrationBuilder.DropIndex(
                name: "IX_Clients_TenantId_BranchId",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_CashRegisters_TenantId_BranchId",
                table: "CashRegisters");

            migrationBuilder.DropIndex(
                name: "IX_AccountingEntries_TenantId_BranchId",
                table: "AccountingEntries");
        }
    }
}
