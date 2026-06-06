using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class WarrantyAttachmentRepository : IWarrantyAttachmentRepository
{
    private readonly ZorvianDbContext _db;

    public WarrantyAttachmentRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<WarrantyAttachment>> GetByWarrantyIdAsync(Guid warrantyId) =>
        await _db.Set<WarrantyAttachment>()
            .Where(a => a.WarrantyId == warrantyId)
            .OrderByDescending(a => a.UploadedAt)
            .ToListAsync();

    public async Task<List<WarrantyAttachment>> GetByClaimIdAsync(Guid claimId) =>
        await _db.Set<WarrantyAttachment>()
            .Where(a => a.ClaimId == claimId)
            .OrderByDescending(a => a.UploadedAt)
            .ToListAsync();

    public async Task<WarrantyAttachment?> GetByIdAsync(Guid id) =>
        await _db.Set<WarrantyAttachment>().FindAsync(id);

    public async Task AddAsync(WarrantyAttachment attachment) =>
        await _db.Set<WarrantyAttachment>().AddAsync(attachment);

    public Task DeleteAsync(WarrantyAttachment attachment)
    {
        _db.Set<WarrantyAttachment>().Remove(attachment);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
