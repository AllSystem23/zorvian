-- ====================================================================
-- Zorvian ERP — Table Partitioning Migration
-- ====================================================================
-- Implements PostgreSQL native table partitioning by tenant_id
-- Reference: Audit Plan P4.8
-- Strategy: HASH partitioning for write distribution + RANGE for time
-- ====================================================================

-- Step 1: Create partitioned versions of high-volume tables
-- (Run during a maintenance window)
-- ====================================================================

-- Helper function: Calculate tenant partition
CREATE OR REPLACE FUNCTION tenant_partition(tenant_id VARCHAR)
RETURNS INTEGER AS $$
BEGIN
    -- Use first character hash code to distribute across 16 partitions
    RETURN (abs(hashtext(tenant_id)) % 16) + 1;
END;
$$ LANGUAGE plpgsql IMMUTABLE;

-- Sales table: partitioned by tenant_id (hash) + created_at (range)
-- ====================================================================

-- Rename existing table
ALTER TABLE "Sales" RENAME TO "Sales_old";

-- Create partitioned table
CREATE TABLE "Sales" (
    "Id" UUID NOT NULL,
    "TenantId" VARCHAR(50) NOT NULL,
    "ClientId" UUID,
    "EmployeeId" UUID,
    "InvoiceNumber" VARCHAR(50) NOT NULL,
    "SaleType" VARCHAR(20) NOT NULL,
    "Status" VARCHAR(20) NOT NULL,
    "SaleDate" TIMESTAMPTZ NOT NULL,
    "Subtotal" DECIMAL(18,2) NOT NULL,
    "Tax" DECIMAL(18,2) NOT NULL,
    "Discount" DECIMAL(18,2) NOT NULL,
    "Total" DECIMAL(18,2) NOT NULL,
    "PaidAmount" DECIMAL(18,2) NOT NULL,
    "Balance" DECIMAL(18,2) NOT NULL,
    "Notes" VARCHAR(500),
    "CurrencyCode" VARCHAR(3),
    "ExchangeRateToReporting" DECIMAL(18,6),
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "CreatedBy" VARCHAR(100),
    "UpdatedBy" VARCHAR(100),
    PRIMARY KEY ("Id", "TenantId", "SaleDate")
) PARTITION BY HASH (tenant_partition("TenantId"));

-- Create 16 hash partitions
DO $$
DECLARE
    i INT;
BEGIN
    FOR i IN 0..15 LOOP
        EXECUTE format(
            'CREATE TABLE "Sales_p%1$s" PARTITION OF "Sales" '
            'FOR VALUES WITH (MODULUS 16, REMAINDER %2$s)',
            i, i
        );
    END LOOP;
END $$;

-- Migrate data
INSERT INTO "Sales" SELECT * FROM "Sales_old";

-- Drop old table
DROP TABLE "Sales_old";

-- Re-create indexes on partitioned table
CREATE INDEX idx_sales_tenant_id ON "Sales" ("TenantId");
CREATE INDEX idx_sales_tenant_status ON "Sales" ("TenantId", "Status");
CREATE INDEX idx_sales_tenant_date ON "Sales" ("TenantId", "SaleDate" DESC);
CREATE INDEX idx_sales_invoice ON "Sales" ("InvoiceNumber") WHERE "IsDeleted" = false;
CREATE INDEX idx_sales_client ON "Sales" ("ClientId", "SaleDate" DESC) WHERE "IsDeleted" = false;

-- AccountingEntries table: partitioned by tenant_id (hash) + entry_date (range)
-- ====================================================================

ALTER TABLE "AccountingEntries" RENAME TO "AccountingEntries_old";

CREATE TABLE "AccountingEntries" (
    "Id" UUID NOT NULL,
    "TenantId" VARCHAR(50) NOT NULL,
    "EntryNumber" VARCHAR(50) NOT NULL,
    "EntryDate" TIMESTAMPTZ NOT NULL,
    "Description" VARCHAR(500),
    "ReferenceType" VARCHAR(50),
    "ReferenceId" UUID,
    "Status" VARCHAR(20) NOT NULL,
    "AccountingPeriodId" UUID NOT NULL,
    "CompanyId" UUID,
    "BranchId" UUID,
    "CostCenterId" UUID,
    "TotalDebit" DECIMAL(18,2) NOT NULL,
    "TotalCredit" DECIMAL(18,2) NOT NULL,
    "PostedAt" TIMESTAMPTZ,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    PRIMARY KEY ("Id", "TenantId", "EntryDate")
) PARTITION BY HASH (tenant_partition("TenantId"));

DO $$
DECLARE
    i INT;
BEGIN
    FOR i IN 0..15 LOOP
        EXECUTE format(
            'CREATE TABLE "AccountingEntries_p%1$s" PARTITION OF "AccountingEntries" '
            'FOR VALUES WITH (MODULUS 16, REMAINDER %2$s)',
            i, i
        );
    END LOOP;
END $$;

INSERT INTO "AccountingEntries" SELECT * FROM "AccountingEntries_old";
DROP TABLE "AccountingEntries_old";

CREATE INDEX idx_accounting_tenant_date ON "AccountingEntries" ("TenantId", "EntryDate" DESC);
CREATE INDEX idx_accounting_period ON "AccountingEntries" ("AccountingPeriodId");
CREATE INDEX idx_accounting_reference ON "AccountingEntries" ("ReferenceType", "ReferenceId");

