using Zorvian.Application.Interfaces;

namespace Zorvian.Application.Services;

public sealed class RegionalFinancialDashboardService
{
    private readonly IRegionalDashboardRepository _repository;

    public RegionalFinancialDashboardService(IRegionalDashboardRepository repository)
    {
        _repository = repository;
    }

    public async Task<object> GetRegionalKpisAsync(Guid companyId, string countryCode)
    {
        var taxes = await _repository.GetActiveTaxesAsync(companyId, countryCode);
        var payrollTotal = await _repository.GetPayrollTotalAsync(companyId, countryCode);

        return new
        {
            Country = countryCode,
            ActiveTaxes = taxes.Select(t => new { t.TaxType, t.Rate }),
            PayrollSummary = payrollTotal
        };
    }
}
