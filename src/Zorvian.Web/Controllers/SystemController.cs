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
    /// entidades, journal de sync y tokens vencidos) y hace <c>VACUUM ANALYZE</c> en las
    /// tablas afectadas. Idempotente: solo borra filas que superaron su retención.
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
}
