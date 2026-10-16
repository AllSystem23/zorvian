using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class StockTransferRepository : IStockTransferRepository
{
    private readonly ZorvianDbContext _db;

    public StockTransferRepository(ZorvianDbContext db) => _db = db;

    public async Task<StockTransfer?> GetByIdAsync(Guid id) =>
        await _db.StockTransfers
            .Include(t => t.FromBranch)
            .Include(t => t.ToBranch)
            .Include(t => t.Details)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<(List<StockTransfer> Items, int Total)> GetFilteredAsync(
        string? search, string? status,
        Guid branchId, int page, int pageSize)
    {
        // Sin columna BranchId propia: la transferencia es visible para la sucursal
        // de ORIGEN y la de DESTINO (el filtro es explícito, no ApplyBranchScope).
        var query = _db.StockTransfers
            .Include(t => t.FromBranch)
            .Include(t => t.ToBranch)
            .Include(t => t.Details)
            .AsQueryable();

        if (branchId != Guid.Empty)
            query = query.Where(t => t.FromBranchId == branchId || t.ToBranchId == branchId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t =>
                t.TransferNumber.Contains(search) ||
                t.FromBranch!.Name.Contains(search) ||
                t.ToBranch!.Name.Contains(search));

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status == status);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<string> GenerateTransferNumberAsync(Guid companyId)
    {
        var now = DateTime.UtcNow;

        if (_db.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            var prefix = $"TR-{now:yyyyMMdd}-";
            var last = await _db.StockTransfers
                .Where(t => t.TransferNumber.StartsWith(prefix))
                .OrderByDescending(t => t.TransferNumber)
                .FirstOrDefaultAsync();
            int next = 1;
            if (last != null && int.TryParse(last.TransferNumber[^4..], out var lastNum))
                next = lastNum + 1;
            return $"{prefix}{next:D4}";
        }

        // PostgreSQL sequence: generación atómica (mismo patrón que OC/COMP/FAC).
        var raw = await _db.Database.SqlQueryRaw<int>(
            "SELECT nextval('seq_stock_transfer_number')::int").FirstOrDefaultAsync();
        return $"TR-{now:yyyyMMdd}-{raw:D4}";
    }

    public async Task AddAsync(StockTransfer transfer) =>
        await _db.StockTransfers.AddAsync(transfer);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
