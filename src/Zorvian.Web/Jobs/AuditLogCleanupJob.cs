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
///
/// <para>
/// Corre dentro de <see cref="AuditRetentionScope"/>: sin él,
/// <see cref="AuditImmutabilityInterceptor"/> (AUD-004) rechazaba el
/// <c>SaveChanges</c> y el job <b>nunca borraba nada</b> — la tabla de auditoría crecía
/// sin techo hasta llenar la base (incidente de Neon). El scope solo habilita el
/// borrado físico de logs vencidos; modificar un log sigue prohibido.
/// </para>
///
/// <para>
/// Hay una SEGUNDA capa en la base de datos: el trigger
/// <c>trg_audit_logs_immutable</c> rechaza <c>DELETE</c>/<c>TRUNCATE</c> con
/// <c>SQLSTATE P0001</c> ("AUD-004: AuditLogs es inmutable..."). Abrir solo el
/// interceptor no bastaba. En PostgreSQL cada lote de borrado corre en una transacción
/// con <see cref="AuditRetentionScope.EnableDatabaseSettingSql"/> (GUC transacción-local
/// que la migración <c>AddAuditLogRetentionBypass</c> hace consultar al trigger).
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

        // Sin este scope el AuditImmutabilityInterceptor (AUD-004) rechaza el borrado y
        // el job falla siempre, sin purgar una sola fila.
        using var retentionScope = AuditRetentionScope.Begin();

        // Segunda capa: el trigger PostgreSQL trg_audit_logs_immutable también bloquea
        // DELETE/TRUNCATE sobre AuditLogs (SQLSTATE P0001). En PostgreSQL el SaveChanges
        // de cada lote va dentro de una transacción que habilita el GUC de bypass; en
        // proveedores no relacionales (tests con InMemory) no hay trigger ni GUC.
        var requiresDatabaseBypass = IsPostgres();

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

            if (requiresDatabaseBypass)
            {
                await using var transaction = await _db.Database.BeginTransactionAsync();

                await _db.Database.ExecuteSqlRawAsync(
                    AuditRetentionScope.EnableDatabaseSettingSql, CancellationToken.None);

                _db.AuditLogs.RemoveRange(rows);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            else
            {
                _db.AuditLogs.RemoveRange(rows);
                await _db.SaveChangesAsync();
            }

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

    /// <summary>
    /// Mismo criterio que <c>DatabaseMaintenanceService.IsPostgres</c>: solo en PostgreSQL
    /// existe el trigger AUD-004 que exige el GUC de bypass.
    /// </summary>
    private bool IsPostgres() =>
        _db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
}
