using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Zorvian.Application.Helpers;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using QUnit = QuestPDF.Infrastructure.Unit;

namespace Zorvian.Infrastructure.Services;

public sealed class VacationReceiptPdfService : IVacationReceiptPdfService
{
    private readonly IVacationRepository _vacationRepo;
    private readonly ICompanyRepository _companyRepo;

    static VacationReceiptPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public VacationReceiptPdfService(
        IVacationRepository vacationRepo,
        ICompanyRepository companyRepo)
    {
        _vacationRepo = vacationRepo;
        _companyRepo = companyRepo;
    }

    public async Task<byte[]> GenerateReceiptPdfAsync(Guid receiptId)
    {
        var receipt = await _vacationRepo.GetReceiptByIdAsync(receiptId)
            ?? throw new KeyNotFoundException("Recibo de vacaciones no encontrado");

        var employee = receipt.Employee
            ?? throw new KeyNotFoundException("Empleado no encontrado");

        var company = await _companyRepo.GetByIdAsync(receipt.CompanyId)
            ?? throw new KeyNotFoundException("Compañía no encontrada");

        var ctx = new PdfContext(receipt, employee, company);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.2f, QUnit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Helvetica"));
                page.PageColor(Colors.White);

                page.Header().Column(h =>
                {
                    BuildHeader(h, ctx);
                });

                page.Content().PaddingTop(6).Column(col =>
                {
                    BuildEmployeeInfoBlock(col, ctx);
                    BuildConceptsTable(col, ctx);
                    BuildTotalsBlock(col, ctx);
                    BuildObservationsBlock(col, ctx);
                    BuildReceiptConfirmation(col, ctx);
                });

                page.Footer().PaddingTop(16).Column(f =>
                {
                    BuildSignatureLines(f, ctx);
                    f.Item().PaddingTop(10).AlignCenter()
                        .Text("Documento generado por Zorvian ERP")
                        .FontSize(7).FontColor(Colors.Grey.Medium);
                });
            });
        });

        return document.GeneratePdf();
    }

    // ── HEADER ────────────────────────────────────────────────────
    private static void BuildHeader(ColumnDescriptor col, PdfContext ctx)
    {
        col.Item().Border(1.5f).BorderColor(Colors.Black).Padding(8).Row(row =>
        {
            // Left: Company info
            row.RelativeItem(0.55f).Column(c =>
            {
                c.Item().Text(ctx.CompanyLegalName).FontSize(11).Bold();
                c.Item().PaddingTop(2).Row(r =>
                {
                    r.ConstantItem(55).Text("Ruc:").FontSize(8).FontColor(Colors.Grey.Darken1);
                    r.RelativeItem().Text(ctx.CompanyTaxId).FontSize(8).SemiBold();
                });
                c.Item().Row(r =>
                {
                    r.ConstantItem(55).Text("Dirección:").FontSize(8).FontColor(Colors.Grey.Darken1);
                    r.RelativeItem().Text(ctx.CompanyAddress).FontSize(8);
                });
                c.Item().Row(r =>
                {
                    r.ConstantItem(55).Text("Teléfono:").FontSize(8).FontColor(Colors.Grey.Darken1);
                    r.RelativeItem().Text(ctx.CompanyPhone).FontSize(8);
                });
            });

            // Right: Document info
            row.RelativeItem(0.45f).Column(c =>
            {
                c.Item().AlignRight().Text("RECIBO DE VACACIONES").FontSize(12).Bold();
                c.Item().AlignRight().PaddingTop(1).Text("ORIGINAL").FontSize(9).FontColor(Colors.Grey.Darken1);
                c.Item().PaddingTop(4).AlignRight().Row(r =>
                {
                    r.ConstantItem(90).Text("Fecha de Impresión:").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    r.ConstantItem(65).AlignRight().Text(ctx.PrintDate).FontSize(7.5f).SemiBold();
                });
                c.Item().AlignRight().Row(r =>
                {
                    r.ConstantItem(90).Text("Fecha de Emisión:").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    r.ConstantItem(65).AlignRight().Text(ctx.IssueDate).FontSize(7.5f).SemiBold();
                });
                c.Item().AlignRight().PaddingTop(2).Row(r =>
                {
                    r.ConstantItem(50).Text("Número").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    r.ConstantItem(100).AlignRight().Background(Colors.Grey.Lighten3).Padding(1)
                        .Text(ctx.Receipt.ReceiptNumber).FontSize(8).Bold();
                });
            });
        });
    }

    // ── EMPLOYEE INFO ─────────────────────────────────────────────
    private static void BuildEmployeeInfoBlock(ColumnDescriptor col, PdfContext ctx)
    {
        col.Item().PaddingTop(6).Border(1f).BorderColor(Colors.Black).Padding(6).Row(row =>
        {
            // Left column
            row.RelativeItem(0.5f).Column(c =>
            {
                void Row(string label, string value)
                {
                    c.Item().Row(r =>
                    {
                        r.ConstantItem(80).Text(label).FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                        r.RelativeItem().Text(value).FontSize(8).SemiBold();
                    });
                }
                Row("Empleado:", ctx.EmployeeName);
                Row("Cédula:", ctx.IdentificationNumber);
                Row("Fecha Ingreso:", ctx.HireDate);
                Row("Sueldo Diario:", $"{ctx.CurrencySymbol} {ctx.Fmt(ctx.Receipt.DailySalary)}");
            });

            // Right column
            row.RelativeItem(0.5f).Column(c =>
            {
                void Row(string label, string value)
                {
                    c.Item().Row(r =>
                    {
                        r.ConstantItem(95).Text(label).FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                        r.RelativeItem().Text(value).FontSize(8).SemiBold();
                    });
                }
                Row("Disfrute Desde:", ctx.ShortDate(ctx.Receipt.StartDate));
                Row("Disfrute Hasta:", ctx.ShortDate(ctx.Receipt.EndDate));
                Row("Fecha Reintegro:", ctx.ShortDate(ctx.Receipt.ReturnDate));
                Row("Sueldo Mensual:", $"{ctx.CurrencySymbol} {ctx.Fmt(ctx.Receipt.MonthlySalary)}");
            });
        });
    }

    // ── CONCEPTS TABLE ────────────────────────────────────────────
    private static void BuildConceptsTable(ColumnDescriptor col, PdfContext ctx)
    {
        col.Item().PaddingTop(4).Border(1f).BorderColor(Colors.Black).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(45);   // Código
                columns.RelativeColumn(2f);   // Descripción
                columns.ConstantColumn(75);   // Valor Auxiliar
                columns.ConstantColumn(75);   // Asignación
                columns.ConstantColumn(75);   // Deduc/Reten
            });

            // Header row
            void HeaderCell(string text, bool alignRight = false)
            {
                table.Cell().Background(Colors.Grey.Lighten3).BorderBottom(0.5f).BorderColor(Colors.Grey.Medium)
                    .Padding(3).Text(text).FontSize(7.5f).Bold();
            }

            HeaderCell("Concepto");
            HeaderCell("Descripción");
            HeaderCell("Valor Auxiliar", true);
            HeaderCell("Asignación", true);
            HeaderCell("Deduc/Reten", true);

            // Vacation pay row (asignación)
            table.Cell().BorderBottom(0.3f).BorderColor(Colors.Grey.Lighten2).Padding(3)
                .Text("A001").FontSize(7.5f);
            table.Cell().BorderBottom(0.3f).BorderColor(Colors.Grey.Lighten2).Padding(3)
                .Text("Vacaciones").FontSize(7.5f);
            table.Cell().BorderBottom(0.3f).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight()
                .Text("").FontSize(7.5f);
            table.Cell().BorderBottom(0.3f).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight()
                .Text($"{ctx.CurrencySymbol} {ctx.Fmt(ctx.Receipt.VacationPay)}").FontSize(7.5f).SemiBold();
            table.Cell().BorderBottom(0.3f).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight()
                .Text("").FontSize(7.5f);

            // INSS row (deducción)
            table.Cell().BorderBottom(0.3f).BorderColor(Colors.Grey.Lighten2).Padding(3)
                .Text("R001").FontSize(7.5f);
            table.Cell().BorderBottom(0.3f).BorderColor(Colors.Grey.Lighten2).Padding(3)
                .Text($"Seguro Social ({ctx.InssRateDisplay})").FontSize(7.5f);
            table.Cell().BorderBottom(0.3f).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight()
                .Text($"{ctx.CurrencySymbol} {ctx.Fmt(ctx.Receipt.InssEmployeeAmount)}").FontSize(7.5f);
            table.Cell().BorderBottom(0.3f).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight()
                .Text("").FontSize(7.5f);
            table.Cell().BorderBottom(0.3f).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight()
                .Text($"{ctx.CurrencySymbol} {ctx.Fmt(ctx.Receipt.InssEmployeeAmount)}").FontSize(7.5f).SemiBold();
        });
    }

    // ── TOTALS ────────────────────────────────────────────────────
    private static void BuildTotalsBlock(ColumnDescriptor col, PdfContext ctx)
    {
        col.Item().PaddingTop(2).Border(1f).BorderColor(Colors.Black).Padding(4).Row(row =>
        {
            row.RelativeItem().AlignRight().Text("Total:").FontSize(9).Bold();
            row.ConstantItem(80).AlignRight()
                .Text($"{ctx.CurrencySymbol} {ctx.Fmt(ctx.Receipt.VacationPay)}").FontSize(9).Bold();
            row.ConstantItem(80).AlignRight()
                .Text($"{ctx.CurrencySymbol} {ctx.Fmt(ctx.Receipt.InssEmployeeAmount)}").FontSize(9).Bold();
        });

        col.Item().PaddingTop(2).Background(Colors.Grey.Lighten3).Border(1f).BorderColor(Colors.Black)
            .Padding(4).Row(row =>
        {
            row.RelativeItem().AlignRight().Text("Neto a Pagar:").FontSize(10).Bold();
            row.ConstantItem(160).AlignRight()
                .Text($"{ctx.CurrencySymbol} {ctx.Fmt(ctx.Receipt.NetPay)}").FontSize(10).Bold();
        });
    }

    // ── OBSERVATIONS ──────────────────────────────────────────────
    private static void BuildObservationsBlock(ColumnDescriptor col, PdfContext ctx)
    {
        col.Item().PaddingTop(8).Border(1f).BorderColor(Colors.Black).Padding(4).Column(c =>
        {
            c.Item().Text("Observaciones:").FontSize(8).Bold();
            c.Item().PaddingTop(1).Text(ctx.Receipt.Observations ?? "").FontSize(8);
        });
    }

    // ── RECEIPT CONFIRMATION ──────────────────────────────────────
    private static void BuildReceiptConfirmation(ColumnDescriptor col, PdfContext ctx)
    {
        col.Item().PaddingTop(6).Text(t =>
        {
            t.Span("He recibido de ").FontSize(8);
            t.Span(ctx.CompanyLegalName).FontSize(8).Bold();
            t.Span(" la cantidad de ").FontSize(8);
            t.Span($"{ctx.CurrencySymbol} {ctx.Fmt(ctx.Receipt.NetPay)}").FontSize(8).Bold();
            t.Span(" en concepto de Vacaciones acumuladas período ").FontSize(8);
            t.Span(ctx.VacationPeriodYear).FontSize(8).Bold();
            t.Span(".").FontSize(8);
        });
    }

    // ── SIGNATURE LINES ───────────────────────────────────────────
    private static void BuildSignatureLines(ColumnDescriptor col, PdfContext ctx)
    {
        col.Item().PaddingTop(12).Row(row =>
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().PaddingTop(20).BorderTop(0.7f).BorderColor(Colors.Black).AlignCenter()
                    .Text("Recibí Conforme").FontSize(8);
                c.Item().AlignCenter().Text(ctx.EmployeeName).FontSize(7.5f).FontColor(Colors.Grey.Darken1);
            });
            row.ConstantItem(30);
            row.RelativeItem().Column(c =>
            {
                c.Item().PaddingTop(20).BorderTop(0.7f).BorderColor(Colors.Black).AlignCenter()
                    .Text("Pagado Por").FontSize(8);
                c.Item().AlignCenter().Text(ctx.PaidByDisplay).FontSize(7.5f).FontColor(Colors.Grey.Darken1);
            });
        });
    }

    // ── PDF CONTEXT ───────────────────────────────────────────────
    private sealed class PdfContext
    {
        public VacationPaymentReceipt Receipt { get; }
        public Employee Employee { get; }
        public Company Company { get; }

        private static readonly Dictionary<string, (string Symbol, string Name)> CurrencyMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["NIC"] = ("C$", "Córdoba"),
            ["HND"] = ("L", "Lempira"),
            ["SLV"] = ("US$", "Dólar"),
            ["GTM"] = ("Q", "Quetzal"),
            ["CRI"] = ("₡", "Colón"),
            ["PAN"] = ("B/.", "Balboa"),
            ["USD"] = ("US$", "Dólar"),
        };

        public PdfContext(VacationPaymentReceipt receipt, Employee employee, Company company)
        {
            Receipt = receipt;
            Employee = employee;
            Company = company;
        }

        public string CurrencySymbol =>
            CurrencyMap.TryGetValue(Receipt.CountryCode, out var c) ? c.Symbol : "$";

        public string CompanyLegalName => Company.LegalName ?? Company.Name;
        public string CompanyTaxId => Company.TaxId ?? "—";
        public string CompanyAddress => Company.Address ?? "—";
        public string CompanyPhone => Company.Phone ?? "—";

        public string EmployeeName => $"{Employee.FirstName} {Employee.LastName}".Trim();
        public string IdentificationNumber => Employee.IdentificationNumber ?? "—";
        public string HireDate => Employee.HireDate.ToString("dd/MM/yyyy");

        public string PrintDate => DateTime.Now.ToString("dd/MM/yyyy");
        public string IssueDate => Receipt.PaymentDate.ToString("dd/MM/yyyy");

        public string InssRateDisplay => $"{Receipt.InssEmployeeRate * 100:0.#}%";

        public string VacationPeriodYear => Receipt.StartDate.Year.ToString();

        public string PaidByDisplay => Receipt.PaidBy ?? "—";

        public string Fmt(decimal value) =>
            value.ToString("N2", CultureInfo.InvariantCulture);

        public string ShortDate(DateOnly date) => date.ToString("dd/MM/yyyy");

        public string NetAmountInWords =>
            NumberToWordsHelper.ToCurrencyWords(Receipt.NetPay, Receipt.CountryCode, Receipt.Currency);
    }
}
