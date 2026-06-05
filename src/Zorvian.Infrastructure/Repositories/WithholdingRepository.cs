using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class WithholdingRepository : IWithholdingRepository
{
    private readonly ZorvianDbContext _db;

    public WithholdingRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<Withholding?> GetByIdAsync(Guid id) =>
        await _db.Set<Withholding>()
            .Include(w => w.Purchase)
            .FirstOrDefaultAsync(w => w.Id == id);

    public async Task<List<Withholding>> GetByPurchaseIdAsync(Guid purchaseId) =>
        await _db.Set<Withholding>()
            .Where(w => w.PurchaseId == purchaseId)
            .ToListAsync();

    public async Task AddAsync(Withholding withholding) =>
        await _db.Set<Withholding>().AddAsync(withholding);

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
