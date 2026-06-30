using Zorvian.Application.Config;
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
    private readonly IDocumentStorageService _storage;
    private readonly IRegionalTaxConfigurationRepository _regionalTaxRepo;

    public CompanyService(ICompanyRepository repo, ITenantContext tenant, IFiscalService fiscalService, IDocumentStorageService storage, IRegionalTaxConfigurationRepository regionalTaxRepo)
    {
        _repo = repo;
        _tenant = tenant;
        _fiscalService = fiscalService;
        _storage = storage;
        _regionalTaxRepo = regionalTaxRepo;
    }

    public async Task<List<CompanyListItemResponse>> GetAllAsync()
    {
        var companies = await _repo.GetAllAsync();
        return companies.Select(c => new CompanyListItemResponse(
            c.Id,
            c.TenantId,
            c.Name,
            c.LegalName,
            c.TaxId,
            c.LogoUrl,
            c.Email,
            c.Phone,
            c.Country,
            c.Currency,
            c.Timezone,
            c.IsActive,
            c.SubscriptionPlan,
            c.MaxEmployees,
            c.CreatedAt
        )).ToList();
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
            Email = request.Email,
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

        var countryCode = MapCountryToCode(request.Country);
        await _fiscalService.SetupDefaultTaxesAsync(company.Id, countryCode);
        await SeedRegionalTaxesAsync(company.Id, countryCode);

        return new CompanyResponse(
            company.Id,
            company.Name,
            company.LegalName,
            company.TaxId ?? "",
            company.Address,
            company.Phone,
            company.Email,
            company.LogoUrl,
            company.Country,
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

    private static List<(string CountryCode, string TaxType, decimal Rate)> GetRegionalTaxes(string countryCode)
    {
        return countryCode switch
        {
            "NIC" => [
                ("NIC", "IVA", 0.15m),
                ("NIC", "IR", 0.02m),
            ],
            "CRI" => [
                ("CR", "IVA", 0.13m),
                ("CR", "IVA Reducido 4%", 0.04m),
                ("CR", "IVA Reducido 2%", 0.02m),
                ("CR", "IVA Reducido 1%", 0.01m),
            ],
            "PAN" => [
                ("PA", "ITBMS", 0.07m),
                ("PA", "ITBMS Especial 10%", 0.10m),
                ("PA", "ITBMS Especial 15%", 0.15m),
            ],
            "HND" => [
                ("HN", "ISV", 0.15m),
                ("HN", "ISV", 0.18m),
            ],
            "SLV" => [
                ("SV", "IVA", 0.13m),
            ],
            "GTM" => [
                ("GT", "IVA", 0.12m),
            ],
            _ => [
                ("XX", "IVA Estándar", 0.15m),
            ],
        };
    }

    private async Task SeedRegionalTaxesAsync(Guid companyId, string countryCode)
    {
        var taxes = GetRegionalTaxes(countryCode);
        if (taxes.Count == 0) return;

        var entities = taxes.Select(t => new RegionalTaxConfiguration
        {
            CompanyId = companyId,
            CountryCode = t.CountryCode,
            TaxType = t.TaxType,
            Rate = t.Rate,
            EffectiveDate = DateTime.UtcNow,
            IsActive = true,
        }).ToList();

        foreach (var entity in entities)
            await _regionalTaxRepo.AddAsync(entity);
        await _regionalTaxRepo.SaveChangesAsync();
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
            company.Country,
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
        if (request.Email is not null) company.Email = request.Email;
        if (request.Country is not null) company.Country = request.Country;
        if (request.Currency is not null) company.Currency = request.Currency;
        if (request.Timezone is not null) company.Timezone = request.Timezone;
        if (request.LogoUrl is not null) company.LogoUrl = request.LogoUrl;
        if (request.MaxEmployees.HasValue) company.MaxEmployees = request.MaxEmployees.Value;

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
            company.Country,
            company.Currency,
            company.Timezone,
            company.MaxEmployees
        );
    }

    public async Task<CompanyResponse?> UpdateByIdAsync(Guid id, UpdateCompanyRequest request)
    {
        var company = await _repo.GetByIdAsync(id);
        if (company is null) return null;

        if (request.Name is not null) company.Name = request.Name;
        if (request.LegalName is not null) company.LegalName = request.LegalName;
        if (request.TaxId is not null) company.TaxId = request.TaxId;
        if (request.Phone is not null) company.Phone = request.Phone;
        if (request.Address is not null) company.Address = request.Address;
        if (request.Email is not null) company.Email = request.Email;
        if (request.Country is not null) company.Country = request.Country;
        if (request.Currency is not null) company.Currency = request.Currency;
        if (request.Timezone is not null) company.Timezone = request.Timezone;
        if (request.LogoUrl is not null) company.LogoUrl = request.LogoUrl;
        if (request.MaxEmployees.HasValue) company.MaxEmployees = request.MaxEmployees.Value;
        if (request.IsActive.HasValue) company.IsActive = request.IsActive.Value;
        if (request.SubscriptionPlan is not null)
        {
            var plan = SubscriptionPlanConfig.GetPlan(request.SubscriptionPlan);
            if (plan is not null)
            {
                company.SubscriptionPlan = plan.Id;
                company.MaxEmployees = SubscriptionPlanConfig.ClampMaxEmployees(plan.Id, company.MaxEmployees);
            }
        }

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
            company.Country,
            company.Currency,
            company.Timezone,
            company.MaxEmployees
        );
    }

    public async Task<string?> UploadLogoAsync(Guid companyId, Stream fileStream, string contentType)
    {
        var company = _tenant.IsSuperAdmin
            ? await _repo.GetByIdAsync(companyId)
            : await _repo.GetByTenantIdAsync(_tenant.TenantId);

        if (company is null) return null;

        var ext = contentType switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/webp" => ".webp",
            _ => ".png"
        };

        var path = $"companies/{company.Id}/logo{ext}";

        var newUrl = await _storage.UploadFileAsync(fileStream, path, contentType);

        // Delete old logo if exists
        if (!string.IsNullOrEmpty(company.LogoUrl))
        {
            try
            {
                var oldPath = Helpers.StoragePathHelper.ExtractStoragePath(company.LogoUrl);
                if (!string.IsNullOrEmpty(oldPath))
                    await _storage.DeleteFileAsync(oldPath);
            }
            catch
            {
                // Best effort: old logo deletion failure should not block the new upload
            }
        }

        company.LogoUrl = newUrl;
        await _repo.UpdateAsync(company);
        await _repo.SaveChangesAsync();

        return newUrl;
    }

    public async Task<bool> DeactivateAsync(Guid id)
    {
        var company = await _repo.GetByIdAsync(id);
        if (company is null) return false;

        var activeCount = await _repo.CountActiveAsync();
        if (activeCount <= 1)
            throw new InvalidOperationException("No se puede desactivar la única empresa activa del sistema.");

        company.IsActive = false;
        await _repo.UpdateAsync(company);
        await _repo.SaveChangesAsync();
        return true;
    }

    private static CompanyResponse DefaultCompanyResponse() => new(
        Id: Guid.Empty,
        Name: "",
        LegalName: "",
        TaxId: "",
        Address: null,
        Phone: null,
        Email: null,
        LogoUrl: null,
        Country: "",
        Currency: "",
        Timezone: "",
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
