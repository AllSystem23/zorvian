using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class CreditPaymentRepository : ICreditPaymentRepository
{
    private readonly ZorvianDbContext _db;

    public CreditPaymentRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<List<CreditPayment>> GetByCreditIdAsync(Guid creditId, int page, int pageSize) =>
        await _db.Set<CreditPayment>()
            .Include(p => p.Employee)
            .Include(p => p.CreditInstallment)
            .Where(p => p.CreditId == creditId)
            .OrderByDescending(p => p.PaymentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<int> GetCountByCreditIdAsync(Guid creditId) =>
        await _db.Set<CreditPayment>().CountAsync(p => p.CreditId == creditId);

    public async Task AddAsync(CreditPayment payment) =>
        await _db.Set<CreditPayment>().AddAsync(payment);

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
