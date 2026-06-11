using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services;

public sealed class FiscalService : IFiscalService
{
    private readonly ITaxCategoryRepository _taxRepo;

    public FiscalService(ITaxCategoryRepository taxRepo)
    {
        _taxRepo = taxRepo;
    }

    public async Task SetupDefaultTaxesAsync(Guid companyId, string countryCode)
    {
        var existing = await _taxRepo.GetByCompanyIdAsync(companyId);
        if (existing.Any()) return;

        var taxes = countryCode.ToUpper() switch
        {
            "NIC" => new List<TaxCategory>
            {
                new() { Name = "IVA 15%", Rate = 0.15m, CompanyId = companyId, SalesAccountCode = "210101", VatAccountCode = "110301" },
                new() { Name = "Exento", Rate = 0m, CompanyId = companyId, SalesAccountCode = "210101", VatAccountCode = "110301" }
            },
            "CRI" => new List<TaxCategory>
            {
                new() { Name = "IVA 13%", Rate = 0.13m, CompanyId = companyId, SalesAccountCode = "210101", VatAccountCode = "110301" },
                new() { Name = "IVA Reducido 4%", Rate = 0.04m, CompanyId = companyId, SalesAccountCode = "210101", VatAccountCode = "110301" },
                new() { Name = "IVA Reducido 2%", Rate = 0.02m, CompanyId = companyId, SalesAccountCode = "210101", VatAccountCode = "110301" },
                new() { Name = "IVA Reducido 1%", Rate = 0.01m, CompanyId = companyId, SalesAccountCode = "210101", VatAccountCode = "110301" },
                new() { Name = "Exento", Rate = 0m, CompanyId = companyId, SalesAccountCode = "210101", VatAccountCode = "110301" }
            },
            "PAN" => new List<TaxCategory>
            {
                new() { Name = "ITBMS 7%", Rate = 0.07m, CompanyId = companyId, SalesAccountCode = "210101", VatAccountCode = "110301" },
                new() { Name = "ITBMS Especial 10%", Rate = 0.10m, CompanyId = companyId, SalesAccountCode = "210101", VatAccountCode = "110301" },
                new() { Name = "ITBMS Especial 15%", Rate = 0.15m, CompanyId = companyId, SalesAccountCode = "210101", VatAccountCode = "110301" },
                new() { Name = "Exento", Rate = 0m, CompanyId = companyId, SalesAccountCode = "210101", VatAccountCode = "110301" }
            },
            "HND" => new List<TaxCategory>
            {
                new() { Name = "ISV 15%", Rate = 0.15m, CompanyId = companyId, SalesAccountCode = "210101", VatAccountCode = "110301" },
                new() { Name = "ISV 18%", Rate = 0.18m, CompanyId = companyId, SalesAccountCode = "210101", VatAccountCode = "110301" },
                new() { Name = "Exento", Rate = 0m, CompanyId = companyId, SalesAccountCode = "210101", VatAccountCode = "110301" }
            },
            "SLV" => new List<TaxCategory>
            {
                new() { Name = "IVA 13%", Rate = 0.13m, CompanyId = companyId, SalesAccountCode = "210101", VatAccountCode = "110301" },
                new() { Name = "Exento", Rate = 0m, CompanyId = companyId, SalesAccountCode = "210101", VatAccountCode = "110301" }
            },
            "GTM" => new List<TaxCategory>
            {
                new() { Name = "IVA 12%", Rate = 0.12m, CompanyId = companyId, SalesAccountCode = "210101", VatAccountCode = "110301" },
                new() { Name = "Exento", Rate = 0m, CompanyId = companyId, SalesAccountCode = "210101", VatAccountCode = "110301" }
            },
            _ => new List<TaxCategory>
            {
                new() { Name = "IVA Estándar", Rate = 0.15m, CompanyId = companyId, SalesAccountCode = "210101", VatAccountCode = "110301" },
                new() { Name = "Exento", Rate = 0m, CompanyId = companyId, SalesAccountCode = "210101", VatAccountCode = "110301" }
            }
        };

        foreach (var tax in taxes)
        {
            await _taxRepo.AddAsync(tax);
        }

        await _taxRepo.SaveChangesAsync();
    }
}
