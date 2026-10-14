-- ====================================================================
-- Zorvian ERP — Row Level Security (RLS) Migration
-- ====================================================================
-- TABLE NAMES VERIFIED against EF Core model snapshot
-- (ZorvianDbContextModelSnapshot.cs) + scripts/create_fleet_tables.sql
-- (tablas Fleet reales: Vehicles, Drivers, Routes, RoutePoints,
-- Deliveries, DeliveryItems, Trips — SIN prefijo "Fleet").
-- Idempotente: seguro de re-ejecutar.
-- Embedded in Zorvian.Infrastructure (LogicalName SecurityScripts.SECURITY_RLS.sql)
-- and applied by EnableRLS migration.
-- Reference: PLAN_ACCION_INTEGRACION I-10 / M-8
--
-- NOTE ON FORCE (actualizado 2026-10-14): SÍ se aplica
-- FORCE ROW LEVEL SECURITY sobre las tablas con política tenant
-- (ver más abajo). El escape de contexto ausente es lo que hace seguro el forzado:
-- las políticas tenant NO acotan cuando app.tenant_id está vacío o en GUID-cero
-- (login, migraciones de arranque y jobs globales con tenant-cero siguen
-- funcionando; SalesPredictionTrainingJob consulta con IgnoreQueryFilters()).
--
-- ORDEN DE OR POLICIES (IMPORTANTE): las políticas permisivas de PostgreSQL se
-- combinan con OR. Por eso las tablas con columna BranchId usan UNA política
-- combinada (tenant AND sucursal) y NO tenant_isolation + branch_scope por
-- separado: branch_scope pasaba siempre que no hubiera sucursal seleccionada y
-- habría dejado visibles filas de OTROS tenants al forzar RLS.
--
-- Runbook del rol no-owner (paso posterior de reducción de privilegios, NO
-- requerido para el forzado por el escape de arriba): crear rol no-owner de la
-- app + GRANT DML + cambiar connection string, y excluir del forzado las tablas
-- de auth (Users, UserSessions, UserInvitations) si en el futuro se les añade
-- política. Ver docs/test_rls_staging.sql antes de promover.
-- ====================================================================

-- ====================================================================
-- Phase 1: Core business (14 tables)
-- ====================================================================

ALTER TABLE IF EXISTS "Companies" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "Employees" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "Sales" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "SaleDetails" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "Clients" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "Products" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "InventoryMovements" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "Purchases" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "Suppliers" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "Credits" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "CashMovements" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "AccountingEntries" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "AccountingEntryDetails" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "Warranties" ENABLE ROW LEVEL SECURITY;

-- ====================================================================
-- Phase 2: Fleet & Logistics (30 tables)
-- ====================================================================

ALTER TABLE IF EXISTS "Vehicles" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "Drivers" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "Routes" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "RoutePoints" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "Deliveries" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "DeliveryItems" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "Trips" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "FuelRefills" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "WorkOrders" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "WorkOrderParts" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "Workshops" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "FleetExpenses" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "ExpenseCategories" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "ExpenseSubcategories" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "GpsPositions" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "Geofences" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "VehicleGeofenceStates" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "VehicleBrands" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "VehicleTypes" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "FuelTypes" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "DriverLicenseCategories" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "DriverInfractions" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "DriverTrainings" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "FailureTypes" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "MaintenanceTemplates" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "MaintenanceSchedules" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "FleetDocuments" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "DocumentTypes" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "FleetAlerts" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "FleetAlertRules" ENABLE ROW LEVEL SECURITY;

-- ====================================================================
-- Phase 3: Payroll & HR (13 tables)
-- ====================================================================

ALTER TABLE IF EXISTS "AttendanceRecords" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "VacationRequests" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "SickLeaveRecords" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "PayrollRuns" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "PayrollDetails" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "PayrollConcepts" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "TerminationRecords" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "PermissionRequests" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "EmployeeSalaries" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "BenefitProvisions" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "BonusRecords" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "CommissionRecords" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "OvertimeRecords" ENABLE ROW LEVEL SECURITY;

-- ====================================================================
-- Phase 4: Goals, Incentives & KPI (8 tables)
-- ====================================================================

ALTER TABLE IF EXISTS "GoalDefinitions" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "GoalAssignments" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "GoalProgressEntries" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "Incentives" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "IncentivePayments" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "KpiDefinitions" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "KpiRecords" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "Budgets" ENABLE ROW LEVEL SECURITY;

-- ====================================================================
-- Phase 5: Treasury & Banking (9 tables)
-- Phase 7: Budgeting & Reconciliation (4 tables)
-- ====================================================================

ALTER TABLE IF EXISTS "Reconciliations" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "ReconciliationDetails" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "BudgetDetails" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "BudgetTrackings" ENABLE ROW LEVEL SECURITY;

