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

    public async Task<CompanyResponse?> UpdateAsync(UpdateCompanyRequest request)
    {
        var company = await _repo.GetByTenantIdAsync(_tenant.TenantId);
        if (company is null) return null;

        if (request.Name is not null) company.Name = request.Name;
        if (request.LegalName is not null) company.LegalName = request.LegalName;
        if (request.TaxId is not null) company.TaxId = request.TaxId;
        if (request.Phone is not null) company.Phone = request.Phone;
        if (request.Address is not null) company.Address = request.Address;
        if (request.Currency is not null) company.Currency = request.Currency;
        if (request.Timezone is not null) company.Timezone = request.Timezone;
        if (request.LogoUrl is not null) company.LogoUrl = request.LogoUrl;

        await _repo.UpdateAsync(company);
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

    public async Task<CompanySettingsResponse?> GetSettingsAsync()
    {
        var company = await _repo.GetByTenantIdAsync(_tenant.TenantId);
        if (company is null) return null;

        var settings = await _repo.GetSettingsAsync(company.Id);
        if (settings is null) return null;

        return MapSettings(settings);
    }

    public async Task<CompanySettingsResponse?> UpdateSettingsAsync(UpdateCompanySettingsRequest request)
    {
        var company = await _repo.GetByTenantIdAsync(_tenant.TenantId);
        if (company is null) return null;

        var settings = await _repo.GetSettingsAsync(company.Id);
        if (settings is null) return null;

        if (request.VacationDaysPerYear.HasValue) settings.VacationDaysPerYear = request.VacationDaysPerYear.Value;
        if (request.VacationAccrualMethod is not null) settings.VacationAccrualMethod = request.VacationAccrualMethod;
        if (request.LateToleranceMinutes.HasValue) settings.LateToleranceMinutes = request.LateToleranceMinutes.Value;
        if (request.WorkingHoursPerDay.HasValue) settings.WorkingHoursPerDay = request.WorkingHoursPerDay.Value;
        if (request.WorkingDays is not null) settings.WorkingDays = request.WorkingDays;
        if (request.OvertimeEnabled.HasValue) settings.OvertimeEnabled = request.OvertimeEnabled.Value;
        if (request.Timezone is not null) settings.Timezone = request.Timezone;
        if (request.Currency is not null) settings.Currency = request.Currency;
        if (request.DateFormat is not null) settings.DateFormat = request.DateFormat;
        if (request.ApprovalFlowConfig is not null) settings.ApprovalFlowConfig = request.ApprovalFlowConfig;

        await _repo.UpdateSettingsAsync(settings);
        await _repo.SaveChangesAsync();

        return MapSettings(settings);
    }

    private static CompanySettingsResponse MapSettings(CompanySettings s) => new(
        s.VacationDaysPerYear,
        s.VacationAccrualMethod,
        s.LateToleranceMinutes,
        s.WorkingHoursPerDay,
        s.WorkingDays,
        s.OvertimeEnabled,
        s.Timezone,
        s.Currency,
        s.DateFormat,
        s.ApprovalFlowConfig
    );
}
