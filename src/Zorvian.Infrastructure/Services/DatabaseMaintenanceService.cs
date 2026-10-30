using System.Data;
using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Services;

/// <summary>
/// Implementación de <see cref="IDatabaseMaintenanceService"/> para PostgreSQL (Neon).
///
/// <para>
/// Contexto del incidente: el proyecto de Neon llegó a su límite de 1024 MB y toda
/// escritura empezó a fallar con <c>53100: could not extend file because project size
/// limit (1024 MB) has been exceeded</c>. El límite se mide sobre el tamaño LÓGICO
/// (relaciones + índices + TOAST), no sobre filas vivas: borrar no basta, hay que
/// reciclar y —para devolver el espacio— reescribir la tabla con <c>VACUUM FULL</c>
/// (fuera de este servicio: necesita espacio libre adicional, ver
/// <c>docs/DB_STORAGE_RECOVERY.md</c>).
/// </para>
///
/// <para>
/// El camino PostgreSQL usa SQL crudo con <c>ctid</c> para borrar en lotes sin cargar
/// filas en memoria (Render tiene 512 MB de RAM). El camino no-PostgreSQL (InMemory en
/// tests) usa EF con reflexión sobre <see cref="RetentionRule.EntityType"/>.
/// </para>
/// </summary>
public sealed class DatabaseMaintenanceService : IDatabaseMaintenanceService
{
    /// <summary>Filas por sentencia de borrado.</summary>
    public const int DefaultBatchSize = 5_000;

    /// <summary>Aviso a partir de este porcentaje del límite del plan.</summary>
    public const double WarningThresholdPercent = 90d;

    /// <summary>Prefijo de configuración para sobreescribir la retención por tabla (días).</summary>
    public const string RetentionConfigPrefix = "Database:Retention";

    /// <summary>Tope de lotes por tabla y corrida (evita un job eterno).</summary>
    private const int MaxBatchesPerTable = 500;

    private readonly ZorvianDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseMaintenanceService> _logger;

    public DatabaseMaintenanceService(
        ZorvianDbContext db,
        IConfiguration configuration,
        ILogger<DatabaseMaintenanceService> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Reporte de almacenamiento
    // ─────────────────────────────────────────────────────────────────────────────

    public async Task<DatabaseStorageReport> GetStorageReportAsync(
        int topTables = 15,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var provider = _db.Database.ProviderName ?? "unknown";
        var isPostgres = IsPostgres();

        if (!isPostgres)
        {
            // Ojo: `GetDbConnection()` es una extensión RELACIONAL — con el proveedor
            // InMemory (tests) lanza. El nombre es informativo, así que se resuelve
            // con tolerancia en vez de tumbar el endpoint.
            return new DatabaseStorageReport(
                Provider: provider,
                DatabaseName: TryGetDatabaseName(provider),
                DatabaseSizeBytes: 0,
                LimitBytes: null,
                UsagePercent: null,
                WarningThresholdPercent: WarningThresholdPercent,
                IsStorageFull: false,
                GeneratedAtUtc: now,
                LargestTables: Array.Empty<DatabaseTableUsage>(),
                Recommendations: new[]
                {
                    "El reporte de almacenamiento solo está disponible en PostgreSQL (Neon).",
                });
        }

        var connection = _db.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere)
            await connection.OpenAsync(cancellationToken);

        try
        {
            var (databaseName, databaseBytes, limitBytes) = await ReadDatabaseSizeAsync(connection, cancellationToken);
            var tables = await ReadLargestTablesAsync(connection, Math.Max(1, topTables), cancellationToken);

            double? usagePercent = limitBytes is > 0
                ? Math.Round(databaseBytes * 100d / limitBytes.Value, 2)
                : null;

            var recommendations = BuildRecommendations(databaseBytes, limitBytes, usagePercent, tables);

            return new DatabaseStorageReport(
                Provider: provider,
                DatabaseName: databaseName,
                DatabaseSizeBytes: databaseBytes,
                LimitBytes: limitBytes,
                UsagePercent: usagePercent,
                WarningThresholdPercent: WarningThresholdPercent,
                IsStorageFull: usagePercent is >= 100,
                GeneratedAtUtc: now,
                LargestTables: tables,
                Recommendations: recommendations);
        }
        finally
        {
            if (openedHere)
                await connection.CloseAsync();
        }
    }

