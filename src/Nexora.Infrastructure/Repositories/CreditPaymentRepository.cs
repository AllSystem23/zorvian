using Microsoft.EntityFrameworkCore;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;
using Nexora.Infrastructure.Data;

namespace Nexora.Infrastructure.Repositories;

public sealed class CreditPaymentRepository : ICreditPaymentRepository
{
    private readonly NexoraDbContext _db;

    public CreditPaymentRepository(NexoraDbContext db)
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
