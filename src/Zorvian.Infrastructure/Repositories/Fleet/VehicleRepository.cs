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
            WITH vehicles AS (
                SELECT ""Status""
                FROM ""Vehicles""
                WHERE (""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false
            ),
            drivers AS (
                SELECT ""Status""
                FROM ""Drivers""
                WHERE (""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false
            ),
            routes AS (
                SELECT ""Status""
                FROM ""Routes""
                WHERE (""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false
            ),
            deliveries AS (
                SELECT ""Status""
                FROM ""Deliveries""
                WHERE (""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false
            ),
            trips AS (
                SELECT ""StartDateTime""
                FROM ""Trips""
                WHERE (""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false
            ),
            documents AS (
                SELECT ""ExpiryDate"", ""Status""
                FROM ""FleetDocuments""
                WHERE (""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false
            )
            SELECT
                (SELECT COUNT(*) FROM vehicles)::int AS ""TotalVehicles"",
                (SELECT COUNT(*) FROM vehicles WHERE ""Status"" = 'Active')::int AS ""ActiveVehicles"",
                (SELECT COUNT(*) FROM vehicles WHERE ""Status"" = 'Maintenance')::int AS ""InMaintenance"",
                (SELECT COUNT(*) FROM drivers WHERE ""Status"" = 'Available')::int AS ""AvailableDrivers"",
                (SELECT COUNT(*) FROM routes WHERE ""Status"" = 'InProgress' OR ""Status"" = 'Planned')::int AS ""ActiveRoutes"",
                (SELECT COUNT(*) FROM deliveries WHERE ""Status"" = 'Pending' OR ""Status"" = 'InRoute')::int AS ""PendingDeliveries"",
                (SELECT COUNT(*) FROM trips WHERE ""StartDateTime""::date = @today)::int AS ""TripsToday"",
                (SELECT COUNT(*) FROM documents WHERE ""ExpiryDate"" IS NOT NULL AND ""ExpiryDate""::date <= @today AND ""Status"" = 'Valid')::int AS ""ExpiringDocuments"",
                (SELECT COUNT(*) FROM documents WHERE ""ExpiryDate"" IS NOT NULL AND ""ExpiryDate""::date <= @thirtyDaysFromNow AND ""Status"" = 'Valid')::int AS ""ExpiringSoon""
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

    public async Task<FleetKpiScalars> GetFleetKpiReportRawAsync(string tenantId, bool isSuperAdmin)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thirtyDaysFromNow = today.AddDays(30);
        var sql = @"
            WITH vehicle_stats AS (
                SELECT
                    (COUNT(*) FILTER (WHERE ""Status"" = 'Active'))::int AS ""ActiveVehicles"",
                    (COUNT(*) FILTER (WHERE ""Status"" = 'Available'))::int AS ""AvailableVehicles"",
                    (COUNT(*) FILTER (WHERE ""Status"" = 'Maintenance'))::int AS ""InMaintenanceVehicles"",
                    (COUNT(*) FILTER (WHERE ""Status"" = 'OutOfService'))::int AS ""OutOfServiceVehicles"",
                    COUNT(*)::int AS ""TotalVehicles""
                FROM ""Vehicles""
                WHERE (""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false
            ),
            delivery_stats AS (
                SELECT
                    COUNT(*)::int AS ""TotalDeliveries"",
                    (COUNT(*) FILTER (WHERE ""Status"" = 'Delivered'))::int AS ""CompletedDeliveries"",
                    (COUNT(*) FILTER (
                        WHERE ""Status"" = 'Delivered'
                          AND ""DeliveredAt"" IS NOT NULL
                          AND ""DeliveredAt"" < ""ScheduledDate""::timestamp + INTERVAL '1 day'
                    ))::int AS ""OnTimeDeliveries""
                FROM ""Deliveries""
                WHERE (""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false
            ),
            document_stats AS (
                SELECT
                    (COUNT(*) FILTER (
                        WHERE ""ExpiryDate"" IS NOT NULL
                          AND ""ExpiryDate""::date <= @thirtyDaysFromNow
                          AND ""Status"" = 'Valid'
                    ))::int AS ""ExpiringDocuments""
                FROM ""FleetDocuments""
                WHERE (""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false
            ),
            work_order_stats AS (
                SELECT
                    (COUNT(*) FILTER (WHERE ""Status"" <> 'Closed' AND ""Status"" <> 'Cancelled'))::int AS ""OpenWorkOrders""
                FROM ""WorkOrders""
                WHERE (""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false
            ),
            fuel_stats AS (
                SELECT
                    COALESCE(SUM(""CurrentKm"") FILTER (WHERE ""ValidForCalculation""), 0) AS ""TotalKm"",
                    COALESCE(SUM(""Liters"") FILTER (WHERE ""ValidForCalculation""), 0) AS ""TotalLiters"",
                    COALESCE(SUM(""TotalCost""), 0) AS ""TotalFuelCost""
                FROM ""FuelRefills""
                WHERE (""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false
            )
            SELECT
                vs.""TotalVehicles"",
                vs.""ActiveVehicles"",
                vs.""AvailableVehicles"",
                vs.""InMaintenanceVehicles"",
                vs.""OutOfServiceVehicles"",
                CASE WHEN vs.""TotalVehicles"" > 0 THEN ROUND((vs.""ActiveVehicles""::decimal / vs.""TotalVehicles"") * 100, 2) ELSE 0 END AS ""FleetAvailabilityRate"",
                CASE WHEN fs.""TotalKm"" > 0 THEN ROUND(fs.""TotalFuelCost"" / fs.""TotalKm"", 2) ELSE 0 END AS ""AverageCostPerKm"",
                CASE WHEN fs.""TotalLiters"" > 0 THEN ROUND(fs.""TotalKm"" / fs.""TotalLiters"", 2) ELSE 0 END AS ""AverageFuelEfficiency"",
                ds.""TotalDeliveries"",
                ds.""CompletedDeliveries"",
                CASE WHEN ds.""TotalDeliveries"" > 0 THEN ROUND((ds.""OnTimeDeliveries""::decimal / ds.""TotalDeliveries"") * 100, 2) ELSE 0 END AS ""OnTimeDeliveryRate"",
                doc.""ExpiringDocuments"",
                vs.""InMaintenanceVehicles"" AS ""OverdueMaintenance"",
                wo.""OpenWorkOrders""
            FROM vehicle_stats vs
            CROSS JOIN delivery_stats ds
            CROSS JOIN document_stats doc
            CROSS JOIN work_order_stats wo
            CROSS JOIN fuel_stats fs
        ";

        var result = await _db.Database
            .SqlQueryRaw<FleetKpiScalars>(sql,
                new Npgsql.NpgsqlParameter("@tenantId", tenantId),
                new Npgsql.NpgsqlParameter("@isSuperAdmin", isSuperAdmin),
                new Npgsql.NpgsqlParameter("@thirtyDaysFromNow", thirtyDaysFromNow))
            .FirstOrDefaultAsync();

        return result ?? new FleetKpiScalars();
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
