using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
namespace Zorvian.Application.Services.Fleet;

public sealed class DeliveryService
{
    private readonly IDeliveryRepository _repo;
    private readonly IMapper _mapper;

    public DeliveryService(IDeliveryRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<List<DeliveryResponse>> GetAllAsync()
    {
        var deliveries = await _repo.GetAllAsync();
        return _mapper.Map<List<DeliveryResponse>>(deliveries);
    }

    public async Task<DeliveryResponse?> GetByIdAsync(Guid id)
    {
        var delivery = await _repo.GetByIdAsync(id);
        return delivery is null ? null : _mapper.Map<DeliveryResponse>(delivery);
    }

    public async Task<DeliveryResponse> CreateAsync(CreateDeliveryRequest request)
    {
        var delivery = _mapper.Map<Delivery>(request);
        delivery.Status = "Pending";
        await _repo.AddAsync(delivery);
        await _repo.SaveChangesAsync();
        return _mapper.Map<DeliveryResponse>(delivery);
    }

    public async Task<DeliveryResponse?> UpdateAsync(Guid id, UpdateDeliveryRequest request)
    {
        var delivery = await _repo.GetByIdAsync(id);
        if (delivery is null) return null;
        _mapper.Map(request, delivery);
        await _repo.SaveChangesAsync();
        return _mapper.Map<DeliveryResponse>(delivery);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var delivery = await _repo.GetByIdAsync(id);
        if (delivery is null) return false;
        await _repo.DeleteAsync(delivery);
        await _repo.SaveChangesAsync();
        return true;
    }
}
