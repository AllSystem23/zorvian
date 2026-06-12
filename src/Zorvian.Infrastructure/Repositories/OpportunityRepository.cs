using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class OpportunityRepository : IOpportunityRepository
{
    private readonly ZorvianDbContext _db;

    public OpportunityRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<Opportunity?> GetByIdAsync(Guid id) =>
        await _db.Opportunities
            .Include(o => o.Stage)
            .Include(o => o.Client)
            .Include(o => o.Lead)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<List<Opportunity>> GetByPipelineStageAsync(Guid stageId, Guid companyId) =>
        await _db.Opportunities
            .Where(o => o.StageId == stageId && o.CompanyId == companyId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    public async Task<List<Opportunity>> GetActiveOpportunitiesAsync(Guid companyId) =>
        await _db.Opportunities
            .Where(o => o.CompanyId == companyId && o.Status == "open")
            .Include(o => o.Stage)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(Opportunity opportunity) =>
        await _db.Opportunities.AddAsync(opportunity);

    public Task UpdateAsync(Opportunity opportunity)
    {
        _db.Opportunities.Update(opportunity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Opportunity opportunity)
    {
        _db.Opportunities.Remove(opportunity);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
