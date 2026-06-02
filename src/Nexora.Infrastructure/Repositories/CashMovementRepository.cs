using Microsoft.EntityFrameworkCore;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;
using Nexora.Infrastructure.Data;

namespace Nexora.Infrastructure.Repositories;

public sealed class CashMovementRepository : ICashMovementRepository
{
    private readonly NexoraDbContext _db;

    public CashMovementRepository(NexoraDbContext db)
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
