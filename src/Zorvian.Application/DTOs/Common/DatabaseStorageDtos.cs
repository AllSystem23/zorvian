namespace Zorvian.Application.DTOs.Common;

/// <summary>
/// Uso de disco de una tabla (tabla + índices + TOAST) y salud de sus filas.
/// Se obtiene de <c>pg_total_relation_size</c> + <c>pg_stat_user_tables</c>.
/// </summary>
public sealed record DatabaseTableUsage(
    string TableName,
    long TotalBytes,
    long TableBytes,
    long IndexBytes,
    long LiveRows,
    long DeadRows)
{
    /// <summary>Tamaño total en MB (redondeado a 2 decimales) para mostrar en la UI.</summary>
    public double TotalMb => Math.Round(TotalBytes / 1024d / 1024d, 2);

    /// <summary>
    /// Las filas muertas (versiones obsoletas que <c>VACUUM</c> aún no recicló) son la
    /// señal de que hay espacio reutilizable pero NO devuelto al sistema de archivos.
    /// </summary>
    public double DeadRowRatio => LiveRows + DeadRows == 0 ? 0 : Math.Round((double)DeadRows / (LiveRows + DeadRows), 4);
}

/// <summary>
/// Foto del almacenamiento de la base de datos: tamaño, límite del plan (Neon expone
/// <c>neon.max_cluster_size</c>) y las tablas que más pesan.
///
/// Cuando <see cref="UsagePercent"/> cruza <see cref="WarningThresholdPercent"/> el
/// siguiente paso es <c>POST /zorvian/v1/system/db-storage/purge</c> (retención) y, si
/// hace falta devolver el espacio al sistema de archivos, <c>VACUUM FULL</c> desde el
/// runbook <c>docs/DB_STORAGE_RECOVERY.md</c>.
/// </summary>
public sealed record DatabaseStorageReport(
    string Provider,
    string DatabaseName,
    long DatabaseSizeBytes,
    long? LimitBytes,
    double? UsagePercent,
    double WarningThresholdPercent,
    bool IsStorageFull,
    DateTime GeneratedAtUtc,
    IReadOnlyList<DatabaseTableUsage> LargestTables,
    IReadOnlyList<string> Recommendations)
{
    public double DatabaseSizeMb => Math.Round(DatabaseSizeBytes / 1024d / 1024d, 2);

    public double? LimitMb => LimitBytes is { } limit ? Math.Round(limit / 1024d / 1024d, 2) : null;
}

/// <summary>Resultado de purgar una tabla según su política de retención.</summary>
public sealed record RetentionPurgeResult(
    string TableName,
    string Label,
    int RetentionDays,
    DateTime CutoffUtc,
    long RowsDeleted,
    long SizeBeforeBytes,
    long SizeAfterBytes)
{
    /// <summary>
    /// Diferencia de tamaño tras el borrado. Ojo: un <c>DELETE</c> NO devuelve el espacio
    /// al sistema de archivos — normalmente es 0 y solo <c>VACUUM FULL</c> lo reduce. Lo
    /// que sí consigue el borrado es que esas páginas se reutilicen, que es lo que
    /// desbloquea las escrituras cuando Neon está lleno.
    /// </summary>
    public long SizeDeltaBytes => SizeAfterBytes - SizeBeforeBytes;
}

/// <summary>Resumen de una corrida de retención (job diario o <c>POST .../purge</c>).</summary>
public sealed record DataRetentionPurgeSummary(
    DateTime ExecutedAtUtc,
    long TotalRowsDeleted,
    long TotalSizeBeforeBytes,
    long TotalSizeAfterBytes,
    IReadOnlyList<RetentionPurgeResult> Results,
    IReadOnlyList<string> Notes)
{
    public long TotalSizeDeltaBytes => TotalSizeAfterBytes - TotalSizeBeforeBytes;
}

/// <summary>
/// Resultado de vaciar (<c>TRUNCATE</c>) una tabla de retención en modo emergencia.
/// A diferencia de <see cref="RetentionPurgeResult"/> no hay "filas vencidas": se
/// descarta todo el contenido de una tabla de telemetría/logs.
/// </summary>
public sealed record RetentionTruncateResult(
    string TableName,
    string Label,
    long SizeBeforeBytes,
    long SizeAfterBytes)
{
    public long SizeDeltaBytes => SizeAfterBytes - SizeBeforeBytes;
}

/// <summary>
/// Resumen de una purga de EMERGENCIA (<c>POST .../db-storage/truncate</c>).
///
/// <para>
/// Se usa cuando la base ya está en el límite del plan y ni un <c>DELETE</c> pasa
/// (Neon devuelve <c>53100</c> también al borrar, porque necesita páginas nuevas para
/// las versiones muertas). <c>TRUNCATE</c> vacía el archivo sin pedir espacio: es la
/// única operación que desbloquea las escrituras desde el API. Solo se permite sobre
/// las tablas de <c>DataRetentionRules</c> (+ <c>AuditLogs</c> si se pide explícito):
/// nunca sobre datos de negocio.
/// </para>
/// </summary>
public sealed record DataRetentionTruncateSummary(
    DateTime ExecutedAtUtc,
    long TotalSizeBeforeBytes,
    long TotalSizeAfterBytes,
    IReadOnlyList<RetentionTruncateResult> Results,
    IReadOnlyList<string> Notes)
{
    public long TotalSizeDeltaBytes => TotalSizeAfterBytes - TotalSizeBeforeBytes;

    /// <summary>Tablas que efectivamente quedaron vacías.</summary>
    public int TablesEmptied => Results.Count;
}
