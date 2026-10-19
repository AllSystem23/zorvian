using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Tests.Services;

/// <summary>
/// El seeder de plantillas documentales debe: (1) crear las 26 plantillas
/// (6 multi-país v2.1 + liquidación final multi-país + 8 de tandas 1-2 + 5 de la tanda 3:
/// Recibo de Pago Proveedor, Certificado de Garantía, Hoja de Ruta,
/// Ficha de Activo Fijo y Kardex + Acta de Recepción (Taller) + 2 de la tanda 4:
/// Solicitud de Cotización y Transferencia entre Bodegas + Guía de Remisión
/// + 2 de la tanda 7: Planilla de Pago y Colilla de Pago),
/// (2) ser idempotente (no pisar plantillas ya actuales ni las de usuario),
/// (3) refrescar las versiones viejas (1.0 Nicaragua-hardcodeado / 2.0) aunque
/// los nombres hayan ganado acentos (match por nombre normalizado), y
/// (4) insertar las plantillas sembradas que falten en una BD existente —
/// así las nuevas llegan a instalaciones existentes.
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
    public async Task SeedAsync_CreatesAllSeededTemplates_MultiCountryPlusLiquidation()
    {
        await DocumentTemplateSeeder.SeedAsync(_db, _logger);

        var templates = await AllTemplates();

        Assert.Equal(28, templates.Count);
        Assert.Contains(templates, t => t.Name == "Contrato de Trabajo a Término Indefinido");
        Assert.Contains(templates, t => t.Name == "Carta de Oferta Laboral");
        Assert.Contains(templates, t => t.Name == "Constancia de Empleo");
        Assert.Contains(templates, t => t.Name == "Cotización Comercial");
        Assert.Contains(templates, t => t.Name == "Nota de Entrega");
        Assert.Contains(templates, t => t.Name == "Poder Notarial General");
        Assert.Contains(templates, t => t.Name == "Liquidación Final");
        Assert.Contains(templates, t => t.Name == "Factura");
        Assert.Contains(templates, t => t.Name == "Guía de Despacho");
        Assert.Contains(templates, t => t.Name == "Guía de Remisión");
        Assert.Contains(templates, t => t.Name == "Nota de Crédito");
        Assert.Contains(templates, t => t.Name == "Orden de Compra");
        Assert.Contains(templates, t => t.Name == "Acta de Recepción de Compras");
        Assert.Contains(templates, t => t.Name == "Recibo de Pago (Cliente)");
        Assert.Contains(templates, t => t.Name == "Comprobante de Caja");
        Assert.Contains(templates, t => t.Name == "Orden de Trabajo");
        Assert.Contains(templates, t => t.Name == "Acta de Recepción (Taller)");
        Assert.Contains(templates, t => t.Name == "Recibo de Pago (Proveedor)");
        Assert.Contains(templates, t => t.Name == "Certificado de Garantía");
        Assert.Contains(templates, t => t.Name == "Hoja de Ruta / Entrega");
        Assert.Contains(templates, t => t.Name == "Lista de Empaque");
        Assert.Contains(templates, t => t.Name == "Ficha de Activo Fijo");
        Assert.Contains(templates, t => t.Name == "Kardex de Producto");
        Assert.Contains(templates, t => t.Name == "Solicitud de Cotización");
        Assert.Contains(templates, t => t.Name == "Transferencia entre Bodegas");
        Assert.Contains(templates, t => t.Name == "Planilla de Pago");
        Assert.Contains(templates, t => t.Name == "Colilla de Pago");
        Assert.Contains(templates, t => t.Name == "Pago a Prestador de Servicios");

        var multiCountry = templates.ToList();
        Assert.All(multiCountry, t => Assert.Equal("ALL", t.CountryCode));

        // Las 7 originales van en v2.1; las nuevas arrancan en v1.0.
        foreach (var name in new[]
                 {
                     "Contrato de Trabajo a Término Indefinido", "Carta de Oferta Laboral",
                     "Constancia de Empleo", "Cotización Comercial", "Nota de Entrega",
                     "Poder Notarial General"
                 })
            Assert.Equal("2.1", templates.Single(t => t.Name == name).Version);

        foreach (var name in new[]
                 {
                     "Factura", "Guía de Despacho", "Guía de Remisión", "Nota de Crédito", "Orden de Compra",
                     "Acta de Recepción de Compras", "Recibo de Pago (Cliente)",
                     "Comprobante de Caja", "Orden de Trabajo",
                     "Acta de Recepción (Taller)",
                     "Recibo de Pago (Proveedor)", "Certificado de Garantía",
                     "Hoja de Ruta / Entrega", "Ficha de Activo Fijo", "Kardex de Producto",
                     "Solicitud de Cotización", "Transferencia entre Bodegas",
                     "Planilla de Pago", "Colilla de Pago", "Pago a Prestador de Servicios"
                 })
            Assert.Equal("1.0", templates.Single(t => t.Name == name).Version);

        var liquidacion = templates.Single(t => t.Name == "Liquidación Final");
        Assert.Equal("ALL", liquidacion.CountryCode);
        Assert.Equal("2.0", liquidacion.Version);
        Assert.Equal("Employee", liquidacion.Module);
        Assert.Equal("HR", liquidacion.Category);
        // Las citas al Código del Trabajo nicaragüense quedan detrás del condicional
        // de país y las tasas ya no están fijas en el HTML (salen de CountryTaxConfig).
        Assert.Contains("Country.Code == 'NIC'", liquidacion.Content);
        Assert.Contains("Art. 93", liquidacion.Content);
        Assert.DoesNotContain("7.00%", liquidacion.Content);
        Assert.DoesNotContain("21.50%", liquidacion.Content);

        // Módulos de las plantillas nuevas (los usan los endpoints de venta/compras/caja/fleet).
        Assert.Equal("Sale", templates.Single(t => t.Name == "Factura").Module);
        Assert.Equal("Sale", templates.Single(t => t.Name == "Guía de Despacho").Module);
        Assert.Equal("Sale", templates.Single(t => t.Name == "Guía de Remisión").Module);
        Assert.Equal("Sale", templates.Single(t => t.Name == "Nota de Crédito").Module);
        Assert.Equal("Purchase", templates.Single(t => t.Name == "Orden de Compra").Module);
        Assert.Equal("Purchase", templates.Single(t => t.Name == "Acta de Recepción de Compras").Module);
        Assert.Equal("Credit", templates.Single(t => t.Name == "Recibo de Pago (Cliente)").Module);
        Assert.Equal("Treasury", templates.Single(t => t.Name == "Comprobante de Caja").Module);
        Assert.Equal("Fleet", templates.Single(t => t.Name == "Orden de Trabajo").Module);
        Assert.Equal("Fleet", templates.Single(t => t.Name == "Acta de Recepción (Taller)").Module);
        Assert.Equal("Purchase", templates.Single(t => t.Name == "Recibo de Pago (Proveedor)").Module);
        Assert.Equal("Warranty", templates.Single(t => t.Name == "Certificado de Garantía").Module);
        Assert.Equal("Fleet", templates.Single(t => t.Name == "Hoja de Ruta / Entrega").Module);
        Assert.Equal("Finance", templates.Single(t => t.Name == "Ficha de Activo Fijo").Module);
        Assert.Equal("Inventory", templates.Single(t => t.Name == "Kardex de Producto").Module);
        Assert.Equal("Purchase", templates.Single(t => t.Name == "Solicitud de Cotización").Module);
        Assert.Equal("Inventory", templates.Single(t => t.Name == "Transferencia entre Bodegas").Module);
        Assert.Equal("Employee", templates.Single(t => t.Name == "Planilla de Pago").Module);
        Assert.Equal("Employee", templates.Single(t => t.Name == "Colilla de Pago").Module);
        Assert.Equal("Employee", templates.Single(t => t.Name == "Pago a Prestador de Servicios").Module);
        Assert.Equal("HR", templates.Single(t => t.Name == "Pago a Prestador de Servicios").Category);
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
        Assert.Equal(28, (await AllTemplates()).Count);
    }

    [Fact]
    public async Task SeedAsync_InsertsMissingSeededTemplates_IntoExistingDatabases()
    {
        // BD existente que solo tiene una plantilla de usuario y una sembrada vieja:
        // el seeder debe insertar las 27 sembradas que faltan sin tocar las existentes.
        _db.DocumentTemplates.AddRange(
            Legacy("Mi Plantilla Personalizada", "1.0", "ALL", "contenido de usuario"),
            Legacy("Constancia de Empleo", "2.1", "ALL", "constancia del usuario"));
        await _db.SaveChangesAsync();

        await DocumentTemplateSeeder.SeedAsync(_db, _logger);

        var all = await AllTemplates();
        Assert.Equal(29, all.Count); // 2 preexistentes + 27 sembradas faltantes (Constancia ya existía)

        var userTemplate = all.Single(t => t.Name == "Mi Plantilla Personalizada");
        Assert.Equal("contenido de usuario", userTemplate.Content);

        var constancia = all.Single(t => t.Name == "Constancia de Empleo");
        Assert.Equal("constancia del usuario", constancia.Content);

        Assert.Contains(all, t => t.Name == "Factura");
        Assert.Contains(all, t => t.Name == "Guía de Despacho");
        Assert.Contains(all, t => t.Name == "Guía de Remisión");
        Assert.Contains(all, t => t.Name == "Nota de Crédito");
        Assert.Contains(all, t => t.Name == "Orden de Compra");
        Assert.Contains(all, t => t.Name == "Acta de Recepción de Compras");
        Assert.Contains(all, t => t.Name == "Recibo de Pago (Cliente)");
        Assert.Contains(all, t => t.Name == "Comprobante de Caja");
        Assert.Contains(all, t => t.Name == "Orden de Trabajo");
        Assert.Contains(all, t => t.Name == "Acta de Recepción (Taller)");
        Assert.Contains(all, t => t.Name == "Recibo de Pago (Proveedor)");
        Assert.Contains(all, t => t.Name == "Certificado de Garantía");
        Assert.Contains(all, t => t.Name == "Hoja de Ruta / Entrega");
        Assert.Contains(all, t => t.Name == "Lista de Empaque");
        Assert.Contains(all, t => t.Name == "Ficha de Activo Fijo");
        Assert.Contains(all, t => t.Name == "Kardex de Producto");
        Assert.Contains(all, t => t.Name == "Solicitud de Cotización");
        Assert.Contains(all, t => t.Name == "Transferencia entre Bodegas");
        Assert.Contains(all, t => t.Name == "Planilla de Pago");
        Assert.Contains(all, t => t.Name == "Colilla de Pago");
        Assert.Contains(all, t => t.Name == "Pago a Prestador de Servicios");
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
        // 3 preexistentes + 26 sembradas faltantes (cotización y contrato ya existían).
        Assert.Equal(29, all.Count);

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
    public async Task SeedAsync_UpgradesLegacyNicaraguaLiquidation_InPlace_WithoutDuplicating()
    {
        // La v1.1 "Liquidación Final (Nicaragua)" era NIC-only. El seeder debe refrescarla
        // EN SITIO a la v2.0 multi-país (alias en la clave de emparejamiento) y NO insertar
        // una copia nueva de "Liquidación Final".
        _db.DocumentTemplates.Add(
            Legacy("Liquidación Final (Nicaragua)", "1.1", "NIC", "liquidación vieja nicaragüense"));
        await _db.SaveChangesAsync();

        await DocumentTemplateSeeder.SeedAsync(_db, _logger);

        var all = await AllTemplates();
        var liquidation = all.Where(t => t.Name.StartsWith("Liquidación Final")).ToList();

        Assert.Single(liquidation);
        Assert.Equal("Liquidación Final", liquidation[0].Name);
        Assert.Equal("ALL", liquidation[0].CountryCode);
        Assert.Equal("2.0", liquidation[0].Version);
        Assert.DoesNotContain("liquidación vieja nicaragüense", liquidation[0].Content);
        Assert.Equal(28, all.Count); // 1 preexistente (refrescada) + 27 sembradas faltantes
    }

    [Fact]
    public async Task SeedAsync_UpgradeIsIdempotent_SecondRunChangesNothing()
    {
        _db.DocumentTemplates.Add(
            Legacy("Constancia de Empleo", "1.0", "NI", "vieja constancia"));
        await _db.SaveChangesAsync();

        await DocumentTemplateSeeder.SeedAsync(_db, _logger);
        var afterFirst = (await AllTemplates()).Single(t => t.Name == "Constancia de Empleo");

        await DocumentTemplateSeeder.SeedAsync(_db, _logger);
        var afterSecond = (await AllTemplates()).Single(t => t.Name == "Constancia de Empleo");

        Assert.Equal("2.1", afterSecond.Version);
        Assert.Equal(afterFirst.UpdatedAt, afterSecond.UpdatedAt);
        Assert.Equal(afterFirst.Content, afterSecond.Content);
    }
}
