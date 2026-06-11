using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IPartnerRepository
{
    Task<Partner?> GetByIdAsync(Guid id);
    Task<Partner?> GetByCodeAsync(string code);
    Task<List<Partner>> GetFilteredAsync(string? search, string? status, string? countryCode, string? partnerType, int page, int pageSize);
    Task<int> GetFilteredCountAsync(string? search, string? status, string? countryCode, string? partnerType);
    Task AddAsync(Partner partner);
    Task UpdateAsync(Partner partner);
    Task SaveChangesAsync();
    Task<bool> ExistsByTaxIdAsync(string taxId);
    Task<List<Partner>> GetActiveByCountryAsync(string countryCode);
}
