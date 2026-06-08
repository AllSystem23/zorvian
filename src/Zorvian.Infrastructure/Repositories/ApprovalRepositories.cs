using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class ApprovalFlowConfigRepository : IApprovalFlowConfigRepository
{
    private readonly ZorvianDbContext _db;
    public ApprovalFlowConfigRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<ApprovalFlowConfig>> GetAllAsync(Guid companyId) =>
        await _db.Set<ApprovalFlowConfig>()
            .Include(c => c.Steps.OrderBy(s => s.StepOrder))
            .Where(c => c.CompanyId == companyId)
            .OrderBy(c => c.Module).ThenBy(c => c.EventType)
            .ToListAsync();

    public async Task<ApprovalFlowConfig?> GetByIdAsync(Guid id) =>
        await _db.Set<ApprovalFlowConfig>()
            .Include(c => c.Steps.OrderBy(s => s.StepOrder))
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<ApprovalFlowConfig?> GetByModuleAndEventAsync(string module, string eventType, Guid companyId) =>
        await _db.Set<ApprovalFlowConfig>()
            .Include(c => c.Steps.OrderBy(s => s.StepOrder))
            .FirstOrDefaultAsync(c => c.Module == module && c.EventType == eventType && c.CompanyId == companyId && c.IsActive);

    public async Task AddAsync(ApprovalFlowConfig config) => await _db.Set<ApprovalFlowConfig>().AddAsync(config);
    public Task UpdateAsync(ApprovalFlowConfig config) { _db.Set<ApprovalFlowConfig>().Update(config); return Task.CompletedTask; }
    public Task DeleteAsync(ApprovalFlowConfig config) { _db.Set<ApprovalFlowConfig>().Remove(config); return Task.CompletedTask; }
    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}

public sealed class ApprovalRequestRepository : IApprovalRequestRepository
{
    private readonly ZorvianDbContext _db;
    public ApprovalRequestRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<ApprovalRequest>> GetPendingByRoleAsync(string approverRole, Guid companyId) =>
        await _db.Set<ApprovalRequest>()
            .Include(r => r.Actions)
            .Where(r => r.Status == "pending" && r.CompanyId == companyId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();

    public async Task<List<ApprovalRequest>> GetByReferenceAsync(Guid referenceId) =>
        await _db.Set<ApprovalRequest>()
            .Include(r => r.Actions)
            .Where(r => r.ReferenceId == referenceId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();

    public async Task<ApprovalRequest?> GetByIdAsync(Guid id) =>
        await _db.Set<ApprovalRequest>()
            .Include(r => r.Actions)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task AddAsync(ApprovalRequest request) => await _db.Set<ApprovalRequest>().AddAsync(request);
    public Task UpdateAsync(ApprovalRequest request) { _db.Set<ApprovalRequest>().Update(request); return Task.CompletedTask; }
    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
