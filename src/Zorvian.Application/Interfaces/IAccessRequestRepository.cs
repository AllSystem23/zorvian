using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

/// <summary>
/// Cola global de solicitudes de acceso ("Solicitar Acceso"). No tiene
/// compañía: la revisión es de plataforma.
/// </summary>
public interface IAccessRequestRepository
{
    Task AddAsync(AccessRequest request);

    Task<AccessRequest?> GetByIdAsync(Guid id);

    /// <summary>Lista las solicitudes; <paramref name="status"/> filtra si se informa.</summary>
    Task<List<AccessRequest>> GetAllAsync(string? status = null);

    /// <summary>True si ya existe una solicitud PENDIENTE con ese correo (evita duplicados).</summary>
    Task<bool> ExistsPendingByEmailAsync(string email);

    Task SaveChangesAsync();
}
