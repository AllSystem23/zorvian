using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories.Fleet;

public sealed class FleetDocumentRepository : IFleetDocumentRepository
{
    private readonly ZorvianDbContext _db;

    public FleetDocumentRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<FleetDocument>> GetAllAsync(Guid companyId) =>
        await _db.Set<FleetDocument>()
            .Include(d => d.DocumentType)
            .OrderByDescending(d => d.ExpiryDate)
            .ToListAsync();

    public async Task<List<FleetDocument>> GetByEntityAsync(string entityType, Guid entityId) =>
        await _db.Set<FleetDocument>()
            .Include(d => d.DocumentType)
            .Where(d => d.EntityType == entityType && d.EntityId == entityId)
            .OrderByDescending(d => d.ExpiryDate)
            .ToListAsync();

    public async Task<FleetDocument?> GetByIdAsync(Guid id) =>
        await _db.Set<FleetDocument>()
            .Include(d => d.DocumentType)
            .FirstOrDefaultAsync(d => d.Id == id);

    public async Task AddAsync(FleetDocument document) =>
        await _db.Set<FleetDocument>().AddAsync(document);

    public Task UpdateAsync(FleetDocument document)
    {
        _db.Set<FleetDocument>().Update(document);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(FleetDocument document)
    {
        _db.Set<FleetDocument>().Remove(document);
        return Task.CompletedTask;
    }

    public async Task<List<FleetDocument>> GetExpiringAsync(int days)
    {
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(days));
        return await _db.Set<FleetDocument>()
            .Include(d => d.DocumentType)
            .Where(d => d.ExpiryDate != null && d.ExpiryDate <= cutoff && d.Status == "Valid")
            .ToListAsync();
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
