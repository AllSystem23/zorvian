using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class CreditNoteRepository : ICreditNoteRepository
{
    private readonly ZorvianDbContext _db;

    public CreditNoteRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<List<CreditNote>> GetAllAsync(Guid companyId) =>
        await _db.Set<CreditNote>()
            .Include(cn => cn.Sale)
            .Include(cn => cn.Details).ThenInclude(d => d.Product)
            .Where(cn => companyId == Guid.Empty || cn.CompanyId == companyId)
            .OrderByDescending(cn => cn.IssueDate)
            .ToListAsync();

    public async Task<CreditNote?> GetByIdAsync(Guid id) =>
        await _db.Set<CreditNote>()
            .Include(cn => cn.Sale).ThenInclude(s => s.Client)
            .Include(cn => cn.Details).ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(cn => cn.Id == id);

    public async Task<List<CreditNote>> GetBySaleIdAsync(Guid saleId) =>
        await _db.Set<CreditNote>()
            .Include(cn => cn.Details).ThenInclude(d => d.Product)
            .Where(cn => cn.SaleId == saleId)
            .OrderByDescending(cn => cn.IssueDate)
            .ToListAsync();

    public async Task<string> GenerateCreditNoteNumberAsync(Guid companyId)
    {
        if (_db.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            var count = await _db.Set<CreditNote>().CountAsync(cn => cn.CompanyId == companyId);
            return $"NC-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}";
        }

        // Use PostgreSQL sequence for atomic, thread-safe number generation
        var raw = await _db.Database.SqlQueryRaw<int>("SELECT nextval('seq_credit_note_number')::int").FirstOrDefaultAsync();
        return $"NC-{DateTime.UtcNow:yyyyMMdd}-{raw:D4}";
    }

    public async Task AddAsync(CreditNote creditNote) =>
        await _db.Set<CreditNote>().AddAsync(creditNote);

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
