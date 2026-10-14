using System.Reflection;
using FluentAssertions;
using Fluid;
using Xunit;
using Zorvian.Application.Helpers;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Tests.Services;

/// <summary>
/// El contrato multi-país depende de que Fluid resuelva las claves del diccionario
/// de contexto (employee_name, Company.Name, Country.WeeklyHours, etc.).
/// Fija dos verdades: (1) el Fluid "crudo" no resuelve claves planas con punto, y
/// (2) con LiquidModelNormalizer (la ruta real de DocumentService) sí resuelven,
/// para que el template del seeder no renderice con placeholders vacíos en el PDF.
/// </summary>
public class DocumentTemplateLiquidTests
{
    private static FluidParser Parser => new();

    private static async Task<string> RenderRawAsync(string template, object model)
    {
        var ok = Parser.TryParse(template, out var tpl, out var error);
        ok.Should().BeTrue($"el template debe parsear: {error}");
        var ctx = new TemplateContext(model);
        ctx.Options.MemberAccessStrategy.Register(model.GetType());
        return await tpl!.RenderAsync(ctx);
    }

    private static Task<string> RenderAsync(string template, Dictionary<string, string> model) =>
        RenderRawAsync(template, LiquidModelNormalizer.Normalize(model));

    // ── Realidad del motor crudo (por eso existe LiquidModelNormalizer) ──

    [Fact]
    public async Task Raw_FlatDictionary_DottedKeys_DoNotResolve()
    {
        var model = new Dictionary<string, string>
        {
            ["Company.Name"] = "ACME S.A.",
            ["Country.WeeklyHours"] = "48"
        };
        var result = await RenderRawAsync(
            "{{ Company.Name }}|{{ Country.WeeklyHours }}h|{{ Company.Name | default: 'FB' }}", model);
        result.Should().Be("|h|FB");
    }

    [Fact]
    public async Task Raw_FlatDictionary_FlatKeys_Resolve()
    {
        var model = new Dictionary<string, string> { ["employee_name"] = "Juan Pérez" };
        var result = await RenderRawAsync("Hola {{ employee_name }}", model);
        result.Should().Be("Hola Juan Pérez");
    }

    // ── Modelo anidado (la forma que produce LiquidModelNormalizer) ──

    [Fact]
    public async Task Raw_NestedObjectDictionary_DottedKeys_Resolve()
    {
        var model = new Dictionary<string, object>
        {
            ["Company"] = new Dictionary<string, object> { ["Name"] = "ACME S.A." },
            ["Country"] = new Dictionary<string, object> { ["WeeklyHours"] = "48" },
            ["employee_name"] = "Juan Pérez"
        };
        var result = await RenderRawAsync(
            "{{ Company.Name }} - {{ Country.WeeklyHours }}h - {{ employee_name }}", model);
        result.Should().Be("ACME S.A. - 48h - Juan Pérez");
    }

    [Fact]
    public async Task Raw_NestedStringDictionary_DottedKeys_Resolve()
    {
        var model = new Dictionary<string, object>
        {
            ["Company"] = new Dictionary<string, string> { ["Name"] = "ACME S.A." },
            ["employee_name"] = "Juan Pérez"
        };
        var result = await RenderRawAsync("{{ Company.Name }} - {{ employee_name }}", model);
        result.Should().Be("ACME S.A. - Juan Pérez");
    }

    // ── Normalizador ──

    [Fact]
    public void Normalizer_NestsDottedKeys_AndKeepsFlatKeys()
    {
        var model = new Dictionary<string, string>
        {
            ["Company.Name"] = "ACME",
            ["Country.WeeklyHours"] = "44",
            ["employee_name"] = "María"
        };

        var normalized = LiquidModelNormalizer.Normalize(model)
            .Should().BeOfType<Dictionary<string, object>>().Subject;

        normalized.Should().NotContainKey("Company.Name");
        normalized.Should().ContainKey("employee_name");
        var company = normalized["Company"].Should().BeOfType<Dictionary<string, object>>().Subject;
        company["Name"].Should().Be("ACME");
        var country = normalized["Country"].Should().BeOfType<Dictionary<string, object>>().Subject;
        country["WeeklyHours"].Should().Be("44");
    }

