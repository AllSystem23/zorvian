using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class SupplierPaymentRepository : ISupplierPaymentRepository
{
    private readonly ZorvianDbContext _db;

    public SupplierPaymentRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<SupplierPayment?> GetByIdAsync(Guid id) =>
        await _db.Set<SupplierPayment>()
            .Include(p => p.Purchase)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<List<SupplierPayment>> GetByPurchaseIdAsync(Guid purchaseId) =>
        await _db.Set<SupplierPayment>()
            .Where(p => p.PurchaseId == purchaseId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

    public async Task<List<SupplierPayment>> GetAllAsync(Guid companyId) =>
        await _db.Set<SupplierPayment>()
            .Include(p => p.Purchase)
            .Where(p => p.CompanyId == companyId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

    public async Task AddAsync(SupplierPayment payment) =>
        await _db.Set<SupplierPayment>().AddAsync(payment);

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
