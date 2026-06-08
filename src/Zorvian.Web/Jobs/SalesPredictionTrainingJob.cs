using Microsoft.EntityFrameworkCore;
using Zorvian.Application.DTOs.ML;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Services;

namespace Zorvian.Web.Jobs;

public sealed class SalesPredictionTrainingJob
{
    private readonly ZorvianDbContext _db;
    private readonly SalesPredictionService _mlService;

    public SalesPredictionTrainingJob(ZorvianDbContext db, SalesPredictionService mlService)
    {
        _db = db;
        _mlService = mlService;
    }

    public async Task RunAsync()
    {
        var twoYearsAgo = DateTime.UtcNow.AddYears(-2).Date;

        var sales = await _db.Sales
            .Where(s => s.SaleDate >= twoYearsAgo && !s.IsDeleted)
            .OrderBy(s => s.SaleDate)
            .ToListAsync();

        var dailySales = sales
            .GroupBy(s => s.SaleDate.Date)
            .OrderBy(g => g.Key)
            .ToList();

        var trainingData = new List<SalesData>();
        var salesByDate = dailySales.ToDictionary(g => g.Key, g => g.Sum(s => s.Total));

        for (int i = 0; i < dailySales.Count; i++)
        {
            var date = dailySales[i].Key;
            var previousDaySales = i > 0 ? salesByDate.GetValueOrDefault(date.AddDays(-1), 0) : 0;
            var previousWeekSales = 0m;
            for (int d = 1; d <= 7; d++)
                previousWeekSales += salesByDate.GetValueOrDefault(date.AddDays(-d), 0);

            trainingData.Add(new SalesData
            {
                DayOfWeek = (float)date.DayOfWeek,
                Month = (float)date.Month,
                DayOfMonth = (float)date.Day,
                IsWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday ? 1 : 0,
                IsHoliday = 0,
                PreviousDaySales = (float)previousDaySales,
                PreviousWeekSales = (float)previousWeekSales,
                DayOfYear = (float)date.DayOfYear,
                Label = (float)salesByDate[date]
            });
        }

        _mlService.Train(trainingData);
    }
}
