using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Tests.Services;

/// <summary>
/// El seeder de plantillas documentales debe: (1) crear las 6 plantillas en v2.1
/// multi-país, (2) ser idempotente (no pisar plantillas ya actuales), y (3) refrescar
/// las versiones viejas (1.0 Nicaragua-hardcodeado / 2.0) aunque los nombres hayan
/// ganado acentos (match por nombre normalizado). Es lo que hace el "reset" al
/// arrancar la API sin borrar la BD.
/// </summary>
public sealed class DocumentTemplateSeederTests : IDisposable
{
    private readonly ZorvianDbContext _db;
    private readonly ILogger _logger = NullLogger.Instance;

    public DocumentTemplateSeederTests()
    {
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantMock = new Mock<Core.Interfaces.ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns(new TenantId(Guid.NewGuid()));
        _db = new ZorvianDbContext(options, tenantMock.Object);
    }

    public void Dispose() => _db.Dispose();

    private Task<List<DocumentTemplate>> AllTemplates() =>
        _db.DocumentTemplates.IgnoreQueryFilters().ToListAsync();

    private static DocumentTemplate Legacy(string name, string version, string countryCode, string content) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = "SYSTEM",
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "system",
        Name = name,
        Category = "HR",
        Module = "Employee",
        CountryCode = countryCode,
        IsActive = true,
        Version = version,
        Variables = "[]",
        Content = content
    };

    [Fact]
    public async Task SeedAsync_CreatesSevenTemplates_MultiCountryPlusNicaraguaLiquidation()
    {
        await DocumentTemplateSeeder.SeedAsync(_db, _logger);

        var templates = await AllTemplates();

        Assert.Equal(7, templates.Count);
        Assert.Contains(templates, t => t.Name == "Contrato de Trabajo a Término Indefinido");
        Assert.Contains(templates, t => t.Name == "Carta de Oferta Laboral");
        Assert.Contains(templates, t => t.Name == "Constancia de Empleo");
        Assert.Contains(templates, t => t.Name == "Cotización Comercial");
        Assert.Contains(templates, t => t.Name == "Nota de Entrega");
        Assert.Contains(templates, t => t.Name == "Poder Notarial General");
        Assert.Contains(templates, t => t.Name == "Liquidación Final (Nicaragua)");

        var multiCountry = templates.Where(t => t.Name != "Liquidación Final (Nicaragua)").ToList();
        Assert.All(multiCountry, t => Assert.Equal("2.1", t.Version));
        Assert.All(multiCountry, t => Assert.Equal("ALL", t.CountryCode));

        var liquidacion = templates.Single(t => t.Name == "Liquidación Final (Nicaragua)");
        Assert.Equal("NIC", liquidacion.CountryCode);
        Assert.Equal("Employee", liquidacion.Module);
        Assert.Equal("HR", liquidacion.Category);
        Assert.Contains("Art. 93", liquidacion.Content);
        Assert.Contains("INATEC", liquidacion.Content);
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent_DoesNotTouchCurrentTemplates()
    {
        await DocumentTemplateSeeder.SeedAsync(_db, _logger);

        var quote = (await AllTemplates()).Single(t => t.Name == "Cotización Comercial");
        quote.Content = "MARCADOR-DEL-USUARIO";
        await _db.SaveChangesAsync();

        await DocumentTemplateSeeder.SeedAsync(_db, _logger);

        var refreshed = (await AllTemplates()).Single(t => t.Name == "Cotización Comercial");
        Assert.Equal("MARCADOR-DEL-USUARIO", refreshed.Content);
        Assert.Equal(7, (await AllTemplates()).Count);
    }

    [Fact]
    public async Task SeedAsync_UpgradesOldVersions_ByName_NormalizingAccents()
    {
        // v1 sin acento en el nombre, v2.0 del contrato con CountryCode todavía "NI",
        // y una plantilla de usuario que NO debe tocarse.
        _db.DocumentTemplates.AddRange(
            Legacy("Cotizacion Comercial", "1.0", "NI", "contenido viejo de cotizacion"),
            Legacy("Contrato de Trabajo a Término Indefinido", "2.0", "NI", "contenido v2 sin jornada"),
            Legacy("Mi Plantilla Personalizada", "1.0", "ALL", "contenido de usuario"));
        await _db.SaveChangesAsync();

        await DocumentTemplateSeeder.SeedAsync(_db, _logger);

        var all = await AllTemplates();
        Assert.Equal(3, all.Count);

        var quote = all.Single(t => t.Name == "Cotización Comercial");
        Assert.Equal("2.1", quote.Version);
        Assert.Equal("ALL", quote.CountryCode);
        Assert.Contains("COTIZACIÓN", quote.Content);

        var contract = all.Single(t => t.Name == "Contrato de Trabajo a Término Indefinido");
        Assert.Equal("2.1", contract.Version);
        Assert.Equal("ALL", contract.CountryCode);
        Assert.Contains("EL TRABAJADOR", contract.Content);

        var userTemplate = all.Single(t => t.Name == "Mi Plantilla Personalizada");
        Assert.Equal("1.0", userTemplate.Version);
        Assert.Equal("contenido de usuario", userTemplate.Content);
    }

    [Fact]
    public async Task SeedAsync_UpgradeIsIdempotent_SecondRunChangesNothing()
    {
        _db.DocumentTemplates.Add(
            Legacy("Constancia de Empleo", "1.0", "NI", "vieja constancia"));
        await _db.SaveChangesAsync();

        await DocumentTemplateSeeder.SeedAsync(_db, _logger);
        var afterFirst = (await AllTemplates()).Single();

        await DocumentTemplateSeeder.SeedAsync(_db, _logger);
        var afterSecond = (await AllTemplates()).Single();

        Assert.Equal("2.1", afterSecond.Version);
        Assert.Equal(afterFirst.UpdatedAt, afterSecond.UpdatedAt);
        Assert.Equal(afterFirst.Content, afterSecond.Content);
    }
}
