using Microsoft.EntityFrameworkCore;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories.Fleet;

public sealed class VehicleRepository : IVehicleRepository
{
    private readonly ZorvianDbContext _db;

    public VehicleRepository(ZorvianDbContext db) => _db = db;

    /// <summary>
    /// Ultra-optimized: all fleet dashboard counts in a single raw SQL round-trip.
    /// Replaces 6 GetAllAsync calls that loaded entire tables into memory.
    /// Supports SuperAdmin bypass and specific tenant filtering.
    /// </summary>
    public async Task<FleetDashboardScalars> GetDashboardScalarsRawAsync(string tenantId, bool isSuperAdmin)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thirtyDaysFromNow = today.AddDays(30);

        var sql = @"
            SELECT
                (SELECT COUNT(*) FROM ""Vehicles"" v 
                 WHERE (v.""TenantId"" = @tenantId OR @isSuperAdmin = true) AND v.""IsDeleted"" = false
                ) AS ""TotalVehicles"",

                (SELECT COUNT(*) FROM ""Vehicles"" v 
                 WHERE (v.""TenantId"" = @tenantId OR @isSuperAdmin = true) AND v.""IsDeleted"" = false 
                   AND v.""Status"" = 'Active'
                ) AS ""ActiveVehicles"",

                (SELECT COUNT(*) FROM ""Vehicles"" v 
                 WHERE (v.""TenantId"" = @tenantId OR @isSuperAdmin = true) AND v.""IsDeleted"" = false 
                   AND v.""Status"" = 'Maintenance'
                ) AS ""InMaintenance"",

                (SELECT COUNT(*) FROM ""Drivers"" d 
                 WHERE (d.""TenantId"" = @tenantId OR @isSuperAdmin = true) AND d.""IsDeleted"" = false 
                   AND d.""Status"" = 'Available'
                ) AS ""AvailableDrivers"",

                (SELECT COUNT(*) FROM ""Routes"" r 
                 WHERE (r.""TenantId"" = @tenantId OR @isSuperAdmin = true) AND r.""IsDeleted"" = false 
                   AND (r.""Status"" = 'InProgress' OR r.""Status"" = 'Planned')
                ) AS ""ActiveRoutes"",

                (SELECT COUNT(*) FROM ""Deliveries"" d 
                 WHERE (d.""TenantId"" = @tenantId OR @isSuperAdmin = true) AND d.""IsDeleted"" = false 
                   AND (d.""Status"" = 'Pending' OR d.""Status"" = 'InRoute')
                ) AS ""PendingDeliveries"",

                (SELECT COUNT(*) FROM ""Trips"" t 
                 WHERE (t.""TenantId"" = @tenantId OR @isSuperAdmin = true) AND t.""IsDeleted"" = false 
                   AND t.""StartDateTime""::date = @today
                ) AS ""TripsToday"",

                (SELECT COUNT(*) FROM ""FleetDocuments"" fd 
                 WHERE (fd.""TenantId"" = @tenantId OR @isSuperAdmin = true) AND fd.""IsDeleted"" = false 
                   AND fd.""ExpiryDate"" IS NOT NULL AND fd.""ExpiryDate""::date <= @today 
                   AND fd.""Status"" = 'Valid'
                ) AS ""ExpiringDocuments"",

                (SELECT COUNT(*) FROM ""FleetDocuments"" fd 
                 WHERE (fd.""TenantId"" = @tenantId OR @isSuperAdmin = true) AND fd.""IsDeleted"" = false 
                   AND fd.""ExpiryDate"" IS NOT NULL AND fd.""ExpiryDate""::date <= @thirtyDaysFromNow 
                   AND fd.""Status"" = 'Valid'
                ) AS ""ExpiringSoon""
        ";

        var result = await _db.Database
            .SqlQueryRaw<FleetDashboardScalars>(sql,
                new Npgsql.NpgsqlParameter("@tenantId", tenantId),
                new Npgsql.NpgsqlParameter("@isSuperAdmin", isSuperAdmin),
                new Npgsql.NpgsqlParameter("@today", today),
                new Npgsql.NpgsqlParameter("@thirtyDaysFromNow", thirtyDaysFromNow))
            .FirstOrDefaultAsync();

        return result ?? new FleetDashboardScalars();
    }

    public async Task<List<Vehicle>> GetAllAsync(Guid companyId) =>
        await _db.Set<Vehicle>()
            .Include(v => v.Brand)
            .Include(v => v.VehicleType)
            .Include(v => v.FuelType)
            .Include(v => v.Branch)
            .Include(v => v.Driver)
            .OrderBy(v => v.Code)
            .ToListAsync();

    public async Task<Vehicle?> GetByIdAsync(Guid id) =>
        await _db.Set<Vehicle>()
            .Include(v => v.Brand)
            .Include(v => v.VehicleType)
            .Include(v => v.FuelType)
            .Include(v => v.Branch)
            .Include(v => v.Driver)
            .FirstOrDefaultAsync(v => v.Id == id);

    public async Task AddAsync(Vehicle vehicle) =>
        await _db.Set<Vehicle>().AddAsync(vehicle);

    public Task UpdateAsync(Vehicle vehicle)
    {
        _db.Set<Vehicle>().Update(vehicle);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Vehicle vehicle)
    {
        _db.Set<Vehicle>().Remove(vehicle);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
