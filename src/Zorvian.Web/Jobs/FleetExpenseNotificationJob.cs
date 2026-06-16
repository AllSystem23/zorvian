using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services.Fleet;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Web.Jobs;

/// <summary>
/// Hangfire job that checks for pending fleet expenses across ALL tenants
/// and sends reminder notifications to each company.
/// Runs daily at 08:00 (0 8 * * *) to remind about unapproved expenses.
/// Iterates all active companies using IgnoreQueryFilters to bypass RLS,
/// then sets tenant context per-company to query expenses correctly.
/// </summary>
public sealed class FleetExpenseNotificationJob
{
    private readonly FleetExpenseService _expenseService;
    private readonly INotificationService _notification;
    private readonly ITenantContextWriter _tenantWriter;
    private readonly ZorvianDbContext _db;
    private readonly ILogger<FleetExpenseNotificationJob> _logger;

    public FleetExpenseNotificationJob(
        FleetExpenseService expenseService,
        INotificationService notification,
        ITenantContextWriter tenantWriter,
        ZorvianDbContext db,
        ILogger<FleetExpenseNotificationJob> logger)
    {
        _expenseService = expenseService;
        _notification = notification;
        _tenantWriter = tenantWriter;
        _db = db;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        // Bypass RLS to get all active company tenant IDs
        _tenantWriter.SetIsSuperAdmin(true);
        var companies = await _db.Companies
            .IgnoreQueryFilters()
            .Where(c => !c.IsDeleted)
            .Select(c => new { c.Id, c.TenantId, c.Name })
            .ToListAsync();
        _tenantWriter.SetIsSuperAdmin(false);

        var totalNotified = 0;

        foreach (var company in companies)
        {
            try
            {
                // Set tenant context to this company
                _tenantWriter.SetTenantId(company.TenantId);

                var pendingCount = await _expenseService.GetPendingCountAsync();
                if (pendingCount == 0) continue;

                var pendingAmount = await _expenseService.GetPendingAmountAsync();

                await _notification.NotifyTenantAsync(
                    company.TenantId,
                    $"Hay {pendingCount} gasto(s) de flota pendiente(s) de aprobación",
                    $"Total pendiente: C$ {pendingAmount:N2}. Revisá y aprobá los gastos para mantener la contabilidad al día.",
                    "fleet_expense_pending_reminder",
                    null);

                totalNotified++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check fleet expenses for company {CompanyId} ({TenantId})", company.Id, company.TenantId);
            }
        }

        _logger.LogInformation("FleetExpenseNotificationJob: notified {Count}/{Total} companies", totalNotified, companies.Count);
    }
}
