using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class CertificateRequestRepository : ICertificateRequestRepository
{
    private readonly ZorvianDbContext _db;

    public CertificateRequestRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<CertificateRequest?> GetByIdAsync(Guid id)
    {
        return await _db.CertificateRequests
            .Include(r => r.Employee)
            .Include(r => r.GeneratedDocument)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<CertificateRequest>> GetByEmployeeAsync(Guid employeeId)
    {
        return await _db.CertificateRequests
            .Include(r => r.Employee)
            .Include(r => r.GeneratedDocument)
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();
    }

    public async Task<List<CertificateRequest>> GetAllPendingAsync()
    {
        return await _db.CertificateRequests
            .Include(r => r.Employee)
            .Where(r => r.Status == "pending")
            .OrderBy(r => r.RequestedAt)
            .ToListAsync();
    }

    public async Task<List<CertificateRequest>> GetAllAsync(Guid? employeeId = null, string? status = null)
    {
        var query = _db.CertificateRequests
            .Include(r => r.Employee)
            .Include(r => r.GeneratedDocument)
            .AsQueryable();

        if (employeeId.HasValue)
            query = query.Where(r => r.EmployeeId == employeeId.Value);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);

        return await query
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();
    }

    public async Task AddAsync(CertificateRequest request)
    {
        await _db.CertificateRequests.AddAsync(request);
    }

    public Task UpdateAsync(CertificateRequest request)
    {
        _db.CertificateRequests.Update(request);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
