using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

/// <summary>
/// Cola global de solicitudes de acceso. Sin filtro por compañía: la revisión
/// es de plataforma (SuperAdmin).
/// </summary>
public sealed class AccessRequestRepository : IAccessRequestRepository
{
    private readonly ZorvianDbContext _db;

    public AccessRequestRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(AccessRequest request)
    {
        await _db.AccessRequests.AddAsync(request);
    }

    public Task<AccessRequest?> GetByIdAsync(Guid id)
    {
        return _db.AccessRequests.FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<AccessRequest>> GetAllAsync(string? status = null)
    {
        var query = _db.AccessRequests.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public Task<bool> ExistsPendingByEmailAsync(string email)
    {
        var normalized = email.Trim().ToLower();
        return _db.AccessRequests
            .AnyAsync(r => r.Status == Core.Enums.AccessRequestStatus.Pending &&
                           r.Email.ToLower() == normalized);
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
