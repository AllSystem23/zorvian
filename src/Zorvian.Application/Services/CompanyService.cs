using Zorvian.Application.DTOs.Company;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class CompanyService
{
    private readonly ICompanyRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IFiscalService _fiscalService;

    public CompanyService(ICompanyRepository repo, ITenantContext tenant, IFiscalService fiscalService)
    {
        _repo = repo;
        _tenant = tenant;
        _fiscalService = fiscalService;
    }

    public async Task<CompanyResponse> CreateAsync(CreateCompanyRequest request)
    {
        var tenantId = _tenant.TenantId;

        var existing = await _repo.ExistsByTenantIdAsync(tenantId);
        if (existing)
            throw new InvalidOperationException("Company already exists for this tenant");

        var company = new Company
        {
            TenantId = tenantId,
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

        // Setup default taxes based on country
        var countryCode = MapCountryToCode(request.Country);
        await _fiscalService.SetupDefaultTaxesAsync(company.Id, countryCode);

        return new CompanyResponse(
            company.Id,
            company.Name,
            company.LegalName,
            company.TaxId ?? "",
            company.Address,
            company.Phone,
            company.Email,
            company.LogoUrl,
            company.Currency,
            company.Timezone,
            company.MaxEmployees
        );
    }

    private static string MapCountryToCode(string country)
    {
        return country.ToLower() switch
        {
            "nicaragua" => "NIC",
            "costa rica" => "CRI",
            "panamá" or "panama" => "PAN",
            "honduras" => "HND",
            "el salvador" => "SLV",
            "guatemala" => "GTM",
            _ => "NIC"
        };
    }

    public async Task<CompanyResponse?> GetCurrentAsync()
    {
        var company = await _repo.GetByTenantIdAsync(_tenant.TenantId);
        if (company is null) return DefaultCompanyResponse();

        return new CompanyResponse(
            company.Id,
            company.Name,
            company.LegalName,
            company.TaxId ?? "",
            company.Address,
            company.Phone,
            company.Email,
            company.LogoUrl,
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
            company.Address,
            company.Phone,
            company.Email,
            company.LogoUrl,
            company.Currency,
            company.Timezone,
            company.MaxEmployees
        );
    }

    private static CompanyResponse DefaultCompanyResponse() => new(
        Id: Guid.Empty,
        Name: "Mi Empresa",
        LegalName: "Mi Empresa S.A.",
        TaxId: "",
        Address: null,
        Phone: null,
        Email: null,
        LogoUrl: null,
        Currency: "NIO",
        Timezone: "America/Managua",
        MaxEmployees: 50
    );

    public async Task<CompanySettingsResponse?> GetSettingsAsync()
    {
        var company = await _repo.GetByTenantIdAsync(_tenant.TenantId);
        if (company is null) return DefaultSettings();

        var settings = await _repo.GetSettingsAsync(company.Id);
        if (settings is null) return DefaultSettings();

        return MapSettings(settings);
    }

    private static CompanySettingsResponse DefaultSettings() => new(
        VacationDaysPerYear: 30,
        VacationAccrualMethod: "monthly",
        LateToleranceMinutes: 10,
        WorkingHoursPerDay: 8m,
        WorkingDays: "Monday,Tuesday,Wednesday,Thursday,Friday",
        OvertimeEnabled: false,
        Timezone: "America/Managua",
        Currency: "NIO",
        DateFormat: null,
        ApprovalFlowConfig: null,
        LateFeeDailyRate: 0.001m,
        LateFeePercentage: 0.05m,
        LateFeeGracePeriod: 0,
        TaxEnabled: true,
        TaxRate: 0.15m
    );

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
        if (request.LateFeeDailyRate.HasValue) settings.LateFeeDailyRate = request.LateFeeDailyRate.Value;
        if (request.LateFeePercentage.HasValue) settings.LateFeePercentage = request.LateFeePercentage.Value;
        if (request.LateFeeGracePeriod.HasValue) settings.LateFeeGracePeriod = request.LateFeeGracePeriod.Value;
        if (request.TaxEnabled.HasValue) settings.TaxEnabled = request.TaxEnabled.Value;
        if (request.TaxRate.HasValue) settings.TaxRate = request.TaxRate.Value;

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
        s.ApprovalFlowConfig,
        s.LateFeeDailyRate,
        s.LateFeePercentage,
        s.LateFeeGracePeriod,
        s.TaxEnabled,
        s.TaxRate
    );
}
