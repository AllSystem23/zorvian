using System.Text;
using Moq;
using Zorvian.Application.DTOs.Company;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

/// <summary>
/// Regresiones del logo de compañía. Bugs históricos cubiertos aquí:
/// 1. Se borraba el objeto recién subido al reemplazar con la misma
///    extensión (la ruta nueva y la vieja son idénticas).
/// 2. Un fallo del almacenamiento devolvía <c>null</c>, que el controller
///    traducía a un 404 "Empresa no encontrada" (engañoso).
/// </summary>
public sealed class CompanyServiceLogoTests
{
    private readonly Mock<ICompanyRepository> _repo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IFiscalService> _fiscal = new();
    private readonly Mock<IDocumentStorageService> _storage = new();
    private readonly Mock<IRegionalTaxConfigurationRepository> _regionalTaxRepo = new();
    private readonly Mock<ICountryTaxConfigRepository> _taxConfigRepo = new();
    private readonly Mock<ISeedService> _seed = new();
    private readonly Mock<ISubscriptionPlanRepository> _plans = new();
    private readonly CompanyService _sut;

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly string _tenantId = Guid.NewGuid().ToString();

    public CompanyServiceLogoTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(_tenantId);
        _storage.Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((Stream _, string path, string _) => path);
        _storage.Setup(s => s.GetSignedUrlAsync(It.IsAny<string?>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync((string? v, TimeSpan? _) => v is null ? null : $"https://signed.test/{v}");

        _sut = new CompanyService(
            _repo.Object,
            _tenant.Object,
            _fiscal.Object,
            _storage.Object,
            _regionalTaxRepo.Object,
            _taxConfigRepo.Object,
            _seed.Object,
            _plans.Object);
    }

    private Company ExistingCompany(string? logoUrl = null) => new()
    {
        Id = _companyId,
        TenantId = _tenantId,
        Name = "Mi Empresa",
        LogoUrl = logoUrl,
    };

    private static Stream LogoStream() => new MemoryStream(Encoding.UTF8.GetBytes("fake-png-bytes"));

    [Fact]
    public async Task UploadLogoAsync_SuperAdmin_PersistsPath_AndReturnsSignedUrl()
    {
        _tenant.Setup(t => t.IsSuperAdmin).Returns(true);
        var company = ExistingCompany();
        _repo.Setup(r => r.GetByIdAsync(_companyId)).ReturnsAsync(company);

        var result = await _sut.UploadLogoAsync(_companyId, LogoStream(), "image/png");

        var expectedPath = $"companies/{_companyId}/logo.png";
        Assert.Equal(expectedPath, company.LogoUrl);
        Assert.Equal($"https://signed.test/{expectedPath}", result);
        _storage.Verify(s => s.UploadFileAsync(It.IsAny<Stream>(), expectedPath, "image/png"), Times.Once);
        _repo.Verify(r => r.UpdateAsync(company), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UploadLogoAsync_ReUploadSameExtension_DoesNotDeleteTheFreshObject()
    {
        // Regresión: con el mismo contentType la ruta no cambia, así que borrar
        // "el logo anterior" eliminaba el archivo recién subido y el logo
        // desaparecía para el usuario.
        _tenant.Setup(t => t.IsSuperAdmin).Returns(true);
        var samePath = $"companies/{_companyId}/logo.png";
        var company = ExistingCompany(samePath);
        _repo.Setup(r => r.GetByIdAsync(_companyId)).ReturnsAsync(company);

        await _sut.UploadLogoAsync(_companyId, LogoStream(), "image/png");

        _storage.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
        Assert.Equal(samePath, company.LogoUrl);
    }

    [Fact]
    public async Task UploadLogoAsync_DifferentExtension_DeletesPreviousObject()
    {
        _tenant.Setup(t => t.IsSuperAdmin).Returns(true);
        var oldPath = $"companies/{_companyId}/logo.png";
        var company = ExistingCompany(oldPath);
        _repo.Setup(r => r.GetByIdAsync(_companyId)).ReturnsAsync(company);

        var result = await _sut.UploadLogoAsync(_companyId, LogoStream(), "image/jpeg");

        _storage.Verify(s => s.DeleteFileAsync(oldPath), Times.Once);
        Assert.Equal($"companies/{_companyId}/logo.jpg", company.LogoUrl);
        Assert.Equal($"https://signed.test/companies/{_companyId}/logo.jpg", result);
    }

    [Fact]
    public async Task UploadLogoAsync_LegacyPublicUrl_IsNormalizedBeforeDeleting()
    {
        _tenant.Setup(t => t.IsSuperAdmin).Returns(true);
        var company = ExistingCompany($"https://storage.googleapis.com/zorvian-erp.firebasestorage.app/companies/{_companyId}/logo.webp");
        _repo.Setup(r => r.GetByIdAsync(_companyId)).ReturnsAsync(company);

        await _sut.UploadLogoAsync(_companyId, LogoStream(), "image/png");

        _storage.Verify(s => s.DeleteFileAsync($"companies/{_companyId}/logo.webp"), Times.Once);
    }

    [Fact]
    public async Task UploadLogoAsync_StorageFailure_ThrowsActionableError_AndDoesNotPersist()
    {
        _tenant.Setup(t => t.IsSuperAdmin).Returns(true);
        var company = ExistingCompany();
        _repo.Setup(r => r.GetByIdAsync(_companyId)).ReturnsAsync(company);
        _storage.Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UploadLogoAsync(_companyId, LogoStream(), "image/png"));

        Assert.Contains("almacenamiento", ex.Message);
        Assert.Null(company.LogoUrl);
        _repo.Verify(r => r.UpdateAsync(It.IsAny<Company>()), Times.Never);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UploadLogoAsync_CompanyNotFound_ReturnsNull()
    {
        _tenant.Setup(t => t.IsSuperAdmin).Returns(true);
        _repo.Setup(r => r.GetByIdAsync(_companyId)).ReturnsAsync((Company?)null);

        var result = await _sut.UploadLogoAsync(_companyId, LogoStream(), "image/png");

        Assert.Null(result);
        _storage.Verify(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UploadLogoAsync_NonSuperAdmin_IsScopedToOwnTenant()
    {
        _tenant.Setup(t => t.IsSuperAdmin).Returns(false);
        var company = ExistingCompany();
        _repo.Setup(r => r.GetByTenantIdAsync(_tenantId)).ReturnsAsync(company);

        await _sut.UploadLogoAsync(_companyId, LogoStream(), "image/webp");

        _repo.Verify(r => r.GetByTenantIdAsync(_tenantId), Times.Once);
        _repo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        Assert.Equal($"companies/{_companyId}/logo.webp", company.LogoUrl);
    }

    // ── Huérfanos: subida correcta + fallo al persistir ──────────────────────

    [Fact]
    public async Task UploadLogoAsync_SaveFails_DeletesUploadedObject_AndKeepsPreviousReference()
    {
        // Sin este rollback el objeto quedaba en el bucket para siempre: la BD
        // seguía apuntando al logo anterior y nadie conocía el archivo nuevo.
        _tenant.Setup(t => t.IsSuperAdmin).Returns(true);
        var oldPath = $"companies/{_companyId}/logo.png";
        var company = ExistingCompany(oldPath);
        _repo.Setup(r => r.GetByIdAsync(_companyId)).ReturnsAsync(company);
        _repo.Setup(r => r.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException("db down"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UploadLogoAsync(_companyId, LogoStream(), "image/jpeg"));

        _storage.Verify(s => s.DeleteFileAsync($"companies/{_companyId}/logo.jpg"), Times.Once);
        _storage.Verify(s => s.DeleteFileAsync(oldPath), Times.Never);
        Assert.Equal(oldPath, company.LogoUrl);
    }

    [Fact]
    public async Task UploadLogoAsync_SaveFails_SamePath_DoesNotDeleteTheFile()
    {
        // La ruta no cambia (png → png): el objeto es el mismo archivo que la
        // BD ya referencia, borrarlo dejaría a la empresa sin logo.
        _tenant.Setup(t => t.IsSuperAdmin).Returns(true);
        var samePath = $"companies/{_companyId}/logo.png";
        var company = ExistingCompany(samePath);
        _repo.Setup(r => r.GetByIdAsync(_companyId)).ReturnsAsync(company);
        _repo.Setup(r => r.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException("db down"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UploadLogoAsync(_companyId, LogoStream(), "image/png"));

        _storage.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
    }

    // ── Huérfanos: el logo cambia por un PUT de la compañía ──────────────────

    private static UpdateCompanyRequest UpdateRequestWithLogo(string? logoUrl) => new(
        Name: null, LegalName: null, TaxId: null, Phone: null, Address: null,
        Email: null, Country: null, Currency: null, Timezone: null,
        LogoUrl: logoUrl, MaxEmployees: null, IsActive: null, SubscriptionPlan: null);

    [Fact]
    public async Task UpdateByIdAsync_ReplacingLogo_DeletesPreviousObject()
    {
        var oldPath = $"companies/{_companyId}/logo.png";
        var company = ExistingCompany(oldPath);
        _repo.Setup(r => r.GetByIdAsync(_companyId)).ReturnsAsync(company);

        await _sut.UpdateByIdAsync(_companyId, UpdateRequestWithLogo(
            $"https://storage.googleapis.com/bucket/companies/{_companyId}/logo.webp"));

        _storage.Verify(s => s.DeleteFileAsync(oldPath), Times.Once);
        Assert.Equal($"companies/{_companyId}/logo.webp", company.LogoUrl);
    }

    [Fact]
    public async Task UpdateByIdAsync_ClearingLogo_DeletesPreviousObject()
    {
        var oldPath = $"companies/{_companyId}/logo.png";
        var company = ExistingCompany(oldPath);
        _repo.Setup(r => r.GetByIdAsync(_companyId)).ReturnsAsync(company);

        await _sut.UpdateByIdAsync(_companyId, UpdateRequestWithLogo(""));

        _storage.Verify(s => s.DeleteFileAsync(oldPath), Times.Once);
        Assert.True(string.IsNullOrEmpty(company.LogoUrl));
    }

    [Fact]
    public async Task UpdateByIdAsync_KeepingSameLogo_DoesNotDeleteTheObject()
    {
        var samePath = $"companies/{_companyId}/logo.png";
        var company = ExistingCompany(samePath);
        _repo.Setup(r => r.GetByIdAsync(_companyId)).ReturnsAsync(company);

        await _sut.UpdateByIdAsync(_companyId, UpdateRequestWithLogo(samePath));

        _storage.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
        Assert.Equal(samePath, company.LogoUrl);
    }

    [Fact]
    public async Task UpdateAsync_ReplacingLogo_DeletesPreviousObject()
    {
        var oldPath = $"companies/{_companyId}/logo.png";
        var company = ExistingCompany(oldPath);
        _repo.Setup(r => r.GetByTenantIdAsync(_tenantId)).ReturnsAsync(company);

        await _sut.UpdateAsync(UpdateRequestWithLogo($"companies/{_companyId}/logo.webp"));

        _storage.Verify(s => s.DeleteFileAsync(oldPath), Times.Once);
        Assert.Equal($"companies/{_companyId}/logo.webp", company.LogoUrl);
    }

    [Fact]
    public async Task UpdateByIdAsync_SaveFails_DoesNotDeletePreviousObject()
    {
        // Si el guardado falla, la BD sigue apuntando al logo anterior: hay que
        // conservar el archivo.
        var oldPath = $"companies/{_companyId}/logo.png";
        var company = ExistingCompany(oldPath);
        _repo.Setup(r => r.GetByIdAsync(_companyId)).ReturnsAsync(company);
        _repo.Setup(r => r.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException("db down"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.UpdateByIdAsync(_companyId, UpdateRequestWithLogo($"companies/{_companyId}/logo.webp")));

        _storage.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
    }

    // ── Quitar el logo ───────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveLogoAsync_ClearsReference_AndDeletesObject()
    {
        _tenant.Setup(t => t.IsSuperAdmin).Returns(true);
        var oldPath = $"companies/{_companyId}/logo.png";
        var company = ExistingCompany(oldPath);
        _repo.Setup(r => r.GetByIdAsync(_companyId)).ReturnsAsync(company);

        var removed = await _sut.RemoveLogoAsync(_companyId);

        Assert.True(removed);
        Assert.Null(company.LogoUrl);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _storage.Verify(s => s.DeleteFileAsync(oldPath), Times.Once);
    }

    [Fact]
    public async Task RemoveLogoAsync_WithoutLogo_StillClears_AndDoesNotTouchStorage()
    {
        _tenant.Setup(t => t.IsSuperAdmin).Returns(true);
        var company = ExistingCompany();
        _repo.Setup(r => r.GetByIdAsync(_companyId)).ReturnsAsync(company);

        var removed = await _sut.RemoveLogoAsync(_companyId);

        Assert.True(removed);
        _storage.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RemoveLogoAsync_CompanyNotFound_ReturnsFalse()
    {
        _tenant.Setup(t => t.IsSuperAdmin).Returns(true);
        _repo.Setup(r => r.GetByIdAsync(_companyId)).ReturnsAsync((Company?)null);

        Assert.False(await _sut.RemoveLogoAsync(_companyId));
        _storage.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RemoveLogoAsync_NonSuperAdmin_IsScopedToOwnTenant()
    {
        _tenant.Setup(t => t.IsSuperAdmin).Returns(false);
        var company = ExistingCompany($"companies/{_companyId}/logo.png");
        _repo.Setup(r => r.GetByTenantIdAsync(_tenantId)).ReturnsAsync(company);

        Assert.True(await _sut.RemoveLogoAsync(_companyId));
        _repo.Verify(r => r.GetByTenantIdAsync(_tenantId), Times.Once);
        _repo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    // ── Lectura para la pantalla de configuración ────────────────────────────

    [Fact]
    public async Task GetLogoAsync_ReturnsCompanyIdAndSignedUrl()
    {
        var path = $"companies/{_companyId}/logo.png";
        _repo.Setup(r => r.GetByTenantIdAsync(_tenantId)).ReturnsAsync(ExistingCompany(path));

        var result = await _sut.GetLogoAsync();

        Assert.NotNull(result);
        Assert.Equal(_companyId, result!.Id);
        Assert.Equal($"https://signed.test/{path}", result.LogoUrl);
    }

    [Fact]
    public async Task GetLogoAsync_CompanyNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByTenantIdAsync(_tenantId)).ReturnsAsync((Company?)null);

        Assert.Null(await _sut.GetLogoAsync());
    }
}