    private async Task<(string DatabaseName, long DatabaseBytes, long? LimitBytes)> ReadDatabaseSizeAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        // current_setting(..., true) devuelve NULL en PostgreSQL sin Neon, así que el
        // mismo SQL funciona en local/Docker y en Neon (donde el GUC existe).
        command.CommandText = """
            SELECT current_database(),
                   pg_database_size(current_database()),
                   current_setting('neon.max_cluster_size', true)
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return (connection.Database, 0, null);

        var databaseName = reader.IsDBNull(0) ? connection.Database : reader.GetString(0);
        var databaseBytes = reader.IsDBNull(1) ? 0 : reader.GetInt64(1);
        long? limitBytes = null;
        if (!reader.IsDBNull(2))
        {
            var raw = reader.GetString(2);
            if (long.TryParse(raw, out var parsed) && parsed > 0)
                limitBytes = parsed;
        }

        return (databaseName, databaseBytes, limitBytes);
    }

    private static async Task<IReadOnlyList<DatabaseTableUsage>> ReadLargestTablesAsync(
        DbConnection connection,
        int topTables,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.relname,
                   pg_total_relation_size(c.oid),
                   pg_table_size(c.oid),
                   pg_indexes_size(c.oid),
                   COALESCE(s.n_live_tup, 0),
                   COALESCE(s.n_dead_tup, 0)
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            LEFT JOIN pg_stat_user_tables s ON s.relid = c.oid
            WHERE c.relkind = 'r' AND n.nspname = 'public'
            ORDER BY pg_total_relation_size(c.oid) DESC
            LIMIT @topTables
            """;
        AddParameter(command, "topTables", topTables);

