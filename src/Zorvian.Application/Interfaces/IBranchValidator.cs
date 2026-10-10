namespace Zorvian.Application.Interfaces;

/// <summary>
/// Valida el alcance de sucursal en operaciones de escritura donde el cliente puede
/// enviar un BranchId explícito, evitando la inyección cross-company de sucursales.
/// </summary>
public interface IBranchValidator
{
    /// <summary>
    /// Resuelve la sucursal efectiva a persistir:
    /// - Sin BranchId pedido → la sucursal seleccionada en el contexto (null = ninguna).
    /// - Con BranchId pedido → valida que pertenezca a la compañía operativa;
    ///   si no pertenece, lanza <see cref="InvalidOperationException"/>.
    /// </summary>
    Task<Guid?> ResolveForWriteAsync(Guid? requestedBranchId);
}
