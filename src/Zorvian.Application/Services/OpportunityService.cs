using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class OpportunityService
{
    private readonly IOpportunityRepository _opportunityRepo;
    private readonly ITenantContext _tenantContext;

    public OpportunityService(IOpportunityRepository opportunityRepo, ITenantContext tenantContext)
    {
        _opportunityRepo = opportunityRepo;
        _tenantContext = tenantContext;
    }

    public async Task<Opportunity?> GetOpportunityByIdAsync(Guid id) => await _opportunityRepo.GetByIdAsync(id);

    public async Task<List<Opportunity>> GetActiveOpportunitiesAsync()
    {
        return await _opportunityRepo.GetActiveOpportunitiesAsync(_tenantContext.TenantId.Value);
    }

    public async Task<Opportunity> CreateOpportunityAsync(Opportunity opportunity)
    {
        opportunity.CompanyId = _tenantContext.TenantId.Value;
        await _opportunityRepo.AddAsync(opportunity);
        await _opportunityRepo.SaveChangesAsync();
        return opportunity;
    }

    public async Task UpdateOpportunityAsync(Opportunity opportunity)
    {
        await _opportunityRepo.UpdateAsync(opportunity);
        await _opportunityRepo.SaveChangesAsync();
    }
    
    public async Task<List<Opportunity>> GetByPipelineStageAsync(Guid stageId)
    {
        return await _opportunityRepo.GetByPipelineStageAsync(stageId, _tenantContext.TenantId.Value);
    }
}
