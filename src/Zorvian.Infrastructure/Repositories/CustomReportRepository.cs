using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class CustomReportRepository : ICustomReportRepository
{
    private readonly ZorvianDbContext _db;

    public CustomReportRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<List<CustomReport>> GetAllAsync(Guid companyId)
    {
        return await _db.CustomReports
            .Where(r => r.CompanyId == companyId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<CustomReport?> GetByIdAsync(Guid id)
    {
        return await _db.CustomReports.FindAsync(id);
    }

    public async Task<List<CustomReport>> GetByModuleAsync(string module, Guid companyId)
    {
        return await _db.CustomReports
            .Where(r => r.Module == module && r.CompanyId == companyId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<CustomReport> AddAsync(CustomReport report)
    {
        _db.CustomReports.Add(report);
        await _db.SaveChangesAsync();
        return report;
    }

    public async Task<CustomReport?> UpdateAsync(CustomReport report)
    {
        _db.CustomReports.Update(report);
        await _db.SaveChangesAsync();
        return report;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _db.CustomReports.FindAsync(id);
        if (entity is null) return false;
        _db.CustomReports.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }
}