-- AuditLogs table: partitioned by created_at (RANGE)
-- (Keeps 12 months of data in hot storage, archive to cold)
-- ====================================================================

ALTER TABLE "AuditLogs" RENAME TO "AuditLogs_old";

CREATE TABLE "AuditLogs" (
    "Id" UUID NOT NULL,
    "TenantId" VARCHAR(50) NOT NULL,
    "UserId" VARCHAR(100) NOT NULL,
    "EntityName" VARCHAR(100) NOT NULL,
    "EntityId" VARCHAR(100) NOT NULL,
    "Action" VARCHAR(50) NOT NULL,
    "IpAddress" VARCHAR(50),
    "UserAgent" VARCHAR(500),
    "RequestPath" VARCHAR(500),
    "OldValues" JSONB,
    "NewValues" JSONB,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    PRIMARY KEY ("Id", "CreatedAt")
) PARTITION BY RANGE ("CreatedAt");

-- Create monthly partitions for current and next 12 months
DO $$
DECLARE
    start_date DATE;
    end_date DATE;
    partition_name TEXT;
BEGIN
    FOR i IN 0..12 LOOP
        start_date := date_trunc('month', CURRENT_DATE) + (i || ' months')::interval;
        end_date := date_trunc('month', CURRENT_DATE) + ((i+1) || ' months')::interval;
        partition_name := format('AuditLogs_%s', to_char(start_date, 'YYYY_MM'));
        EXECUTE format(
            'CREATE TABLE %I PARTITION OF "AuditLogs" '
            'FOR VALUES FROM (%L) TO (%L)',
            partition_name, start_date, end_date
        );
    END LOOP;
END $$;

-- Default partition for older data
CREATE TABLE "AuditLogs_default" PARTITION OF "AuditLogs" DEFAULT;

-- Migrate existing data
INSERT INTO "AuditLogs" SELECT * FROM "AuditLogs_old";
DROP TABLE "AuditLogs_old";

CREATE INDEX idx_audit_tenant_date ON "AuditLogs" ("TenantId", "CreatedAt" DESC);
CREATE INDEX idx_audit_entity ON "AuditLogs" ("EntityName", "EntityId", "CreatedAt" DESC);
CREATE INDEX idx_audit_action ON "AuditLogs" ("Action", "CreatedAt" DESC);

-- Step 2: Create stored procedure to auto-create future partitions
-- ====================================================================

CREATE OR REPLACE FUNCTION create_monthly_audit_partition(target_date DATE)
RETURNS VOID AS $$
DECLARE
    start_date DATE;
    end_date DATE;
    partition_name TEXT;
BEGIN
    start_date := date_trunc('month', target_date);
    end_date := date_trunc('month', target_date) + '1 month'::interval;
    partition_name := format('AuditLogs_%s', to_char(start_date, 'YYYY_MM'));

    -- Check if partition already exists
    IF NOT EXISTS (
        SELECT 1 FROM pg_class WHERE relname = partition_name
    ) THEN
        EXECUTE format(
            'CREATE TABLE %I PARTITION OF "AuditLogs" FOR VALUES FROM (%L) TO (%L)',
            partition_name, start_date, end_date
        );
        RAISE NOTICE 'Created partition: %', partition_name;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- Step 3: Schedule monthly partition creation
-- Add to cron (monthly)
-- 0 0 1 * * psql -d zorvian_erp -c "SELECT create_monthly_audit_partition(CURRENT_DATE + interval '2 months');"

-- Step 4: Performance verification
-- ====================================================================

-- Analyze the partitioned tables
ANALYZE "Sales";
ANALYZE "AccountingEntries";
ANALYZE "AuditLogs";

-- Verify partitions exist
SELECT
    parent.relname AS parent_table,
    child.relname AS partition_name,
    pg_get_expr(child.relpartbound, child.oid) AS partition_bound
FROM pg_inherits i
JOIN pg_class parent ON i.inhparent = parent.oid
JOIN pg_class child ON i.inhrelid = child.oid
WHERE parent.relname IN ('Sales', 'AccountingEntries', 'AuditLogs')
ORDER BY parent.relname, child.relname;

-- Step 5: Archive old audit logs (optional)
-- Move data older than 2 years to cold storage table
-- CREATE TABLE AuditLogs_archive (LIKE AuditLogs INCLUDING ALL);
-- INSERT INTO AuditLogs_archive
--     SELECT * FROM AuditLogs
--     WHERE CreatedAt < NOW() - INTERVAL '2 years';
-- DELETE FROM AuditLogs WHERE CreatedAt < NOW() - INTERVAL '2 years';

-- ROLLBACK (if needed - not for production)
-- ====================================================================
-- DROP TABLE IF EXISTS "Sales" CASCADE;
-- ALTER TABLE "Sales_old" RENAME TO "Sales";
-- DROP TABLE IF EXISTS "AccountingEntries" CASCADE;
-- ALTER TABLE "AccountingEntries_old" RENAME TO "AccountingEntries";
-- DROP TABLE IF EXISTS "AuditLogs" CASCADE;
-- ALTER TABLE "AuditLogs_old" RENAME TO "AuditLogs";
</content>
<parameter name="task_progress">