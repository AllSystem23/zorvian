using Zorvian.Application.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Web.Jobs;

/// <summary>
/// Retención de las tablas de alto volumen (telemetría GPS, logs de webhooks, historial
/// de entidades, journal de sync y tokens vencidos) según
/// <see cref="DataRetentionRules"/>.
///
/// <para>
/// Motivo: Neon limita el proyecto a 1024 MB y al llegar al tope toda escritura falla
/// con <c>53100: could not extend file because project size limit (1024 MB) has been
/// exceeded</c>. Sin una purga periódica esas tablas crecen para siempre. Los
/// <c>AuditLogs</c> tienen su propio job (<c>audit-log-cleanup</c>).
/// </para>
///
/// <para>
/// Solo borra datos vencidos (nunca datos de negocio) y después corre <c>VACUUM ANALYZE</c>
/// sobre las tablas tocadas para que PostgreSQL reutilice las páginas. Devolver el
/// espacio al sistema de archivos requiere <c>VACUUM FULL</c>, que necesita espacio libre
/// adicional y por eso se hace a mano (ver <c>docs/DB_STORAGE_RECOVERY.md</c>).
/// </para>
/// </summary>
public sealed class DataRetentionJob
{
    private readonly IDatabaseMaintenanceService _maintenance;
    private readonly ILogger<DataRetentionJob> _logger;

    public DataRetentionJob(
        IDatabaseMaintenanceService maintenance,
        ILogger<DataRetentionJob> logger)
    {
        _maintenance = maintenance;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        try
        {
            var summary = await _maintenance.PurgeExpiredDataAsync();

            if (summary.TotalRowsDeleted == 0)
            {
                _logger.LogInformation("Data retention: nothing to purge (all tables within retention)");
                return;
            }

            _logger.LogInformation(
                "Data retention: {Rows} row(s) purged across {Tables} table(s) (size {Before} → {After} bytes)",
                summary.TotalRowsDeleted,
                summary.Results.Count(r => r.RowsDeleted > 0),
                summary.TotalSizeBeforeBytes,
                summary.TotalSizeAfterBytes);
        }
        catch (OperationCanceledException)
        {
            // Deploy/shutdown de Render: Hangfire debe verlo como cancelado, no como éxito.
            // (Estos jobs no reciben token propio, pero un OCE puede venir del proveedor.)
            throw;
        }
        catch (Exception ex) when (PostgresStorageErrors.IsStorageFull(ex))
        {
            // Justo cuando la base está llena el DELETE/LIMPIEZA es lo que libera espacio:
            // se loguea como advertencia accionable y el ciclo siguiente reintenta.
            _logger.LogWarning(
                ex,
                "Data retention: la base de datos alcanzó su límite de almacenamiento (53100). " +
                "Ejecute POST /zorvian/v1/system/db-storage/purge o siga docs/DB_STORAGE_RECOVERY.md");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data retention failed");
        }
    }
}
