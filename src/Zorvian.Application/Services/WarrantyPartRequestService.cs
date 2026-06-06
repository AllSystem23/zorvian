using AutoMapper;
using Zorvian.Application.DTOs.Inventory;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class WarrantyPartRequestService
{
    private readonly IWarrantyPartRequestRepository _repo;
    private readonly IInventoryMovementService _inventoryService;
    private readonly IMapper _mapper;

    public WarrantyPartRequestService(IWarrantyPartRequestRepository repo, IInventoryMovementService inventoryService, IMapper mapper)
    {
        _repo = repo;
        _inventoryService = inventoryService;
        _mapper = mapper;
    }

    public async Task<List<WarrantyPartRequestResponse>> GetByClaimIdAsync(Guid claimId)
    {
        var requests = await _repo.GetByClaimIdAsync(claimId);
        return _mapper.Map<List<WarrantyPartRequestResponse>>(requests);
    }

    public async Task<List<WarrantyPartRequestResponse>> GetByProviderIdAsync(Guid providerId)
    {
        var requests = await _repo.GetByProviderIdAsync(providerId);
        return _mapper.Map<List<WarrantyPartRequestResponse>>(requests);
    }

    public async Task<WarrantyPartRequestResponse?> GetByIdAsync(Guid id)
    {
        var request = await _repo.GetByIdAsync(id);
        return request is null ? null : _mapper.Map<WarrantyPartRequestResponse>(request);
    }

    public async Task<WarrantyPartRequestResponse> CreateAsync(CreateWarrantyPartRequestRequest request)
    {
        var entity = _mapper.Map<WarrantyPartRequest>(request);
        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        return _mapper.Map<WarrantyPartRequestResponse>(entity);
    }

    public async Task<WarrantyPartRequestResponse?> UpdateStatusAsync(Guid id, UpdateWarrantyPartRequestStatusRequest request)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;

        if (request.Status == "received" && entity.Status != "received")
        {
            entity.ReceivedAt = DateTime.UtcNow;

            // We need BranchId. If not in entity, maybe in Warranty?
            // Assuming Warranty is loaded.
            await _inventoryService.CreateAsync(new CreateInventoryMovementRequest(
                entity.ProductId,
                "entry",
                entity.QuantityRequested,
                entity.UnitPrice ?? 0,
                entity.RequestNumber,
                $"Recepcion de repuesto por garantia {entity.WarrantyId}",
                entity.Warranty?.BranchId ?? Guid.Empty 
            ));
        }

        if (request.ProviderAuthorizationCode is not null)
            entity.ProviderAuthorizationCode = request.ProviderAuthorizationCode;

        entity.Status = request.Status;
        await _repo.SaveChangesAsync();
        return _mapper.Map<WarrantyPartRequestResponse>(entity);
    }
}