    [Fact]
    public void Normalizer_FlatKeyDoesNotOverwriteNestedStructure()
    {
        var model = new Dictionary<string, string>
        {
            ["Company"] = "plana",
            ["Company.Name"] = "anidada"
        };

        var normalized = LiquidModelNormalizer.Normalize(model)
            .Should().BeOfType<Dictionary<string, object>>().Subject;

        normalized["Company"].Should().BeOfType<Dictionary<string, object>>()
            .Which["Name"].Should().Be("anidada");
    }

    // ── Liquid de plantillas (con normalización, como DocumentService) ──

    [Fact]
    public async Task DefaultFilter_UsesFallback_WhenMissingOrEmpty()
    {
        var model = new Dictionary<string, string> { ["a"] = "" };
        var result = await RenderAsync("{{ a | default: 'X' }}|{{ missing | default: 'Y' }}", model);
        result.Should().Be("X|Y");
    }

    [Fact]
    public async Task IfBlankBlock_HidesWhenEmpty()
    {
        var model = new Dictionary<string, string> { ["empty"] = "", ["filled"] = "sí" };
        var result = await RenderAsync(
            "{% if empty != blank %}[{{ empty }}]{% endif %}{% if filled != blank %}[{{ filled }}]{% endif %}",
            model);
        result.Should().Be("[sí]");
    }

