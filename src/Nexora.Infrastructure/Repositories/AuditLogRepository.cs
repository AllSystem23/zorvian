using Microsoft.EntityFrameworkCore;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;
using Nexora.Infrastructure.Data;

namespace Nexora.Infrastructure.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly NexoraDbContext _db;

    public AuditLogRepository(NexoraDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(AuditLog log)
    {
        await _db.AuditLogs.AddAsync(log);
        await _db.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetFilteredAsync(string? entityName, string? action, DateTime? from, DateTime? to, int page, int pageSize)
    {
        var query = _db.AuditLogs.AsQueryable();

        if (!string.IsNullOrEmpty(entityName))
            query = query.Where(a => a.EntityName == entityName);
        if (!string.IsNullOrEmpty(action))
            query = query.Where(a => a.Action == action);
        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(a => a.CreatedAt <= to.Value);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(string? entityName, string? action, DateTime? from, DateTime? to)
    {
        var query = _db.AuditLogs.AsQueryable();

        if (!string.IsNullOrEmpty(entityName))
            query = query.Where(a => a.EntityName == entityName);
        if (!string.IsNullOrEmpty(action))
            query = query.Where(a => a.Action == action);
        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(a => a.CreatedAt <= to.Value);

        return await query.CountAsync();
    }
}
