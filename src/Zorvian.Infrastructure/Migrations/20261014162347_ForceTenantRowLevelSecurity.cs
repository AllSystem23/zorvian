using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ForceTenantRowLevelSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AvgSalary6Months",
                table: "TerminationRecords",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVariableSalary",
                table: "TerminationRecords",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxSalary6Months",
                table: "TerminationRecords",
                type: "numeric",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VacationPaymentReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VacationRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReturnDate = table.Column<DateOnly>(type: "date", nullable: false),
                    BusinessDays = table.Column<decimal>(type: "numeric", nullable: false),
                    DailySalary = table.Column<decimal>(type: "numeric", nullable: false),
                    MonthlySalary = table.Column<decimal>(type: "numeric", nullable: false),
                    VacationPay = table.Column<decimal>(type: "numeric", nullable: false),
                    InssEmployeeAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    InssEmployeeRate = table.Column<decimal>(type: "numeric", nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Currency = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Observations = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PaidBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_VacationPaymentReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VacationPaymentReceipts_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VacationPaymentReceipts_VacationRequests_VacationRequestId",
                        column: x => x.VacationRequestId,
                        principalTable: "VacationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VacationPaymentReceipts_EmployeeId_Status",
                table: "VacationPaymentReceipts",
                columns: new[] { "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VacationPaymentReceipts_ReceiptNumber",
                table: "VacationPaymentReceipts",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VacationPaymentReceipts_VacationRequestId",
                table: "VacationPaymentReceipts",
                column: "VacationRequestId");

            ForceRowLevelSecurity(migrationBuilder);
        }

        /// <summary>
        /// Activa <c>FORCE ROW LEVEL SECURITY</c> y reescribe las políticas tenant/sucursal para que
        /// el forzado sea seguro. Hasta ahora las políticas eran inertes (owner bypass).
        /// <list type="number">
        ///   <item>Escape de contexto ausente: <c>app.tenant_id</c> vacío o GUID-cero → no se acota.
        ///   Paridad con los <c>HasQueryFilter</c> de EF; es lo que mantiene funcionando login,
        ///   migraciones de arranque y jobs globales (SalesPredictionTrainingJob consulta con
        ///   <c>IgnoreQueryFilters()</c> y tenant-cero: con política estricta devolvería 0 filas).</item>
        ///   <item>Tablas con <c>BranchId</c>: política COMBINADA tenant AND sucursal. Las políticas
        ///   permisivas de PostgreSQL se combinan con OR, así que mantener <c>branch_scope_*</c>
        ///   aparte habría dejado visibles filas de OTROS tenants al forzar RLS.</item>
        ///   <item><c>FORCE ROW LEVEL SECURITY</c> en todas las tablas con política tenant.</item>
        /// </list>
        /// <c>super_admin_bypass_*</c> se mantiene (OR): SuperAdmin sin compañía seleccionada sigue
        /// viendo todo vía el GUC <c>app.is_super_admin</c>. El runbook del rol no-owner queda como
        /// paso posterior (reducción de privilegios); el forzado no lo requiere.
        /// </summary>
        private static void ForceRowLevelSecurity(MigrationBuilder migrationBuilder)
        {
            const string NoTenantContext =
                "coalesce(current_setting('app.tenant_id', true), '') IN ('', '00000000-0000-0000-0000-000000000000')";

            migrationBuilder.Sql($"""
                DO $$
                DECLARE
                    tbl TEXT;
                BEGIN
                    FOR tbl IN SELECT unnest(ARRAY[
                        'Companies','Employees','Sales','Clients','Products',
                        'InventoryMovements','Purchases','Suppliers','Credits','CashMovements',
                        'AccountingEntries','Warranties','VacationRequests','VacationPaymentReceipts',
                        'Vehicles','Drivers','Routes','RoutePoints',
                        'Deliveries','DeliveryItems','Trips',
                        'FuelRefills','WorkOrders','WorkOrderParts','Workshops',
                        'FleetExpenses','ExpenseCategories','ExpenseSubcategories',
                        'GpsPositions','Geofences','VehicleGeofenceStates',
                        'VehicleBrands','VehicleTypes','FuelTypes',
                        'DriverLicenseCategories','DriverInfractions','DriverTrainings',
                        'FailureTypes','MaintenanceTemplates','MaintenanceSchedules',
                        'FleetDocuments','DocumentTypes','FleetAlerts','FleetAlertRules',
                        'AttendanceRecords','SickLeaveRecords',
                        'PayrollRuns','PayrollDetails','PayrollConcepts',
                        'TerminationRecords','PermissionRequests',
                        'EmployeeSalaries','BenefitProvisions','BonusRecords',
                        'CommissionRecords','OvertimeRecords',
                        'GoalDefinitions','GoalAssignments','GoalProgressEntries',
                        'Incentives','IncentivePayments',
                        'KpiDefinitions','KpiRecords','Budgets',
                        'Banks','BankAccounts','Checkbooks','Checks',
                        'CheckAuditTrails','CheckPrintTemplates',
                        'CashRegisters','CashRegisterArqueos','CashArqueoDenominations',
                        'Reconciliations','ReconciliationDetails',
                        'BudgetDetails','BudgetTrackings',
                        'SupplierPayments','SupplierCreditNotes',
                        'CreditNotes','CreditNoteDetails',
                        'PurchaseOrders','PurchaseOrderDetails','PurchaseDetails'
                    ])
                    LOOP
                        -- Guarda de existencia: en una BD nueva EnableRLS corre antes de que
                        -- existan tablas de migraciones posteriores (no-op en ellas).
                        IF to_regclass(format('public.%I', tbl)) IS NULL THEN
                            CONTINUE;
                        END IF;

                        EXECUTE format('DROP POLICY IF EXISTS tenant_isolation_%I ON %I', tbl, tbl);
                        EXECUTE format(
                            $$CREATE POLICY tenant_isolation_%I ON %I FOR ALL USING (
                                {NoTenantContext}
                                OR "TenantId" = current_setting('app.tenant_id', true)
                            )$$,
                            tbl, tbl
                        );
                        EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', tbl);
                        EXECUTE format('ALTER TABLE %I FORCE ROW LEVEL SECURITY', tbl);
                    END LOOP;
                END;
                $$;
                """);

            // Tablas hijas con TenantId heredado por FK del padre.
            migrationBuilder.Sql($"""
                DROP POLICY IF EXISTS tenant_isolation_SaleDetails ON "SaleDetails";
                CREATE POLICY tenant_isolation_SaleDetails ON "SaleDetails" FOR ALL USING (
                    {NoTenantContext}
                    OR "SaleId" IN (SELECT "Id" FROM "Sales" WHERE "TenantId" = current_setting('app.tenant_id', true))
                );
                ALTER TABLE "SaleDetails" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "SaleDetails" FORCE ROW LEVEL SECURITY;

                DROP POLICY IF EXISTS tenant_isolation_AccountingEntryDetails ON "AccountingEntryDetails";
                CREATE POLICY tenant_isolation_AccountingEntryDetails ON "AccountingEntryDetails" FOR ALL USING (
                    {NoTenantContext}
                    OR "AccountingEntryId" IN (SELECT "Id" FROM "AccountingEntries" WHERE "TenantId" = current_setting('app.tenant_id', true))
                );
                ALTER TABLE "AccountingEntryDetails" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "AccountingEntryDetails" FORCE ROW LEVEL SECURITY;
                """);

            // Tablas con columna BranchId: una sola política combinada (tenant AND sucursal).
            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    tbl TEXT;
                BEGIN
                    FOR tbl IN
                        SELECT table_name
                        FROM information_schema.columns
                        WHERE column_name = 'BranchId' AND table_schema = 'public'
                    LOOP
                        IF to_regclass(format('public.%I', tbl)) IS NULL THEN
                            CONTINUE;
                        END IF;

                        EXECUTE format('DROP POLICY IF EXISTS tenant_isolation_%I ON %I', tbl, tbl);
                        EXECUTE format('DROP POLICY IF EXISTS branch_scope_%I ON %I', tbl, tbl);
                        EXECUTE format(
                            $$CREATE POLICY tenant_branch_scope_%I ON %I FOR ALL USING (
                                (
                                    coalesce(current_setting('app.tenant_id', true), '') IN ('', '00000000-0000-0000-0000-000000000000')
                                    OR "TenantId" = current_setting('app.tenant_id', true)
                                )
                                AND (
                                    current_setting('app.branch_id', true) = ''
                                    OR "BranchId"::text = current_setting('app.branch_id', true)
                                )
                            )$$,
                            tbl, tbl
                        );
                        EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', tbl);
                        EXECUTE format('ALTER TABLE %I FORCE ROW LEVEL SECURITY', tbl);
                    END LOOP;
                END;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revierte al estado previo: políticas estrictas SIN escape y sin forzado (owner bypass).
            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    tbl TEXT;
                BEGIN
                    FOR tbl IN
                        SELECT table_name
                        FROM information_schema.columns
                        WHERE column_name = 'BranchId' AND table_schema = 'public'
                    LOOP
                        IF to_regclass(format('public.%I', tbl)) IS NULL THEN
                            CONTINUE;
                        END IF;

                        EXECUTE format('DROP POLICY IF EXISTS tenant_branch_scope_%I ON %I', tbl, tbl);
                        EXECUTE format('DROP POLICY IF EXISTS tenant_isolation_%I ON %I', tbl, tbl);
                        EXECUTE format('ALTER TABLE %I NO FORCE ROW LEVEL SECURITY', tbl);
                        EXECUTE format(
                            $$CREATE POLICY tenant_isolation_%I ON %I FOR ALL USING ("TenantId" = current_setting('app.tenant_id', true))$$,
                            tbl, tbl
                        );
                        EXECUTE format(
                            $$CREATE POLICY branch_scope_%I ON %I FOR ALL USING (
                                current_setting('app.branch_id', true) = ''
                                OR "BranchId"::text = current_setting('app.branch_id', true)
                            )$$,
                            tbl, tbl
                        );
                    END LOOP;
                END;
                $$;
                """);

            migrationBuilder.Sql($"""
                DO $$
                DECLARE
                    tbl TEXT;
                BEGIN
                    FOR tbl IN SELECT unnest(ARRAY[
                        'Companies','Employees','Sales','Clients','Products',
                        'InventoryMovements','Purchases','Suppliers','Credits','CashMovements',
                        'AccountingEntries','Warranties','VacationRequests','VacationPaymentReceipts',
                        'Vehicles','Drivers','Routes','RoutePoints',
                        'Deliveries','DeliveryItems','Trips',
                        'FuelRefills','WorkOrders','WorkOrderParts','Workshops',
                        'FleetExpenses','ExpenseCategories','ExpenseSubcategories',
                        'GpsPositions','Geofences','VehicleGeofenceStates',
                        'VehicleBrands','VehicleTypes','FuelTypes',
                        'DriverLicenseCategories','DriverInfractions','DriverTrainings',
                        'FailureTypes','MaintenanceTemplates','MaintenanceSchedules',
                        'FleetDocuments','DocumentTypes','FleetAlerts','FleetAlertRules',
                        'AttendanceRecords','SickLeaveRecords',
                        'PayrollRuns','PayrollDetails','PayrollConcepts',
                        'TerminationRecords','PermissionRequests',
                        'EmployeeSalaries','BenefitProvisions','BonusRecords',
                        'CommissionRecords','OvertimeRecords',
                        'GoalDefinitions','GoalAssignments','GoalProgressEntries',
                        'Incentives','IncentivePayments',
                        'KpiDefinitions','KpiRecords','Budgets',
                        'Banks','BankAccounts','Checkbooks','Checks',
                        'CheckAuditTrails','CheckPrintTemplates',
                        'CashRegisters','CashRegisterArqueos','CashArqueoDenominations',
                        'Reconciliations','ReconciliationDetails',
                        'BudgetDetails','BudgetTrackings',
                        'SupplierPayments','SupplierCreditNotes',
                        'CreditNotes','CreditNoteDetails',
                        'PurchaseOrders','PurchaseOrderDetails','PurchaseDetails',
                        'SaleDetails','AccountingEntryDetails'
                    ])
                    LOOP
                        EXECUTE format('DROP POLICY IF EXISTS tenant_isolation_%I ON %I', tbl, tbl);
                        EXECUTE format('DROP POLICY IF EXISTS tenant_branch_scope_%I ON %I', tbl, tbl);
                        EXECUTE format('ALTER TABLE %I NO FORCE ROW LEVEL SECURITY', tbl);
                    END LOOP;
                END;
                $$;
                """);

            migrationBuilder.DropTable(
                name: "VacationPaymentReceipts");

            migrationBuilder.DropColumn(
                name: "AvgSalary6Months",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "IsVariableSalary",
                table: "TerminationRecords");

            migrationBuilder.DropColumn(
                name: "MaxSalary6Months",
                table: "TerminationRecords");
        }
    }
}
