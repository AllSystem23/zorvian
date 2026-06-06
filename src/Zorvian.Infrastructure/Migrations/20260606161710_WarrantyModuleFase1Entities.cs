using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WarrantyModuleFase1Entities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Accessories",
                table: "WarrantyClaims",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureDescription",
                table: "WarrantyClaims",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureType",
                table: "WarrantyClaims",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "WarrantyClaims",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductCondition",
                table: "WarrantyClaims",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderAuthorizationCode",
                table: "WarrantyClaims",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProviderReferredAt",
                table: "WarrantyClaims",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SlaBreachedAt",
                table: "WarrantyClaims",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SlaDeadline",
                table: "WarrantyClaims",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WorkshopAssignedAt",
                table: "WarrantyClaims",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierId",
                table: "SupplierCreditNotes",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "SupplierCreditNotes",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "SupplierCreditNotes",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "SupplierCreditNotes",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "SupplierCreditNotes",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WarrantyCostId",
                table: "SupplierCreditNotes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WarrantyId",
                table: "SupplierCreditNotes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WarrantyPartRequestId",
                table: "SupplierCreditNotes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WarrantyProviderId",
                table: "SupplierCreditNotes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WarrantyAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarrantyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uuid", nullable: true),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FileUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UploadedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_WarrantyAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarrantyAttachments_Employees_UploadedByEmployeeId",
                        column: x => x.UploadedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WarrantyAttachments_Warranties_WarrantyId",
                        column: x => x.WarrantyId,
                        principalTable: "Warranties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WarrantyAttachments_WarrantyClaims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "WarrantyClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WarrantyCommunications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarrantyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uuid", nullable: true),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Body = table.Column<string>(type: "text", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SentByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_WarrantyCommunications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarrantyCommunications_Employees_SentByEmployeeId",
                        column: x => x.SentByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WarrantyCommunications_Warranties_WarrantyId",
                        column: x => x.WarrantyId,
                        principalTable: "Warranties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WarrantyCommunications_WarrantyClaims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "WarrantyClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WarrantyCosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarrantyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uuid", nullable: true),
                    CostCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    PaidBy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PaidByPartyId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    InvoiceUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsBilled = table.Column<bool>(type: "boolean", nullable: false),
                    AccountingEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RegisteredByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_WarrantyCosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarrantyCosts_Employees_RegisteredByEmployeeId",
                        column: x => x.RegisteredByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WarrantyCosts_Warranties_WarrantyId",
                        column: x => x.WarrantyId,
                        principalTable: "Warranties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WarrantyCosts_WarrantyClaims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "WarrantyClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WarrantyEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarrantyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EventData = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsMilestone = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_WarrantyEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarrantyEvents_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WarrantyEvents_Warranties_WarrantyId",
                        column: x => x.WarrantyId,
                        principalTable: "Warranties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WarrantyEvents_WarrantyClaims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "WarrantyClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WarrantyPartRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarrantyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityRequested = table.Column<int>(type: "integer", nullable: false),
                    QuantityReceived = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    RequestNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpectedDeliveryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProviderAuthorizationCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProviderNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    InternalNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RequestedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_WarrantyPartRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarrantyPartRequests_Employees_ApprovedByEmployeeId",
                        column: x => x.ApprovedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WarrantyPartRequests_Employees_RequestedByEmployeeId",
                        column: x => x.RequestedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WarrantyPartRequests_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarrantyPartRequests_Warranties_WarrantyId",
                        column: x => x.WarrantyId,
                        principalTable: "Warranties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WarrantyPartRequests_WarrantyClaims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "WarrantyClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarrantyPartRequests_WarrantyProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "WarrantyProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarrantyStateHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarrantyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uuid", nullable: true),
                    FromStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ChangedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SlaBreached = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_WarrantyStateHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarrantyStateHistories_Employees_ChangedByEmployeeId",
                        column: x => x.ChangedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WarrantyStateHistories_Warranties_WarrantyId",
                        column: x => x.WarrantyId,
                        principalTable: "Warranties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WarrantyStateHistories_WarrantyClaims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "WarrantyClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WarrantyPartReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    QuantityReceived = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchLot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Condition = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    StorageLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    InventoryMovementId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceivedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_WarrantyPartReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarrantyPartReceipts_Employees_ReceivedByEmployeeId",
                        column: x => x.ReceivedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WarrantyPartReceipts_Locations_StorageLocationId",
                        column: x => x.StorageLocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WarrantyPartReceipts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarrantyPartReceipts_WarrantyPartRequests_PartRequestId",
                        column: x => x.PartRequestId,
                        principalTable: "WarrantyPartRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarrantyPartUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartReceiptId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityUsed = table.Column<int>(type: "integer", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_WarrantyPartUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarrantyPartUsages_Employees_UsedByEmployeeId",
                        column: x => x.UsedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WarrantyPartUsages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarrantyPartUsages_WarrantyClaims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "WarrantyClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarrantyPartUsages_WarrantyPartReceipts_PartReceiptId",
                        column: x => x.PartReceiptId,
                        principalTable: "WarrantyPartReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCreditNotes_WarrantyCostId",
                table: "SupplierCreditNotes",
                column: "WarrantyCostId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCreditNotes_WarrantyId",
                table: "SupplierCreditNotes",
                column: "WarrantyId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCreditNotes_WarrantyPartRequestId",
                table: "SupplierCreditNotes",
                column: "WarrantyPartRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCreditNotes_WarrantyProviderId",
                table: "SupplierCreditNotes",
                column: "WarrantyProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyAttachments_ClaimId",
                table: "WarrantyAttachments",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyAttachments_UploadedByEmployeeId",
                table: "WarrantyAttachments",
                column: "UploadedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyAttachments_WarrantyId",
                table: "WarrantyAttachments",
                column: "WarrantyId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyCommunications_ClaimId",
                table: "WarrantyCommunications",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyCommunications_SentByEmployeeId",
                table: "WarrantyCommunications",
                column: "SentByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyCommunications_WarrantyId",
                table: "WarrantyCommunications",
                column: "WarrantyId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyCosts_ClaimId",
                table: "WarrantyCosts",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyCosts_RegisteredByEmployeeId",
                table: "WarrantyCosts",
                column: "RegisteredByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyCosts_WarrantyId",
                table: "WarrantyCosts",
                column: "WarrantyId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyEvents_ClaimId",
                table: "WarrantyEvents",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyEvents_EmployeeId",
                table: "WarrantyEvents",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyEvents_WarrantyId_OccurredAt",
                table: "WarrantyEvents",
                columns: new[] { "WarrantyId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyPartReceipts_PartRequestId",
                table: "WarrantyPartReceipts",
                column: "PartRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyPartReceipts_ProductId",
                table: "WarrantyPartReceipts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyPartReceipts_ReceivedByEmployeeId",
                table: "WarrantyPartReceipts",
                column: "ReceivedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyPartReceipts_StorageLocationId",
                table: "WarrantyPartReceipts",
                column: "StorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyPartRequests_ApprovedByEmployeeId",
                table: "WarrantyPartRequests",
                column: "ApprovedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyPartRequests_ClaimId",
                table: "WarrantyPartRequests",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyPartRequests_ProductId",
                table: "WarrantyPartRequests",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyPartRequests_ProviderId",
                table: "WarrantyPartRequests",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyPartRequests_RequestedByEmployeeId",
                table: "WarrantyPartRequests",
                column: "RequestedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyPartRequests_RequestNumber",
                table: "WarrantyPartRequests",
                column: "RequestNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyPartRequests_WarrantyId",
                table: "WarrantyPartRequests",
                column: "WarrantyId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyPartUsages_ClaimId",
                table: "WarrantyPartUsages",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyPartUsages_PartReceiptId",
                table: "WarrantyPartUsages",
                column: "PartReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyPartUsages_ProductId",
                table: "WarrantyPartUsages",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyPartUsages_UsedByEmployeeId",
                table: "WarrantyPartUsages",
                column: "UsedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyStateHistories_ChangedByEmployeeId",
                table: "WarrantyStateHistories",
                column: "ChangedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyStateHistories_ClaimId",
                table: "WarrantyStateHistories",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyStateHistories_WarrantyId_ChangedAt",
                table: "WarrantyStateHistories",
                columns: new[] { "WarrantyId", "ChangedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierCreditNotes_Warranties_WarrantyId",
                table: "SupplierCreditNotes",
                column: "WarrantyId",
                principalTable: "Warranties",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierCreditNotes_WarrantyCosts_WarrantyCostId",
                table: "SupplierCreditNotes",
                column: "WarrantyCostId",
                principalTable: "WarrantyCosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierCreditNotes_WarrantyPartRequests_WarrantyPartReques~",
                table: "SupplierCreditNotes",
                column: "WarrantyPartRequestId",
                principalTable: "WarrantyPartRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierCreditNotes_WarrantyProviders_WarrantyProviderId",
                table: "SupplierCreditNotes",
                column: "WarrantyProviderId",
                principalTable: "WarrantyProviders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierCreditNotes_Warranties_WarrantyId",
                table: "SupplierCreditNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierCreditNotes_WarrantyCosts_WarrantyCostId",
                table: "SupplierCreditNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierCreditNotes_WarrantyPartRequests_WarrantyPartReques~",
                table: "SupplierCreditNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierCreditNotes_WarrantyProviders_WarrantyProviderId",
                table: "SupplierCreditNotes");

            migrationBuilder.DropTable(
                name: "WarrantyAttachments");

            migrationBuilder.DropTable(
                name: "WarrantyCommunications");

            migrationBuilder.DropTable(
                name: "WarrantyCosts");

            migrationBuilder.DropTable(
                name: "WarrantyEvents");

            migrationBuilder.DropTable(
                name: "WarrantyPartUsages");

            migrationBuilder.DropTable(
                name: "WarrantyStateHistories");

            migrationBuilder.DropTable(
                name: "WarrantyPartReceipts");

            migrationBuilder.DropTable(
                name: "WarrantyPartRequests");

            migrationBuilder.DropIndex(
                name: "IX_SupplierCreditNotes_WarrantyCostId",
                table: "SupplierCreditNotes");

            migrationBuilder.DropIndex(
                name: "IX_SupplierCreditNotes_WarrantyId",
                table: "SupplierCreditNotes");

            migrationBuilder.DropIndex(
                name: "IX_SupplierCreditNotes_WarrantyPartRequestId",
                table: "SupplierCreditNotes");

            migrationBuilder.DropIndex(
                name: "IX_SupplierCreditNotes_WarrantyProviderId",
                table: "SupplierCreditNotes");

            migrationBuilder.DropColumn(
                name: "Accessories",
                table: "WarrantyClaims");

            migrationBuilder.DropColumn(
                name: "FailureDescription",
                table: "WarrantyClaims");

            migrationBuilder.DropColumn(
                name: "FailureType",
                table: "WarrantyClaims");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "WarrantyClaims");

            migrationBuilder.DropColumn(
                name: "ProductCondition",
                table: "WarrantyClaims");

            migrationBuilder.DropColumn(
                name: "ProviderAuthorizationCode",
                table: "WarrantyClaims");

            migrationBuilder.DropColumn(
                name: "ProviderReferredAt",
                table: "WarrantyClaims");

            migrationBuilder.DropColumn(
                name: "SlaBreachedAt",
                table: "WarrantyClaims");

            migrationBuilder.DropColumn(
                name: "SlaDeadline",
                table: "WarrantyClaims");

            migrationBuilder.DropColumn(
                name: "WorkshopAssignedAt",
                table: "WarrantyClaims");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "SupplierCreditNotes");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "SupplierCreditNotes");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "SupplierCreditNotes");

            migrationBuilder.DropColumn(
                name: "WarrantyCostId",
                table: "SupplierCreditNotes");

            migrationBuilder.DropColumn(
                name: "WarrantyId",
                table: "SupplierCreditNotes");

            migrationBuilder.DropColumn(
                name: "WarrantyPartRequestId",
                table: "SupplierCreditNotes");

            migrationBuilder.DropColumn(
                name: "WarrantyProviderId",
                table: "SupplierCreditNotes");

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierId",
                table: "SupplierCreditNotes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "SupplierCreditNotes",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);
        }
    }
}
