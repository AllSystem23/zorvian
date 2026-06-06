using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class WarrantyProfitabilityReportService
{
    private readonly IWarrantyCostRepository _costRepo;
    private readonly IWarrantyRepository _warrantyRepo;
    private readonly ITenantContext _tenant;

    public WarrantyProfitabilityReportService(
        IWarrantyCostRepository costRepo,
        IWarrantyRepository warrantyRepo,
        ITenantContext tenant)
    {
        _costRepo = costRepo;
        _warrantyRepo = warrantyRepo;
        _tenant = tenant;
    }

    private Guid CompanyId => Guid.TryParse(_tenant.TenantId, out var id) ? id : throw new InvalidOperationException("Invalid tenant");

    public async Task<WarrantyCostSummary> GetCostByWarrantyAsync(Guid warrantyId)
    {
        var breakdown = await _costRepo.GetCostBreakdownByWarrantyAsync(warrantyId);
        var total = breakdown.Sum(b => b.Total);
        return new WarrantyCostSummary(
            TotalCost: total,
            PartsCost: breakdown.Where(b => b.Category == "parts").Sum(b => b.Total),
            LaborCost: breakdown.Where(b => b.Category == "labor").Sum(b => b.Total),
            OtherCost: breakdown.Where(b => b.Category != "parts" && b.Category != "labor").Sum(b => b.Total),
            BilledCostCount: breakdown.Count
        );
    }

    public async Task<WarrantyProfitabilityReport> GetProfitabilityReportAsync(DateTime from, DateTime to)
    {
        var companyId = CompanyId;

        var totalCost = await _costRepo.GetTotalCostByPeriodAsync(companyId, from, to);
        var costBreakdown = await _costRepo.GetCostBreakdownByPeriodAsync(companyId, from, to);
        var monthlyTrend = await _costRepo.GetMonthlyCostTrendAsync(companyId, 12);

        var warranties = await _warrantyRepo.GetWarrantiesWithCostsByPeriodAsync(companyId, from, to);
        var totalSaleValue = warranties
            .Where(w => w.Sale != null)
            .Sum(w => w.Sale!.Total);

        var byBrand = warranties
            .Where(w => w.Product?.Brand != null)
            .GroupBy(w => w.Product!.Brand!.Name)
            .Select(g => new BrandProfitability(
                BrandName: g.Key,
                WarrantyCount: g.Count(),
                TotalWarrantyCost: g.Sum(w => w.Costs.Sum(c => (decimal?)c.Quantity * c.UnitCost) ?? 0),
                TotalSaleValue: g.Sum(w => w.Sale?.Total ?? 0),
                ProfitMarginPercent: CalculateMargin(
                    g.Sum(w => w.Sale?.Total ?? 0),
                    g.Sum(w => w.Costs.Sum(c => (decimal?)c.Quantity * c.UnitCost) ?? 0))
            ))
            .OrderByDescending(b => b.TotalWarrantyCost)
            .ToList();

        var totalBreakdown = costBreakdown.Sum(b => b.Total);
        var breakdownPercent = costBreakdown
            .Select(b => new CostCategoryBreakdown(
                Category: b.Category,
                Total: b.Total,
                Percent: totalBreakdown > 0 ? Math.Round(b.Total / totalBreakdown * 100, 2) : 0
            ))
            .ToList();

        return new WarrantyProfitabilityReport(
            TotalWarrantyCost: totalCost,
            TotalSaleValue: totalSaleValue,
            ProfitMarginPercent: CalculateMargin(totalSaleValue, totalCost),
            ByBrand: byBrand,
            CostBreakdown: breakdownPercent,
            MonthlyTrend: monthlyTrend.Select(m => new MonthlyCostTrend(m.Year, m.Month, m.Total)).ToList(),
            From: from,
            To: to
        );
    }

    private static decimal CalculateMargin(decimal revenue, decimal cost)
    {
        if (revenue <= 0) return 0;
        return Math.Round((revenue - cost) / revenue * 100, 2);
    }
}
