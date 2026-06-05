using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class FixedAssetRepository : IFixedAssetRepository
{
    private readonly ZorvianDbContext _db;

    public FixedAssetRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<FixedAsset?> GetByIdAsync(Guid id) =>
        await _db.Set<FixedAsset>()
            .Include(a => a.Category)
            .Include(a => a.Location)
            .Include(a => a.Department)
            .Include(a => a.Supplier)
            .Include(a => a.DepreciationEntries)
            .Include(a => a.Revaluations)
            .Include(a => a.MaintenanceRecords)
            .Include(a => a.Disposal)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<FixedAsset?> GetByCodeAsync(string code, Guid companyId) =>
        await _db.Set<FixedAsset>().FirstOrDefaultAsync(a => a.Code == code && a.CompanyId == companyId);

    public async Task<List<FixedAsset>> GetFilteredAsync(Guid? categoryId, string? status, Guid? locationId, Guid? departmentId,
        string? search, DateTime? fromDate, DateTime? toDate, Guid companyId, int page, int pageSize)
    {
        var query = _db.Set<FixedAsset>()
            .Include(a => a.Category)
            .Include(a => a.Location)
            .Include(a => a.Department)
            .Where(a => a.CompanyId == companyId)
            .AsQueryable();

        if (categoryId.HasValue) query = query.Where(a => a.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(a => a.Status == status);
        if (locationId.HasValue) query = query.Where(a => a.LocationId == locationId.Value);
        if (departmentId.HasValue) query = query.Where(a => a.DepartmentId == departmentId.Value);
        if (fromDate.HasValue) query = query.Where(a => a.AcquisitionDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(a => a.AcquisitionDate <= toDate.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(a =>
                a.Name.ToLower().Contains(s) ||
                a.Code.ToLower().Contains(s) ||
                (a.SerialNumber != null && a.SerialNumber.ToLower().Contains(s)) ||
                (a.Barcode != null && a.Barcode.Contains(s)));
        }

        return await query
            .OrderBy(a => a.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(Guid? categoryId, string? status, Guid? locationId, Guid? departmentId,
        string? search, DateTime? fromDate, DateTime? toDate, Guid companyId)
    {
        var query = _db.Set<FixedAsset>()
            .Where(a => a.CompanyId == companyId)
            .AsQueryable();

        if (categoryId.HasValue) query = query.Where(a => a.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(a => a.Status == status);
        if (locationId.HasValue) query = query.Where(a => a.LocationId == locationId.Value);
        if (departmentId.HasValue) query = query.Where(a => a.DepartmentId == departmentId.Value);
        if (fromDate.HasValue) query = query.Where(a => a.AcquisitionDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(a => a.AcquisitionDate <= toDate.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(a =>
                a.Name.ToLower().Contains(s) ||
                a.Code.ToLower().Contains(s) ||
                (a.SerialNumber != null && a.SerialNumber.ToLower().Contains(s)) ||
                (a.Barcode != null && a.Barcode.Contains(s)));
        }

        return await query.CountAsync();
    }

    public async Task<List<FixedAsset>> GetActiveForDepreciationAsync(DateTime periodDate, Guid companyId)
    {
        return await _db.Set<FixedAsset>()
            .Include(a => a.DepreciationEntries)
            .Where(a => a.CompanyId == companyId && a.Status == "active" && a.AcquisitionDate <= periodDate)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(Guid companyId) =>
        await _db.Set<FixedAsset>().CountAsync(a => a.CompanyId == companyId);

    public async Task<string> GenerateCodeAsync(Guid companyId)
    {
        var count = await _db.Set<FixedAsset>()
            .CountAsync(a => a.CompanyId == companyId);
        return $"AF-{DateTime.UtcNow:yyyy}-{(count + 1):D6}";
    }

    public async Task AddAsync(FixedAsset asset) => await _db.Set<FixedAsset>().AddAsync(asset);
    public Task UpdateAsync(FixedAsset asset) { _db.Set<FixedAsset>().Update(asset); return Task.CompletedTask; }
    public Task DeleteAsync(FixedAsset asset) { _db.Set<FixedAsset>().Remove(asset); return Task.CompletedTask; }
    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
