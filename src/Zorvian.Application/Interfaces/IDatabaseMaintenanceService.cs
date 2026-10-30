using Zorvian.Application.DTOs.Common;

namespace Zorvian.Application.Interfaces;

/// <summary>
/// Mantenimiento del almacenamiento de la base de datos.
///
/// Existe por el incidente de Neon: el proyecto llegó a su límite de 1024 MB y toda
/// escritura empezó a fallar con <c>53100: could not extend file because project size
/// limit (1024 MB) has been exceeded</c>. Desde el API se puede ver qué está pesando
/// (<see cref="GetStorageReportAsync"/>) y purgar datos vencidos según las políticas de
/// retención (<see cref="PurgeExpiredDataAsync"/>), sin acceso a psql.
/// </summary>
public interface IDatabaseMaintenanceService
{
    /// <summary>
    /// Tamaño de la base, límite del plan (si el proveedor lo expone) y top de tablas
    /// por espacio ocupado. En proveedores distintos de PostgreSQL devuelve un reporte
    /// marcado como no disponible (nunca lanza).
    /// </summary>
    Task<DatabaseStorageReport> GetStorageReportAsync(int topTables = 15, CancellationToken cancellationToken = default);

    /// <summary>
    /// Borra, en lotes, las filas que superaron la retención de cada tabla de alto
    /// volumen (logs de webhooks, telemetría GPS, historial de entidades, tokens
    /// vencidos…), y luego corre <c>VACUUM ANALYZE</c> sobre las tablas tocadas para que
    /// PostgreSQL pueda reutilizar las páginas (VACUUM simple: no necesita espacio libre
    /// adicional, VACUUM FULL sí y por eso vive en el runbook, no en el API).
    /// </summary>
    Task<DataRetentionPurgeSummary> PurgeExpiredDataAsync(int batchSize = 0, CancellationToken cancellationToken = default);
}
