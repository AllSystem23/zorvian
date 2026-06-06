using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WarrantyModuleFase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProviderId",
                table: "WarrantyClaims",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TechnicianId",
                table: "WarrantyClaims",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkshopId",
                table: "WarrantyClaims",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServiceWorkshops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    LegalName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    TaxId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ContactName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AvgResponseHours = table.Column<int>(type: "integer", nullable: false),
                    AvgRepairHours = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_ServiceWorkshops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceWorkshops_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarrantyProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    LegalName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    TaxId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ContactName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Website = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    AvgResponseHours = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_WarrantyProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkshopBrands",
                columns: table => new
                {
                    WorkshopId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SlaHours = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkshopBrands", x => new { x.WorkshopId, x.BrandId });
                    table.ForeignKey(
                        name: "FK_WorkshopBrands_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkshopBrands_ServiceWorkshops_WorkshopId",
                        column: x => x.WorkshopId,
                        principalTable: "ServiceWorkshops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkshopTechnicians",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkshopId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Identification = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Specialties = table.Column<string[]>(type: "text[]", nullable: false),
                    IsCertified = table.Column<bool>(type: "boolean", nullable: false),
                    CertificationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AvgRepairMinutes = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_WorkshopTechnicians", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkshopTechnicians_ServiceWorkshops_WorkshopId",
                        column: x => x.WorkshopId,
                        principalTable: "ServiceWorkshops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProviderBrands",
                columns: table => new
                {
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SlaHours = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderBrands", x => new { x.ProviderId, x.BrandId });
                    table.ForeignKey(
                        name: "FK_ProviderBrands_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProviderBrands_WarrantyProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "WarrantyProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProviderContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_ProviderContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderContacts_WarrantyProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "WarrantyProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_ProviderId",
                table: "WarrantyClaims",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_TechnicianId",
                table: "WarrantyClaims",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyClaims_WorkshopId",
                table: "WarrantyClaims",
                column: "WorkshopId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderBrands_BrandId",
                table: "ProviderBrands",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderContacts_ProviderId",
                table: "ProviderContacts",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceWorkshops_BranchId",
                table: "ServiceWorkshops",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceWorkshops_TenantId_Code",
                table: "ServiceWorkshops",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyProviders_TenantId_Code",
                table: "WarrantyProviders",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkshopBrands_BrandId",
                table: "WorkshopBrands",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkshopTechnicians_WorkshopId",
                table: "WorkshopTechnicians",
                column: "WorkshopId");

            migrationBuilder.AddForeignKey(
                name: "FK_WarrantyClaims_ServiceWorkshops_WorkshopId",
                table: "WarrantyClaims",
                column: "WorkshopId",
                principalTable: "ServiceWorkshops",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_WarrantyClaims_WarrantyProviders_ProviderId",
                table: "WarrantyClaims",
                column: "ProviderId",
                principalTable: "WarrantyProviders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_WarrantyClaims_WorkshopTechnicians_TechnicianId",
                table: "WarrantyClaims",
                column: "TechnicianId",
                principalTable: "WorkshopTechnicians",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarrantyClaims_ServiceWorkshops_WorkshopId",
                table: "WarrantyClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_WarrantyClaims_WarrantyProviders_ProviderId",
                table: "WarrantyClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_WarrantyClaims_WorkshopTechnicians_TechnicianId",
                table: "WarrantyClaims");

            migrationBuilder.DropTable(
                name: "ProviderBrands");

            migrationBuilder.DropTable(
                name: "ProviderContacts");

            migrationBuilder.DropTable(
                name: "WorkshopBrands");

            migrationBuilder.DropTable(
                name: "WorkshopTechnicians");

            migrationBuilder.DropTable(
                name: "WarrantyProviders");

            migrationBuilder.DropTable(
                name: "ServiceWorkshops");

            migrationBuilder.DropIndex(
                name: "IX_WarrantyClaims_ProviderId",
                table: "WarrantyClaims");

            migrationBuilder.DropIndex(
                name: "IX_WarrantyClaims_TechnicianId",
                table: "WarrantyClaims");

            migrationBuilder.DropIndex(
                name: "IX_WarrantyClaims_WorkshopId",
                table: "WarrantyClaims");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                table: "WarrantyClaims");

            migrationBuilder.DropColumn(
                name: "TechnicianId",
                table: "WarrantyClaims");

            migrationBuilder.DropColumn(
                name: "WorkshopId",
                table: "WarrantyClaims");
        }
    }
}
