using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Services.Fleet;

public sealed class RouteService
{
    private readonly IRouteRepository _repo;
    private readonly IMapper _mapper;

    public RouteService(IRouteRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<List<RouteResponse>> GetAllAsync()
    {
        var routes = await _repo.GetAllAsync();
        return _mapper.Map<List<RouteResponse>>(routes);
    }

    public async Task<RouteResponse?> GetByIdAsync(Guid id)
    {
        var route = await _repo.GetByIdAsync(id);
        return route is null ? null : _mapper.Map<RouteResponse>(route);
    }

    public async Task<RouteResponse> CreateAsync(CreateRouteRequest request)
    {
        var route = _mapper.Map<Route>(request);
        route.Status = "Planned";
        await _repo.AddAsync(route);
        await _repo.SaveChangesAsync();
        return _mapper.Map<RouteResponse>(route);
    }

    public async Task<RouteResponse?> UpdateAsync(Guid id, UpdateRouteRequest request)
    {
        var route = await _repo.GetByIdAsync(id);
        if (route is null) return null;
        _mapper.Map(request, route);
        await _repo.SaveChangesAsync();
        return _mapper.Map<RouteResponse>(route);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var route = await _repo.GetByIdAsync(id);
        if (route is null) return false;
        await _repo.DeleteAsync(route);
        await _repo.SaveChangesAsync();
        return true;
    }
}
