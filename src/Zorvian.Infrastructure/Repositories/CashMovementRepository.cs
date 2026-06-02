using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class CashMovementRepository : ICashMovementRepository
{
    private readonly ZorvianDbContext _db;

    public CashMovementRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<List<CashMovement>> GetByCashRegisterIdAsync(Guid cashRegisterId) =>
        await _db.Set<CashMovement>()
            .Include(m => m.Employee)
            .Include(m => m.CashRegister)
            .Where(m => m.CashRegisterId == cashRegisterId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(CashMovement movement) =>
        await _db.Set<CashMovement>().AddAsync(movement);

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
