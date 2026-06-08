using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class CashRegisterArqueoRepository : ICashRegisterArqueoRepository
{
    private readonly ZorvianDbContext _db;

    public CashRegisterArqueoRepository(ZorvianDbContext db) => _db = db;

    public async Task<CashRegisterArqueo?> GetByRegisterIdAsync(Guid cashRegisterId)
    {
        return await _db.Set<CashRegisterArqueo>()
            .Include(a => a.Employee)
            .Include(a => a.Denominations)
            .FirstOrDefaultAsync(a => a.CashRegisterId == cashRegisterId);
    }

    public async Task AddAsync(CashRegisterArqueo arqueo)
        => await _db.Set<CashRegisterArqueo>().AddAsync(arqueo);

    public async Task SaveChangesAsync()
        => await _db.SaveChangesAsync();
}
