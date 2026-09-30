-- ══════════════════════════════════════════════════════════════
-- Zorvian ERP — Local development: migration history bootstrap
-- ══════════════════════════════════════════════════════════════
-- Context
-- -------
-- This project's EF Core migration chain is a BASELINE:
--   * 20260616184129_BaselineSync  → empty by design ("all tables already exist")
--   * every later migration        → ALTER TABLE patches
--
-- The real schema is created out of band on Neon, so a brand-new empty
-- database cannot be built by replaying migrations: the first ALTER TABLE
-- fails with `relation "DocumentTemplates" does not exist`.
--
-- For local development the schema is generated directly from the EF model
-- instead:
--   dotnet ef dbcontext script -p src/Zorvian.Infrastructure \
--     -s src/Zorvian.Web -o scripts/init_local_schema.sql
--
-- This script then records that baseline chain as already applied, so the
-- API's startup auto-migrate finds nothing pending and boots cleanly.
--
-- Apply order (postgres entrypoint runs /docker-entrypoint-initdb.d in
-- alphabetical order):
--   01_schema.sql      → scripts/init_local_schema.sql
--   02_migrations.sql  → this file
--
-- To regenerate init_local_schema.sql after adding entities:
--   dotnet ef dbcontext script ... -o scripts/init_local_schema.sql
-- ══════════════════════════════════════════════════════════════

CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES
    ('20260616184129_BaselineSync', '9.0.0'),
    ('20260618000719_FixFleetDocumentTableName', '9.0.0'),
    ('20260618000814_FixFleetDocumentTable', '9.0.0'),
    ('20260622203341_AddVariablesToDocumentTemplate', '9.0.0'),
    ('20260623224522_AddCompanyIdToBaseEntity', '9.0.0'),
    ('20260625210433_SyncAllEntityColumns', '9.0.0'),
    ('20260627180132_FixLeaveTypeIndexAndQueryFilter', '9.0.0'),
    ('20260627213330_AddCountryCodeToProviders', '9.0.0'),
    ('20260702160302_AddMissingEntities', '9.0.0'),
    ('20260703185729_PendingModelChanges_Jul2026', '9.0.0'),
    ('20260703211855_AddReconciliationAndBudgetDetailTracking', '9.0.0'),
    ('20260703231117_FixPendingModelChanges', '9.0.0'),
    ('20260715234213_AddExpiresToInvitation', '9.0.0'),
    ('20260829232511_AddPalmTrackEntities', '9.0.0'),
    ('20260909174455_AddPalmTrackFeatureFlags', '9.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;
