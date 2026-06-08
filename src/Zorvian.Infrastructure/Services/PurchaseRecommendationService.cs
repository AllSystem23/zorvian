using Microsoft.EntityFrameworkCore;
using Zorvian.Application.DTOs.ML;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Services;

public sealed class PurchaseRecommendationService
{
    private readonly ZorvianDbContext _db;
    private readonly ITenantContext _tenant;

    public PurchaseRecommendationService(ZorvianDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PurchaseRecommendationSummaryDto> GetRecommendationsAsync(int demandDays = 30, int leadTimeDays = 7)
    {
        var tenantId = _tenant.TenantId;
        var demandSince = DateTime.UtcNow.Date.AddDays(-demandDays);

        var productDemand = await _db.SaleDetails
            .Where(sd => sd.Sale.TenantId == tenantId
                && sd.Sale.SaleDate >= demandSince
                && !sd.Sale.IsDeleted)
            .GroupBy(sd => sd.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                TotalSold = g.Sum(sd => sd.Quantity),
                TotalRevenue = g.Sum(sd => sd.Subtotal)
            })
            .ToDictionaryAsync(x => x.ProductId);

        var products = await _db.Products
            .Where(p => p.TenantId == tenantId && !p.IsDeleted && p.IsActive)
            .Include(p => p.Supplier)
            .Include(p => p.Category)
            .OrderBy(p => p.Stock)
            .ToListAsync();

        var recommendations = new List<PurchaseRecommendationDto>();
        var criticalCount = 0;
        var warningCount = 0;
        var healthyCount = 0;

        foreach (var product in products)
        {
            var demand = productDemand.GetValueOrDefault(product.Id);
            var totalSoldLastDays = demand?.TotalSold ?? 0;
            var avgDailyDemand = demandDays > 0 ? (double)totalSoldLastDays / demandDays : 0;
            var daysUntilStockout = avgDailyDemand > 0 ? product.Stock / avgDailyDemand : double.MaxValue;

            var priority = "healthy";
            if (product.Stock <= 0 || daysUntilStockout <= leadTimeDays)
            {
                priority = "critical";
                criticalCount++;
            }
            else if (product.Stock <= product.MinStock || daysUntilStockout <= leadTimeDays * 2)
            {
                priority = "warning";
                warningCount++;
            }
            else
            {
                healthyCount++;
            }

            if (priority != "healthy")
            {
                var reorderPoint = avgDailyDemand > 0
                    ? Math.Max(product.MinStock, (int)Math.Ceiling(avgDailyDemand * leadTimeDays))
                    : product.MinStock;
                var safetyStock = (int)Math.Ceiling(avgDailyDemand * leadTimeDays * 0.3);
                var recommendedQty = Math.Max(0, reorderPoint + safetyStock - product.Stock);

                recommendations.Add(new PurchaseRecommendationDto
                {
                    ProductId = product.Id,
                    ProductCode = product.Code,
                    ProductName = product.Name,
                    CategoryName = product.Category?.Name,
                    CurrentStock = product.Stock,
                    MinStock = product.MinStock,
                    CostPrice = product.CostPrice,
                    AverageDailyDemand = Math.Round(avgDailyDemand, 2),
                    DaysUntilStockout = daysUntilStockout == double.MaxValue ? -1 : Math.Round(daysUntilStockout, 1),
                    RecommendedQuantity = recommendedQty,
                    LastMonthSold = totalSoldLastDays,
                    SupplierName = product.Supplier?.Name,
                    SupplierId = product.SupplierId,
                    Priority = priority
                });
            }
        }

        return new PurchaseRecommendationSummaryDto
        {
            TotalProducts = products.Count,
            CriticalCount = criticalCount,
            WarningCount = warningCount,
            HealthyCount = healthyCount,
            TotalRecommendedCost = recommendations.Sum(r => r.RecommendedQuantity * r.CostPrice),
            Recommendations = [.. recommendations.OrderBy(r => r.Priority == "critical" ? 0 : 1).ThenBy(r => r.DaysUntilStockout)]
        };
    }
}
