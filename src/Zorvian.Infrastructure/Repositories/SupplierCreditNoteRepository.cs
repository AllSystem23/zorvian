using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class SupplierCreditNoteRepository : ISupplierCreditNoteRepository
{
    private readonly ZorvianDbContext _db;

    public SupplierCreditNoteRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<SupplierCreditNote?> GetByIdAsync(Guid id) =>
        await _db.Set<SupplierCreditNote>()
            .Include(cn => cn.Supplier)
            .Include(cn => cn.Purchase)
            .FirstOrDefaultAsync(cn => cn.Id == id);

    public async Task<List<SupplierCreditNote>> GetBySupplierIdAsync(Guid supplierId) =>
        await _db.Set<SupplierCreditNote>()
            .Include(cn => cn.Supplier)
            .Where(cn => cn.SupplierId == supplierId)
            .OrderByDescending(cn => cn.CreditNoteDate)
            .ToListAsync();

    public async Task<List<SupplierCreditNote>> GetByPurchaseIdAsync(Guid purchaseId) =>
        await _db.Set<SupplierCreditNote>()
            .Where(cn => cn.PurchaseId == purchaseId)
            .ToListAsync();

    public async Task<List<SupplierCreditNote>> GetAllAsync(Guid companyId) =>
        await _db.Set<SupplierCreditNote>()
            .Include(cn => cn.Supplier)
            .Where(cn => cn.CompanyId == companyId)
            .OrderByDescending(cn => cn.CreditNoteDate)
            .ToListAsync();

    public async Task<string> GenerateCreditNoteNumberAsync(Guid companyId)
    {
        var count = await _db.Set<SupplierCreditNote>().CountAsync(cn => cn.CompanyId == companyId);
        return $"NC-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}";
    }

    public async Task AddAsync(SupplierCreditNote creditNote) =>
        await _db.Set<SupplierCreditNote>().AddAsync(creditNote);

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
