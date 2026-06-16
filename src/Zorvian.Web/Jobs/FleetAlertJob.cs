using Zorvian.Application.Services.Fleet;

namespace Zorvian.Web.Jobs;

/// <summary>
/// Hangfire job that periodically checks fleet conditions and dispatches notifications.
/// Runs every 6 hours (0 */6 * * *) to check for document expiry, license expiry,
/// maintenance overdue, fuel anomalies, and open high-priority work orders.
/// </summary>
public sealed class FleetAlertJob
{
    private readonly FleetAlertService _alertService;

    public FleetAlertJob(FleetAlertService alertService)
    {
        _alertService = alertService;
    }

    public async Task RunAsync()
    {
        var dispatched = await _alertService.DispatchPendingNotificationsAsync();
        // Log would go here: $"FleetAlertJob: dispatched {dispatched} notifications"
    }
}
