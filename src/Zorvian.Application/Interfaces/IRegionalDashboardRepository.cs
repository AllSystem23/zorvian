using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IRegionalDashboardRepository
{
    Task<IEnumerable<RegionalTaxConfiguration>> GetActiveTaxesAsync(Guid companyId, string countryCode);
    Task<decimal> GetPayrollTotalAsync(Guid companyId, string countryCode);
}
