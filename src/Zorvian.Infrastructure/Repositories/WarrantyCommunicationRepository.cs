using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class WarrantyCommunicationRepository : IWarrantyCommunicationRepository
{
    private readonly ZorvianDbContext _db;

    public WarrantyCommunicationRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<WarrantyCommunication>> GetByWarrantyIdAsync(Guid warrantyId) =>
        await _db.Set<WarrantyCommunication>()
            .Where(c => c.WarrantyId == warrantyId)
            .OrderByDescending(c => c.SentAt)
            .ToListAsync();

    public async Task<WarrantyCommunication?> GetByIdAsync(Guid id) =>
        await _db.Set<WarrantyCommunication>().FindAsync(id);

    public async Task AddAsync(WarrantyCommunication communication) =>
        await _db.Set<WarrantyCommunication>().AddAsync(communication);

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
