-- ══════════════════════════════════════════════════════════════
-- Zorvian ERP — Local development: schema patch
-- ══════════════════════════════════════════════════════════════
-- Why this file exists
-- --------------------
-- scripts/init_local_schema.sql is generated from the EF Core model.
-- The production database, however, diverges from that model in one
-- deliberate way: it DROPS the CompanyId foreign keys on the tables that
-- hold global/system-scoped rows.
--
-- System rows use `CompanyId = Guid.Empty` (see TenantContextExtensions:
-- "Para SuperAdmin sin empresa seleccionada, devuelve Guid.Empty"). The EF
-- model still declares the relationship, so `dotnet ef dbcontext script`
-- emits the FK — and then creating the global `Super Admin` role fails with:
--
--   23503: insert or update on table "Roles" violates foreign key
--          constraint "FK_Roles_Companies_CompanyId"
--
-- The drops below mirror scripts/init_neon.sql exactly, so the local schema
-- matches what the application was written against.
--
-- ⚠️ If you add entities whose global rows use Guid.Empty, add the drop here.
-- ══════════════════════════════════════════════════════════════

ALTER TABLE "Departments" DROP CONSTRAINT IF EXISTS "FK_Departments_Companies_CompanyId";
ALTER TABLE "LeaveTypes"  DROP CONSTRAINT IF EXISTS "FK_LeaveTypes_Companies_CompanyId";
ALTER TABLE "Roles"       DROP CONSTRAINT IF EXISTS "FK_Roles_Companies_CompanyId";
ALTER TABLE "Users"       DROP CONSTRAINT IF EXISTS "FK_Users_Companies_CompanyId";
