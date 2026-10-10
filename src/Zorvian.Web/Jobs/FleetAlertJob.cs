using Zorvian.Application.Interfaces;
using Zorvian.Application.Services.Fleet;
using Zorvian.Core.Interfaces;

namespace Zorvian.Web.Jobs;

/// <summary>
/// Hangfire job that periodically checks fleet conditions and dispatches notifications.
/// Runs every 6 hours (0 */6 * * *) to check for document expiry, license expiry,
/// maintenance overdue, fuel anomalies, and open high-priority work orders.
/// </summary>
public sealed class FleetAlertJob
{
    private readonly FleetAlertService _alertService;
    private readonly IAuthRepository _authRepo;
    private readonly ITenantContextWriter _tenantWriter;

    public FleetAlertJob(FleetAlertService alertService, IAuthRepository authRepo, ITenantContextWriter tenantWriter)
    {
        _alertService = alertService;
        _authRepo = authRepo;
        _tenantWriter = tenantWriter;
    }

    public async Task RunAsync()
    {
        // Sin request HTTP los query filters no ven ninguna compañía → iterar por
        // empresa y fijar el tenant antes de generar/disparar las alertas.
        var companies = await _authRepo.GetAllCompaniesAsync();

        foreach (var company in companies)
        {
            _tenantWriter.SetTenantId(company.TenantId);
            await _alertService.DispatchPendingNotificationsAsync();
        }
    }
}
