using Zorvian.Application.DTOs.Warranty;

namespace Zorvian.Application.Interfaces;

public interface IWarrantyCostService
{
    Task<List<WarrantyCostResponse>> GetByWarrantyIdAsync(Guid warrantyId);
    Task<List<WarrantyCostResponse>> GetByClaimIdAsync(Guid claimId);
    Task<WarrantyCostResponse?> GetByIdAsync(Guid id);
    Task<WarrantyCostResponse> CreateAsync(CreateWarrantyCostRequest request);
    Task<WarrantyCostResponse?> UpdateAsync(Guid id, UpdateWarrantyCostRequest request);
    Task<bool> DeleteAsync(Guid id);
}
