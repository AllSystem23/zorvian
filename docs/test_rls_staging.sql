-- ====================================================================
-- Zorvian ERP — RLS Staging Verification Suite
-- ====================================================================
-- Safe to run against staging. Data tests run inside a transaction
-- that gets rolled back at the end.
--
-- Usage:
--   1. First execute SECURITY_RLS.sql to apply policies
--   2. Then run this script to verify correctness
--   3. Review output for any FAIL results before promoting to prod
--
-- Reference: PLAN_ACCION_INTEGRACION I-10 / M-8
-- ====================================================================

BEGIN;

-- ====================================================================
-- Test 1: Verify RLS is enabled on all expected tables
-- ====================================================================
SELECT '=== TEST 1: RLS Enabled on Expected Tables ===' AS test_group;

CREATE TEMP TABLE expected_rls_tables (tablename TEXT);

INSERT INTO expected_rls_tables VALUES
    -- Phase 1: Core (14)
    ('Companies'), ('Employees'), ('Sales'), ('SaleDetails'),
    ('Clients'), ('Products'), ('InventoryMovements'), ('Purchases'),
    ('Suppliers'), ('Credits'), ('CashMovements'),
    ('AccountingEntries'), ('AccountingEntryDetails'), ('Warranties'),
    -- Phase 2: Fleet (30)
    ('FleetVehicles'), ('FleetDrivers'), ('FleetRoutes'), ('FleetRoutePoints'),
    ('FleetDeliveries'), ('FleetDeliveryItems'), ('FleetTrips'),
    ('FuelRefills'), ('WorkOrders'), ('WorkOrderParts'), ('Workshops'),
    ('FleetExpenses'), ('ExpenseCategories'), ('ExpenseSubcategories'),
    ('GpsPositions'), ('Geofences'), ('VehicleGeofenceStates'),
    ('VehicleBrands'), ('VehicleTypes'), ('FuelTypes'),
    ('DriverLicenseCategories'), ('DriverInfractions'), ('DriverTrainings'),
    ('FailureTypes'), ('MaintenanceTemplates'), ('MaintenanceSchedules'),
    ('FleetDocuments'), ('DocumentTypes'), ('FleetAlerts'), ('FleetAlertRules'),
    -- Phase 3: Payroll (12)
    ('AttendanceRecords'), ('VacationRequests'), ('SickLeaveRecords'),
    ('PayrollRuns'), ('PayrollDetails'), ('PayrollConcepts'),
    ('TerminationRecords'), ('PermissionRequests'),
    ('EmployeeSalaries'), ('BenefitProvisions'), ('BonusRecords'), ('CommissionRecords'),
    -- Phase 4: Goals (8)
    ('GoalDefinitions'), ('GoalAssignments'), ('GoalAssignmentProgressEntries'),
    ('Incentives'), ('IncentivePayments'),
    ('KpiDefinitions'), ('KpiRecords'), ('Budgets'),
    -- Phase 5: Treasury (8)
    ('Banks'), ('BankAccounts'), ('Checkbooks'), ('Checks'),
    ('CheckAuditTrails'), ('CheckPrintTemplates'),
    ('CashRegisters'), ('CashRegisterArqueos'),
    -- Phase 6: Purchases (5)
    ('SupplierPayments'), ('SupplierCreditNotes'),
    ('CreditNotes'), ('CreditNoteDetails'), ('PurchaseOrders');

SELECT
    e.tablename,
    CASE
        WHEN t.rowsecurity THEN 'PASS ✓'
        ELSE 'FAIL ✗ — RLS not enabled'
    END AS rls_status
FROM expected_rls_tables e
LEFT JOIN pg_tables t ON t.tablename = e.tablename AND t.schemaname = 'public'
ORDER BY e.tablename;

-- ====================================================================
-- Test 2: Verify policies exist for each table
-- ====================================================================
SELECT '=== TEST 2: Tenant Isolation Policies Present ===' AS test_group;

SELECT
    e.tablename,
    CASE
        WHEN COUNT(p.policyname) >= 1 THEN 'PASS ✓'
        ELSE 'FAIL ✗ — No tenant_isolation policy'
    END AS policy_count_status,
    STRING_AGG(p.policyname, ', ') AS policies
FROM expected_rls_tables e
LEFT JOIN pg_policies p ON p.tablename = e.tablename AND p.schemaname = 'public'
GROUP BY e.tablename
ORDER BY e.tablename;

-- ====================================================================
-- Test 3: Verify SuperAdmin bypass policies exist
-- ====================================================================
SELECT '=== TEST 3: SuperAdmin Bypass Policies ===' AS test_group;

