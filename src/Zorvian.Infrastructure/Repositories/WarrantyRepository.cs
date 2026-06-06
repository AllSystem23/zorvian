using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class WarrantyRepository : IWarrantyRepository
{
    private readonly ZorvianDbContext _db;

    public WarrantyRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<Warranty?> GetByIdAsync(Guid id) =>
        await _db.Set<Warranty>()
            .Include(w => w.Client)
            .Include(w => w.Product)
            .Include(w => w.Sale)
            .Include(w => w.Brand)
            .Include(w => w.Category)
            .Include(w => w.Claims)
            .FirstOrDefaultAsync(w => w.Id == id);

    public async Task<Warranty?> GetByWarrantyNumberAsync(string warrantyNumber) =>
        await _db.Set<Warranty>()
            .Include(w => w.Claims)
            .FirstOrDefaultAsync(w => w.WarrantyNumber == warrantyNumber);

    public async Task<WarrantyClaim?> GetClaimByIdAsync(Guid claimId) =>
        await _db.Set<WarrantyClaim>()
            .Include(c => c.Warranty)
            .FirstOrDefaultAsync(c => c.Id == claimId);

    public async Task<List<Warranty>> GetFilteredAsync(Guid? clientId, string? status, bool? expiringSoon, Guid branchId, int page, int pageSize)
    {
        var query = _db.Set<Warranty>().AsQueryable();
        if (clientId.HasValue) query = query.Where(w => w.ClientId == clientId.Value);
        if (!string.IsNullOrEmpty(status)) query = query.Where(w => w.Status.ToDbValue() == status);
        if (branchId != Guid.Empty) query = query.Where(w => w.BranchId == branchId);
        
        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(Guid? clientId, string? status, bool? expiringSoon, Guid branchId)
    {
        var query = _db.Set<Warranty>().AsQueryable();
        if (clientId.HasValue) query = query.Where(w => w.ClientId == clientId.Value);
        if (!string.IsNullOrEmpty(status)) query = query.Where(w => w.Status.ToDbValue() == status);
        if (branchId != Guid.Empty) query = query.Where(w => w.BranchId == branchId);
        
        return await query.CountAsync();
    }

    public async Task<List<Warranty>> GetWarrantiesWithCostsByPeriodAsync(Guid companyId, DateTime from, DateTime to)
    {
        return await _db.Set<Warranty>()
            .Where(w => w.CompanyId == companyId)
            .Include(w => w.Claims)
            .ToListAsync();
    }

    public async Task<List<Warranty>> GetAtRiskWarrantiesAsync()
    {
        return await _db.Set<Warranty>()
            .Where(w => w.Status != WarrantyStatus.Closed && w.Status != WarrantyStatus.Delivered && w.SlaDueAt != null && w.SlaBreachedAt == null)
            .ToListAsync();
    }

    public async Task<int> GetCountByStatusAsync(string status)
    {
        return await _db.Set<Warranty>().CountAsync(w => w.Status.ToDbValue() == status);
    }

    public async Task<int> GetBreachedSlaCountAsync()
    {
        return await _db.Set<Warranty>().CountAsync(w => w.SlaBreachedAt != null);
    }

    public async Task<string> GenerateWarrantyNumberAsync(Guid companyId)
    {
        var count = await _db.Set<Warranty>().CountAsync(w => w.CompanyId == companyId);
        return $"GAR-{DateTime.UtcNow:yyyyMMdd}-{count + 1:D4}";
    }

    public async Task AddAsync(Warranty warranty) => await _db.Set<Warranty>().AddAsync(warranty);

    public async Task UpdateAsync(Warranty warranty) => await Task.FromResult(_db.Set<Warranty>().Update(warranty));

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
