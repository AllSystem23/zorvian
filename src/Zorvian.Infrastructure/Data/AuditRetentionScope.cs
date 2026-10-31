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
/// </summary>
public static class AuditRetentionScope
{
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
