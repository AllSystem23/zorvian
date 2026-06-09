-- ============================================================
-- RLS (Row Level Security) para Zorvian ERP
-- 
-- Este script habilita RLS en todas las tablas que contienen
-- una columna TenantId, garantizando aislamiento entre tenants
-- a nivel de base de datos (defensa en profundidad).
--
-- La variable de sesión app.tenant_id es establecida por el
-- TenantSessionInterceptor al abrir cada conexión.
--
-- Ejecutar: psql -d ZorvianDb -f RLS_Enable.sql
-- ============================================================

-- Función helper: obtiene el tenant_id de la sesión
CREATE OR REPLACE FUNCTION app.current_tenant_id()
RETURNS TEXT
LANGUAGE SQL
STABLE
AS $$
  SELECT NULLIF(current_setting('app.tenant_id', TRUE), '')::TEXT;
$$;

-- Función helper: verifica si el tenant_id actual coincide
CREATE OR REPLACE FUNCTION app.is_tenant_allowed(row_tenant_id TEXT)
RETURNS BOOLEAN
LANGUAGE SQL
STABLE
AS $$
  SELECT app.current_tenant_id() IS NULL
      OR row_tenant_id = app.current_tenant_id();
$$;

-- ============================================================
-- Aplicar RLS a todas las tablas con columna TenantId
-- ============================================================
DO $$
DECLARE
    tbl RECORD;
BEGIN
    FOR tbl IN
        SELECT c.relname AS table_name
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE c.relkind = 'r'
          AND n.nspname = 'public'
          AND EXISTS (
              SELECT 1 FROM information_schema.columns col
              WHERE col.table_schema = n.nspname
                AND col.table_name = c.relname
                AND col.column_name = 'TenantId'
          )
          AND c.relname NOT IN ('__EFMigrationsHistory')
        ORDER BY c.relname
    LOOP
        -- Habilitar RLS
        EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY;', tbl.table_name);

        -- Forzar RLS incluso para el owner de la tabla
        EXECUTE format('ALTER TABLE %I FORCE ROW LEVEL SECURITY;', tbl.table_name);

        -- Eliminar política existente si la hay (idempotente)
        EXECUTE format('DROP POLICY IF EXISTS tenant_isolation ON %I;', tbl.table_name);

        -- Crear política de aislamiento por tenant
        EXECUTE format(
            'CREATE POLICY tenant_isolation ON %I FOR ALL USING (app.is_tenant_allowed("TenantId"::TEXT));',
            tbl.table_name
        );

        RAISE NOTICE 'RLS habilitado en: %', tbl.table_name;
    END LOOP;
END;
$$;

-- ============================================================
-- Excepciones: tablas sin TenantId (compartidas entre tenants)
-- ============================================================
-- Las siguientes tablas NO tienen TenantId y por lo tanto NO
-- tienen RLS aplicado (son globales/compartidas):
--   - __EFMigrationsHistory

-- ============================================================
-- Nota: Las tablas AuditLog y EntityHistory ya tienen TenantId
-- y quedan cubiertas por el RLS general. AuditLog adicionalmente
-- tiene triggers de inmutabilidad (ver migración AddAuditLogImmutability).
-- ============================================================

-- ============================================================
-- Verificación: listar tablas con RLS
-- ============================================================
-- Descomentar para verificar:
-- SELECT relname AS tabla
-- FROM pg_class
-- WHERE relrowsecurity = TRUE
--   AND relkind = 'r'
--   AND relnamespace = 'public'::regnamespace
-- ORDER BY relname;