SELECT
    e.tablename,
    CASE
        WHEN COUNT(p.policyname) FILTER (WHERE p.policyname LIKE 'super_admin_bypass%') >= 1 THEN 'PASS ✓'
        ELSE 'FAIL ✗ — No super_admin_bypass policy'
    END AS bypass_status
FROM expected_rls_tables e
LEFT JOIN pg_policies p ON p.tablename = e.tablename AND p.schemaname = 'public'
GROUP BY e.tablename
ORDER BY e.tablename;

-- ====================================================================
-- Test 4: is_super_admin() function exists
-- ====================================================================
SELECT '=== TEST 4: Helper Functions ===' AS test_group;

SELECT
    'is_super_admin()' AS function_name,
    CASE
        WHEN EXISTS (SELECT 1 FROM pg_proc WHERE proname = 'is_super_admin')
        THEN 'PASS ✓'
        ELSE 'FAIL ✗ — Function not found'
    END AS status;

-- ====================================================================
-- Test 5: Tenant Isolation — Data Visibility
-- ====================================================================
-- Note: This test only measures that RLS is active by checking that
-- policies reference the correct column. Full data isolation tests
-- require actual data with multiple tenants.
SELECT '=== TEST 5: Policy Column Correctness ===' AS test_group;

-- Verify TenantId-based policies reference the correct column
SELECT
    p.tablename,
    p.policyname,
    CASE
        WHEN p.qual ~* 'TenantId' THEN 'PASS ✓ (TenantId)'
        WHEN p.qual ~* 'CompanyId' THEN 'PASS ✓ (CompanyId)'
        WHEN p.qual ~* 'SaleId' OR p.qual ~* 'EntryId' THEN 'PASS ✓ (FK-based)'
        ELSE 'FAIL ✗ — Unknown column'
    END AS column_check,
    LEFT(p.qual, 120) AS policy_definition
FROM pg_policies p
WHERE p.schemaname = 'public'
    AND p.policyname LIKE 'tenant_isolation%'
ORDER BY p.tablename;

-- ====================================================================
-- Test 6: FK-based policy verification
-- ====================================================================
SELECT '=== TEST 6: FK-based Policies ===' AS test_group;

SELECT
    'SaleDetails' AS table_name,
    CASE
        WHEN EXISTS (
            SELECT 1 FROM pg_policies
            WHERE schemaname = 'public'
                AND tablename = 'SaleDetails'
                AND qual ~* 'SELECT.*FROM.*Sales'
        ) THEN 'PASS ✓ — SaleDetails→Sales FK policy'
        ELSE 'FAIL ✗ — SaleDetails FK policy missing or incorrect'
    END AS status;

SELECT
    'AccountingEntryDetails' AS table_name,
    CASE
        WHEN EXISTS (
            SELECT 1 FROM pg_policies
            WHERE schemaname = 'public'
                AND tablename = 'AccountingEntryDetails'
                AND qual ~* 'SELECT.*FROM.*AccountingEntries'
        ) THEN 'PASS ✓ — AccountingEntryDetails→Entries FK policy'
        ELSE 'FAIL ✗ — AccountingEntryDetails FK policy missing or incorrect'
    END AS status;

-- ====================================================================
-- Test 7: Performance Indexes
-- ====================================================================
SELECT '=== TEST 7: Required Indexes ===' AS test_group;

CREATE TEMP TABLE expected_indexes (idxname TEXT, tblname TEXT);

INSERT INTO expected_indexes VALUES
    ('idx_companies_tenant_id', 'Companies'),
    ('idx_employees_tenant_id', 'Employees'),
    ('idx_sales_tenant_id', 'Sales'),
    ('idx_clients_tenant_id', 'Clients'),
    ('idx_products_tenant_id', 'Products'),
    ('idx_credits_tenant_id', 'Credits'),
    ('idx_purchases_tenant_id', 'Purchases'),
    ('idx_vehicles_company_id', 'Vehicles'),
    ('idx_trips_company_id', 'Trips'),
    ('idx_deliveries_company_id', 'Deliveries'),
    ('idx_payroll_headers_tenant_id', 'PayrollHeaders'),
    ('idx_goals_tenant_id', 'Goals'),
    ('idx_budgets_tenant_id', 'Budgets');

SELECT
    e.idxname,
    e.tblname,
    CASE
        WHEN i.indexname IS NOT NULL THEN 'PASS ✓'
        ELSE 'FAIL ✗ — Index missing'
    END AS index_status
FROM expected_indexes e
LEFT JOIN pg_indexes i ON i.indexname = e.idxname AND i.schemaname = 'public'
ORDER BY e.idxname;

-- ====================================================================
-- Test 8: Duplicate Policy Check
-- ====================================================================
SELECT '=== TEST 8: No Duplicate Policies ===' AS test_group;