        var tables = new List<DatabaseTableUsage>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(new DatabaseTableUsage(
                TableName: reader.GetString(0),
                TotalBytes: reader.GetInt64(1),
                TableBytes: reader.GetInt64(2),
                IndexBytes: reader.GetInt64(3),
                LiveRows: reader.GetInt64(4),
                DeadRows: reader.GetInt64(5)));
        }

        return tables;
    }

    private static IReadOnlyList<string> BuildRecommendations(
        long databaseBytes,
        long? limitBytes,
        double? usagePercent,
        IReadOnlyList<DatabaseTableUsage> tables)
    {
        var notes = new List<string>();

        if (limitBytes is null)
        {
            notes.Add("El proveedor no expone un límite de tamaño; vigile el tamaño total de la base.");
        }
        else if (usagePercent is >= 100)
        {
            notes.Add(
                "La base alcanzó el límite del plan: PostgreSQL rechaza escrituras con 53100 " +
                "(no se pueden guardar ventas, nóminas ni documentos). Ejecute la purga de retención, " +
                "libere espacio con VACUUM FULL (docs/DB_STORAGE_RECOVERY.md) o amplíe el plan.");
        }
        else if (usagePercent is >= WarningThresholdPercent)
        {
            notes.Add(
                $"La base está al {usagePercent:0.##}% de su límite ({Usage(limitBytes)}). " +
                "Ejecute la purga de retención y planifique VACUUM FULL antes de llegar al 100%.");
        }

        var deadHeavy = tables
            .Where(t => t.DeadRows > 5_000 && t.DeadRowRatio >= 0.25)
            .Take(5)
            .ToList();
        if (deadHeavy.Count > 0)
        {
            notes.Add(
                "Tablas con muchas filas muertas (espacio ocupado pero reutilizable): " +
                string.Join(", ", deadHeavy.Select(t => $"{t.TableName} ({t.DeadRows:N0})")) +
                ". VACUUM FULL devuelve ese espacio al sistema de archivos.");
        }

        var dominant = tables.FirstOrDefault();
        if (dominant is not null && databaseBytes > 0 && dominant.TotalBytes > databaseBytes * 0.4)
        {
            notes.Add(
                $"La tabla {dominant.TableName} ocupa el {dominant.TotalBytes * 100d / databaseBytes:0.#}% " +
                $"de la base ({dominant.TotalMb:0.##} MB). Revise su política de retención antes que el resto.");
        }

        return notes;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Retención
    // ─────────────────────────────────────────────────────────────────────────────

    public async Task<DataRetentionPurgeSummary> PurgeExpiredDataAsync(
        int batchSize = 0,
        CancellationToken cancellationToken = default)
    {
        var size = batchSize > 0 ? batchSize : DefaultBatchSize;
        var now = DateTime.UtcNow;
        var isPostgres = IsPostgres();
        var results = new List<RetentionPurgeResult>();
        var notes = new List<string>();

        foreach (var rule in DataRetentionRules.All)
        {
            var retentionDays = ResolveRetentionDays(rule);
            var cutoff = now.AddDays(-retentionDays);

            var before = isPostgres ? await TryGetTableSizeAsync(rule.Table, cancellationToken) : 0;

            long deleted;
            try
            {
                deleted = isPostgres
                    ? await PurgePostgresAsync(rule, cutoff, size, cancellationToken)
                    : await PurgeWithEntityFrameworkAsync(rule, cutoff, size, cancellationToken);
            }
            catch (Exception ex)
            {
                // Una tabla que falla no debe impedir purgar las demás (y si la BD está
                // llena, este es justamente el camino que libera espacio).
                _logger.LogWarning(ex, "Retención: no se pudo purgar {Table}", rule.Table);
                notes.Add($"No se pudo purgar {rule.Table}: {ex.Message}");
                continue;
            }

            if (deleted > 0 && isPostgres)
                await TryVacuumAsync(rule.Table, cancellationToken);

            var after = isPostgres ? await TryGetTableSizeAsync(rule.Table, cancellationToken) : 0;

            if (deleted > 0)
            {
                _logger.LogInformation(
                    "Retención: {Table} — {Deleted} fila(s) de más de {Days} día(s) eliminadas ({Before} → {After} bytes)",
                    rule.Table, deleted, retentionDays, before, after);
            }

            results.Add(new RetentionPurgeResult(
                TableName: rule.Table,
                Label: rule.Label,
                RetentionDays: retentionDays,
                CutoffUtc: cutoff,
                RowsDeleted: deleted,
                SizeBeforeBytes: before,
                SizeAfterBytes: after));
        }

        var totalDeleted = results.Sum(r => r.RowsDeleted);
        if (totalDeleted > 0 && isPostgres)
        {
            notes.Add(
                "Un DELETE no reduce el tamaño del proyecto en Neon: el espacio queda reutilizable. " +
                "Para recuperarlo ejecute VACUUM FULL (docs/DB_STORAGE_RECOVERY.md) o amplíe el plan.");
        }

        if (!isPostgres)
            notes.Add("Proveedor sin PostgreSQL: la retención se ejecutó vía EF (solo válido para tests).");

        return new DataRetentionPurgeSummary(
            ExecutedAtUtc: now,
            TotalRowsDeleted: totalDeleted,
            TotalSizeBeforeBytes: results.Sum(r => r.SizeBeforeBytes),
            TotalSizeAfterBytes: results.Sum(r => r.SizeAfterBytes),
            Results: results,
            Notes: notes);
    }

    /// <summary>Días efectivos de retención: configuración (<c>Database:Retention:{tabla}</c>) o el default de la regla.</summary>
    public int ResolveRetentionDays(RetentionRule rule)
    {
        var raw = _configuration[$"{RetentionConfigPrefix}:{rule.Table}"];
        return int.TryParse(raw, out var days) && days > 0 ? days : rule.RetentionDays;
    }

    private async Task<long> PurgePostgresAsync(
        RetentionRule rule,
        DateTime cutoffUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        long total = 0;

        // Identificadores de tabla/columna vienen de DataRetentionRules (constantes del
        // código, nunca de la petición), por eso se interpolan; el corte va parametrizado.
        var sql =
            $"DELETE FROM \"{rule.Table}\" WHERE ctid IN (" +
            $"SELECT ctid FROM \"{rule.Table}\" WHERE \"{rule.DateColumn}\" < @cutoff LIMIT {batchSize})";

        var cutoffParameter = new NpgsqlParameter("cutoff", NpgsqlTypes.NpgsqlDbType.TimestampTz)
        {
            Value = cutoffUtc,
        };

        for (var batch = 0; batch < MaxBatchesPerTable; batch++)
        {
            var deleted = await _db.Database.ExecuteSqlRawAsync(
                sql, new object[] { cutoffParameter }, cancellationToken);

            total += deleted;
            if (deleted < batchSize)
                break;

            _logger.LogDebug(
                "Retención: {Table} — lote de {Deleted} fila(s) eliminado ({Total} acumuladas)",
                rule.Table, deleted, total);
        }

        if (total >= (long)batchSize * MaxBatchesPerTable)
        {
            _logger.LogWarning(
                "Retención: {Table} alcanzó el tope de {Batches} lotes en esta corrida; el resto se purga en la próxima",
                rule.Table, MaxBatchesPerTable);
        }

        return total;
    }

    /// <summary>
    /// Camino sin PostgreSQL (tests con InMemory): se resuelve el tipo mapeado y se borra
    /// en lotes vía EF para no cargar la tabla completa en memoria.
    /// </summary>
    private async Task<long> PurgeWithEntityFrameworkAsync(
        RetentionRule rule,
        DateTime cutoffUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var method = typeof(DatabaseMaintenanceService)
            .GetMethod(nameof(PurgeWithEntityFrameworkCoreAsync), BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("No se encontró el método genérico de purga");

        var generic = method.MakeGenericMethod(rule.EntityType);
        var task = (Task<long>)generic.Invoke(null, new object?[] { _db, rule, cutoffUtc, batchSize, cancellationToken })!;
        return await task;
    }

    private static async Task<long> PurgeWithEntityFrameworkCoreAsync<TEntity>(
        ZorvianDbContext db,
        RetentionRule rule,
        DateTime cutoffUtc,
        int batchSize,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        long total = 0;

        for (var batch = 0; batch < MaxBatchesPerTable; batch++)
        {
            var rows = await db.Set<TEntity>()
                .IgnoreQueryFilters()
                .Where(e => EF.Property<DateTime>(e, rule.DateColumn) < cutoffUtc)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (rows.Count == 0)
                break;

            db.Set<TEntity>().RemoveRange(rows);
            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();

            total += rows.Count;
            if (rows.Count < batchSize)
                break;
        }

        return total;
    }

    private async Task<long> TryGetTableSizeAsync(string table, CancellationToken cancellationToken)
    {
        try
        {
            var connection = _db.Database.GetDbConnection();
            var openedHere = connection.State != ConnectionState.Open;
            if (openedHere)
                await connection.OpenAsync(cancellationToken);

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT COALESCE(pg_total_relation_size(to_regclass(@table)), 0)";
                AddParameter(command, "table", $"public.{table}");

                var result = await command.ExecuteScalarAsync(cancellationToken);
                return result is null or DBNull ? 0 : Convert.ToInt64(result);
            }
            finally
            {
                if (openedHere)
                    await connection.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Retención: no se pudo leer el tamaño de {Table}", table);
            return 0;
        }
    }

    private async Task TryVacuumAsync(string table, CancellationToken cancellationToken)
    {
        try
        {
            // VACUUM simple (no FULL): libera las páginas para reutilización y actualiza
            // estadísticas sin necesitar espacio libre adicional. VACUUM FULL reescribe la
            // tabla y por eso NO se ejecuta aquí.
            //
            // El nombre de tabla viene de DataRetentionRules (constante del código, nunca
            // de la petición) y VACUUM no admite identificadores parametrizados, así que se
            // arma en una variable local: se evita EF1002 sin abrir superficie de inyección.
            var sql = "VACUUM (ANALYZE) \"" + table + "\"";
            await _db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
        catch (Exception ex)
        {
            // Neon/PG no permiten VACUUM dentro de una transacción (25001). No es fatal:
            // el autovacuum hará el trabajo.
            _logger.LogDebug(ex, "Retención: VACUUM no ejecutado en {Table}", table);
        }
    }

    private bool IsPostgres() =>
        _db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// Nombre de la base para el reporte. En proveedores no relacionales (InMemory) no
    /// existe <c>DbConnection</c>; se devuelve el nombre del proveedor como etiqueta.
    /// </summary>
    private string TryGetDatabaseName(string provider)
    {
        try
        {
            var name = _db.Database.GetDbConnection().Database;
            return string.IsNullOrWhiteSpace(name) ? provider : name;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "No se pudo resolver el nombre de la base de datos");
            return provider;
        }
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static string Usage(long? limitBytes) =>
        limitBytes is { } limit ? $"{limit / 1024d / 1024d:0.##} MB" : "sin límite";
}
