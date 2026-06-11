using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Services.CommissionEngine;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Services;

public sealed class CommissionDataSource : ICommissionDataSource
{
    private readonly ZorvianDbContext _db;

    public CommissionDataSource(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<List<Sale>> GetSalesByPeriodAsync(Guid periodId, Guid companyId, Guid? employeeId = null)
    {
        var period = await _db.PayrollPeriods.FindAsync(periodId);
        if (period is null) return [];

        var query = _db.Sales
            .Include(s => s.Details)
                .ThenInclude(d => d.Product)
                    .ThenInclude(p => p!.Category)
            .Include(s => s.Details)
                .ThenInclude(d => d.Product)
                    .ThenInclude(p => p!.Brand)
            .Where(s => s.CompanyId == companyId
                && s.SaleDate >= period.StartDate.ToDateTime(TimeOnly.MinValue)
                && s.SaleDate <= period.EndDate.ToDateTime(TimeOnly.MaxValue)
                && s.Status == "completed"
                && !s.IsDeleted);

        if (employeeId.HasValue)
            query = query.Where(s => s.EmployeeId == employeeId.Value);

        return await query.ToListAsync();
    }

    public async Task<List<SalePayment>> GetCollectionsByPeriodAsync(Guid periodId, Guid companyId, Guid? employeeId = null)
    {
        var period = await _db.PayrollPeriods.FindAsync(periodId);
        if (period is null) return [];

        var query = _db.Set<SalePayment>()
            .Where(p => p.CompanyId == companyId
                && p.PaymentDate >= period.StartDate.ToDateTime(TimeOnly.MinValue)
                && p.PaymentDate <= period.EndDate.ToDateTime(TimeOnly.MaxValue));

        if (employeeId.HasValue)
        {
            query = query.Where(p =>
                _db.Sales.Any(s => s.Id == p.SaleId && s.EmployeeId == employeeId.Value));
        }

        return await query.ToListAsync();
    }

    public async Task<decimal> GetProfitBySaleAsync(Guid saleId)
    {
        var sale = await _db.Sales
            .Include(s => s.Details)
            .FirstOrDefaultAsync(s => s.Id == saleId);

        if (sale is null) return 0;
        return sale.Details?.Sum(d => d.Subtotal) ?? sale.Total;
    }

    public async Task<List<CommissionAssignment>> GetActiveAssignmentsAsync(Guid companyId)
    {
        return await _db.CommissionAssignments
            .Include(a => a.CommissionScheme)
            .Where(a => a.IsActive
                && a.EffectiveDate <= DateOnly.FromDateTime(DateTime.UtcNow)
                && (!a.ExpirationDate.HasValue || a.ExpirationDate >= DateOnly.FromDateTime(DateTime.UtcNow)))
            .ToListAsync();
    }
}
