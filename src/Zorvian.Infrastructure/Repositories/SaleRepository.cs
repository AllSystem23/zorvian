using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class SaleRepository : ISaleRepository
{
    private readonly ZorvianDbContext _db;

    public SaleRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<Sale?> GetByIdAsync(Guid id) =>
        await _db.Set<Sale>()
            .Include(s => s.Client)
            .Include(s => s.Employee)
            .Include(s => s.Details)
                .ThenInclude(d => d.Product)
            .Include(s => s.Payments)
            .Include(s => s.Credit)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<List<Sale>> GetFilteredAsync(Guid? clientId, string? saleType, string? status, DateTime? fromDate, DateTime? toDate, Guid branchId, int page, int pageSize)
    {
        var query = _db.Set<Sale>()
            .Include(s => s.Client)
            .Where(s => s.BranchId == branchId)
            .AsQueryable();

        if (clientId.HasValue)
            query = query.Where(s => s.ClientId == clientId.Value);
        if (!string.IsNullOrWhiteSpace(saleType))
            query = query.Where(s => s.SaleType == saleType);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(s => s.Status == status);
        if (fromDate.HasValue)
            query = query.Where(s => s.SaleDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(s => s.SaleDate <= toDate.Value);

        return await query
            .OrderByDescending(s => s.SaleDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(Guid? clientId, string? saleType, string? status, DateTime? fromDate, DateTime? toDate, Guid branchId)
    {
        var query = _db.Set<Sale>().Where(s => s.BranchId == branchId).AsQueryable();

        if (clientId.HasValue) query = query.Where(s => s.ClientId == clientId.Value);
        if (!string.IsNullOrWhiteSpace(saleType)) query = query.Where(s => s.SaleType == saleType);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(s => s.Status == status);
        if (fromDate.HasValue) query = query.Where(s => s.SaleDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(s => s.SaleDate <= toDate.Value);

        return await query.CountAsync();
    }

    public async Task<string> GenerateInvoiceNumberAsync(Guid companyId)
    {
        var count = await _db.Set<Sale>().CountAsync(s => s.CompanyId == companyId);
        return $"FAC-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}";
    }

    public async Task<decimal> GetTodaySalesAsync(Guid branchId)
    {
        var today = DateTime.UtcNow.Date;
        return await _db.Set<Sale>()
            .Where(s => s.BranchId == branchId && s.SaleDate >= today)
            .SumAsync(s => s.Total);
    }

    public async Task<decimal> GetMonthSalesAsync(Guid branchId)
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return await _db.Set<Sale>()
            .Where(s => s.BranchId == branchId && s.SaleDate >= startOfMonth)
            .SumAsync(s => s.Total);
    }

    public async Task<decimal> GetAverageTicketAsync(Guid branchId)
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return await _db.Set<Sale>()
            .Where(s => s.BranchId == branchId && s.SaleDate >= startOfMonth)
            .AverageAsync(s => s.Total);
    }

    public async Task<int> GetTodaySalesCountAsync(Guid branchId)
    {
        var today = DateTime.UtcNow.Date;
        return await _db.Set<Sale>()
            .Where(s => s.BranchId == branchId && s.SaleDate >= today)
            .CountAsync();
    }

    public async Task AddAsync(Sale sale) =>
        await _db.Set<Sale>().AddAsync(sale);

    public Task UpdateAsync(Sale sale)
    {
        _db.Set<Sale>().Update(sale);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
