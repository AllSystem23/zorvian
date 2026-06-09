using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Web.Jobs;

/// <summary>
/// Periodic health check job that monitors system metrics and writes to a log
/// </summary>
public class HealthCheckJob
{
    private readonly ZorvianDbContext _context;
    private readonly ILogger<HealthCheckJob> _logger;

    public HealthCheckJob(ZorvianDbContext context, ILogger<HealthCheckJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Check database connectivity
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                _logger.LogError("[HEALTH] Database connection failed");
                return;
            }

            // Get metrics
            var totalEmployees = await _context.Set<Core.Entities.Employee>().CountAsync(e => !e.IsDeleted);
            var activeUsers = await _context.Set<Core.Entities.User>().CountAsync();

            stopwatch.Stop();
            _logger.LogInformation(
                "[HEALTH] System OK | Employees={Employees} Users={Users} ResponseTime={Ms}ms",
                totalEmployees, activeUsers, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HEALTH] Health check failed");
        }
    }
}
