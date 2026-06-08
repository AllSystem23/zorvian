using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class CreditRefinancingRepository : ICreditRefinancingRepository
{
    private readonly ZorvianDbContext _db;

    public CreditRefinancingRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<CreditRefinancing>> GetByCreditIdAsync(Guid creditId)
    {
        return await _db.Set<CreditRefinancing>()
            .Where(r => r.CreditId == creditId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(CreditRefinancing refinancing)
        => await _db.Set<CreditRefinancing>().AddAsync(refinancing);

    public async Task SaveChangesAsync()
        => await _db.SaveChangesAsync();
}
