-- ============================================================================
--  PURGA DE EMERGENCIA de tablas de retención (Neon / PostgreSQL)
--
--  Úsalo SOLO cuando la base ya está en el límite del plan y PostgreSQL rechaza
--  cualquier escritura con:
--
--      53100: could not extend file because project size limit (1024 MB)
--             has been exceeded
--
--  Por qué TRUNCATE y no DELETE: con la base llena, incluso un DELETE necesita
--  páginas nuevas para las versiones muertas y devuelve el MISMO 53100. TRUNCATE
--  crea un archivo vacío nuevo y suelta el viejo, así que libera el 100% del
--  espacio de la tabla sin pedir espacio libre. Es la única purga que pasa.
--
--  ⚠️  TRUNCATE descarta TODO el contenido de la tabla: no hay "filas vencidas",
--      no hay rollback útil (si en el mismo batch fallara algo, PostgreSQL sí
--      deshace el lote). Ninguna tabla de NEGOCIO aparece aquí: ventas, compras,
--      nómina, asientos, clientes, productos y documentos no se tocan.
--
--  Mismo efecto que POST /zorvian/v1/system/db-storage/truncate?includeAuditLogs=true
--  desde el API (permiso company.manage), útil si la API no responde.
--
--  ⚠️  "AuditLogs" tiene DOS guardias de inmutabilidad (AUD-004) y hay que abrir las dos:
--      1) el interceptor de EF (solo aplica al API: ya está abierto dentro del scope de
--         retención) y
--      2) el trigger PostgreSQL trg_audit_logs_immutable, que rechaza
--         UPDATE/DELETE/TRUNCATE con "SQLSTATE P0001: AUD-004: AuditLogs es inmutable…".
--      El trigger acepta el borrado si la MISMA transacción habilita el GUC
--      zorvian.audit_retention (bloque 1.b). Si la migración
--      AddAuditLogRetentionBypass todavía NO está aplicada en la base, usa el bloque
--      1.a (deshabilitar el trigger a mano y volver a habilitarlo).
--
--  Uso:
--    • Neon: pegar la SECCIÓN 1 en la consola SQL y ejecutar; después, en una
--      ejecución APARTE, la SECCIÓN 2 (VACUUM no puede correr dentro de un
--      bloque de transacción, y la consola puede envolver el lote completo).
--    • psql: `psql "<connection-string>" -f scripts/db_storage_emergency_purge.sql`
--      (psql envía cada sentencia por separado, así que el VACUUM del final es
--      válido).
-- ============================================================================


-- ============================================================================
--  SECCIÓN 1 — Vaciar (TRUNCATE) las tablas de retención
--  Ordenadas de la que más suele crecer a la que menos.
--  Comenta con "--" las que quieras conservar.
-- ============================================================================

-- Telemetría y logs (el sistema los vuelve a llenar desde cero)
TRUNCATE TABLE "GpsPositions";            -- 90 d   histórico de posiciones GPS
TRUNCATE TABLE "WebhookDeliveryLogs";     -- 30 d   entregas de webhooks + payload
TRUNCATE TABLE "PalmTrackWebhookLogs";    -- 30 d   payload de eventos de PalmTrack
TRUNCATE TABLE "SyncJournals";            -- 90 d   journal de sincronización offline
TRUNCATE TABLE "EntityHistories";         -- 180 d  historial campo a campo

-- Tokens vencidos (los usuarios vuelven a iniciar sesión)
TRUNCATE TABLE "RefreshTokens";           -- 30 d tras ExpiresAt
TRUNCATE TABLE "PasswordResetTokens";     -- 30 d

-- Log de auditoría: se conserva 6 meses por diseño (AUD-004).
-- Descomenta SOLO si la tabla es la que está llenando el proyecto; pierde
-- trazabilidad legal/operativa de forma IRREVERSIBLE.
--
-- 1.a  SIN la migración AddAuditLogRetentionBypass (build viejo en producción).
--      Deshabilita el trigger dentro de la transacción: si el TRUNCATE fallara,
--      el rollback vuelve a dejar el trigger habilitado.
-- BEGIN;
-- ALTER TABLE "AuditLogs" DISABLE TRIGGER trg_audit_logs_immutable;
-- TRUNCATE TABLE "AuditLogs";
-- ALTER TABLE "AuditLogs" ENABLE TRIGGER trg_audit_logs_immutable;
-- COMMIT;
--
-- 1.b  CON la migración aplicada (recomendado): el trigger permite DELETE/TRUNCATE
--      cuando la MISMA transacción habilita el GUC. UPDATE sigue bloqueado siempre.
-- BEGIN;
-- SELECT set_config('zorvian.audit_retention', 'on', true);
-- TRUNCATE TABLE "AuditLogs";
-- COMMIT;

-- NO truncar (se necesitan para operar):
--   "DocumentVersions"   -> el HTML ya se migra a Storage con el sweep job
--   "GeneratedDocuments" -> documentos emitidos (solo VACUUM FULL / dump+restore)
--   CUALQUIER tabla de negocio (Sales, Purchases, PayrollDetails, AccountingEntries...)


-- ============================================================================
--  SECCIÓN 2 — Devolver el espacio y actualizar estadísticas
--  ⚠️ Ejecutar en una EJECUCIÓN APARTE (VACUUM no admite bloque de transacción).
--     Con psql -f no hace falta separar: cada sentencia va por su cuenta.
-- ============================================================================

VACUUM (ANALYZE) "GpsPositions";
VACUUM (ANALYZE) "WebhookDeliveryLogs";
VACUUM (ANALYZE) "PalmTrackWebhookLogs";
VACUUM (ANALYZE) "SyncJournals";
VACUUM (ANALYZE) "EntityHistories";
VACUUM (ANALYZE) "RefreshTokens";
VACUUM (ANALYZE) "PasswordResetTokens";
-- VACUUM (ANALYZE) "AuditLogs";


-- ============================================================================
--  SECCIÓN 3 — Verificación (solo lectura)
-- ============================================================================

-- 3.1 Uso del proyecto vs límite del plan.
SELECT
    current_database()                                    AS database,
    pg_size_pretty(pg_database_size(current_database()))  AS database_size,
    current_setting('neon.max_cluster_size', true)        AS neon_limit_bytes,
    CASE
        WHEN current_setting('neon.max_cluster_size', true) IS NULL THEN NULL
        ELSE round(
            100.0 * pg_database_size(current_database())
            / nullif(current_setting('neon.max_cluster_size', true)::bigint, 0), 2)
    END                                                   AS usage_percent;

-- 3.2 Top 10 tablas: confirma que las vaciadas quedaron en 0.
SELECT
    c.relname                                     AS table_name,
    pg_size_pretty(pg_total_relation_size(c.oid)) AS total,
    COALESCE(s.n_live_tup, 0)                     AS live_rows
FROM pg_class c
JOIN pg_namespace n ON n.oid = c.relnamespace
LEFT JOIN pg_stat_user_tables s ON s.relid = c.oid
WHERE c.relkind = 'r'
  AND n.nspname = 'public'
ORDER BY pg_total_relation_size(c.oid) DESC
LIMIT 10;

-- Si el % sigue alto: la tabla dominante necesita VACUUM FULL (con headroom) o
-- dump + restore a un proyecto nuevo. Ver docs/DB_STORAGE_RECOVERY.md.
