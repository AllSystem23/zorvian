-- ====================================================================
-- Zorvian ERP — Row Level Security (RLS) Migration
-- ====================================================================
-- Implements PostgreSQL Row Level Security for multi-tenant isolation
-- Reference: Audit Plan P4.9
-- ====================================================================

-- Step 1: Enable RLS on critical tables
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

-- Step 2: Create RLS policies for SELECT
-- ====================================================================

-- Companies: tenant can only see their own company
DROP POLICY IF EXISTS tenant_isolation_companies_select ON "Companies";
CREATE POLICY tenant_isolation_companies_select ON "Companies"
    FOR SELECT
    USING ("TenantId" = current_setting('app.tenant_id', true));

-- Employees
DROP POLICY IF EXISTS tenant_isolation_employees ON "Employees";
CREATE POLICY tenant_isolation_employees ON "Employees"
    FOR ALL
    USING ("TenantId" = current_setting('app.tenant_id', true));

-- Sales
DROP POLICY IF EXISTS tenant_isolation_sales ON "Sales";
CREATE POLICY tenant_isolation_sales ON "Sales"
    FOR ALL
    USING ("TenantId" = current_setting('app.tenant_id', true));

-- SaleDetails (inherits from Sales)
DROP POLICY IF EXISTS tenant_isolation_sale_details ON "SaleDetails";
CREATE POLICY tenant_isolation_sale_details ON "SaleDetails"
    FOR ALL
    USING ("SaleId" IN (
        SELECT "Id" FROM "Sales"
        WHERE "TenantId" = current_setting('app.tenant_id', true)
    ));

-- Clients
DROP POLICY IF EXISTS tenant_isolation_clients ON "Clients";
CREATE POLICY tenant_isolation_clients ON "Clients"
    FOR ALL
    USING ("TenantId" = current_setting('app.tenant_id', true));

-- Products
DROP POLICY IF EXISTS tenant_isolation_products ON "Products";
CREATE POLICY tenant_isolation_products ON "Products"
    FOR ALL
    USING ("TenantId" = current_setting('app.tenant_id', true));

-- InventoryMovements
DROP POLICY IF EXISTS tenant_isolation_inventory ON "InventoryMovements";
CREATE POLICY tenant_isolation_inventory ON "InventoryMovements"
    FOR ALL
    USING ("TenantId" = current_setting('app.tenant_id', true));

-- Purchases
DROP POLICY IF EXISTS tenant_isolation_purchases ON "Purchases";
CREATE POLICY tenant_isolation_purchases ON "Purchases"
    FOR ALL
    USING ("TenantId" = current_setting('app.tenant_id', true));

-- Suppliers
DROP POLICY IF EXISTS tenant_isolation_suppliers ON "Suppliers";
CREATE POLICY tenant_isolation_suppliers ON "Suppliers"
    FOR ALL
    USING ("TenantId" = current_setting('app.tenant_id', true));

-- Credits
DROP POLICY IF EXISTS tenant_isolation_credits ON "Credits";
CREATE POLICY tenant_isolation_credits ON "Credits"
    FOR ALL
    USING ("TenantId" = current_setting('app.tenant_id', true));

-- CashMovements
DROP POLICY IF EXISTS tenant_isolation_cash ON "CashMovements";
CREATE POLICY tenant_isolation_cash ON "CashMovements"
    FOR ALL
    USING ("TenantId" = current_setting('app.tenant_id', true));

-- AccountingEntries
DROP POLICY IF EXISTS tenant_isolation_accounting ON "AccountingEntries";
CREATE POLICY tenant_isolation_accounting ON "AccountingEntries"
    FOR ALL
    USING ("TenantId" = current_setting('app.tenant_id', true));

-- AccountingEntryDetails
DROP POLICY IF EXISTS tenant_isolation_accounting_details ON "AccountingEntryDetails";
CREATE POLICY tenant_isolation_accounting_details ON "AccountingEntryDetails"
    FOR ALL
    USING ("EntryId" IN (
        SELECT "Id" FROM "AccountingEntries"
        WHERE "TenantId" = current_setting('app.tenant_id', true)
    ));

-- Warranties
DROP POLICY IF EXISTS tenant_isolation_warranties ON "Warranties";
CREATE POLICY tenant_isolation_warranties ON "Warranties"
    FOR ALL
    USING ("TenantId" = current_setting('app.tenant_id', true));

-- Step 3: SuperAdmin bypass policy
-- ====================================================================

-- Create a function to check if current user is super admin
CREATE OR REPLACE FUNCTION is_super_admin()
RETURNS BOOLEAN AS $$
BEGIN
    RETURN current_setting('app.is_super_admin', true)::boolean = true;
END;
$$ LANGUAGE plpgsql STABLE;

-- Update all policies to allow super admin bypass
DROP POLICY IF EXISTS super_admin_bypass_companies ON "Companies";
CREATE POLICY super_admin_bypass_companies ON "Companies"
    FOR ALL
    USING (is_super_admin() OR "TenantId" = current_setting('app.tenant_id', true));

DROP POLICY IF EXISTS super_admin_bypass_employees ON "Employees";
CREATE POLICY super_admin_bypass_employees ON "Employees"
    FOR ALL
    USING (is_super_admin() OR "TenantId" = current_setting('app.tenant_id', true));

-- Repeat for other tables...

-- Step 4: Verify policies
-- ====================================================================

-- Query to check RLS status
SELECT
    schemaname,
    tablename,
    rowsecurity AS rls_enabled,
    (SELECT count(*) FROM pg_policies WHERE tablename = t.tablename) AS policy_count
FROM pg_tables t
WHERE schemaname = 'public'
    AND tablename IN ('Companies', 'Employees', 'Sales', 'Clients', 'Products')
ORDER BY tablename;

-- Step 5: Performance considerations
-- ====================================================================

-- Add index on TenantId if not exists
CREATE INDEX IF NOT EXISTS idx_companies_tenant_id ON "Companies" ("TenantId");
CREATE INDEX IF NOT EXISTS idx_employees_tenant_id ON "Employees" ("TenantId");
CREATE INDEX IF NOT EXISTS idx_sales_tenant_id ON "Sales" ("TenantId");
CREATE INDEX IF NOT EXISTS idx_clients_tenant_id ON "Clients" ("TenantId");
CREATE INDEX IF NOT EXISTS idx_products_tenant_id ON "Products" ("TenantId");

-- Step 6: Testing queries
-- ====================================================================

-- Test 1: Set tenant context and verify only that tenant's data is visible
-- SET app.tenant_id = 'ten_001';
-- SELECT COUNT(*) FROM "Sales"; -- Should return only ten_001 sales

-- Test 2: Try to access another tenant's data
-- SET app.tenant_id = 'ten_002';
-- SELECT * FROM "Sales" WHERE "TenantId" = 'ten_001'; -- Should return 0 rows

-- Test 3: Super admin bypass
-- SET app.is_super_admin = 'true';
-- SELECT COUNT(*) FROM "Sales"; -- Should return all sales

-- ====================================================================
-- ROLLBACK (only for testing, NOT production)
-- ====================================================================
-- ALTER TABLE "Companies" DISABLE ROW LEVEL SECURITY;
-- ALTER TABLE "Employees" DISABLE ROW LEVEL SECURITY;
-- ... (repeat for all tables)
</content>
<parameter name="task_progress">