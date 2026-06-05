using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class PayrollConceptRepository : IPayrollConceptRepository
{
    private readonly ZorvianDbContext _db;

    public PayrollConceptRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<List<PayrollConceptDefinition>> GetAllAsync(string tenantId) =>
        await _db.PayrollConceptDefinitions.Where(c => c.TenantId == tenantId).ToListAsync();

    public async Task<PayrollConceptDefinition?> GetByIdAsync(Guid id, string tenantId) =>
        await _db.PayrollConceptDefinitions.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);

    public async Task<PayrollConceptDefinition?> GetByCodeAsync(string code, string tenantId) =>
        await _db.PayrollConceptDefinitions.FirstOrDefaultAsync(c => c.Code == code && c.TenantId == tenantId);

    public async Task AddAsync(PayrollConceptDefinition definition) { await _db.PayrollConceptDefinitions.AddAsync(definition); await Task.CompletedTask; }

    public async Task UpdateAsync(PayrollConceptDefinition definition) { _db.PayrollConceptDefinitions.Update(definition); await Task.CompletedTask; }

    public async Task DeleteAsync(Guid id, string tenantId)
    {
        var definition = await GetByIdAsync(id, tenantId);
        if (definition != null)
        {
            _db.PayrollConceptDefinitions.Remove(definition);
        }
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
