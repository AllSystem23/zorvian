using Microsoft.EntityFrameworkCore;
using Nexora.Application.DTOs.ML;
using Nexora.Infrastructure.Data;
using Nexora.Infrastructure.Services;

namespace Nexora.Web.Jobs;

public sealed class AbsenteeismTrainingJob
{
    private readonly NexoraDbContext _db;
    private readonly AbsenteeismPredictionService _mlService;

    public AbsenteeismTrainingJob(NexoraDbContext db, AbsenteeismPredictionService mlService)
    {
        _db = db;
        _mlService = mlService;
    }

    public async Task RunAsync()
    {
        // 1. Fetch historical data (e.g., last 6 months)
        var sixMonthsAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6));
        var records = await _db.AttendanceRecords
            .Include(r => r.Employee)
            .Where(r => r.Date >= sixMonthsAgo)
            .ToListAsync();

        // 2. Transform into ML data
        var trainingData = records.Select(r => new AttendanceData
        {
            DayOfWeek = (float)r.Date.DayOfWeek,
            Month = (float)r.Date.Month,
            IsHoliday = 0, // Simplified: needs integration with a HolidayService
            PreviousAbsenceCount = (float)_db.AttendanceRecords.Count(ar => ar.EmployeeId == r.EmployeeId && ar.Date < r.Date && ar.Status != "present"),
            Label = r.Status != "present" ? 1 : 0
        }).ToList();

        // 3. Train
        _mlService.Train(trainingData);
    }
}
