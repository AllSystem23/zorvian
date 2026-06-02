using Microsoft.EntityFrameworkCore;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;
using Nexora.Infrastructure.Data;

namespace Nexora.Infrastructure.Repositories;

public sealed class WarrantyRepository : IWarrantyRepository
{
    private readonly NexoraDbContext _db;

    public WarrantyRepository(NexoraDbContext db)
    {
        _db = db;
    }

    public async Task<Warranty?> GetByIdAsync(Guid id) =>
        await _db.Set<Warranty>()
            .Include(w => w.Client)
            .Include(w => w.Product)
            .Include(w => w.Sale)
            .Include(w => w.Claims)
            .FirstOrDefaultAsync(w => w.Id == id);

    public async Task<List<Warranty>> GetFilteredAsync(Guid? clientId, string? status, bool? expiringSoon, Guid branchId, int page, int pageSize)
    {
        var query = _db.Set<Warranty>()
            .Include(w => w.Client)
            .Include(w => w.Product)
            .Where(w => w.BranchId == branchId)
            .AsQueryable();

        if (clientId.HasValue) query = query.Where(w => w.ClientId == clientId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(w => w.Status == status);
        if (expiringSoon == true)
        {
            var threshold = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
            query = query.Where(w => w.EndDate <= threshold && w.EndDate >= DateOnly.FromDateTime(DateTime.UtcNow));
        }

        return await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(Guid? clientId, string? status, bool? expiringSoon, Guid branchId)
    {
        var query = _db.Set<Warranty>().Where(w => w.BranchId == branchId).AsQueryable();

        if (clientId.HasValue) query = query.Where(w => w.ClientId == clientId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(w => w.Status == status);
        if (expiringSoon == true)
        {
            var threshold = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
            query = query.Where(w => w.EndDate <= threshold && w.EndDate >= DateOnly.FromDateTime(DateTime.UtcNow));
        }

        return await query.CountAsync();
    }

    public async Task<string> GenerateWarrantyNumberAsync(Guid companyId)
    {
        var count = await _db.Set<Warranty>().CountAsync(w => w.CompanyId == companyId);
        return $"GAR-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}";
    }

    public async Task AddAsync(Warranty warranty) =>
        await _db.Set<Warranty>().AddAsync(warranty);

    public Task UpdateAsync(Warranty warranty)
    {
        _db.Set<Warranty>().Update(warranty);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
