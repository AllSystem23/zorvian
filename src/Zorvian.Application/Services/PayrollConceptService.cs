using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class PayrollConceptService
{
    private readonly IPayrollConceptRepository _repo;
    private readonly ITenantContext _tenant;

    public PayrollConceptService(IPayrollConceptRepository repo, ITenantContext tenant)
    {
        _repo = repo;
        _tenant = tenant;
    }

    private string TenantId => _tenant.TenantId;

    public async Task<List<PayrollConceptDefinition>> GetAllAsync() => await _repo.GetAllAsync(TenantId);

    public async Task<PayrollConceptDefinition?> GetByIdAsync(Guid id) => await _repo.GetByIdAsync(id, TenantId);

    public async Task<PayrollConceptDefinition> CreateAsync(PayrollConceptDefinition definition)
    {
        definition.TenantId = TenantId;
        await _repo.AddAsync(definition);
        await _repo.SaveChangesAsync();
        return definition;
    }

    public async Task UpdateAsync(PayrollConceptDefinition definition)
    {
        var existing = await _repo.GetByIdAsync(definition.Id, TenantId);
        if (existing == null) throw new KeyNotFoundException("Concept not found");
        
        definition.TenantId = TenantId;
        await _repo.UpdateAsync(definition);
        await _repo.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await _repo.DeleteAsync(id, TenantId);
        await _repo.SaveChangesAsync();
    }
}
