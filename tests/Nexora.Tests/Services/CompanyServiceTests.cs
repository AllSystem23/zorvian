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
}
