using Moq;
using Nexora.Application.DTOs.Company;
using Nexora.Application.Interfaces;
using Nexora.Application.Services;
using Nexora.Core.Interfaces;

namespace Nexora.Tests.Services;

public sealed class CompanyServiceTests
{
    private readonly Mock<ICompanyRepository> _repo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly CompanyService _sut;

    public CompanyServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns("tenant-123");
        _sut = new CompanyService(_repo.Object, _tenant.Object);
    }

    [Fact]
    public async Task CreateAsync_CreatesCompany_WithDefaults()
    {
        _repo.Setup(r => r.ExistsByTenantIdAsync("tenant-123")).ReturnsAsync(false);

        var request = new CreateCompanyRequest(
            Name: "Mi Empresa",
            LegalName: "Mi Empresa S.A.",
            TaxId: "J123456789",
            Phone: "+50588888888",
            Address: "Managua, Nicaragua"
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
        _repo.Setup(r => r.ExistsByTenantIdAsync("tenant-123")).ReturnsAsync(true);

        var request = new CreateCompanyRequest(
            Name: "Otra",
            LegalName: "Otra S.A.",
            TaxId: null,
            Phone: null,
            Address: null
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesCompanyFields()
    {
        var company = new Core.Entities.Company
        {
            Id = Guid.NewGuid(),
            TenantId = "tenant-123",
            Name = "Original",
            LegalName = "Original S.A.",
            TaxId = "J123",
            Currency = "NIO",
            Timezone = "America/Managua",
            MaxEmployees = 50,
        };
        _repo.Setup(r => r.GetByTenantIdAsync("tenant-123")).ReturnsAsync(company);

        var request = new UpdateCompanyRequest(
            Name: "Nuevo Nombre",
            LegalName: null,
            TaxId: "J999",
            Phone: null,
            Address: null,
            Currency: "USD",
            Timezone: null,
            LogoUrl: null
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

        _repo.Setup(r => r.GetByTenantIdAsync("tenant-123")).ReturnsAsync(company);
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

        _repo.Setup(r => r.GetByTenantIdAsync("tenant-123")).ReturnsAsync(company);
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
            ApprovalFlowConfig: "[{\"step\":1,\"role\":\"Rrhh\"}]"
        );

        var result = await _sut.UpdateSettingsAsync(request);

        Assert.NotNull(result);
        Assert.Equal(18, result.VacationDaysPerYear);
        Assert.Equal(20, result.LateToleranceMinutes);
        Assert.True(result.OvertimeEnabled);
        Assert.Equal("[{\"step\":1,\"role\":\"Rrhh\"}]", result.ApprovalFlowConfig);
        _repo.Verify(r => r.UpdateSettingsAsync(It.IsAny<Core.Entities.CompanySettings>()), Times.Once);
    }
}
