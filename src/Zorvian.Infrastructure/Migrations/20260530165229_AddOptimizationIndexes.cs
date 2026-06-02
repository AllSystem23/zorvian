using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOptimizationIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VacationRequests_EmployeeId",
                table: "VacationRequests");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_PermissionRequests_EmployeeId",
                table: "PermissionRequests");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeHistories_EmployeeId",
                table: "EmployeeHistories");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalFlows_ApproverId",
                table: "ApprovalFlows");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalFlows_RequestId",
                table: "ApprovalFlows");

            migrationBuilder.CreateIndex(
                name: "IX_VacationRequests_EmployeeId_Status",
                table: "VacationRequests",
                columns: new[] { "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VacationRequests_StartDate_EndDate",
                table: "VacationRequests",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_ExpiresAt",
                table: "RefreshTokens",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequests_EmployeeId_Status",
                table: "PermissionRequests",
                columns: new[] { "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequests_StartDate_EndDate",
                table: "PermissionRequests",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Status_DepartmentId",
                table: "Employees",
                columns: new[] { "Status", "DepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeHistories_EmployeeId_CreatedAt",
                table: "EmployeeHistories",
                columns: new[] { "EmployeeId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalFlows_ApproverId_Status",
                table: "ApprovalFlows",
                columns: new[] { "ApproverId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalFlows_RequestId_RequestType",
                table: "ApprovalFlows",
                columns: new[] { "RequestId", "RequestType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VacationRequests_EmployeeId_Status",
                table: "VacationRequests");

            migrationBuilder.DropIndex(
                name: "IX_VacationRequests_StartDate_EndDate",
                table: "VacationRequests");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId_ExpiresAt",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_PermissionRequests_EmployeeId_Status",
                table: "PermissionRequests");

            migrationBuilder.DropIndex(
                name: "IX_PermissionRequests_StartDate_EndDate",
                table: "PermissionRequests");

            migrationBuilder.DropIndex(
                name: "IX_Employees_Status_DepartmentId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeHistories_EmployeeId_CreatedAt",
                table: "EmployeeHistories");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalFlows_ApproverId_Status",
                table: "ApprovalFlows");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalFlows_RequestId_RequestType",
                table: "ApprovalFlows");

            migrationBuilder.CreateIndex(
                name: "IX_VacationRequests_EmployeeId",
                table: "VacationRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequests_EmployeeId",
                table: "PermissionRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeHistories_EmployeeId",
                table: "EmployeeHistories",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalFlows_ApproverId",
                table: "ApprovalFlows",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalFlows_RequestId",
                table: "ApprovalFlows",
                column: "RequestId");
        }
    }
}