-- ====================================================================
-- Phase 6: Purchases & Suppliers (7 tables)
-- ====================================================================

ALTER TABLE IF EXISTS "Banks" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "BankAccounts" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "Checkbooks" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "Checks" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "CheckAuditTrails" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "CheckPrintTemplates" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "CashRegisters" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "CashRegisterArqueos" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "CashArqueoDenominations" ENABLE ROW LEVEL SECURITY;

-- ====================================================================
-- Phase 6: Purchases & Suppliers (7 tables)
-- ====================================================================

ALTER TABLE IF EXISTS "SupplierPayments" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "SupplierCreditNotes" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "CreditNotes" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "CreditNoteDetails" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "PurchaseOrders" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "PurchaseOrderDetails" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "PurchaseDetails" ENABLE ROW LEVEL SECURITY;

-- ====================================================================
-- Create RLS policies via DO blocks
-- ====================================================================

-- TenantId-based tables (includes ALL except FK-based child tables)
-- Escape de contexto ausente: sin tenant resuelto (vacío o GUID-cero) NO se acota.
DO $do$
DECLARE
    tbl TEXT;
BEGIN
    FOR tbl IN SELECT unnest(ARRAY[
        -- Phase 1: Core (exclude SaleDetails, AccountingEntryDetails — FK-based)
        'Companies','Employees','Sales','Clients','Products',
        'InventoryMovements','Purchases','Suppliers','Credits','CashMovements',
        'AccountingEntries','Warranties',
        -- Phase 3: Payroll (13) — VacationRequests + recibos de pago
        'AttendanceRecords','VacationRequests','VacationPaymentReceipts','SickLeaveRecords',
        'PayrollRuns','PayrollDetails','PayrollConcepts',
        'TerminationRecords','PermissionRequests',
        'EmployeeSalaries','BenefitProvisions','BonusRecords',
        'CommissionRecords','OvertimeRecords',
        -- Phase 2: Fleet (30)
        'Vehicles','Drivers','Routes','RoutePoints',
        'Deliveries','DeliveryItems','Trips',
        'FuelRefills','WorkOrders','WorkOrderParts','Workshops',
        'FleetExpenses','ExpenseCategories','ExpenseSubcategories',
        'GpsPositions','Geofences','VehicleGeofenceStates',
        'VehicleBrands','VehicleTypes','FuelTypes',
        'DriverLicenseCategories','DriverInfractions','DriverTrainings',
        'FailureTypes','MaintenanceTemplates','MaintenanceSchedules',
        'FleetDocuments','DocumentTypes','FleetAlerts','FleetAlertRules',
        -- Phase 4: Goals (8)
        'GoalDefinitions','GoalAssignments','GoalProgressEntries',
        'Incentives','IncentivePayments',
        'KpiDefinitions','KpiRecords','Budgets',
        -- Phase 5: Treasury (9)
        'Banks','BankAccounts','Checkbooks','Checks',
        'CheckAuditTrails','CheckPrintTemplates',
        'CashRegisters','CashRegisterArqueos','CashArqueoDenominations',
        -- Phase 7: Budgeting & Reconciliation (4)
        'Reconciliations','ReconciliationDetails',
        'BudgetDetails','BudgetTrackings',
        -- Phase 6: Purchases (7)
        'SupplierPayments','SupplierCreditNotes',
        'CreditNotes','CreditNoteDetails',
        'PurchaseOrders','PurchaseOrderDetails','PurchaseDetails'
    ])
    LOOP
        IF to_regclass(format('public.%I', tbl)) IS NULL THEN
            CONTINUE;
        END IF;

        EXECUTE format('DROP POLICY IF EXISTS tenant_isolation_%I ON %I', tbl, tbl);
        EXECUTE format(
            $$CREATE POLICY tenant_isolation_%I ON %I FOR ALL USING (
                coalesce(current_setting('app.tenant_id', true), '') IN ('', '00000000-0000-0000-0000-000000000000')
                OR "TenantId" = current_setting('app.tenant_id', true)
            )$$,
            tbl, tbl
        );
        EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', tbl);
        EXECUTE format('ALTER TABLE %I FORCE ROW LEVEL SECURITY', tbl);
    END LOOP;
END;
$do$;

-- FK-based policies (child tables where TenantId is inherited via parent FK)
DROP POLICY IF EXISTS tenant_isolation_SaleDetails ON "SaleDetails";
CREATE POLICY tenant_isolation_SaleDetails ON "SaleDetails" FOR ALL USING (
    coalesce(current_setting('app.tenant_id', true), '') IN ('', '00000000-0000-0000-0000-000000000000')
    OR "SaleId" IN (SELECT "Id" FROM "Sales" WHERE "TenantId" = current_setting('app.tenant_id', true))
);
ALTER TABLE "SaleDetails" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "SaleDetails" FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS tenant_isolation_AccountingEntryDetails ON "AccountingEntryDetails";
CREATE POLICY tenant_isolation_AccountingEntryDetails ON "AccountingEntryDetails" FOR ALL USING (
    coalesce(current_setting('app.tenant_id', true), '') IN ('', '00000000-0000-0000-0000-000000000000')
    OR "AccountingEntryId" IN (SELECT "Id" FROM "AccountingEntries" WHERE "TenantId" = current_setting('app.tenant_id', true))
);
ALTER TABLE "AccountingEntryDetails" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "AccountingEntryDetails" FORCE ROW LEVEL SECURITY;

