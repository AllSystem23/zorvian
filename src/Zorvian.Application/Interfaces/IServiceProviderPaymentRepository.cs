using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IServiceProviderPaymentRepository
{
    Task<ServiceProviderPayment?> GetByIdAsync(Guid id);

    /// <summary>Historial: búsqueda por número/nombre del prestador + filtros de agente/período/estado.
    /// El filtro de sucursal se aplica sobre la sucursal DEL PRESTADOR (Employee.BranchId):
    /// Guid.Empty o null = todas las sucursales.</summary>
    Task<(List<ServiceProviderPayment> Items, int Total)> GetFilteredAsync(
        string? search, Guid? employeeId, int? year, int? month, int? half,
        string? status, Guid? branchId, int page, int pageSize);

    /// <summary>Guard de duplicado: pago NO cancelado para el mismo prestador y período quincenal.</summary>
    Task<ServiceProviderPayment?> GetByPeriodAsync(Guid employeeId, int year, int month, int half);

    Task<string> GeneratePaymentNumberAsync(Guid companyId);

    Task AddAsync(ServiceProviderPayment payment);
    Task SaveChangesAsync();
}
