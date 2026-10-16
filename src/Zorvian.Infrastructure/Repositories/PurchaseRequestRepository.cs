using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class PurchaseRequestRepository : IPurchaseRequestRepository
{
    private readonly ZorvianDbContext _db;

    public PurchaseRequestRepository(ZorvianDbContext db) => _db = db;

    public async Task<PurchaseRequest?> GetByIdAsync(Guid id) =>
        await _db.PurchaseRequests
            .Include(r => r.Branch)
            .Include(r => r.Details)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<(List<PurchaseRequest> Items, int Total)> GetFilteredAsync(
        string? search, string? status, string? priority,
        Guid branchId, int page, int pageSize)
    {
        var query = _db.PurchaseRequests
            .Include(r => r.Branch)
            .Include(r => r.Details)
            .AsQueryable()
            .ApplyBranchScope(branchId, r => r.BranchId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r =>
                r.RequestNumber.Contains(search) ||
                r.Title.Contains(search));

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);

        if (!string.IsNullOrWhiteSpace(priority))
            query = query.Where(r => r.Priority == priority);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<string> GenerateRequestNumberAsync(Guid companyId)
    {
        var now = DateTime.UtcNow;

        if (_db.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            var prefix = $"SC-{now:yyyyMMdd}-";
            var last = await _db.PurchaseRequests
                .Where(r => r.RequestNumber.StartsWith(prefix))
                .OrderByDescending(r => r.RequestNumber)
                .FirstOrDefaultAsync();
            int next = 1;
            if (last != null && int.TryParse(last.RequestNumber[^4..], out var lastNum))
                next = lastNum + 1;
            return $"{prefix}{next:D4}";
        }

        // PostgreSQL sequence: generación atómica (mismo patrón que OC/COMP/FAC).
        var raw = await _db.Database.SqlQueryRaw<int>(
            "SELECT nextval('seq_purchase_request_number')::int").FirstOrDefaultAsync();
        return $"SC-{now:yyyyMMdd}-{raw:D4}";
    }

    public async Task AddAsync(PurchaseRequest request) =>
        await _db.PurchaseRequests.AddAsync(request);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
