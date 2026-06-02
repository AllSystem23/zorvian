using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Web.Jobs;

public sealed class CheckInReminderJob
{
    private readonly IServiceScopeFactory _scopeFactory;

    public CheckInReminderJob(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task RunAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZorvianDbContext>();
        var notification = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var checkedInEmployeeIds = await db.AttendanceRecords
            .Where(a => a.Date == today)
            .Select(a => a.EmployeeId)
            .Distinct()
            .ToListAsync();

        var pending = await db.Employees
            .Where(e => e.Status == "active" && !checkedInEmployeeIds.Contains(e.Id))
            .Join(db.Users.Where(u => u.EmployeeId != null),
                e => e.Id,
                u => u.EmployeeId!.Value,
                (e, u) => new { e.TenantId, UserId = u.Id })
            .ToListAsync();

        foreach (var item in pending)
        {
            await notification.NotifyUserAsync(
                item.TenantId,
                item.UserId.ToString(),
                "Recordatorio de entrada",
                "No has registrado tu entrada hoy. Por favor, registra tu asistencia.",
                "attendance",
                null);
        }
    }
}