    [Fact]
    public async Task ContractTemplate_Renders_WithoutLeftoverPlaceholders()
    {
        var method = typeof(DocumentTemplateSeeder).GetMethod(
            "BuildContractHtml", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();
        var html = (string)method!.Invoke(null, null)!;

        var ok = Parser.TryParse(html, out _, out var error);
        ok.Should().BeTrue($"BuildContractHtml debe ser Liquid válido: {error}");

        var model = new Dictionary<string, string>
        {
            ["Company.LegalName"] = "Comercializadora Zorvian S.A.",
            ["Company.Name"] = "Comercializadora Zorvian S.A.",
            ["Company.Date"] = "12/10/2026",
            ["Company.TaxId"] = "J0310000000000",
            ["Company.TaxIdLabel"] = "NIT",
            ["Company.Address"] = "6a Avenida 12-45, Zona 1, Guatemala",
            ["Contract.City"] = "6a Avenida 12-45, Zona 1, Guatemala",
            ["Branch.Name"] = "Sucursal Central",
            ["Branch.Address"] = "6a Avenida 12-45, Zona 1, Guatemala",
            ["Country.Name"] = "Guatemala",
            ["Country.Currency"] = "Q",
            ["Country.CurrencyName"] = "quetzales",
            ["Country.WeeklyHours"] = "44",
            ["Country.WeeklySchedule"] = "de lunes a viernes",
            ["Country.ProbationMonths"] = "2",
            ["Country.VacationDays"] = "15",
            ["Country.IdDocument"] = "Documento Personal de Identificación (DPI)",
            ["Country.LaborLaw"] = "Código de Trabajo de la República de Guatemala",
            ["Country.SocialSecurity"] = "Instituto Guatemalteco de Seguridad Social (IGSS)",
            ["Country.ThirteenthMonth"] = "Bono 14 y Aguinaldo",
            ["Employee.Address"] = "Residencial Las Palmas, Villa 4",
            ["employee_name"] = "María López",
            ["employee_id"] = "0101 12345 678",
            ["position"] = "Coordinadora de Ventas",
            ["salary"] = "8,500.00",
            ["start_date"] = "01/11/2026",
            ["department"] = "Comercial",
        };

        var rendered = await RenderAsync(html, model);

        rendered.Should().NotContain("{{", "no deben quedar placeholders sin resolver");
        rendered.Should().Contain("Comercializadora Zorvian S.A.");
        rendered.Should().Contain("María López");
        rendered.Should().Contain("8,500.00");
        rendered.Should().Contain("44 horas semanales");
        rendered.Should().Contain("de lunes a viernes");
        rendered.Should().Contain("EL TRABAJADOR");
        rendered.Replace("EMPLEADOR", "").Should().NotContain("EMPLEADO",
            "el contrato usa 'trabajador', nunca 'empleado' (EMPLEADOR se excluye para no confundir)");
        rendered.Should().Contain("2 meses");
        rendered.Should().Contain("Q");
        rendered.Should().Contain("Bono 14 y Aguinaldo");
        rendered.Should().Contain("IGSS");
        rendered.Should().Contain("Documento Personal de Identificación (DPI)");
        rendered.Should().Contain("Guatemala");
        rendered.Should().Contain("Sucursal Central");
    }

    // ── Las 6 plantillas sembradas: parsean, renderizan y no hardcodean país ──

    private static Dictionary<string, string> RichModel() => new()
    {
        ["Company.LegalName"] = "Comercializadora Zorvian S.A.",
        ["Company.Name"] = "Comercializadora Zorvian S.A.",
        ["Company.Date"] = "12/10/2026",
        ["Company.TaxId"] = "J0610000000000",
        ["Company.TaxIdLabel"] = "NIT",
        ["Company.Address"] = "Av. Central 123",
        ["Contract.City"] = "Av. Central 123",
        ["Branch.Name"] = "Sucursal Central",
        ["Branch.Address"] = "Av. Central 123",
        ["Country.Name"] = "Nicaragua",
        ["Country.Currency"] = "C$",
        ["Country.CurrencyName"] = "córdobas",
        ["Country.WeeklyHours"] = "48",
        ["Country.WeeklySchedule"] = "de lunes a sábado",
        ["Country.ProbationMonths"] = "3",
        ["Country.VacationDays"] = "15",
        ["Country.IdDocument"] = "Cédula de Identidad",
        ["Country.LaborLaw"] = "Código del Trabajo de la República de Nicaragua",
        ["Country.TaxAuthority"] = "Dirección General de Ingresos (DGI)",
        ["Country.SocialSecurity"] = "Instituto Nicaragüense de Seguridad Social (INSS)",
        ["Country.ThirteenthMonth"] = "Aguinaldo (mes adicional en diciembre)",
        ["Country.VatLabel"] = "IVA (15%)",
        ["Employee.Address"] = "Residencial Las Palmas, Villa 4",
        ["employee_name"] = "María López",
        ["employee_id"] = "001-050100-0001X",
        ["candidate_name"] = "Carlos Rodríguez",
        ["position"] = "Coordinadora de Ventas",
        ["salary"] = "15,000.00",
        ["start_date"] = "01/11/2026",
        ["department"] = "Comercial",
        ["purpose"] = "Trámite bancario",
        ["benefits"] = "Seguro médico",
        ["client_name"] = "Distribuidora Norte S.A.",
        ["quote_number"] = "COT-001",
        ["quote_date"] = "12/10/2026",
        ["validity_days"] = "15",
        ["payment_terms"] = "30 días",
        ["notes"] = "Observaciones de la cotización",
        ["Sale.Subtotal"] = "1,000.00",
        ["Sale.Tax"] = "150.00",
        ["Sale.Discount"] = "50.00",
        ["Sale.Total"] = "1,100.00",
        ["delivery_date"] = "12/10/2026",
        ["invoice_ref"] = "FAC-001",
        ["carrier"] = "Transportes Pacífico",
        ["received_by"] = "Ana Torres",
        ["grantor_name"] = "Roberto Gómez",
        ["grantor_id"] = "001-050100-0002A",
        ["attorney_name"] = "Lucía Herrera",
        ["attorney_id"] = "001-050100-0003B",
        ["powers"] = "Administrar cuentas bancarias",
        ["scope"] = "Actos de administración",
    };

    private static string InvokeBuilder(string builderName)
    {
        var method = typeof(DocumentTemplateSeeder).GetMethod(
            builderName, BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull($"{builderName} debe existir");
        return (string)method!.Invoke(null, null)!;
    }

    [Theory]
    [InlineData("BuildContractHtml")]
    [InlineData("BuildOfferLetterHtml")]
    [InlineData("BuildEmploymentCertHtml")]
    [InlineData("BuildQuoteHtml")]
    [InlineData("BuildDeliveryNoteHtml")]
    [InlineData("BuildPowerOfAttorneyHtml")]
    [InlineData("BuildLiquidationFinalHtml")]
    public async Task SeededBuilder_Renders_WithoutLeftoverPlaceholders(string builderName)
    {
        var html = InvokeBuilder(builderName);

        var ok = Parser.TryParse(html, out _, out var error);
        ok.Should().BeTrue($"{builderName} debe ser Liquid válido: {error}");

        var rendered = await RenderAsync(html, RichModel());

        rendered.Should().NotContain("{{", $"{builderName} no debe dejar placeholders sin resolver");
        rendered.Should().NotContain("Managua",
            $"{builderName} debe usar Contract.City, no una ciudad hardcodeada");
        rendered.Should().NotContain("Republica de Nicaragua",
            $"{builderName} debe usar Country.Name, no un país hardcodeado");
    }

    [Fact]
    public async Task OfferLetter_UsesCountryCurrencyProbationAndIdDocument()
    {
        var rendered = await RenderAsync(InvokeBuilder("BuildOfferLetterHtml"), RichModel());

        rendered.Should().Contain("15,000.00");
        rendered.Should().Contain("C$ (córdobas)");
        rendered.Should().Contain("3 meses");
        rendered.Should().Contain("Cédula de Identidad");
        rendered.Should().Contain("Av. Central 123");
    }

    [Fact]
    public async Task Quote_UsesVatLabelAndSaleTotals()
    {
        var rendered = await RenderAsync(InvokeBuilder("BuildQuoteHtml"), RichModel());

        rendered.Should().Contain("COTIZACIÓN");
        rendered.Should().Contain("IVA (15%)");
        rendered.Should().Contain("1,100.00");
        rendered.Should().Contain("150.00");
        rendered.Should().Contain("COT-001");
        rendered.Should().Contain("15 días");
    }

    [Fact]
    public async Task PowerOfAttorney_UsesCityCountryAndIdDocument()
    {
        var rendered = await RenderAsync(InvokeBuilder("BuildPowerOfAttorneyHtml"), RichModel());

        rendered.Should().Contain("En Av. Central 123, Nicaragua,");
        rendered.Should().Contain("Cédula de Identidad No.");
        rendered.Should().Contain("Roberto Gómez");
        rendered.Should().Contain("Lucía Herrera");
    }

    [Fact]
    public async Task EmploymentCert_UsesCityCurrencyAndTrabajadorWording()
    {
        var rendered = await RenderAsync(InvokeBuilder("BuildEmploymentCertHtml"), RichModel());

        rendered.Should().Contain("Av. Central 123,");
        rendered.Should().Contain("15,000.00");
        rendered.Should().Contain("C$ (córdobas)");
        rendered.Should().Contain("señor(a)");
        rendered.Should().Contain("María López");
    }

    [Fact]
    public async Task LiquidationFinal_UsesNicaraguaLaborReferencesAndTotals()
    {
        var rendered = await RenderAsync(InvokeBuilder("BuildLiquidationFinalHtml"), RichModel());

        rendered.Should().Contain("LIQUIDACIÓN FINAL");
        rendered.Should().Contain("Art. 93 Código del Trabajo");
        rendered.Should().Contain("Art. 78 Código del Trabajo");
        rendered.Should().Contain("Art. 45 Código del Trabajo");
        rendered.Should().Contain("7.00%");
        rendered.Should().Contain("21.50%");
        rendered.Should().Contain("2.00%");
        rendered.Should().Contain("INATEC");
        rendered.Should().Contain("NETO A RECIBIR");
        rendered.Should().Contain("Cantidad en letras");
        rendered.Should().Contain("finiquito");
        rendered.Should().Contain("María López");
        rendered.Should().Contain("001-050100-0001X");
        rendered.Should().Contain("C$");
        rendered.Should().Contain("Av. Central 123, 12/10/2026", "fecha de elaboración cae a Company.Date por defecto");
    }
}
