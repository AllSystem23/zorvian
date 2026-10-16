using Microsoft.EntityFrameworkCore;
using Moq;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories;

namespace Zorvian.Tests.Services;

/// <summary>
/// En producción (Render + Neon) el seeder crea las 23 plantillas con
/// TenantId = "SYSTEM" una sola vez (e inserta las que falten en arranques
/// posteriores). El query filter híbrido de DocumentTemplate
/// debe permitir que cualquier compañía las liste y genere desde el Motor
/// Documental y desde Talento Humano, manteniendo aisladas las plantillas
/// personalizadas de otras compañías (Sólo SuperAdmin con bypass las ve todas).
/// </summary>
public sealed class DocumentTemplateSystemVisibilityTests : IDisposable
{
    private readonly ZorvianDbContext _db;
    private readonly Guid _tenantA;
    private readonly Mock<Core.Interfaces.ITenantContext> _tenant;

    private readonly Guid _systemTemplateId;
    private readonly Guid _ownTemplateId;
    private readonly Guid _otherTemplateId;

    public DocumentTemplateSystemVisibilityTests()
    {
        _tenantA = Guid.NewGuid();
        var otherTenant = Guid.NewGuid();

        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenant = new Mock<Core.Interfaces.ITenantContext>();
        _tenant.Setup(t => t.TenantId).Returns(new TenantId(_tenantA));
        _tenant.Setup(t => t.BypassTenantFilter).Returns(false);
        _db = new ZorvianDbContext(options, _tenant.Object);

        _systemTemplateId = Guid.NewGuid();
        _ownTemplateId = Guid.NewGuid();
        _otherTemplateId = Guid.NewGuid();

        _db.DocumentTemplates.AddRange(
            Template(_systemTemplateId, DocumentTemplate.SystemTenantId, "Contrato de Trabajo a Término Indefinido"),
            Template(_ownTemplateId, _tenantA.ToString(), "Plantilla Propia de la Compañía"),
            Template(_otherTemplateId, otherTenant.ToString(), "Plantilla de Otra Compañía"));
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    private static DocumentTemplate Template(Guid id, string tenantId, string name) => new()
    {
        Id = id,
        TenantId = tenantId,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "test",
        Name = name,
        Category = "HR",
        CountryCode = "ALL",
        Module = "Employee",
        IsActive = true,
        Version = "2.1",
        Content = "<html></html>"
    };

    [Fact]
    public async Task CompanyUser_ListsSystemTemplates_ButNotOtherCompanyTemplates()
    {
        var repo = new DocumentTemplateRepository(_db);

        var (items, total) = await repo.GetFilteredAsync(null, null, null, null, null, 1, 50);

        Assert.Equal(2, total);
        Assert.Contains(items, t => t.Id == _systemTemplateId);
        Assert.Contains(items, t => t.Id == _ownTemplateId);
        Assert.DoesNotContain(items, t => t.Id == _otherTemplateId);
    }

    [Fact]
    public async Task CompanyUser_CanFetchSystemTemplateById_ForQuickGenerate()
    {
        var repo = new DocumentTemplateRepository(_db);

        var system = await repo.GetByIdAsync(_systemTemplateId);
        var foreign = await repo.GetByIdAsync(_otherTemplateId);

        Assert.NotNull(system);
        Assert.Equal("Contrato de Trabajo a Término Indefinido", system!.Name);
        Assert.Null(foreign);
    }

    [Fact]
    public async Task CompanyUser_SeesSystemTemplates_ByCountryAll()
    {
        var repo = new DocumentTemplateRepository(_db);

        var byCountry = await repo.GetByCountryAsync("NIC");

        Assert.Contains(byCountry, t => t.Id == _systemTemplateId);
        Assert.DoesNotContain(byCountry, t => t.Id == _otherTemplateId);
    }

    [Fact]
    public async Task SuperAdminWithBypass_SeesEveryTemplate()
    {
        _tenant.Setup(t => t.BypassTenantFilter).Returns(true);
        var repo = new DocumentTemplateRepository(_db);

        var (items, total) = await repo.GetFilteredAsync(null, null, null, null, null, 1, 50);

        Assert.Equal(3, total);
        Assert.NotNull(await repo.GetByIdAsync(_otherTemplateId));
    }

    [Fact]
    public async Task SeedAsync_TemplatesUseSystemTenant_AndAreVisibleToCompanyUsers()
    {
        var freshOptions = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        await using var freshDb = new ZorvianDbContext(freshOptions, _tenant.Object);

        await DocumentTemplateSeeder.SeedAsync(freshDb, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);

        var repo = new DocumentTemplateRepository(freshDb);
        var (items, total) = await repo.GetFilteredAsync(null, null, null, null, null, 1, 50);

        Assert.Equal(26, total);
        Assert.All(items, t => Assert.Equal(DocumentTemplate.SystemTenantId, t.TenantId));
        Assert.Equal(6, items.Count(t => t.Version == "2.1"));
        Assert.Contains(items, t => t.Name == "Liquidación Final (Nicaragua)" && t.CountryCode == "NIC");
        Assert.Contains(items, t => t.Name == "Factura" && t.Version == "1.0");
        Assert.Contains(items, t => t.Name == "Guía de Despacho" && t.Version == "1.0");
        Assert.Contains(items, t => t.Name == "Guía de Remisión" && t.Version == "1.0");
        Assert.Contains(items, t => t.Name == "Nota de Crédito" && t.Version == "1.0");
        Assert.Contains(items, t => t.Name == "Orden de Compra" && t.Version == "1.0");
        Assert.Contains(items, t => t.Name == "Acta de Recepción de Compras" && t.Version == "1.0");
        Assert.Contains(items, t => t.Name == "Recibo de Pago (Cliente)" && t.Version == "1.0");
        Assert.Contains(items, t => t.Name == "Comprobante de Caja" && t.Version == "1.0");
        Assert.Contains(items, t => t.Name == "Orden de Trabajo" && t.Version == "1.0");
        Assert.Contains(items, t => t.Name == "Acta de Recepción (Taller)" && t.Version == "1.0");
        Assert.Contains(items, t => t.Name == "Recibo de Pago (Proveedor)" && t.Version == "1.0");
        Assert.Contains(items, t => t.Name == "Certificado de Garantía" && t.Version == "1.0");
        Assert.Contains(items, t => t.Name == "Hoja de Ruta / Entrega" && t.Version == "1.0");
        Assert.Contains(items, t => t.Name == "Ficha de Activo Fijo" && t.Version == "1.0");
        Assert.Contains(items, t => t.Name == "Kardex de Producto" && t.Version == "1.0");
        Assert.Contains(items, t => t.Name == "Solicitud de Cotización" && t.Version == "1.0");
        Assert.Contains(items, t => t.Name == "Transferencia entre Bodegas" && t.Version == "1.0");
        Assert.Contains(items, t => t.Name == "Planilla de Pago" && t.Version == "1.0");
        Assert.Contains(items, t => t.Name == "Colilla de Pago" && t.Version == "1.0");
    }
}
