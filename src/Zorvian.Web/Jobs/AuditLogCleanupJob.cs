using Microsoft.EntityFrameworkCore;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Web.Jobs;

/// <summary>
/// Purga los <c>AuditLogs</c> más viejos que la retención de
/// <see cref="DataRetentionRules.AuditLogs"/> (6 meses).
///
/// <para>
/// Antes hacía <c>ToListAsync()</c> de TODO lo vencido antes de borrar: con la tabla
/// crecida eso carga cientos de miles de entidades en memoria (Render tiene 512 MB) y
/// puede tumbar el proceso justo cuando la base está llena. Ahora borra en lotes de
/// <see cref="BatchSize"/> filas, con tope de lotes por corrida.
/// </para>
/// </summary>
public sealed class AuditLogCleanupJob
{
    /// <summary>Filas por lote (mantiene acotado el uso de memoria).</summary>
    public const int BatchSize = 5_000;

    /// <summary>Tope de lotes por corrida: el resto se purga en la siguiente.</summary>
    private const int MaxBatches = 400;

    private readonly ZorvianDbContext _db;
    private readonly ILogger<AuditLogCleanupJob> _logger;

    public AuditLogCleanupJob(ZorvianDbContext db, ILogger<AuditLogCleanupJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        var rule = DataRetentionRules.AuditLogs;
        var cutoff = DateTime.UtcNow.AddDays(-rule.RetentionDays);

        _logger.LogInformation(
            "Starting audit log cleanup. Removing logs older than {Cutoff} in batches of {BatchSize}",
            cutoff, BatchSize);

        long total = 0;

        for (var batch = 0; batch < MaxBatches; batch++)
        {
            // IgnoreQueryFilters: la limpieza es global por diseño (job sin tenant), igual
            // que el resto de jobs de mantenimiento.
            var rows = await _db.AuditLogs
                .IgnoreQueryFilters()
                .Where(a => a.CreatedAt < cutoff)
                .Take(BatchSize)
                .ToListAsync();

            if (rows.Count == 0)
                break;

            _db.AuditLogs.RemoveRange(rows);
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();

            total += rows.Count;

            if (rows.Count < BatchSize)
                break;
        }

        if (total == 0)
        {
            _logger.LogInformation("No audit logs to clean up");
            return;
        }

        _logger.LogInformation("Deleted {Count} old audit log entries", total);
    }
}
