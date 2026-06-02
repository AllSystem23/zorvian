using Microsoft.EntityFrameworkCore;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;
using Nexora.Infrastructure.Data;

namespace Nexora.Infrastructure.Repositories;

public sealed class QuoteRepository : IQuoteRepository
{
    private readonly NexoraDbContext _db;

    public QuoteRepository(NexoraDbContext db)
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

    public async Task<List<Quote>> GetFilteredAsync(Guid? clientId, string? status, DateTime? fromDate, DateTime? toDate, Guid branchId, int page, int pageSize)
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
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(q => q.Status == status);
        if (fromDate.HasValue)
            query = query.Where(q => q.QuoteDate >= DateOnly.FromDateTime(fromDate.Value));
        if (toDate.HasValue)
            query = query.Where(q => q.QuoteDate <= DateOnly.FromDateTime(toDate.Value));

        return await query
            .OrderByDescending(q => q.QuoteDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(Guid? clientId, string? status, DateTime? fromDate, DateTime? toDate, Guid branchId)
    {
        var query = _db.Set<Quote>().Where(q => q.BranchId == branchId).AsQueryable();

        if (clientId.HasValue)
            query = query.Where(q => q.ClientId == clientId.Value);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(q => q.Status == status);
        if (fromDate.HasValue)
            query = query.Where(q => q.QuoteDate >= DateOnly.FromDateTime(fromDate.Value));
        if (toDate.HasValue)
            query = query.Where(q => q.QuoteDate <= DateOnly.FromDateTime(toDate.Value));

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

    public Task DeleteAsync(Quote quote)
    {
        _db.Set<Quote>().Remove(quote);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
