using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zorvian.Application.DTOs.ML;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Services;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/sales/predictions")]
public sealed class SalesPredictionController : ControllerBase
{
    private readonly SalesPredictionService _mlService;
    private readonly ZorvianDbContext _db;
    private readonly ITenantContext _tenant;

    public SalesPredictionController(SalesPredictionService mlService, ZorvianDbContext db, ITenantContext tenant)
    {
        _mlService = mlService;
        _db = db;
        _tenant = tenant;
    }

    [HttpGet("next-week")]
    [RequirePermission(Permissions.SaleRead)]
    public async Task<IActionResult> GetNextWeekPredictions()
    {
        var predictions = new List<object>();
        var today = DateTime.UtcNow.Date;

        for (int i = 1; i <= 7; i++)
        {
            var date = today.AddDays(i);
            var data = await BuildSalesDataAsync(date);
            var prediction = _mlService.Predict(data);

            predictions.Add(new
            {
                Date = date.ToString("yyyy-MM-dd"),
                DayOfWeek = date.DayOfWeek.ToString(),
                PredictedSales = Math.Round(prediction.PredictedSales, 2),
                LowerBound = Math.Round(prediction.LowerBound, 2),
                UpperBound = Math.Round(prediction.UpperBound, 2)
            });
        }

        return Ok(predictions);
    }

    [HttpGet("next-month")]
    [RequirePermission(Permissions.SaleRead)]
    public async Task<IActionResult> GetNextMonthPredictions()
    {
        var predictions = new List<object>();
        var today = DateTime.UtcNow.Date;
        var totalPredicted = 0m;

        for (int i = 1; i <= 30; i++)
        {
            var date = today.AddDays(i);
            var data = await BuildSalesDataAsync(date);
            var prediction = _mlService.Predict(data);
            var predicted = Math.Round(prediction.PredictedSales, 2);
            totalPredicted += (decimal)predicted;

            predictions.Add(new
            {
                Date = date.ToString("yyyy-MM-dd"),
                DayOfWeek = date.DayOfWeek.ToString(),
                PredictedSales = predicted,
                LowerBound = Math.Round(prediction.LowerBound, 2),
                UpperBound = Math.Round(prediction.UpperBound, 2)
            });
        }

        return Ok(new
        {
            TotalPredicted = Math.Round(totalPredicted, 2),
            DailyAverage = Math.Round(totalPredicted / 30, 2),
            Predictions = predictions
        });
    }

    [HttpGet("monthly-total")]
    [RequirePermission(Permissions.SaleRead)]
    public async Task<IActionResult> GetMonthlyPrediction()
    {
        var today = DateTime.UtcNow.Date;
        var totalPredicted = 0m;
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);

        for (int day = today.Day; day <= daysInMonth; day++)
        {
            var date = new DateTime(today.Year, today.Month, day, 0, 0, 0, DateTimeKind.Utc);
            var data = await BuildSalesDataAsync(date);
            var prediction = _mlService.Predict(data);
            totalPredicted += (decimal)prediction.PredictedSales;
        }

        var currentMonthSales = await _db.Sales
            .Where(s => (s.TenantId == _tenant.TenantId || _tenant.IsSuperAdmin)
                && s.SaleDate.Year == today.Year
                && s.SaleDate.Month == today.Month
                && !s.IsDeleted)
            .SumAsync(s => s.Total);

        return Ok(new
        {
            Month = today.Month,
            Year = today.Year,
            SalesSoFar = currentMonthSales,
            PredictedTotal = Math.Round(totalPredicted, 2),
            ProjectedTotal = Math.Round(currentMonthSales + totalPredicted, 2),
            RemainingDays = daysInMonth - today.Day + 1
        });
    }

    private async Task<SalesData> BuildSalesDataAsync(DateTime date)
    {
        var tenantId = _tenant.TenantId;

        var previousDaySales = await _db.Sales
            .Where(s => (s.TenantId == tenantId || _tenant.IsSuperAdmin)
                && s.SaleDate.Date == date.AddDays(-1).Date
                && !s.IsDeleted)
            .SumAsync(s => s.Total);

        var weekAgo = date.AddDays(-7);
        var previousWeekSales = await _db.Sales
            .Where(s => (s.TenantId == tenantId || _tenant.IsSuperAdmin)
                && s.SaleDate.Date >= weekAgo.Date
                && s.SaleDate.Date < date.Date
                && !s.IsDeleted)
            .SumAsync(s => s.Total);

        return new SalesData
        {
            DayOfWeek = (float)date.DayOfWeek,
            Month = (float)date.Month,
            DayOfMonth = (float)date.Day,
            IsWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday ? 1 : 0,
            IsHoliday = 0,
            PreviousDaySales = (float)previousDaySales,
            PreviousWeekSales = (float)previousWeekSales,
            DayOfYear = (float)date.DayOfYear,
            Label = 0
        };
    }
}
