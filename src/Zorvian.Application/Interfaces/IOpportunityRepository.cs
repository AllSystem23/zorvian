using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IOpportunityRepository
{
    Task<Opportunity?> GetByIdAsync(Guid id);
    Task<List<Opportunity>> GetByPipelineStageAsync(Guid stageId, Guid companyId);
    Task<List<Opportunity>> GetActiveOpportunitiesAsync(Guid companyId);
    Task AddAsync(Opportunity opportunity);
    Task UpdateAsync(Opportunity opportunity);
    Task DeleteAsync(Opportunity opportunity);
    Task SaveChangesAsync();
}
