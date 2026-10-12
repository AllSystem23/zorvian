using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Core.Interfaces;
namespace Zorvian.Application.Services.Fleet;

public sealed class DeliveryService
{
    private readonly IDeliveryRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public DeliveryService(IDeliveryRepository repo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<List<DeliveryResponse>> GetAllAsync(Guid? branchId = null)
    {
        var deliveries = await _repo.GetAllAsync(branchId ?? _tenant.ResolveBranchId());
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