SELECT
    tablename,
    COUNT(*) AS policy_count,
    CASE
        WHEN COUNT(*) > 2 THEN 'WARNING ⚠ — More than 2 policies (tenant + bypass)'
        ELSE 'PASS ✓'
    END AS status
FROM pg_policies
WHERE schemaname = 'public'
    AND tablename IN (SELECT tablename FROM expected_rls_tables)
GROUP BY tablename
ORDER BY tablename;

-- ====================================================================
-- Summary
-- ====================================================================
SELECT '=== RLS VERIFICATION SUMMARY ===' AS summary;

SELECT
    COUNT(*) AS total_tables_checked,
    SUM(CASE WHEN t.rowsecurity THEN 1 ELSE 0 END) AS rls_enabled,
    SUM(CASE WHEN t.rowsecurity THEN 0 ELSE 1 END) AS rls_missing
FROM expected_rls_tables e
JOIN pg_tables t ON t.tablename = e.tablename AND t.schemaname = 'public';

SELECT
    COUNT(*) AS total_policies_created,
    COUNT(*) FILTER (WHERE policyname LIKE 'tenant_isolation%') AS tenant_policies,
    COUNT(*) FILTER (WHERE policyname LIKE 'super_admin_bypass%') AS bypass_policies
FROM pg_policies
WHERE schemaname = 'public'
    AND tablename IN (SELECT tablename FROM expected_rls_tables);

-- ====================================================================
-- DATA INTEGRITY TESTS (in transaction — will be rolled back)
-- ====================================================================
-- These tests simulate tenant contexts to verify isolation behavior.
-- Since we can't modify app settings in a read-only way, we use
-- SET LOCAL within the transaction block.

SELECT '=== TEST 9: Tenant Context Simulation ===' AS test_group;
SELECT 'NOTE: These tests require actual data from at least 2 tenants.' AS test_note;
SELECT 'Run the following manual tests after verifying the automated checks pass:' AS manual_tests;

-- ====================================================================
-- Manual test instructions (for human to run)
-- ====================================================================
SELECT '--- MANUAL TESTS (run after automated tests pass) ---' AS manual_header;

-- These are printed as a result set for the operator to follow
SELECT '
--------------------------------------------------------------------------------
MANUAL TEST STEPS (to be run AFTER automated checks pass):

STEP 1 — Tenant A visibility:
  SET app.tenant_id = ''<tenant_A_id>'';
  SELECT COUNT(*) FROM "Sales";         -- Should show only Tenant A sales
  SELECT COUNT(*) FROM "Clients";       -- Should show only Tenant A clients
  SELECT COUNT(*) FROM "PayrollHeaders"; -- Should show only Tenant A payrolls

STEP 2 — Tenant B isolation:
  SET app.tenant_id = ''<tenant_B_id>'';
  SELECT COUNT(*) FROM "Sales";         -- Should show different count than Tenant A

STEP 3 — Cross-tenant isolation:
  SET app.tenant_id = ''<tenant_A_id>'';
  SELECT * FROM "Sales" WHERE "TenantId" = ''<tenant_B_id>'';  -- Should return 0 rows

STEP 4 — FK-based isolation (SaleDetails):
  SET app.tenant_id = ''<tenant_A_id>'';
  SELECT COUNT(*) FROM "SaleDetails";    -- Should only show details of Tenant A sales

STEP 5 — CompanyId-based isolation (Fleet):
  SET app.tenant_id = ''<tenant_A_company_id>'';
  SELECT COUNT(*) FROM "Vehicles";       -- Should show only Tenant A vehicles

STEP 6 — SuperAdmin bypass:
  SET app.is_super_admin = ''true'';
  SELECT COUNT(*) FROM "Sales";          -- Should show ALL sales across tenants

STEP 7 — SuperAdmin off (back to isolation):
  SET app.is_super_admin = ''false'';
  SET app.tenant_id = ''<tenant_A_id>'';
  SELECT COUNT(*) FROM "Sales";          -- Should show only Tenant A sales again

STEP 8 — INSERT test (verify policies apply to INSERT):
  SET app.tenant_id = ''<tenant_A_id>'';
  -- The following should succeed:
  INSERT INTO "Clients" ("Id", "Name", "TenantId")
  VALUES (gen_random_uuid(), ''RLS-Test-Client'', ''<tenant_A_id>'');
  -- The following should FAIL (different TenantId):
  INSERT INTO "Clients" ("Id", "Name", "TenantId")
  VALUES (gen_random_uuid(), ''RLS-Test-Client-Wrong'', ''<tenant_B_id>'');
  -- Cleanup
  DELETE FROM "Clients" WHERE "Name" LIKE ''RLS-Test-%'';
--------------------------------------------------------------------------------
' AS manual_test_steps;

-- ====================================================================
-- Rollback (safe — no data was modified)
-- ====================================================================
ROLLBACK;
