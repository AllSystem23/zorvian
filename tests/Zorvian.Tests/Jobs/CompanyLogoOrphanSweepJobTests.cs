using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Web.Jobs;

namespace Zorvian.Tests.Jobs;

/// <summary>
/// Barrido de logos huérfanos (red de seguridad de los borrados en línea).
/// Reglas: solo el prefijo `companies/`, nunca un objeto referenciado (incluidas
/// compañías desactivadas / soft-deleted) y nunca uno más nuevo que la ventana
/// de gracia.
/// </summary>
public sealed class CompanyLogoOrphanSweepJobTests
{
    private readonly Mock<IDocumentStorageService> _storage = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<ILogger<CompanyLogoOrphanSweepJob>> _logger = new();
    private readonly string _tenantId = Guid.NewGuid().ToString();

    public CompanyLogoOrphanSweepJobTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(_tenantId);
    }

    private ZorvianDbContext CreateDb() => new(
        new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options,
        _tenant.Object);

    private CompanyLogoOrphanSweepJob CreateSut(ZorvianDbContext db) =>
        new(db, _storage.Object, _logger.Object);

    private static Company NewCompany(string? logoUrl, bool isDeleted = false) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid().ToString(),
        Name = "Empresa",
        LegalName = "Empresa S.A.",
        Country = "Nicaragua",
        Timezone = "America/Managua",
        LogoUrl = logoUrl,
        IsDeleted = isDeleted,
    };

    private void StubObjects(params StorageObjectInfo[] objects) =>
        _storage.Setup(s => s.ListFilesAsync(CompanyLogoOrphanSweepJob.LogoPrefix))
            .ReturnsAsync(objects);

    private static StorageObjectInfo Old(string path) =>
        new(path, DateTimeOffset.UtcNow.AddDays(-3));

    private static StorageObjectInfo Fresh(string path) =>
        new(path, DateTimeOffset.UtcNow.AddMinutes(-5));

    [Fact]
    public async Task RunAsync_DeletesUnreferencedOldObject_AndKeepsReferenced()
    {
        using var db = CreateDb();
        db.Companies.Add(NewCompany("companies/keep/logo.png"));
        await db.SaveChangesAsync();

        StubObjects(Old("companies/keep/logo.png"), Old("companies/orphan/logo.png"));

        await CreateSut(db).RunAsync();

        _storage.Verify(s => s.DeleteFileAsync("companies/orphan/logo.png"), Times.Once);
        _storage.Verify(s => s.DeleteFileAsync("companies/keep/logo.png"), Times.Never);
    }

    [Fact]
    public async Task RunAsync_KeepsUnreferencedObjectInsideGracePeriod()
    {
        // Subida + guardado no son atómicos: un archivo recién subido todavía no
        // referenciado no debe borrarse.
        using var db = CreateDb();

        StubObjects(Fresh("companies/new/logo.png"));

        await CreateSut(db).RunAsync();

        _storage.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_KeepsLogoOfSoftDeletedCompany()
    {
        // `IgnoreQueryFilters`: desactivar/soft-delete una empresa no debe
        // borrar su logo (reactivarla lo deja intacto).
        using var db = CreateDb();
        db.Companies.Add(NewCompany("companies/off/logo.png", isDeleted: true));
        await db.SaveChangesAsync();

        StubObjects(Old("companies/off/logo.png"));

        await CreateSut(db).RunAsync();

        _storage.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_LegacyPublicUrlReference_IsNormalizedAndKept()
    {
        using var db = CreateDb();
        db.Companies.Add(NewCompany(
            "https://storage.googleapis.com/zorvian-erp.firebasestorage.app/companies/legacy/logo.png?v=1"));
        await db.SaveChangesAsync();

        StubObjects(Old("companies/legacy/logo.png"));

        await CreateSut(db).RunAsync();

        _storage.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_ExternalUrlReference_DoesNotProtectStorageObject()
    {
        // Una URL externa (no del bucket) no es un path de storage: no protege
        // ningún objeto y tampoco se intenta borrar.
        using var db = CreateDb();
        db.Companies.Add(NewCompany("https://cdn.otro-sitio.com/logo.png"));
        await db.SaveChangesAsync();

        StubObjects(Old("companies/other/logo.png"));

        await CreateSut(db).RunAsync();

        _storage.Verify(s => s.DeleteFileAsync("companies/other/logo.png"), Times.Once);
        _storage.Verify(s => s.DeleteFileAsync("https://cdn.otro-sitio.com/logo.png"), Times.Never);
    }

    [Fact]
    public async Task RunAsync_ListFailure_DoesNotThrow()
    {
        using var db = CreateDb();
        _storage.Setup(s => s.ListFilesAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("sin credenciales"));

        await CreateSut(db).RunAsync();

        _storage.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_DeleteFailure_ContinuesWithTheRest()
    {
        using var db = CreateDb();
        StubObjects(Old("companies/a/logo.png"), Old("companies/b/logo.png"));
        _storage.Setup(s => s.DeleteFileAsync("companies/a/logo.png"))
            .ThrowsAsync(new InvalidOperationException("boom"));

        await CreateSut(db).RunAsync();

        _storage.Verify(s => s.DeleteFileAsync("companies/b/logo.png"), Times.Once);
    }

    [Fact]
    public async Task RunAsync_NoObjects_DoesNotDeleteAnything()
    {
        using var db = CreateDb();
        _storage.Setup(s => s.ListFilesAsync(CompanyLogoOrphanSweepJob.LogoPrefix))
            .ReturnsAsync(Array.Empty<StorageObjectInfo>());

        await CreateSut(db).RunAsync();

        _storage.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
    }
}
