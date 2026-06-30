using Moq;
using Zorvian.Application.DTOs.Company;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

public sealed class CompanyServiceTests
{
    private readonly Mock<ICompanyRepository> _repo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IFiscalService> _fiscal = new();
    private readonly Mock<IDocumentStorageService> _storage = new();
    private readonly Mock<IRegionalTaxConfigurationRepository> _regionalTaxRepo = new();
    private readonly CompanyService _sut;
    private readonly string _tenantId = Guid.NewGuid().ToString();

    public CompanyServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(_tenantId);
        _sut = new CompanyService(_repo.Object, _tenant.Object, _fiscal.Object, _storage.Object, _regionalTaxRepo.Object);
    }

    [Fact]
    public async Task CreateAsync_CreatesCompany_WithDefaults()
    {
        _repo.Setup(r => r.ExistsByTenantIdAsync(_tenantId)).ReturnsAsync(false);

        var request = new CreateCompanyRequest(
            Name: "Mi Empresa",
            LegalName: "Mi Empresa S.A.",
            TaxId: "J123456789",
            Phone: "+50588888888",
            Address: "Managua, Nicaragua",
            Email: null,
            Country: "Nicaragua",
            Currency: "NIO",
            Timezone: "America/Managua"
        );

        var result = await _sut.CreateAsync(request);

        Assert.Equal("Mi Empresa", result.Name);
        Assert.Equal("NIO", result.Currency);
        Assert.Equal("America/Managua", result.Timezone);
        Assert.Equal(50, result.MaxEmployees);
        _repo.Verify(r => r.AddAsync(It.IsAny<Core.Entities.Company>()), Times.Once);
        _repo.Verify(r => r.AddSettingsAsync(It.IsAny<Core.Entities.CompanySettings>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenCompanyExists()
    {
        _repo.Setup(r => r.ExistsByTenantIdAsync(_tenantId)).ReturnsAsync(true);

        var request = new CreateCompanyRequest(
            Name: "Otra",
            LegalName: "Otra S.A.",
            TaxId: null,
            Phone: null,
            Address: null,
            Email: null,
            Country: "Nicaragua",
            Currency: "NIO",
            Timezone: "America/Managua"
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesCompanyFields()
    {
        var company = new Core.Entities.Company
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Original",
            LegalName = "Original S.A.",
            TaxId = "J123",
            Currency = "NIO",
            Timezone = "America/Managua",
            MaxEmployees = 50,
        };
        _repo.Setup(r => r.GetByTenantIdAsync(_tenantId)).ReturnsAsync(company);

        var request = new UpdateCompanyRequest(
            Name: "Nuevo Nombre",
            LegalName: null,
            TaxId: "J999",
            Phone: null,
            Address: null,
            Email: null,
            Country: "Nicaragua",
            Currency: "USD",
            Timezone: null,
            LogoUrl: null,
            MaxEmployees: null,
            IsActive: null
        );

        var result = await _sut.UpdateAsync(request);

        Assert.NotNull(result);
        Assert.Equal("Nuevo Nombre", result.Name);
        Assert.Equal("USD", result.Currency);
        _repo.Verify(r => r.UpdateAsync(It.IsAny<Core.Entities.Company>()), Times.Once);
    }

    [Fact]
    public async Task GetSettingsAsync_ReturnsSettings_WhenCompanyExists()
    {
        var companyId = Guid.NewGuid();
        var company = new Core.Entities.Company { Id = companyId, TenantId = "tenant-123", Name = "Test" };
        var settings = new Core.Entities.CompanySettings
        {
            CompanyId = companyId,
            VacationDaysPerYear = 20,
            LateToleranceMinutes = 10,
            WorkingHoursPerDay = 9,
            WorkingDays = "MON,TUE,WED,THU,FRI",
            OvertimeEnabled = true,
            Currency = "USD",
            Timezone = "America/Managua",
            ApprovalFlowConfig = "[{\"step\":1,\"role\":\"Supervisor\"}]",
        };

        _repo.Setup(r => r.GetByTenantIdAsync(_tenantId)).ReturnsAsync(company);
        _repo.Setup(r => r.GetSettingsAsync(companyId)).ReturnsAsync(settings);

        var result = await _sut.GetSettingsAsync();

        Assert.NotNull(result);
        Assert.Equal(20, result.VacationDaysPerYear);
        Assert.Equal(10, result.LateToleranceMinutes);
        Assert.Equal("[{\"step\":1,\"role\":\"Supervisor\"}]", result.ApprovalFlowConfig);
    }

    [Fact]
    public async Task UpdateSettingsAsync_UpdatesApprovalFlow()
    {
        var companyId = Guid.NewGuid();
        var company = new Core.Entities.Company { Id = companyId, TenantId = "tenant-123", Name = "Test" };
        var settings = new Core.Entities.CompanySettings
        {
            CompanyId = companyId,
            VacationDaysPerYear = 15,
            LateToleranceMinutes = 15,
            WorkingHoursPerDay = 8,
            WorkingDays = "MON,TUE,WED,THU,FRI",
        };

        _repo.Setup(r => r.GetByTenantIdAsync(_tenantId)).ReturnsAsync(company);
        _repo.Setup(r => r.GetSettingsAsync(companyId)).ReturnsAsync(settings);

        var request = new UpdateCompanySettingsRequest(
            VacationDaysPerYear: 18,
            VacationAccrualMethod: null,
            LateToleranceMinutes: 20,
            WorkingHoursPerDay: null,
            WorkingDays: null,
            OvertimeEnabled: true,
            Timezone: null,
            Currency: null,
            DateFormat: null,
            ApprovalFlowConfig: "[{\"step\":1,\"role\":\"Rrhh\"}]",
            LateFeeDailyRate: 0.05m,
            LateFeePercentage: 5.0m,
            LateFeeGracePeriod: 3,
            TaxEnabled: true,
            TaxRate: 15.0m
        );

        var result = await _sut.UpdateSettingsAsync(request);

        Assert.NotNull(result);
        Assert.Equal(18, result.VacationDaysPerYear);
        Assert.Equal(20, result.LateToleranceMinutes);
        Assert.True(result.OvertimeEnabled);
        Assert.Equal("[{\"step\":1,\"role\":\"Rrhh\"}]", result.ApprovalFlowConfig);
        _repo.Verify(r => r.UpdateSettingsAsync(It.IsAny<Core.Entities.CompanySettings>()), Times.Once);
    }

    // ── SeedRegionalTaxesAsync tests ──

    [Fact]
    public async Task CreateAsync_SeedsRegionalTaxesForNicaragua()
    {
        _repo.Setup(r => r.ExistsByTenantIdAsync(_tenantId)).ReturnsAsync(false);

        var request = new CreateCompanyRequest(
            Name: "Empresa NIC",
            LegalName: "Empresa NIC S.A.",
            TaxId: null, Phone: null, Address: null, Email: null,
            Country: "Nicaragua", Currency: "NIO", Timezone: "America/Managua"
        );

        await _sut.CreateAsync(request);

        _regionalTaxRepo.Verify(r => r.AddAsync(It.Is<RegionalTaxConfiguration>(x =>
            x.CountryCode == "NIC" && x.TaxType == "IVA" && x.Rate == 0.15m)), Times.Once);
        _regionalTaxRepo.Verify(r => r.AddAsync(It.Is<RegionalTaxConfiguration>(x =>
            x.CountryCode == "NIC" && x.TaxType == "IR" && x.Rate == 0.02m)), Times.Once);
        _regionalTaxRepo.Verify(r => r.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CreateAsync_SeedsRegionalTaxesForCostaRica()
    {
        _repo.Setup(r => r.ExistsByTenantIdAsync(_tenantId)).ReturnsAsync(false);

        var request = new CreateCompanyRequest(
            Name: "Empresa CR",
            LegalName: "Empresa CR S.A.",
            TaxId: null, Phone: null, Address: null, Email: null,
            Country: "Costa Rica", Currency: "CRC", Timezone: "America/Costa_Rica"
        );

        await _sut.CreateAsync(request);

        _regionalTaxRepo.Verify(r => r.AddAsync(It.Is<RegionalTaxConfiguration>(x =>
            x.CountryCode == "CR" && x.TaxType == "IVA" && x.Rate == 0.13m)), Times.Once);
        _regionalTaxRepo.Verify(r => r.AddAsync(It.Is<RegionalTaxConfiguration>(x =>
            x.CountryCode == "CR" && x.TaxType == "IVA Reducido 4%")), Times.Once);
        _regionalTaxRepo.Verify(r => r.AddAsync(It.Is<RegionalTaxConfiguration>(x =>
            x.CountryCode == "CR" && x.TaxType == "IVA Reducido 2%")), Times.Once);
        _regionalTaxRepo.Verify(r => r.AddAsync(It.Is<RegionalTaxConfiguration>(x =>
            x.CountryCode == "CR" && x.TaxType == "IVA Reducido 1%")), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SeedsRegionalTaxesForPanama()
    {
        _repo.Setup(r => r.ExistsByTenantIdAsync(_tenantId)).ReturnsAsync(false);

        var request = new CreateCompanyRequest(
            Name: "Empresa PA",
            LegalName: "Empresa PA S.A.",
            TaxId: null, Phone: null, Address: null, Email: null,
            Country: "Panamá", Currency: "USD", Timezone: "America/Panama"
        );

        await _sut.CreateAsync(request);

        _regionalTaxRepo.Verify(r => r.AddAsync(It.Is<RegionalTaxConfiguration>(x =>
            x.CountryCode == "PA" && x.TaxType == "ITBMS" && x.Rate == 0.07m)), Times.Once);
        _regionalTaxRepo.Verify(r => r.AddAsync(It.Is<RegionalTaxConfiguration>(x =>
            x.CountryCode == "PA" && x.TaxType == "ITBMS Especial 10%")), Times.Once);
        _regionalTaxRepo.Verify(r => r.AddAsync(It.Is<RegionalTaxConfiguration>(x =>
            x.CountryCode == "PA" && x.TaxType == "ITBMS Especial 15%")), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SeedsRegionalTaxesForHonduras()
    {
        _repo.Setup(r => r.ExistsByTenantIdAsync(_tenantId)).ReturnsAsync(false);

        var request = new CreateCompanyRequest(
            Name: "Empresa HN",
            LegalName: "Empresa HN S.A.",
            TaxId: null, Phone: null, Address: null, Email: null,
            Country: "Honduras", Currency: "HNL", Timezone: "America/Tegucigalpa"
        );

        await _sut.CreateAsync(request);

        _regionalTaxRepo.Verify(r => r.AddAsync(It.Is<RegionalTaxConfiguration>(x =>
            x.CountryCode == "HN" && x.TaxType == "ISV" && x.Rate == 0.15m)), Times.Once);
        _regionalTaxRepo.Verify(r => r.AddAsync(It.Is<RegionalTaxConfiguration>(x =>
            x.CountryCode == "HN" && x.TaxType == "ISV" && x.Rate == 0.18m)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SeedsRegionalTaxesForElSalvador()
    {
        _repo.Setup(r => r.ExistsByTenantIdAsync(_tenantId)).ReturnsAsync(false);

        var request = new CreateCompanyRequest(
            Name: "Empresa SV",
            LegalName: "Empresa SV S.A.",
            TaxId: null, Phone: null, Address: null, Email: null,
            Country: "El Salvador", Currency: "USD", Timezone: "America/El_Salvador"
        );

        await _sut.CreateAsync(request);

        _regionalTaxRepo.Verify(r => r.AddAsync(It.Is<RegionalTaxConfiguration>(x =>
            x.CountryCode == "SV" && x.TaxType == "IVA" && x.Rate == 0.13m)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SeedsRegionalTaxesForGuatemala()
    {
        _repo.Setup(r => r.ExistsByTenantIdAsync(_tenantId)).ReturnsAsync(false);

        var request = new CreateCompanyRequest(
            Name: "Empresa GT",
            LegalName: "Empresa GT S.A.",
            TaxId: null, Phone: null, Address: null, Email: null,
            Country: "Guatemala", Currency: "GTQ", Timezone: "America/Guatemala"
        );

        await _sut.CreateAsync(request);

        _regionalTaxRepo.Verify(r => r.AddAsync(It.Is<RegionalTaxConfiguration>(x =>
            x.CountryCode == "GT" && x.TaxType == "IVA" && x.Rate == 0.12m)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_CallsFiscalServiceAndRegionalTaxRepo()
    {
        _repo.Setup(r => r.ExistsByTenantIdAsync(_tenantId)).ReturnsAsync(false);

        var request = new CreateCompanyRequest(
            Name: "Test",
            LegalName: "Test S.A.",
            TaxId: null, Phone: null, Address: null, Email: null,
            Country: "Nicaragua", Currency: "NIO", Timezone: "America/Managua"
        );

        await _sut.CreateAsync(request);

        _fiscal.Verify(f => f.SetupDefaultTaxesAsync(It.IsAny<Guid>(), "NIC"), Times.Once);
        _regionalTaxRepo.Verify(r => r.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CreateAsync_RegionalTaxesHaveCorrectCompanyId()
    {
        _repo.Setup(r => r.ExistsByTenantIdAsync(_tenantId)).ReturnsAsync(false);

        // Simulate EF setting the Company.Id when AddAsync is called
        var capturedCompanyId = Guid.Empty;
        _repo.Setup(r => r.AddAsync(It.IsAny<Core.Entities.Company>()))
            .Callback<Core.Entities.Company>(c => c.Id = Guid.NewGuid())
            .Returns(Task.CompletedTask);

        var request = new CreateCompanyRequest(
            Name: "Test",
            LegalName: "Test S.A.",
            TaxId: null, Phone: null, Address: null, Email: null,
            Country: "Nicaragua", Currency: "NIO", Timezone: "America/Managua"
        );

        await _sut.CreateAsync(request);

        // Verify regional taxes were created with a non-empty CompanyId
        _regionalTaxRepo.Verify(r => r.AddAsync(It.Is<RegionalTaxConfiguration>(x =>
            x.CompanyId != Guid.Empty)), Times.AtLeastOnce);
    }
}
