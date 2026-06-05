using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTotalEmployerCostsToPayroll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CountryTaxConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CountryCode = table.Column<string>(type: "text", nullable: false),
                    CountryName = table.Column<string>(type: "text", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    InssEmployeeRate = table.Column<decimal>(type: "numeric", nullable: false),
                    InssEmployeeMax = table.Column<decimal>(type: "numeric", nullable: false),
                    InssEmployerRate = table.Column<decimal>(type: "numeric", nullable: false),
                    InssEmployerMax = table.Column<decimal>(type: "numeric", nullable: false),
                    OtherEmployerRate = table.Column<decimal>(type: "numeric", nullable: false),
                    OtherEmployerName = table.Column<string>(type: "text", nullable: true),
                    IrExemptAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    IrTableJson = table.Column<string>(type: "text", nullable: false),
                    VacationDaysPerYear = table.Column<int>(type: "integer", nullable: false),
                    ChristmasBonusPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    IndemnityDaysPerYear = table.Column<int>(type: "integer", nullable: false),
                    MaxIndemnityYears = table.Column<int>(type: "integer", nullable: false),
                    HasThirteenthMonth = table.Column<bool>(type: "boolean", nullable: false),
                    HasFourteenthMonth = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CountryTaxConfigs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CountryTaxConfigs");
        }
    }
}
