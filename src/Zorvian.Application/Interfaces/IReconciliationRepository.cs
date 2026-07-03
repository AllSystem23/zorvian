using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IReconciliationRepository
{
    Task<Reconciliation?> GetByIdAsync(Guid id);
    Task<List<Reconciliation>> GetFilteredAsync(Guid? bankAccountId, string? status, DateOnly? dateFrom, DateOnly? dateTo, Guid companyId, int page, int pageSize);
    Task<int> GetFilteredCountAsync(Guid? bankAccountId, string? status, DateOnly? dateFrom, DateOnly? dateTo, Guid companyId);
    Task AddAsync(Reconciliation reconciliation);
    Task UpdateAsync(Reconciliation reconciliation);
    Task DeleteAsync(Reconciliation reconciliation);
    Task SaveChangesAsync();
    Task<List<ReconciliationDetail>> GetDetailsByReconciliationIdAsync(Guid reconciliationId);
    Task AddDetailAsync(ReconciliationDetail detail);
    Task AddDetailsBulkAsync(List<ReconciliationDetail> details);
    Task ClearDetailsAsync(Guid reconciliationId);
}
