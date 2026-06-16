using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories.Fleet;

public sealed class GpsPositionRepository : IGpsPositionRepository
{
    private readonly ZorvianDbContext _db;

    public GpsPositionRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<GpsPosition>> GetAllByVehicleAsync(Guid vehicleId) =>
        await _db.Set<GpsPosition>()
            .Include(g => g.Vehicle)
            .Where(g => g.VehicleId == vehicleId)
            .OrderByDescending(g => g.GpsTimestamp)
            .ToListAsync();

    public async Task<List<GpsPosition>> GetAllByCompanyAsync(Guid companyId) =>
        await _db.Set<GpsPosition>()
            .Include(g => g.Vehicle)
            .ThenInclude(v => v.Branch)
            .Where(g => g.Vehicle.Branch.CompanyId == companyId)
            .OrderByDescending(g => g.GpsTimestamp)
            .ToListAsync();

    public async Task<GpsPosition?> GetLatestByVehicleAsync(Guid vehicleId) =>
        await _db.Set<GpsPosition>()
            .Include(g => g.Vehicle)
            .Where(g => g.VehicleId == vehicleId)
            .OrderByDescending(g => g.GpsTimestamp)
            .FirstOrDefaultAsync();

    public async Task<List<GpsPosition>> GetByVehicleAndDateRangeAsync(Guid vehicleId, DateTime from, DateTime to) =>
        await _db.Set<GpsPosition>()
            .Include(g => g.Vehicle)
            .Where(g => g.VehicleId == vehicleId && g.GpsTimestamp >= from && g.GpsTimestamp <= to)
            .OrderBy(g => g.GpsTimestamp)
            .ToListAsync();

    public async Task<List<GpsPosition>> GetLatestPerVehicleAsync(Guid companyId) =>
        await _db.Set<GpsPosition>()
            .Include(g => g.Vehicle)
            .ThenInclude(v => v.Brand)
            .Include(g => g.Vehicle)
            .ThenInclude(v => v.Driver)
            .Include(g => g.Vehicle)
            .ThenInclude(v => v.Branch)
            .Where(g => g.Vehicle.Branch.CompanyId == companyId)
            .GroupBy(g => g.VehicleId)
            .Select(g => g.OrderByDescending(x => x.GpsTimestamp).First())
            .ToListAsync();

    public async Task AddAsync(GpsPosition position) =>
        await _db.Set<GpsPosition>().AddAsync(position);

    public async Task AddRangeAsync(IEnumerable<GpsPosition> positions) =>
        await _db.Set<GpsPosition>().AddRangeAsync(positions);

    public async Task<int> DeleteOlderThanAsync(DateTime cutoff)
    {
        var old = await _db.Set<GpsPosition>()
            .Where(g => g.GpsTimestamp < cutoff)
            .ToListAsync();
        _db.Set<GpsPosition>().RemoveRange(old);
        return old.Count;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
