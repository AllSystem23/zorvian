using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly ZorvianDbContext _db;

    public PurchaseOrderRepository(ZorvianDbContext db) => _db = db;

    public async Task<PurchaseOrder?> GetByIdAsync(Guid id) =>
        await _db.PurchaseOrders
            .Include(o => o.Supplier)
            .Include(o => o.Details)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<(List<PurchaseOrder> Items, int Total)> GetFilteredAsync(
        string? search, Guid? supplierId, string? status,
        DateTime? fromDate, DateTime? toDate, Guid branchId,
        int page, int pageSize)
    {
        var query = _db.PurchaseOrders
            .Include(o => o.Supplier)
            .Include(o => o.Details)
            .Where(o => o.BranchId == branchId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(o =>
                o.OrderNumber.Contains(search) ||
                o.Supplier.Name.Contains(search));

        if (supplierId.HasValue)
            query = query.Where(o => o.SupplierId == supplierId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);

        if (fromDate.HasValue)
            query = query.Where(o => o.OrderDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(o => o.OrderDate <= toDate.Value);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<string> GenerateOrderNumberAsync(Guid companyId)
    {
        var year = DateTime.UtcNow.Year;
        var month = DateTime.UtcNow.Month;

        if (_db.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            var prefix = $"OC-{year}{month:D2}-";
            var last = await _db.PurchaseOrders
                .Where(o => o.OrderNumber.StartsWith(prefix))
                .OrderByDescending(o => o.OrderNumber)
                .FirstOrDefaultAsync();
            int next = 1;
            if (last != null && int.TryParse(last.OrderNumber[^4..], out var lastNum))
                next = lastNum + 1;
            return $"{prefix}{next:D4}";
        }

        // Use PostgreSQL sequence for atomic, thread-safe number generation
        var raw = await _db.Database.SqlQueryRaw<int>("SELECT nextval('seq_order_number')::int").FirstOrDefaultAsync();
        return $"OC-{year}{month:D2}-{raw:D4}";
    }

    public async Task AddAsync(PurchaseOrder order) =>
        await _db.PurchaseOrders.AddAsync(order);

    public Task UpdateAsync(PurchaseOrder order)
    {
        _db.PurchaseOrders.Update(order);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
