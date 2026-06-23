using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
namespace Zorvian.Application.Services.Fleet;

/// <summary>
/// Geofence management service: CRUD operations and containment checks.
/// </summary>
public sealed class GeofenceService
{
    private readonly IGeofenceRepository _repository;
    private readonly IMapper _mapper;

    public GeofenceService(IGeofenceRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<List<GeofenceResponse>> GetAllAsync() =>
        _mapper.Map<List<GeofenceResponse>>(await _repository.GetAllAsync());

    public async Task<GeofenceResponse?> GetByIdAsync(Guid id) =>
        _mapper.Map<GeofenceResponse?>(await _repository.GetByIdAsync(id));

    public async Task<List<GeofenceResponse>> GetActiveAsync() =>
        _mapper.Map<List<GeofenceResponse>>(await _repository.GetActiveAsync());

    public async Task<GeofenceResponse> CreateAsync(CreateGeofenceRequest request)
    {
        var geofence = new Geofence
        {
            Name = request.Name,
            Type = request.Type,
            CoordinatesJson = request.CoordinatesJson,
            Radius = request.Radius,
            Active = true
        };

        await _repository.AddAsync(geofence);
        await _repository.SaveChangesAsync();
        return _mapper.Map<GeofenceResponse>(geofence);
    }

    public async Task<GeofenceResponse?> UpdateAsync(Guid id, UpdateGeofenceRequest request)
    {
        var geofence = await _repository.GetByIdAsync(id);
        if (geofence == null) return null;

        if (request.Name != null) geofence.Name = request.Name;
        if (request.Type != null) geofence.Type = request.Type;
        if (request.CoordinatesJson != null) geofence.CoordinatesJson = request.CoordinatesJson;
        if (request.Radius.HasValue) geofence.Radius = request.Radius;
        if (request.Active.HasValue) geofence.Active = request.Active.Value;
        await _repository.UpdateAsync(geofence);
        await _repository.SaveChangesAsync();
        return _mapper.Map<GeofenceResponse>(geofence);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var geofence = await _repository.GetByIdAsync(id);
        if (geofence == null) return false;

        await _repository.DeleteAsync(geofence);
        await _repository.SaveChangesAsync();
        return true;
    }
}
