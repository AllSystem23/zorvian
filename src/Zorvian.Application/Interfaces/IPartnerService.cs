using Zorvian.Application.DTOs.Partner;

namespace Zorvian.Application.Interfaces;

public interface IPartnerService
{
    Task<PartnerDto> CreateAsync(CreatePartnerRequest request);
    Task<PartnerDto> UpdateAsync(Guid id, UpdatePartnerRequest request);
    Task<PartnerDto?> GetByIdAsync(Guid id);
    Task<List<PartnerDto>> GetFilteredAsync(string? search, string? status, string? countryCode, string? partnerType, int page, int pageSize);
    Task<int> GetFilteredCountAsync(string? search, string? status, string? countryCode, string? partnerType);
    Task<PartnerDto> ActivateAsync(Guid id);
    Task<PartnerDto> DeactivateAsync(Guid id, string reason);
    Task<List<PartnerDto>> GetActiveByCountryAsync(string countryCode);
}
