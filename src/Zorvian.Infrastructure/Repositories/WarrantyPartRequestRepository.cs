using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class WarrantyPartRequestRepository : IWarrantyPartRequestRepository
{
    private readonly ZorvianDbContext _db;

    public WarrantyPartRequestRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<WarrantyPartRequest>> GetByClaimIdAsync(Guid claimId) =>
        await _db.Set<WarrantyPartRequest>()
            .Where(r => r.ClaimId == claimId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();

    public async Task<List<WarrantyPartRequest>> GetByProviderIdAsync(Guid providerId) =>
        await _db.Set<WarrantyPartRequest>()
            .Where(r => r.ProviderId == providerId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();

    public async Task<WarrantyPartRequest?> GetByIdAsync(Guid id) =>
        await _db.Set<WarrantyPartRequest>()
            .Include(r => r.Warranty)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task AddAsync(WarrantyPartRequest request) =>
        await _db.Set<WarrantyPartRequest>().AddAsync(request);

    public Task UpdateAsync(WarrantyPartRequest request)
    {
        _db.Set<WarrantyPartRequest>().Update(request);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
