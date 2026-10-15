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

    // ── Las 20 plantillas sembradas: parsean, renderizan y no hardcodean país ──

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
        // ── Factura / Guía de Despacho ──
        ["invoice_number"] = "FAC-2026-001",
        ["invoice_date"] = "12/10/2026",
        ["guide_number"] = "GD-2026-001",
        ["client_id"] = "J0510000000001A",
        ["client_address"] = "Km 12 Carretera Norte, Bodega 4",
        // ── Nota de Crédito ──
        ["credit_note_number"] = "NC-2026-001",
        ["issue_date"] = "12/10/2026",
        ["reason"] = "Devolución parcial de mercancía",
        // ── Orden de Compra ──
        ["order_number"] = "OC-2026-001",
        ["order_date"] = "12/10/2026",
        ["expected_date"] = "20/10/2026",
        ["supplier_name"] = "Distribuidora Norte S.A.",
        ["supplier_id"] = "J0510000000002B",
        ["supplier_address"] = "Barrio Industrial, Local 7",
        ["Order.Subtotal"] = "5,000.00",
        ["Order.Tax"] = "750.00",
        ["Order.Discount"] = "100.00",
        ["Order.Total"] = "5,650.00",
        ["Order.Currency"] = "C$",
        // ── Acta de Recepción de Compras ──
        ["acta_number"] = "COM-2026-001",
        ["acta_date"] = "12/10/2026",
        ["order_ref"] = "OC-2026-001",
        ["Purchase.Subtotal"] = "5,000.00",
        ["Purchase.Tax"] = "750.00",
        ["Purchase.Discount"] = "100.00",
        ["Purchase.Total"] = "5,650.00",
        ["Purchase.PaidAmount"] = "2,000.00",
        ["Purchase.Balance"] = "3,650.00",
        ["Purchase.Currency"] = "C$",
        // ── Recibo de Pago (Cliente) ──
        ["receipt_number"] = "RP-2026-001",
        ["payment_date"] = "12/10/2026",
        ["credit_number"] = "CR-2026-001",
        ["payment_method"] = "Efectivo",
        ["payment_amount"] = "1,500.00",
        ["principal_amount"] = "1,300.00",
        ["interest_amount"] = "200.00",
        ["credit_total"] = "12,000.00",
        ["credit_balance"] = "10,500.00",
        // ── Comprobante de Caja ──
        ["movement_title"] = "COMPROBANTE DE INGRESO",
        ["movement_date"] = "12/10/2026",
        ["concept"] = "Pago de cliente en efectivo",
        ["amount"] = "1,500.00",
        ["register_code"] = "CAJA-01",
        ["reference"] = "REF-789",
        // ── Orden de Trabajo ──
        ["wo_number"] = "OT-2026-001",
        ["report_date"] = "10/10/2026",
        ["vehicle_plate"] = "MZ-123",
        ["vehicle_desc"] = "Hilux 2020",
        ["driver_name"] = "Pedro Alonso",
        ["problem_description"] = "Ruido en el motor al arrancar",
        ["diagnosis"] = "Correa de distribución desgastada",
        ["root_cause"] = "Desgaste por kilometraje",
        ["solution_applied"] = "Cambio de correa y tensor",
        ["priority"] = "High",
        ["workshop_name"] = "Taller Central",
        ["mechanic"] = "Jorge Ruiz",
        ["start_date"] = "10/10/2026",
        ["end_date"] = "12/10/2026",
        ["downtime_hours"] = "16",
        ["WorkOrder.CostLabor"] = "800.00",
        ["WorkOrder.CostParts"] = "1,200.00",
        ["WorkOrder.CostTotal"] = "2,000.00",
        ["cost_total"] = "2,000.00",
        // ── Recibo de Pago (Proveedor) ──
        ["purchase_ref"] = "COM-2026-001",
        ["purchase_balance"] = "3,650.00",
        // ── Certificado de Garantía ──
        ["warranty_number"] = "GAR-2026-001",
        ["product_name"] = "Laptop Lenovo ThinkPad E14",
        ["serial_number"] = "SN-8842190",
        ["duration_months"] = "12",
        ["terms"] = "Cobertura de partes y mano de obra",
        // ── Hoja de Ruta / Entrega ──
        ["delivery_code"] = "ENT-2026-001",
        ["scheduled_date"] = "14/10/2026",
        ["time_window"] = "08:00 - 12:00",
        ["delivery_address"] = "Km 12 Carretera Norte, Bodega 4",
        ["route_name"] = "Ruta Norte",
        ["receiver_name"] = "Ana Torres",
        ["observations"] = "Entregar en recepción",
        // ── Ficha de Activo Fijo ──
        ["asset_code"] = "AF-001",
        ["asset_name"] = "Servidor Dell PowerEdge R750",
        ["acquisition_date"] = "01/06/2026",
        ["acquisition_cost"] = "85,000.00",
        ["useful_life_years"] = "5",
        ["residual_value"] = "5,000.00",
        ["depreciation_method"] = "Línea Recta",
        ["assigned_to"] = "Área de TI",
        ["status"] = "Activo",
        // ── Kardex de Producto ──
        ["product_code"] = "PRD-001",
        ["unit_of_measure"] = "unidad",
        ["current_stock"] = "120",
        ["period"] = "01/10/2026 - 12/10/2026",
        ["total_in_qty"] = "80",
        ["total_out_qty"] = "45",
        ["current_cost"] = "250.00",
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
    [InlineData("BuildInvoiceHtml")]
    [InlineData("BuildDeliveryGuideHtml")]
    [InlineData("BuildCreditNoteHtml")]
    [InlineData("BuildPurchaseOrderHtml")]
    [InlineData("BuildReceivingReportHtml")]
    [InlineData("BuildClientPaymentReceiptHtml")]
    [InlineData("BuildCashVoucherHtml")]
    [InlineData("BuildWorkOrderHtml")]
    [InlineData("BuildWorkshopReceiptHtml")]
    [InlineData("BuildSupplierPaymentReceiptHtml")]
    [InlineData("BuildWarrantyCertificateHtml")]
    [InlineData("BuildDeliveryRouteSheetHtml")]
    [InlineData("BuildFixedAssetCardHtml")]
    [InlineData("BuildKardexHtml")]
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

    [Fact]
    public async Task Invoice_UsesTotalsVatLabelAndClientData()
    {
        var rendered = await RenderAsync(InvokeBuilder("BuildInvoiceHtml"), RichModel());

        rendered.Should().Contain("FACTURA");
        rendered.Should().Contain("FAC-2026-001");
        rendered.Should().Contain("Distribuidora Norte S.A.");
        rendered.Should().Contain("J0510000000001A");
        rendered.Should().Contain("Km 12 Carretera Norte, Bodega 4");
        rendered.Should().Contain("IVA (15%)");
        rendered.Should().Contain("1,100.00", "total de la venta");
        rendered.Should().Contain("150.00", "impuesto de la venta");
        rendered.Should().Contain("Cédula de Identidad", "usa el documento del país, no hardcodeado");
    }

    [Fact]
    public async Task DeliveryGuide_UsesGuideNumberDeliveryDateAndCarrier()
    {
        var rendered = await RenderAsync(InvokeBuilder("BuildDeliveryGuideHtml"), RichModel());

        rendered.Should().Contain("GUÍA DE DESPACHO");
        rendered.Should().Contain("GD-2026-001");
        rendered.Should().Contain("Transportes Pacífico");
        rendered.Should().Contain("Distribuidora Norte S.A.");
        rendered.Should().Contain("Km 12 Carretera Norte, Bodega 4");
        rendered.Should().Contain("Ref. Factura");
    }

    [Fact]
    public async Task CreditNote_UsesNumberReasonInvoiceRefAndTotals()
    {
        var rendered = await RenderAsync(InvokeBuilder("BuildCreditNoteHtml"), RichModel());

        rendered.Should().Contain("NOTA DE CRÉDITO");
        rendered.Should().Contain("NC-2026-001");
        rendered.Should().Contain("Devolución parcial de mercancía");
        rendered.Should().Contain("FAC-001");
        rendered.Should().Contain("Distribuidora Norte S.A.");
        rendered.Should().Contain("IVA (15%)");
        rendered.Should().Contain("1,100.00");
    }

    [Fact]
    public async Task PurchaseOrder_UsesSupplierOrderTotalsAndCountryLabels()
    {
        var rendered = await RenderAsync(InvokeBuilder("BuildPurchaseOrderHtml"), RichModel());

        rendered.Should().Contain("ORDEN DE COMPRA");
        rendered.Should().Contain("OC-2026-001");
        rendered.Should().Contain("Distribuidora Norte S.A.");
        rendered.Should().Contain("J0510000000002B");
        rendered.Should().Contain("Barrio Industrial, Local 7");
        rendered.Should().Contain("IVA (15%)");
        rendered.Should().Contain("5,650.00");
        rendered.Should().Contain("C$", "moneda de la orden");
        rendered.Should().Contain("Cédula de Identidad", "documento del país para el proveedor");
    }

    [Fact]
    public async Task ReceivingReport_UsesActaNumberSupplierOrderRefAndTotals()
    {
        var rendered = await RenderAsync(InvokeBuilder("BuildReceivingReportHtml"), RichModel());

        rendered.Should().Contain("ACTA DE RECEPCIÓN");
        rendered.Should().Contain("COM-2026-001");
        rendered.Should().Contain("Distribuidora Norte S.A.");
        rendered.Should().Contain("J0510000000002B");
        rendered.Should().Contain("OC-2026-001");
        rendered.Should().Contain("FAC-001", "ref. factura del proveedor");
        rendered.Should().Contain("5,650.00", "total recibido");
        rendered.Should().Contain("IVA (15%)");
    }

    [Fact]
    public async Task ClientPaymentReceipt_UsesAmountClientCreditAndMethod()
    {
        var rendered = await RenderAsync(InvokeBuilder("BuildClientPaymentReceiptHtml"), RichModel());

        rendered.Should().Contain("RECIBO DE PAGO");
        rendered.Should().Contain("RP-2026-001");
        rendered.Should().Contain("Distribuidora Norte S.A.");
        rendered.Should().Contain("CR-2026-001");
        rendered.Should().Contain("Efectivo");
        rendered.Should().Contain("1,500.00");
        rendered.Should().Contain("10,500.00", "saldo restante");
    }

    [Fact]
    public async Task CashVoucher_UsesMovementTitleConceptAmountAndRegister()
    {
        var rendered = await RenderAsync(InvokeBuilder("BuildCashVoucherHtml"), RichModel());

        rendered.Should().Contain("COMPROBANTE DE INGRESO");
        rendered.Should().Contain("Pago de cliente en efectivo");
        rendered.Should().Contain("1,500.00");
        rendered.Should().Contain("CAJA-01");
        rendered.Should().Contain("María López", "employee_name del modelo rico");
        rendered.Should().Contain("REF-789");
    }

    [Fact]
    public async Task WorkOrder_UsesVehicleProblemDiagnosisAndCosts()
    {
        var rendered = await RenderAsync(InvokeBuilder("BuildWorkOrderHtml"), RichModel());

        rendered.Should().Contain("ORDEN DE TRABAJO");
        rendered.Should().Contain("OT-2026-001");
        rendered.Should().Contain("MZ-123");
        rendered.Should().Contain("Hilux 2020");
        rendered.Should().Contain("Pedro Alonso");
        rendered.Should().Contain("Ruido en el motor al arrancar");
        rendered.Should().Contain("Cambio de correa y tensor");
        rendered.Should().Contain("Taller Central");
        rendered.Should().Contain("Jorge Ruiz");
        rendered.Should().Contain("2,000.00", "costo total");
    }

    [Fact]
    public async Task SupplierPaymentReceipt_UsesSupplierAmountMethodAndBalance()
    {
        var rendered = await RenderAsync(InvokeBuilder("BuildSupplierPaymentReceiptHtml"), RichModel());

        rendered.Should().Contain("RECIBO DE PAGO A PROVEEDOR");
        rendered.Should().Contain("RP-2026-001");
        rendered.Should().Contain("Distribuidora Norte S.A.");
        rendered.Should().Contain("COM-2026-001");
        rendered.Should().Contain("Efectivo");
        rendered.Should().Contain("1,500.00", "monto pagado");
        rendered.Should().Contain("3,650.00", "saldo pendiente");
    }

    [Fact]
    public async Task WarrantyCertificate_UsesNumberClientProductAndValidity()
    {
        var rendered = await RenderAsync(InvokeBuilder("BuildWarrantyCertificateHtml"), RichModel());

        rendered.Should().Contain("CERTIFICADO DE GARANTÍA");
        rendered.Should().Contain("GAR-2026-001");
        rendered.Should().Contain("Distribuidora Norte S.A.");
        rendered.Should().Contain("Laptop Lenovo ThinkPad E14");
        rendered.Should().Contain("SN-8842190");
        rendered.Should().Contain("10/10/2026", "fecha de vencimiento");
        rendered.Should().Contain("12 meses");
    }

    [Fact]
    public async Task DeliveryRouteSheet_UsesCodeAddressDriverAndReceiver()
    {
        var rendered = await RenderAsync(InvokeBuilder("BuildDeliveryRouteSheetHtml"), RichModel());

        rendered.Should().Contain("HOJA DE RUTA / ENTREGA");
        rendered.Should().Contain("ENT-2026-001");
        rendered.Should().Contain("14/10/2026");
        rendered.Should().Contain("08:00 - 12:00");
        rendered.Should().Contain("Km 12 Carretera Norte, Bodega 4");
        rendered.Should().Contain("Pedro Alonso");
        rendered.Should().Contain("MZ-123");
        rendered.Should().Contain("Ruta Norte");
        rendered.Should().Contain("Ana Torres");
    }

    [Fact]
    public async Task FixedAssetCard_UsesCodeCostLifeAndDepreciation()
    {
        var rendered = await RenderAsync(InvokeBuilder("BuildFixedAssetCardHtml"), RichModel());

        rendered.Should().Contain("FICHA DE ACTIVO FIJO");
        rendered.Should().Contain("AF-001");
        rendered.Should().Contain("Servidor Dell PowerEdge R750");
        rendered.Should().Contain("01/06/2026");
        rendered.Should().Contain("85,000.00");
        rendered.Should().Contain("5 años");
        rendered.Should().Contain("Línea Recta");
        rendered.Should().Contain("Área de TI");
    }

    [Fact]
    public async Task Kardex_UsesProductCodeStockAndTotals()
    {
        var rendered = await RenderAsync(InvokeBuilder("BuildKardexHtml"), RichModel());

        rendered.Should().Contain("KARDEX DE PRODUCTO");
        rendered.Should().Contain("PRD-001");
        rendered.Should().Contain("Laptop Lenovo ThinkPad E14");
        rendered.Should().Contain("unidad");
        rendered.Should().Contain("120");
        rendered.Should().Contain("01/10/2026 - 12/10/2026");
        rendered.Should().Contain("80", "total entradas");
        rendered.Should().Contain("45", "total salidas");
        rendered.Should().Contain("250.00", "costo actual");
    }

    // ── Ítems tabulados (listas de diccionarios en el contexto) ──

    [Fact]
    public async Task Invoice_ItemsLoop_RendersRowsFromListOfDictionaries()
    {
        var html = InvokeBuilder("BuildInvoiceHtml");
        var model = new Dictionary<string, object>
        {
            ["invoice_number"] = "FAC-001",
            ["invoice_date"] = "01/10/2026",
            ["client_name"] = "Cliente Demo",
            ["Sale.Subtotal"] = "100.00",
            ["Sale.Tax"] = "15.00",
            ["Sale.Discount"] = "0.00",
            ["Sale.Total"] = "115.00",
            ["items"] = new List<Dictionary<string, object>>
            {
                new(StringComparer.Ordinal)
                {
                    ["code"] = "PRD-1",
                    ["description"] = "Laptop Lenovo",
                    ["quantity"] = "2",
                    ["unit_price"] = "50.00",
                    ["discount"] = "0.00",
                    ["subtotal"] = "100.00",
                },
            },
        };

        var rendered = await RenderRawAsync(html, LiquidModelNormalizer.Normalize(model));

        rendered.Should().Contain("Laptop Lenovo", "el bucle {% for %} debe renderizar la fila");
        rendered.Should().Contain("FAC-001");
        rendered.Should().NotContain("Los items se cargan dinámicamente",
            "con items presentes el fallback no debe aparecer");
        rendered.Should().NotContain("{{");
    }

    [Fact]
    public async Task Invoice_ItemsLoop_FallsBackToPlaceholder_WhenItemsMissing()
    {
        var html = InvokeBuilder("BuildInvoiceHtml");
        var model = new Dictionary<string, object>
        {
            ["invoice_number"] = "FAC-002",
            ["client_name"] = "Cliente Demo",
        };

        var rendered = await RenderRawAsync(html, LiquidModelNormalizer.Normalize(model));

        rendered.Should().Contain("Los items se cargan dinámicamente",
            "sin items (entrada manual del wizard) debe conservarse el placeholder");
        rendered.Should().NotContain("{{");
    }

    [Fact]
    public async Task DeliveryRoute_ItemsLoop_RendersProductRows()
    {
        var html = InvokeBuilder("BuildDeliveryRouteSheetHtml");
        var model = new Dictionary<string, object>
        {
            ["delivery_code"] = "ENT-001",
            ["scheduled_date"] = "15/10/2026",
            ["delivery_address"] = "Centro",
            ["items"] = new List<Dictionary<string, object>>
            {
                new(StringComparer.Ordinal)
                {
                    ["product"] = "PRD-9 Refacciones Bomba",
                    ["qty_ordered"] = "10",
                    ["qty_delivered"] = "8",
                    ["qty_returned"] = "2",
                    ["lot_serial"] = "LOTE-77",
                },
            },
        };

        var rendered = await RenderRawAsync(html, LiquidModelNormalizer.Normalize(model));

        rendered.Should().Contain("Refacciones Bomba");
        rendered.Should().Contain("LOTE-77");
        rendered.Should().NotContain("{{");
    }

    [Fact]
    public async Task Kardex_MovementsLoop_RendersMovementRows()
    {
        var html = InvokeBuilder("BuildKardexHtml");
        var model = new Dictionary<string, object>
        {
            ["product_code"] = "PRD-001",
            ["product_name"] = "Laptop",
            ["movements"] = new List<Dictionary<string, object>>
            {
                new(StringComparer.Ordinal)
                {
                    ["date"] = "01/10/2026",
                    ["type"] = "Compra",
                    ["direction"] = "Entrada",
                    ["quantity"] = "20",
                    ["stock_before"] = "0",
                    ["stock_after"] = "20",
                    ["reference"] = "COM-001",
                },
            },
        };

        var rendered = await RenderRawAsync(html, LiquidModelNormalizer.Normalize(model));

        rendered.Should().Contain("COM-001");
        rendered.Should().Contain("Entrada");
        rendered.Should().NotContain("{{");
    }

    // ── Orden de Compra: sello por estado (mismo template, badge condicional) ──

    [Fact]
    public async Task PurchaseOrder_ShowsApprovedSeal_OnlyWhenOrderApproved()
    {
        var html = InvokeBuilder("BuildPurchaseOrderHtml");

        var approved = new Dictionary<string, object>
        {
            ["order_number"] = "OC-2026-001",
            ["order_status"] = "approved",
        };
        var draft = new Dictionary<string, object>
        {
            ["order_number"] = "OC-2026-001",
            ["order_status"] = "draft",
        };
        var cancelled = new Dictionary<string, object>
        {
            ["order_number"] = "OC-2026-001",
            ["order_status"] = "cancelled",
        };

        var approvedHtml = await RenderRawAsync(html, LiquidModelNormalizer.Normalize(approved));
        var draftHtml = await RenderRawAsync(html, LiquidModelNormalizer.Normalize(draft));
        var cancelledHtml = await RenderRawAsync(html, LiquidModelNormalizer.Normalize(cancelled));

        approvedHtml.Should().Contain("APROBADA");
        approvedHtml.Should().NotContain("CANCELADA");
        draftHtml.Should().NotContain("APROBADA", "un borrador no lleva sello aprobado");
        cancelledHtml.Should().Contain("CANCELADA");
        cancelledHtml.Should().NotContain("APROBADA");
    }

    // ── Acta de Recepción (Taller) ──

    [Fact]
    public async Task WorkshopReceipt_UsesWorkshopVehicleProblemAndSignatures()
    {
        var model = new Dictionary<string, object>
        {
            ["acta_number"] = "OT-2026-001",
            ["report_date"] = "15/10/2026",
            ["workshop_name"] = "Taller Central",
            ["workshop_address"] = "Km 5 Carretera Sur",
            ["workshop_phone"] = "2222-3333",
            ["vehicle_plate"] = "MZ-123",
            ["vehicle_desc"] = "Hilux 2020",
            ["driver_name"] = "Pedro Alonso",
            ["problem_description"] = "Ruido en motor al arrancar",
            ["priority"] = "High",
            ["cost_estimate"] = "1,500.00",
        };

        var rendered = await RenderRawAsync(
            InvokeBuilder("BuildWorkshopReceiptHtml"), LiquidModelNormalizer.Normalize(model));

        rendered.Should().Contain("ACTA DE RECEPCIÓN (TALLER)");
        rendered.Should().Contain("OT-2026-001");
        rendered.Should().Contain("Taller Central");
        rendered.Should().Contain("Km 5 Carretera Sur");
        rendered.Should().Contain("MZ-123");
        rendered.Should().Contain("Hilux 2020");
        rendered.Should().Contain("Pedro Alonso");
        rendered.Should().Contain("Ruido en motor al arrancar");
        rendered.Should().Contain("1,500.00");
        rendered.Should().Contain("Recibido por (Taller)");
        rendered.Should().NotContain("{{");
    }
}
