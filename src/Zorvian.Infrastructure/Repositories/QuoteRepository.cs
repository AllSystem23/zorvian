using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Enums;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class QuoteRepository : IQuoteRepository
{
    private readonly ZorvianDbContext _db;

    public QuoteRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<Quote?> GetByIdAsync(Guid id) =>
        await _db.Set<Quote>()
            .Include(q => q.Client)
            .Include(q => q.Employee)
            .Include(q => q.Details)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(q => q.Id == id);

    public async Task<List<Quote>> GetFilteredAsync(Guid? clientId, QuoteStatus? status, DateTime? fromDate, DateTime? toDate, string? search, Guid branchId, int page, int pageSize)
    {
        var query = _db.Set<Quote>()
            .Include(q => q.Client)
            .Include(q => q.Employee)
            .Include(q => q.Details)
                .ThenInclude(d => d.Product)
            .Where(q => q.BranchId == branchId)
            .AsQueryable();

        if (clientId.HasValue)
            query = query.Where(q => q.ClientId == clientId.Value);
        if (status.HasValue)
            query = query.Where(q => q.Status == status.Value);
        if (fromDate.HasValue)
            query = query.Where(q => q.QuoteDate >= DateOnly.FromDateTime(fromDate.Value));
        if (toDate.HasValue)
            query = query.Where(q => q.QuoteDate <= DateOnly.FromDateTime(toDate.Value));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.ToLower();
            query = query.Where(quote => quote.QuoteNumber.ToLower().Contains(q)
                || quote.Client.FirstName.ToLower().Contains(q)
                || quote.Client.LastName.ToLower().Contains(q));
        }

        return await query
            .OrderByDescending(q => q.QuoteDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(Guid? clientId, QuoteStatus? status, DateTime? fromDate, DateTime? toDate, string? search, Guid branchId)
    {
        var query = _db.Set<Quote>()
            .Include(q => q.Client)
            .Where(q => q.BranchId == branchId)
            .AsQueryable();

        if (clientId.HasValue)
            query = query.Where(q => q.ClientId == clientId.Value);
        if (status.HasValue)
            query = query.Where(q => q.Status == status.Value);
        if (fromDate.HasValue)
            query = query.Where(q => q.QuoteDate >= DateOnly.FromDateTime(fromDate.Value));
        if (toDate.HasValue)
            query = query.Where(q => q.QuoteDate <= DateOnly.FromDateTime(toDate.Value));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.ToLower();
            query = query.Where(quote => quote.QuoteNumber.ToLower().Contains(q)
                || quote.Client.FirstName.ToLower().Contains(q)
                || quote.Client.LastName.ToLower().Contains(q));
        }

        return await query.CountAsync();
    }

    public async Task<string> GenerateNumberAsync(Guid companyId)
    {
        var count = await _db.Set<Quote>().CountAsync(q => q.CompanyId == companyId);
        return $"COT-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}";
    }

    public async Task AddAsync(Quote quote) =>
        await _db.Set<Quote>().AddAsync(quote);

    public Task UpdateAsync(Quote quote)
    {
        _db.Set<Quote>().Update(quote);
        return Task.CompletedTask;
    }

    public async Task UpdateStatusAsync(Guid id, QuoteStatus status)
    {
        var quote = await _db.Set<Quote>().FindAsync(id);
        if (quote != null)
        {
            quote.Status = status;
            quote.UpdatedAt = DateTime.UtcNow;
        }
    }

    public Task DeleteAsync(Quote quote)
    {
        _db.Set<Quote>().Remove(quote);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
