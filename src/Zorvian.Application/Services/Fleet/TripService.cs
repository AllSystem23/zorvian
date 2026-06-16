using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Services.Fleet;

public sealed class TripService
{
    private readonly ITripRepository _repo;
    private readonly IMapper _mapper;

    public TripService(ITripRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<List<TripResponse>> GetAllAsync()
    {
        var trips = await _repo.GetAllAsync();
        return _mapper.Map<List<TripResponse>>(trips);
    }

    public async Task<TripResponse?> GetByIdAsync(Guid id)
    {
        var trip = await _repo.GetByIdAsync(id);
        return trip is null ? null : _mapper.Map<TripResponse>(trip);
    }

    public async Task<TripResponse> CreateAsync(CreateTripRequest request)
    {
        var trip = _mapper.Map<Trip>(request);
        trip.Status = "Planned";
        await _repo.AddAsync(trip);
        await _repo.SaveChangesAsync();
        return _mapper.Map<TripResponse>(trip);
    }

    public async Task<TripResponse?> UpdateAsync(Guid id, UpdateTripRequest request)
    {
        var trip = await _repo.GetByIdAsync(id);
        if (trip is null) return null;
        _mapper.Map(request, trip);
        await _repo.SaveChangesAsync();
        return _mapper.Map<TripResponse>(trip);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var trip = await _repo.GetByIdAsync(id);
        if (trip is null) return false;
        await _repo.DeleteAsync(trip);
        await _repo.SaveChangesAsync();
        return true;
    }
}
