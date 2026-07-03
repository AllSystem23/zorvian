-- ====================================================================
-- Zorvian ERP — Row Level Security (RLS) Migration
-- ====================================================================
-- TABLE NAMES VERIFIED against EF Core model snapshot
-- (ZorvianDbContextModelSnapshot.cs) — NO invented names.
-- Reference: PLAN_ACCION_INTEGRACION I-10 / M-8
-- ====================================================================

-- ====================================================================
-- Phase 1: Core business (14 tables)
-- ====================================================================

ALTER TABLE "Companies" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Employees" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Sales" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "SaleDetails" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Clients" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Products" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "InventoryMovements" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Purchases" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Suppliers" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Credits" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "CashMovements" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "AccountingEntries" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "AccountingEntryDetails" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Warranties" ENABLE ROW LEVEL SECURITY;

-- ====================================================================
-- Phase 2: Fleet & Logistics (30 tables)
-- ====================================================================

ALTER TABLE IF EXISTS "FleetVehicles" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "FleetDrivers" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "FleetRoutes" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "FleetRoutePoints" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "FleetDeliveries" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "FleetDeliveryItems" ENABLE ROW LEVEL SECURITY;
ALTER TABLE IF EXISTS "FleetTrips" ENABLE ROW LEVEL SECURITY;
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
ALTER TABLE IF EXISTS "GoalAssignmentProgressEntries" ENABLE ROW LEVEL SECURITY;
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
DO $$
DECLARE
    tbl TEXT;
BEGIN
    FOR tbl IN SELECT unnest(ARRAY[
        -- Phase 1: Core (exclude SaleDetails, AccountingEntryDetails — FK-based)
        'Companies','Employees','Sales','Clients','Products',
        'InventoryMovements','Purchases','Suppliers','Credits','CashMovements',
        'AccountingEntries','Warranties',
        -- Phase 2: Fleet (30)
        'FleetVehicles','FleetDrivers','FleetRoutes','FleetRoutePoints',
        'FleetDeliveries','FleetDeliveryItems','FleetTrips',
        'FuelRefills','WorkOrders','WorkOrderParts','Workshops',
        'FleetExpenses','ExpenseCategories','ExpenseSubcategories',
        'GpsPositions','Geofences','VehicleGeofenceStates',
        'VehicleBrands','VehicleTypes','FuelTypes',
        'DriverLicenseCategories','DriverInfractions','DriverTrainings',
        'FailureTypes','MaintenanceTemplates','MaintenanceSchedules',
        'FleetDocuments','DocumentTypes','FleetAlerts','FleetAlertRules',
        -- Phase 3: Payroll (13)
        'AttendanceRecords','VacationRequests','SickLeaveRecords',
        'PayrollRuns','PayrollDetails','PayrollConcepts',
        'TerminationRecords','PermissionRequests',
        'EmployeeSalaries','BenefitProvisions','BonusRecords',
        'CommissionRecords','OvertimeRecords',
        -- Phase 4: Goals (8)
        'GoalDefinitions','GoalAssignments','GoalAssignmentProgressEntries',
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
        EXECUTE format('DROP POLICY IF EXISTS tenant_isolation_%I ON %I', tbl, tbl);
        EXECUTE format(
            $$CREATE POLICY tenant_isolation_%I ON %I FOR ALL USING ("TenantId" = current_setting('app.tenant_id', true))$$,
            tbl, tbl
        );
    END LOOP;
END;
$$;

-- FK-based policies (child tables where TenantId is inherited via parent FK)
DROP POLICY IF EXISTS tenant_isolation_SaleDetails ON "SaleDetails";
CREATE POLICY tenant_isolation_SaleDetails ON "SaleDetails" FOR ALL USING (
    "SaleId" IN (SELECT "Id" FROM "Sales" WHERE "TenantId" = current_setting('app.tenant_id', true))
);

DROP POLICY IF EXISTS tenant_isolation_AccountingEntryDetails ON "AccountingEntryDetails";
CREATE POLICY tenant_isolation_AccountingEntryDetails ON "AccountingEntryDetails" FOR ALL USING (
    "AccountingEntryId" IN (SELECT "Id" FROM "AccountingEntries" WHERE "TenantId" = current_setting('app.tenant_id', true))
);

-- ====================================================================
-- SuperAdmin bypass policy
-- ====================================================================

CREATE OR REPLACE FUNCTION is_super_admin()
RETURNS BOOLEAN AS $$
BEGIN
    RETURN current_setting('app.is_super_admin', true)::boolean = true;
END;
$$ LANGUAGE plpgsql STABLE;

DO $$
DECLARE
    tbl TEXT;
BEGIN
    FOR tbl IN SELECT unnest(ARRAY[
        'Companies','Employees','Sales','SaleDetails','Clients','Products',
        'InventoryMovements','Purchases','Suppliers','Credits','CashMovements',
        'AccountingEntries','AccountingEntryDetails','Warranties',
        'FleetVehicles','FleetDrivers','FleetRoutes','FleetRoutePoints',
        'FleetDeliveries','FleetDeliveryItems','FleetTrips',
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
        'GoalDefinitions','GoalAssignments','GoalAssignmentProgressEntries',
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
        EXECUTE format('DROP POLICY IF EXISTS super_admin_bypass_%I ON %I', tbl, tbl);
        EXECUTE format(
            $$CREATE POLICY super_admin_bypass_%I ON %I FOR ALL USING (is_super_admin())$$,
            tbl, tbl
        );
    END LOOP;
END;
$$;

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
CREATE INDEX IF NOT EXISTS idx_fleet_vehicles_tenant_id ON "FleetVehicles" ("TenantId");
CREATE INDEX IF NOT EXISTS idx_fleet_trips_tenant_id ON "FleetTrips" ("TenantId");
CREATE INDEX IF NOT EXISTS idx_fleet_deliveries_tenant_id ON "FleetDeliveries" ("TenantId");
CREATE INDEX IF NOT EXISTS idx_payroll_runs_tenant_id ON "PayrollRuns" ("TenantId");
CREATE INDEX IF NOT EXISTS idx_goal_defs_tenant_id ON "GoalDefinitions" ("TenantId");
CREATE INDEX IF NOT EXISTS idx_budgets_tenant_id ON "Budgets" ("TenantId");
