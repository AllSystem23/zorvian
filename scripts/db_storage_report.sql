-- ============================================================================
--  Reporte de almacenamiento de la base de datos (SOLO LECTURA)
--
--  Uso: pegar en la consola SQL de Neon, o `psql "<connection-string>" -f scripts/db_storage_report.sql`
--
--  Responde: ¿qué tabla está llenando el proyecto de 1024 MB?
--  Complementa `GET /zorvian/v1/system/db-storage` (mismo dato, desde el API).
-- ============================================================================

-- 1) Tamaño total de la base y límite del plan.
--    `neon.max_cluster_size` existe solo en Neon; en PostgreSQL normal devuelve NULL.
SELECT
    current_database()                                          AS database,
    pg_size_pretty(pg_database_size(current_database()))         AS database_size,
    pg_database_size(current_database())                         AS database_bytes,
    current_setting('neon.max_cluster_size', true)               AS neon_limit_bytes,
    CASE
        WHEN current_setting('neon.max_cluster_size', true) IS NULL THEN NULL
        ELSE round(
            100.0 * pg_database_size(current_database())
            / nullif(current_setting('neon.max_cluster_size', true)::bigint, 0), 2)
    END                                                          AS usage_percent;

-- 2) Top 25 tablas por espacio total (tabla + índices + TOAST).
--    `dead_ratio` alto = hay espacio ocupado que VACUUM aún no recicló:
--    reutilizable por nuevas filas, pero NO devuelto al sistema de archivos.
SELECT
    c.relname                                                           AS table_name,
    pg_size_pretty(pg_total_relation_size(c.oid))                       AS total,
    pg_size_pretty(pg_table_size(c.oid))                                AS table_only,
    pg_size_pretty(pg_indexes_size(c.oid))                              AS indexes,
    COALESCE(s.n_live_tup, 0)                                           AS live_rows,
    COALESCE(s.n_dead_tup, 0)                                           AS dead_rows,
    CASE
        WHEN COALESCE(s.n_live_tup, 0) + COALESCE(s.n_dead_tup, 0) = 0 THEN 0
        ELSE round(
            s.n_dead_tup::numeric
            / (COALESCE(s.n_live_tup, 0) + COALESCE(s.n_dead_tup, 0)), 3)
    END                                                                 AS dead_ratio,
    pg_size_pretty(pg_total_relation_size(c.oid) - pg_relation_size(c.oid)) AS toast_and_aux,
    s.last_autovacuum,
    s.last_vacuum
FROM pg_class c
JOIN pg_namespace n ON n.oid = c.relnamespace
LEFT JOIN pg_stat_user_tables s ON s.relid = c.oid
WHERE c.relkind = 'r'
  AND n.nspname = 'public'
ORDER BY pg_total_relation_size(c.oid) DESC
LIMIT 25;

-- 3) Columnas de texto más pesadas (dónde está el contenido grande).
--    Útil para detectar blobs/HTML guardados en la base (p. ej. DocumentVersions.Content).
SELECT
    c.relname       AS table_name,
    a.attname       AS column_name,
    format_type(a.atttypid, a.atttypmod) AS type
FROM pg_class c
JOIN pg_namespace n ON n.oid = c.relnamespace
JOIN pg_attribute a ON a.attrelid = c.oid
WHERE n.nspname = 'public'
  AND c.relkind = 'r'
  AND a.attnum > 0
  AND NOT a.attisdropped
  AND a.atttypid IN ('text'::regtype, 'jsonb'::regtype, 'json'::regtype, 'bytea'::regtype)
ORDER BY pg_total_relation_size(c.oid) DESC, c.relname, a.attnum;

-- 4) Índices más grandes (a veces el 30-50% del proyecto son índices).
SELECT
    schemaname,
    relname                                          AS table_name,
    indexrelname                                     AS index_name,
    pg_size_pretty(pg_relation_size(indexrelid))     AS index_size,
    idx_scan                                         AS times_used
FROM pg_stat_user_indexes
ORDER BY pg_relation_size(indexrelid) DESC
LIMIT 20;

-- 5) Actividad de autovacuum: si `n_dead_tup` crece y `last_autovacuum` es viejo,
--    el proyecto acumula espacio muerto sin reciclar.
SELECT
    relname,
    n_live_tup,
    n_dead_tup,
    n_tup_ins,
    n_tup_upd,
    n_tup_del,
    last_vacuum,
    last_autovacuum
FROM pg_stat_user_tables
ORDER BY n_dead_tup DESC
LIMIT 15;