-- ====================================================================
-- SuperAdmin bypass policy
-- ====================================================================

CREATE OR REPLACE FUNCTION is_super_admin()
RETURNS BOOLEAN AS $$
BEGIN
    RETURN current_setting('app.is_super_admin', true)::boolean = true;
END;
$$ LANGUAGE plpgsql STABLE;

DO $do$
DECLARE
    tbl TEXT;
BEGIN
    FOR tbl IN SELECT unnest(ARRAY[
        'Companies','Employees','Sales','SaleDetails','Clients','Products',
        'InventoryMovements','Purchases','Suppliers','Credits','CashMovements',
        'AccountingEntries','AccountingEntryDetails','Warranties',
        'Vehicles','Drivers','Routes','RoutePoints',
        'Deliveries','DeliveryItems','Trips',
        'FuelRefills','WorkOrders','WorkOrderParts','Workshops',
        'FleetExpenses','ExpenseCategories','ExpenseSubcategories',
        'GpsPositions','Geofences','VehicleGeofenceStates',
        'VehicleBrands','VehicleTypes','FuelTypes',
        'DriverLicenseCategories','DriverInfractions','DriverTrainings',
        'FailureTypes','MaintenanceTemplates','MaintenanceSchedules',
        'FleetDocuments','DocumentTypes','FleetAlerts','FleetAlertRules',
        'AttendanceRecords','VacationRequests','SickLeaveRecords',
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
        IF to_regclass(format('public.%I', tbl)) IS NULL THEN
            CONTINUE;
        END IF;

        EXECUTE format('DROP POLICY IF EXISTS super_admin_bypass_%I ON %I', tbl, tbl);
        EXECUTE format(
            $$CREATE POLICY super_admin_bypass_%I ON %I FOR ALL USING (is_super_admin())$$,
            tbl, tbl
        );
    END LOOP;
END;
$do$;

-- ====================================================================
-- Branch-scope policies (defensa en profundidad) — COMBINADAS con tenant
-- ====================================================================
-- Semántica PARIDAD con el invariante del proyecto: la sucursal es filtro EXPLÍCITO opt-in.
--   app.branch_id = '' (sin sucursal seleccionada) → la política NO acota (company-wide).
--   cualquier otro valor        → solo filas de esa sucursal (BranchId::text = app.branch_id).
-- Las tablas con columna BranchId usan UNA política combinada tenant AND sucursal: con FORCE
-- activo, mantener tenant_isolation y branch_scope por separado (OR) habría dejado visibles
-- filas de OTROS tenants cuando no hay sucursal seleccionada.
DO $do$
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
$do$;

-- ====================================================================
-- Performance indexes
-- ====================================================================

CREATE INDEX IF NOT EXISTS idx_companies_tenant_id ON "Companies" ("TenantId");
CREATE INDEX IF NOT EXISTS idx_employees_tenant_id ON "Employees" ("TenantId");
CREATE INDEX IF NOT EXISTS idx_sales_tenant_id ON "Sales" ("TenantId");
CREATE INDEX IF NOT EXISTS idx_clients_tenant_id ON "Clients" ("TenantId");
CREATE INDEX IF NOT EXISTS idx_products_tenant_id ON "Products" ("TenantId");
CREATE INDEX IF NOT EXISTS idx_credits_tenant_id ON "Credits" ("TenantId");
CREATE INDEX IF NOT EXISTS idx_purchases_tenant_id ON "Purchases" ("TenantId");
CREATE INDEX IF NOT EXISTS idx_fleet_vehicles_tenant_id ON "Vehicles" ("TenantId");
CREATE INDEX IF NOT EXISTS idx_fleet_trips_tenant_id ON "Trips" ("TenantId");
CREATE INDEX IF NOT EXISTS idx_fleet_deliveries_tenant_id ON "Deliveries" ("TenantId");
CREATE INDEX IF NOT EXISTS idx_payroll_runs_tenant_id ON "PayrollRuns" ("TenantId");
CREATE INDEX IF NOT EXISTS idx_goal_defs_tenant_id ON "GoalDefinitions" ("TenantId");
CREATE INDEX IF NOT EXISTS idx_budgets_tenant_id ON "Budgets" ("TenantId");
