using Microsoft.EntityFrameworkCore;
using Zorvian.Application.DTOs.ML;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Services;

namespace Zorvian.Web.Jobs;

public sealed class AbsenteeismTrainingJob
{
    private readonly ZorvianDbContext _db;
    private readonly AbsenteeismPredictionService _mlService;

    public AbsenteeismTrainingJob(ZorvianDbContext db, AbsenteeismPredictionService mlService)
    {
        _db = db;
        _mlService = mlService;
    }

    public async Task RunAsync()
    {
        var sixMonthsAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6));

        var records = await _db.AttendanceRecords
            .Include(r => r.Employee)
            .Where(r => r.Date >= sixMonthsAgo)
            .OrderBy(r => r.EmployeeId)
            .ThenBy(r => r.Date)
            .ToListAsync();

        var absenceCounts = records
            .Where(r => r.Status != "present")
            .GroupBy(r => r.EmployeeId)
            .ToDictionary(g => g.Key, g => g.Count());

        var trainingData = records.Select(r => new AttendanceData
        {
            DayOfWeek = (float)r.Date.DayOfWeek,
            Month = (float)r.Date.Month,
            IsHoliday = 0,
            PreviousAbsenceCount = (float)(absenceCounts.GetValueOrDefault(r.EmployeeId, 0)),
            Label = r.Status != "present" ? 1 : 0
        }).ToList();

        _mlService.Train(trainingData);
    }
}
