using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Zorvian.Core.Entities;

namespace Zorvian.Infrastructure.Data;

/// <summary>
/// AUD-004: Interceptor que garantiza la inmutabilidad de los registros de auditoría
/// (<see cref="AuditLog"/>) a nivel de Entity Framework Core.
///
/// Este interceptor se complementa con la restricción a nivel de base de datos
/// (REVOKE UPDATE/DELETE + trigger) que se crea en la migración
/// <c>AddAuditLogImmutability</c>. De esta forma, la inmutabilidad se aplica
/// en dos capas (defensa en profundidad):
///
///   1. <b>EF Core</b>: ningún código de la aplicación puede marcar un
///      <c>AuditLog</c> como <c>Modified</c> o <c>Deleted</c> sin que se
///      lance una excepción clara antes de llegar a la base de datos.
///
///   2. <b>PostgreSQL</b>: incluso si un atacante evade la capa de aplicación
///      (por ejemplo, accediendo directamente a la base de datos con
///      SQL crudo o un cliente externo), el trigger de inmutabilidad
///      rechaza la operación.
///
/// La única operación permitida sobre los <c>AuditLog</c> es
/// <c>EntityState.Added</c> (inserción). No se permite modificar ni
/// eliminar ningún registro, ni siquiera como soft-delete.
/// </summary>
public sealed class AuditImmutabilityInterceptor : ISaveChangesInterceptor
{
    public ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ThrowIfAuditLogMutation(eventData.Context);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    public InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ThrowIfAuditLogMutation(eventData.Context);
        return result;
    }

    private static void ThrowIfAuditLogMutation(DbContext? dbContext)
    {
        if (dbContext is null) return;

        foreach (EntityEntry entry in dbContext.ChangeTracker.Entries<AuditLog>())
        {
            // Los AuditLog solo pueden ser creados (Added). Cualquier intento
            // de modificar o eliminar (incluyendo soft-delete) es rechazado.
            if (entry.State == EntityState.Modified ||
                entry.State == EntityState.Deleted)
            {
                var auditLog = (AuditLog)entry.Entity;
                throw new InvalidOperationException(
                    "AUD-004: Los registros de AuditLog son inmutables. " +
                    $"Operación '{entry.State}' rechazada sobre AuditLog " +
                    $"{{ Id = {auditLog.Id}, EntityName = {auditLog.EntityName}, " +
                    $"EntityId = {auditLog.EntityId}, Action = {auditLog.Action} }}. " +
                    "Esta restricción aplica tanto a nivel de aplicación (EF Core) como " +
                    "a nivel de base de datos (PostgreSQL trigger).");
            }
        }
    }
}
