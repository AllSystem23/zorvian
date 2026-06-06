using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class WarrantyPartUsageRepository : IWarrantyPartUsageRepository
{
    private readonly ZorvianDbContext _db;

    public WarrantyPartUsageRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(WarrantyPartUsage usage) =>
        await _db.Set<WarrantyPartUsage>().AddAsync(usage);

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
