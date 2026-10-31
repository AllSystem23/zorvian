namespace Zorvian.Infrastructure.Data;

/// <summary>
/// Excepción ACOTADA a la regla AUD-004 (inmutabilidad de <c>AuditLog</c>): permite
/// borrar registros de auditoría únicamente mientras el proceso de retención lo declare.
///
/// <para>
/// Motivo: <see cref="AuditImmutabilityInterceptor"/> rechaza cualquier
/// <c>EntityState.Deleted</c> sobre <c>AuditLog</c>, así que <c>AuditLogCleanupJob</c>
/// (y la purga de mantenimiento) nunca borraban nada: el job corría, encontraba filas
/// vencidas y explotaba con <c>InvalidOperationException</c> en el <c>SaveChanges</c>.
/// El log de auditoría se volvía, en la práctica, imposible de purgar.
/// </para>
///
/// <para>
/// La inmutabilidad se mantiene para el código de aplicación: nadie puede marcar un
/// <c>AuditLog</c> como <c>Deleted</c> fuera de un scope de retención, y
/// <c>Modified</c> (alterar el contenido o el soft-delete) sigue prohibido SIEMPRE —
/// incluidas las purgas. Solo se habilita el borrado físico.
/// </para>
///
/// <para>
/// El scope usa <see cref="AsyncLocal{T}"/> (igual que el contexto de tenant) porque los
/// interceptores son singletons de DI y el borrado ocurre en jobs/procesos distintos.
/// </para>
///
/// <para>
/// <b>Segunda capa: el trigger de la base.</b> AUD-004 también vive en PostgreSQL
/// (<c>trg_audit_logs_immutable</c> → <c>audit_logs_block_mutation()</c>, definido en
/// <c>Migrations/20261031120000_AddAuditLogRetentionBypass</c>), y ese trigger rechaza
/// <c>UPDATE</c>/<c>DELETE</c>/<c>TRUNCATE</c> con <c>SQLSTATE P0001</c>. Abrir solo el
/// interceptor de EF no basta: la sentencia muere en el servidor. El trigger consulta el
/// GUC <see cref="DatabaseSetting"/> y, dentro de la MISMA transacción del borrado, se
/// habilita con <see cref="EnableDatabaseSettingSql"/>.
/// </para>
/// </summary>
public static class AuditRetentionScope
{
    /// <summary>
    /// GUC de sesión/transacción que el trigger <c>audit_logs_block_mutation()</c>
    /// consulta para permitir el borrado físico de auditoría durante la retención.
    /// </summary>
    public const string DatabaseSetting = "zorvian.audit_retention";

    /// <summary>Valor que habilita el bypass en el trigger.</summary>
    public const string DatabaseSettingValue = "on";

    /// <summary>
    /// Sentencia que habilita el bypass <b>solo para la transacción actual</b>
    /// (<c>set_config(..., is_local = true)</c>). Debe ejecutarse dentro de la misma
    /// transacción que el <c>DELETE</c>/<c>TRUNCATE</c>: al terminar, la GUC vuelve sola
    /// a su valor anterior y nunca queda pegada en la conexión del pool (el contexto de
    /// EF y la conexión se reutilizan entre requests).
    /// </summary>
    public const string EnableDatabaseSettingSql =
        "SELECT set_config('" + DatabaseSetting + "', '" + DatabaseSettingValue + "', true)";

    private static readonly AsyncLocal<int> Depth = new();

    /// <summary>
    /// <c>true</c> mientras el hilo/contexto de ejecución está dentro de una purga de
    /// auditoría. Anidable: el scope externo decide cuándo se cierra.
    /// </summary>
    public static bool IsActive => Depth.Value > 0;

    /// <summary>
    /// Abre el scope de retención. Usar SIEMPRE en un <c>using</c>: el permiso dura lo
    /// que dura el bloque y no se filtra al resto del request/job.
    /// </summary>
    public static IDisposable Begin()
    {
        Depth.Value++;
        return new Scope();
    }

    private sealed class Scope : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (Depth.Value > 0)
                Depth.Value--;
        }
    }
}
