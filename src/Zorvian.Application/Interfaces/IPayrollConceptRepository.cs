using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IPayrollConceptRepository
{
    Task<List<PayrollConceptDefinition>> GetAllAsync(string tenantId);
    Task<PayrollConceptDefinition?> GetByIdAsync(Guid id, string tenantId);
    Task<PayrollConceptDefinition?> GetByCodeAsync(string code, string tenantId);
    Task AddAsync(PayrollConceptDefinition definition);
    Task UpdateAsync(PayrollConceptDefinition definition);
    Task DeleteAsync(Guid id, string tenantId);
    Task SaveChangesAsync();
}
