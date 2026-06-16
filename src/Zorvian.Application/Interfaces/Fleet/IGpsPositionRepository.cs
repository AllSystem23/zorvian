using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Interfaces.Fleet;

public interface IGpsPositionRepository
{
    Task<List<GpsPosition>> GetAllByVehicleAsync(Guid vehicleId);
    Task<List<GpsPosition>> GetAllByCompanyAsync(Guid companyId);
    Task<GpsPosition?> GetLatestByVehicleAsync(Guid vehicleId);
    Task<List<GpsPosition>> GetByVehicleAndDateRangeAsync(Guid vehicleId, DateTime from, DateTime to);
    Task<List<GpsPosition>> GetLatestPerVehicleAsync(Guid companyId);
    Task AddAsync(GpsPosition position);
    Task AddRangeAsync(IEnumerable<GpsPosition> positions);
    Task<int> DeleteOlderThanAsync(DateTime cutoff);
    Task SaveChangesAsync();
}
