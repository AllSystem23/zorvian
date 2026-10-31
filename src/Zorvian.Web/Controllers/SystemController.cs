using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.Interfaces;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Diagnóstico y mantenimiento del almacenamiento de la base de datos.
///
/// <para>
/// Nació del incidente de Neon: el proyecto llegó su límite de 1024 MB y toda escritura
/// pasó a fallar con <c>53100: could not extend file because project size limit (1024 MB)
/// has been exceeded</c>. Con estos endpoints el operador ve qué tabla ocupa el espacio y
/// puede ejecutar la purga de retención sin abrir psql.
/// </para>
///
/// <para>
/// Son endpoints de plataforma (<c>company.manage</c> = SuperAdmin). No llevan
/// <c>[Audit]</c> a propósito: escribir el log de auditoría fallaría justo cuando la base
/// está llena, que es el escenario para el que existen.
/// </para>
/// </summary>
[ApiController]
[Authorize]
[Route("zorvian/v1/system")]
public sealed class SystemController : ControllerBase
{
    private readonly IDatabaseMaintenanceService _maintenance;

    public SystemController(IDatabaseMaintenanceService maintenance)
    {
        _maintenance = maintenance;
    }

    /// <summary>
    /// Tamaño de la base, límite del plan (Neon expone <c>neon.max_cluster_size</c>),
    /// porcentaje de uso, top de tablas por espacio y recomendaciones.
    /// </summary>
    [HttpGet("db-storage")]
    [RequirePermission(Permissions.CompanyManage)]
    public async Task<IActionResult> GetStorage([FromQuery] int topTables = 15, CancellationToken cancellationToken = default)
    {
        var report = await _maintenance.GetStorageReportAsync(
            topTables is < 1 or > 100 ? 15 : topTables, cancellationToken);

        return Ok(report);
    }

    /// <summary>
    /// Ejecuta AHORA la purga de retención (telemetría GPS, logs de webhooks, historial de
    /// entidades, journal de sync, tokens vencidos y logs de auditoría con más de 6 meses)
    /// y hace <c>VACUUM ANALYZE</c> en las tablas afectadas. Idempotente: solo borra filas
    /// que superaron su retención.
    /// </summary>
    [HttpPost("db-storage/purge")]
    [RequirePermission(Permissions.CompanyManage)]
    public async Task<IActionResult> Purge([FromQuery] int batchSize = 0, CancellationToken cancellationToken = default)
    {
        // Tope defensivo: un lote gigante haría una sentencia que puede tardar minutos.
        var safeBatchSize = batchSize is < 100 or > 50_000 ? 0 : batchSize;
        var summary = await _maintenance.PurgeExpiredDataAsync(safeBatchSize, cancellationToken);

        return Ok(summary);
    }

    /// <summary>
    /// Purga de EMERGENCIA: vacía (<c>TRUNCATE</c>) tablas de telemetría/logs cuando la
    /// base ya está en el límite del plan y ni un <c>DELETE</c> pasa (Neon responde
    /// <c>53100</c> también al borrar). <c>TRUNCATE</c> no necesita espacio libre, así que
    /// es la única operación que desbloquea las escrituras desde el API.
    ///
    /// <para>
    /// Descarta TODO el contenido de las tablas afectadas, no solo lo vencido. Solo se
    /// permite sobre las tablas de <c>DataRetentionRules</c>; los nombres desconocidos
    /// (incluidas tablas de negocio) se ignoran y se reportan en <c>notes</c>.
    /// <c>AuditLogs</c> requiere <c>includeAuditLogs=true</c> porque se conserva 6 meses
    /// por diseño (AUD-004).
    /// </para>
    /// </summary>
    /// <param name="tables">Lista separada por comas (p. ej. <c>AuditLogs,GpsPositions</c>). Vacío = todas las permitidas.</param>
    /// <param name="includeAuditLogs">Habilita <c>AuditLogs</c> como destino del vaciado.</param>
    /// <param name="cancellationToken">Cancelación de la operación.</param>
    [HttpPost("db-storage/truncate")]
    [RequirePermission(Permissions.CompanyManage)]
    public async Task<IActionResult> Truncate(
        [FromQuery] string? tables = null,
        [FromQuery] bool includeAuditLogs = false,
        CancellationToken cancellationToken = default)
    {
        var requested = string.IsNullOrWhiteSpace(tables)
            ? null
            : tables
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToArray();

        var summary = await _maintenance.TruncateRetentionTablesAsync(
            requested, includeAuditLogs, cancellationToken);

        return Ok(summary);
    }
}
