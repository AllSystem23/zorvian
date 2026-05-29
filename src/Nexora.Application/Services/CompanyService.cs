using Nexora.Application.DTOs.Company;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;

namespace Nexora.Application.Services;

public sealed class CompanyService
{
    private readonly ICompanyRepository _repo;
    private readonly ITenantContext _tenant;

    public CompanyService(ICompanyRepository repo, ITenantContext tenant)
    {
        _repo = repo;
        _tenant = tenant;
    }

    public async Task<CompanyResponse> CreateAsync(CreateCompanyRequest request)
    {
        var tenantId = _tenant.TenantId;

        var existing = await _repo.ExistsByTenantIdAsync(tenantId);
        if (existing)
            throw new InvalidOperationException("Company already exists for this tenant");

        var company = new Company
        {
            Name = request.Name,
            LegalName = request.LegalName,
            TaxId = request.TaxId,
            Phone = request.Phone,
            Address = request.Address,
            MaxEmployees = request.MaxEmployees,
            Country = request.Country,
            Currency = request.Currency,
            Timezone = request.Timezone,
        };

        await _repo.AddAsync(company);
        await _repo.SaveChangesAsync();

        var settings = new CompanySettings
        {
            CompanyId = company.Id,
            Timezone = request.Timezone,
            Currency = request.Currency,
        };

        await _repo.AddSettingsAsync(settings);
        await _repo.SaveChangesAsync();

        return new CompanyResponse(
            company.Id,
            company.Name,
            company.LegalName,
            company.TaxId ?? "",
            company.Currency,
            company.Timezone,
            company.MaxEmployees
        );
    }

    public async Task<CompanyResponse?> GetCurrentAsync()
    {
        var company = await _repo.GetByTenantIdAsync(_tenant.TenantId);
        if (company is null) return null;

        return new CompanyResponse(
            company.Id,
            company.Name,
            company.LegalName,
            company.TaxId ?? "",
            company.Currency,
            company.Timezone,
            company.MaxEmployees
        );
    }
}
