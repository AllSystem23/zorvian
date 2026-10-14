using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Zorvian.Application.Helpers;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using QUnit = QuestPDF.Infrastructure.Unit;

namespace Zorvian.Infrastructure.Services;

public sealed class SettlementPdfService : ISettlementPdfService
{
    private readonly ITerminationRepository _terminationRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly ICompanyRepository _companyRepo;

    static SettlementPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public SettlementPdfService(
        ITerminationRepository terminationRepo,
        IEmployeeRepository employeeRepo,
        ICompanyRepository companyRepo)
    {
        _terminationRepo = terminationRepo;
        _employeeRepo = employeeRepo;
        _companyRepo = companyRepo;
    }

    public async Task<byte[]> GenerateSettlementPdfAsync(Guid terminationRecordId)
    {
        var record = await _terminationRepo.GetByIdAsync(terminationRecordId)
            ?? throw new KeyNotFoundException("Termination record not found");

        var employee = await _employeeRepo.GetByIdAsync(record.EmployeeId)
            ?? throw new KeyNotFoundException("Employee not found");

        var company = await _companyRepo.GetByIdAsync(record.CompanyId)
            ?? throw new KeyNotFoundException("Company not found");

        var ctx = new PdfContext(record, employee, company);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, QUnit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Helvetica"));
                page.PageColor(Colors.White);

                page.Header().Column(h =>
                {
                    h.Item().AlignCenter().Text($"LIQUIDACIÓN FINAL EN {ctx.CurrencySymbol}")
                        .FontSize(14).Bold();
                    h.Item().AlignCenter().Text(company.LegalName)
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                    h.Item().PaddingTop(4).BorderBottom(1.5f).BorderColor(Colors.Black);
                });

                page.Content().PaddingTop(8).Column(col =>
                {
                    BuildInfoBlock(col, ctx);
                    BuildIngresosBlock(col, ctx);
                    BuildPrestacionesBlock(col, ctx);
                    BuildDeduccionesBlock(col, ctx);
                    BuildTotalsBlock(col, ctx);
                    BuildLegalBlock(col, ctx);
                });

                page.Footer().PaddingTop(30).Column(f =>
                {
                    BuildSignatureLines(f, ctx);
                    f.Item().PaddingTop(12).AlignCenter()
                        .Text("Documento generado por Zorvian ERP")
                        .FontSize(7).FontColor(Colors.Grey.Medium);
                });
            });
        });

        return document.GeneratePdf();
    }

    // ── INFO ──────────────────────────────────────────────────────
    private static void BuildInfoBlock(ColumnDescriptor col, PdfContext ctx)
    {
        void InfoRow(string label, string value)
        {
            col.Item().Row(row =>
            {
                row.ConstantItem(170).Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
                row.RelativeItem().Text(value).FontSize(8.5f).SemiBold();
            });
        }

        InfoRow("Fecha de Elaboración:", ctx.ElabDate);
        InfoRow("Nombre del Trabajador:", ctx.EmployeeName);
        InfoRow("Cédula de Identidad:", ctx.IdentificationNumber);
        InfoRow("Motivo de Retiro:", ctx.ReasonLabel);
        InfoRow("Tipo de Contrato:", "Tiempo Indefinido");
        InfoRow("Tipo de Salario:", ctx.SalaryTypeLabel);
        InfoRow("Puesto (Cargo Desempeñado):", ctx.Position);

        col.Item().PaddingTop(6).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);

        col.Item().PaddingTop(4).Text("RESUMEN SALARIAL").FontSize(9).Bold();

        void SalaryRow(string l1, string v1, string l2, string v2)
        {
            col.Item().PaddingTop(1).Row(row =>
            {
                row.RelativeItem().Text(l1).FontSize(8).FontColor(Colors.Grey.Darken1);
                row.ConstantItem(90).AlignRight().Text(v1).FontSize(8.5f).SemiBold();
                row.ConstantItem(10).Text("");
                row.RelativeItem().Text(l2).FontSize(8).FontColor(Colors.Grey.Darken1);
                row.ConstantItem(90).AlignRight().Text(v2).FontSize(8.5f).SemiBold();
            });
        }

        SalaryRow("Salario Ordinario Mensual:", $"{ctx.CurrencySymbol} {ctx.Fmt(x => x.MonthlySalary)}",
                   "Sueldo Diario:", $"{ctx.CurrencySymbol} {ctx.Fmt(x => x.DailySalary)}");
        SalaryRow("Sueldo por Hora:", $"{ctx.CurrencySymbol} {ctx.Fmt(_ => ctx.HourlySalary)}",
                   "Valor de una Hora Extra:", $"{ctx.CurrencySymbol} {ctx.Fmt(_ => ctx.HourlySalary * 2)}");

        // Salario variable: mostrar promedio y máximo de 6 meses (Art. 36 CT)
        if (ctx.Record.IsVariableSalary && ctx.Record.AvgSalary6Months.HasValue)
        {
            SalaryRow("Salario Prom. Últimos 6 Meses:", $"{ctx.CurrencySymbol} {ctx.Fmt(_ => ctx.Record.AvgSalary6Months!.Value)}",
                       "Salario Más Alto Últ. 6 Meses:", $"{ctx.CurrencySymbol} {ctx.Fmt(_ => ctx.Record.MaxSalary6Months ?? 0m)}");
        }

        col.Item().PaddingTop(3).Row(row =>
        {
            row.RelativeItem().Text("Fecha de Ingreso:").FontSize(8).FontColor(Colors.Grey.Darken1);
            row.ConstantItem(200).Text(ctx.HireDate).FontSize(8.5f).SemiBold();
            row.RelativeItem().Text("Fecha de Salida:").FontSize(8).FontColor(Colors.Grey.Darken1);
            row.ConstantItem(200).Text(ctx.TerminationDate).FontSize(8.5f).SemiBold();
        });
    }

    // ── INGRESOS ──────────────────────────────────────────────────
    private static void BuildIngresosBlock(ColumnDescriptor col, PdfContext ctx)
    {
        var r = ctx.Record;
        col.Item().PaddingTop(10).Text("INGRESOS").FontSize(9).Bold();
        col.Item().BorderBottom(1).BorderColor(Colors.Black);

        void MoneyRow(string label, string value, bool bold = false)
        {
            col.Item().PaddingTop(1).Row(row =>
            {
                var lt = row.RelativeItem().Text(label).FontSize(bold ? 8.5f : 8);
                if (bold) lt.SemiBold();
                var vt = row.ConstantItem(100).AlignRight().Text(value).FontSize(bold ? 8.5f : 8);
                if (bold) vt.SemiBold();
            });
        }

        MoneyRow(r.PendingSalaryDays > 0
            ? $"Sueldo pendiente de pago ({r.PendingSalaryDays} días × {ctx.CurrencySymbol} {ctx.Fmt(x => x.DailySalary)})"
            : "Sueldo pendiente de pago: AL DÍA",
            r.PendingSalaryDays > 0 ? ctx.Fmt(x => x.PendingSalaryPay) : "-");

        if (r.OvertimePay > 0)
            MoneyRow($"Horas extras ({r.OvertimeHours} hrs)", ctx.Fmt(x => x.OvertimePay));

        MoneyRow("SUMAN SALARIOS", ctx.Fmt(x => x.PendingSalaryPay + x.OvertimePay), bold: true);
        MoneyRow("Otros conceptos", "-");
        MoneyRow("SUMAN OTROS CONCEPTOS", "-", bold: true);
    }

    // ── PRESTACIONES ──────────────────────────────────────────────
    private static void BuildPrestacionesBlock(ColumnDescriptor col, PdfContext ctx)
    {
        var r = ctx.Record;
        col.Item().PaddingTop(10).Text("PRESTACIONES SOCIALES").FontSize(9).Bold();
        col.Item().BorderBottom(1).BorderColor(Colors.Black);

        var period = $"{ctx.ShortDate(r.TerminationDate.AddMonths(-6))} - {ctx.ShortDate(r.TerminationDate)}";

        // Header
        col.Item().PaddingTop(3).Background(Colors.Grey.Lighten3).Padding(3).Row(row =>
        {
            row.ConstantItem(130).Text("CONCEPTO").FontSize(7).Bold().AlignCenter();
            row.RelativeItem().Text("PERIODO").FontSize(7).Bold().AlignCenter();
            row.ConstantItem(50).Text("DÍAS").FontSize(7).Bold().AlignCenter();
            row.ConstantItem(60).Text("A FAVOR").FontSize(7).Bold().AlignCenter();
            row.ConstantItem(80).Text("MONTO").FontSize(7).Bold().AlignCenter();
        });

        void PrestRow(string concepto, string dias, string aFavor, string monto)
        {
            col.Item().Padding(3).BorderBottom(0.25f).BorderColor(Colors.Grey.Lighten2).Row(row =>
            {
                row.ConstantItem(130).Text(concepto).FontSize(7.5f);
                row.RelativeItem().Text(period).FontSize(7).AlignCenter();
                row.ConstantItem(50).Text(dias).FontSize(7.5f).AlignCenter();
                row.ConstantItem(60).Text(aFavor).FontSize(7.5f).AlignCenter();
                row.ConstantItem(80).Text(monto).FontSize(7.5f).SemiBold().AlignRight();
            });
        }

        PrestRow("AGUINALDO (Art. 93 CT)", r.DaysWorked.ToString(), "-",
            ctx.Fmt(x => x.AguinaldoPay));
        PrestRow("VACACIONES (Art. 78 CT)", r.DaysWorked.ToString(),
            $"{ctx.Fmt(x => x.VacationDaysToPay)} d", ctx.Fmt(x => x.VacationPay));
        PrestRow("INDEM. ANT. (Art. 45 CT)", r.DaysWorked.ToString(), "-",
            ctx.Fmt(x => x.SeverancePay));

        if (r.IsTrustPosition && r.TrustPositionPay > 0)
            PrestRow("INDEM. CONFIANZA (Art. 46)", r.DaysWorked.ToString(), "-",
                ctx.Fmt(x => x.TrustPositionPay));

        col.Item().PaddingTop(2).Text(
            $"Vacaciones: {ctx.Fmt(x => x.VacationDaysAccrued)} acumulados - {ctx.Fmt(x => x.VacationDaysTaken)} tomados = {ctx.Fmt(x => x.VacationDaysToPay)} a pagar")
            .FontSize(7).FontColor(Colors.Grey.Darken1);
    }

    // ── DEDUCCIONES ───────────────────────────────────────────────
    private static void BuildDeduccionesBlock(ColumnDescriptor col, PdfContext ctx)
    {
        var r = ctx.Record;
        col.Item().PaddingTop(10).Text("DEDUCCIONES").FontSize(9).Bold();
        col.Item().BorderBottom(1).BorderColor(Colors.Black);

        void DedRow(string label, string value)
        {
            col.Item().PaddingTop(1).Row(row =>
            {
                row.RelativeItem().Text(label).FontSize(8);
                row.ConstantItem(100).AlignRight().Text(value).FontSize(8);
            });
        }

        DedRow($"{ctx.SsEntityName} Laboral {ctx.Pct(r.InssEmployeeRateDisplay)}%",
            r.InssLaboralAmount > 0 ? ctx.Fmt(x => x.InssLaboralAmount) : "-");
        DedRow("Impuesto sobre la Renta (IR) - Salario Ordinario",
            r.IrSalaryAmount > 0 ? ctx.Fmt(x => x.IrSalaryAmount) : "-");

        var irOccasional = Math.Max(0, r.IrTotalAmount - r.IrSalaryAmount);
        if (irOccasional > 0)
            DedRow("IR - Pagos Ocasionales (Vacaciones, Bonos)", ctx.Fmt(_ => irOccasional));

        if (r.IsTrustPosition && r.TrustPositionPay > 0)
            DedRow("IR - Retención Definitiva (Cargo de Confianza)", "-");

        DedRow("Saldo de Préstamo", "-");
        DedRow("Aguinaldo Pagado", "-");

        // Employer
        col.Item().PaddingTop(8).Text("EMPLEADOR").FontSize(9).Bold();
        col.Item().BorderBottom(1).BorderColor(Colors.Black);

        void EmpRow(string label, string rate, string amount)
        {
            col.Item().PaddingTop(1).Row(row =>
            {
                row.RelativeItem().Text(label).FontSize(8);
                row.ConstantItem(60).AlignCenter().Text(rate).FontSize(8);
                row.ConstantItem(100).AlignRight().Text(amount).FontSize(8);
            });
        }

        EmpRow($"{ctx.SsEntityName} Patronal", ctx.Pct(r.InssEmployerRateDisplay) + "%",
            $"{ctx.CurrencySymbol} {ctx.Fmt(x => x.InssPatronalAmount)}");

        if (r.OtherEmployerRateDisplay > 0 && !string.IsNullOrEmpty(r.OtherEmployerName))
            EmpRow(r.OtherEmployerName, ctx.Pct(r.OtherEmployerRateDisplay) + "%",
                $"{ctx.CurrencySymbol} {ctx.Fmt(x => x.InatecAmount)}");
    }

    // ── TOTALS ────────────────────────────────────────────────────
    private static void BuildTotalsBlock(ColumnDescriptor col, PdfContext ctx)
    {
        col.Item().PaddingTop(8).BorderTop(1).BorderColor(Colors.Black).PaddingTop(4)
            .Row(row =>
            {
                row.RelativeItem().AlignRight().Text("TOTAL DEDUCCIONES:").FontSize(9).Bold();
                row.ConstantItem(110).AlignRight().Text(ctx.Fmt(x => x.TotalDeductions)).FontSize(9).Bold();
            });

        col.Item().PaddingTop(6).Background(Colors.Grey.Lighten4).Padding(8)
            .Row(row =>
            {
                row.RelativeItem().AlignRight().Text("NETO A RECIBIR:").FontSize(11).Bold();
                row.ConstantItem(130).AlignRight()
                    .Text($"{ctx.CurrencySymbol} {ctx.Fmt(x => x.NetSettlement)}").FontSize(11).Bold();
            });

        var words = NumberToWordsHelper.ToCurrencyWords(
            ctx.Record.NetSettlement,
            ctx.CurrencySingular, ctx.CurrencyPlural,
            ctx.CentsSingular, ctx.CentsPlural);

        col.Item().PaddingTop(4).Row(row =>
        {
            row.ConstantItem(130).Text("CANTIDAD EN LETRAS:").FontSize(8).Bold();
            row.RelativeItem().Text(words).FontSize(8).SemiBold();
        });
    }

    // ── LEGAL ─────────────────────────────────────────────────────
    private static void BuildLegalBlock(ColumnDescriptor col, PdfContext ctx)
    {
        col.Item().PaddingTop(10).Text(
            $"Por este medio hago constar que he recibido de {ctx.CompanyLegalName} la cantidad de " +
            $"{ctx.NetAmountInWords}, a mi entera satisfacción mi liquidación final, a la que tengo derecho " +
            $"según nuestras leyes. Eximiendo a {ctx.CompanyLegalName} de cualquier reclamo posterior. " +
            $"Finiquitando el vínculo laboral entre Empleador y Trabajador al firmar la presente liquidación " +
            $"y otorgando el más amplio y total finiquito a favor.")
            .FontSize(7.5f).FontColor(Colors.Grey.Darken2).LineHeight(1.5f);
    }

    // ── SIGNATURES ────────────────────────────────────────────────
    private static void BuildSignatureLines(ColumnDescriptor col, PdfContext ctx)
    {
        // Two rows of two signatures each
        void SigPair(string l1, string n1, string l2, string n2)
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(sc =>
                {
                    sc.Item().PaddingTop(25).Width(200).BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1);
                    sc.Item().PaddingTop(2).Text(l1).FontSize(7).FontColor(Colors.Grey.Darken1);
                    if (!string.IsNullOrEmpty(n1)) sc.Item().Text(n1).FontSize(7).SemiBold();
                });
                row.ConstantItem(20);
                row.RelativeItem().Column(sc =>
                {
                    sc.Item().PaddingTop(25).Width(200).BorderBottom(0.5f).BorderColor(Colors.Grey.Darken1);
                    sc.Item().PaddingTop(2).Text(l2).FontSize(7).FontColor(Colors.Grey.Darken1);
                    if (!string.IsNullOrEmpty(n2)) sc.Item().Text(n2).FontSize(7).SemiBold();
                });
            });
        }

        SigPair("ELABORADO POR:", "", "REVISADO POR:", "");
        col.Item().PaddingTop(10);
        SigPair("RECIBÍ CONFORME:", $"{ctx.EmployeeName}\n{ctx.IdentificationNumber}",
                "AUTORIZADO POR:", "");
    }

    // ══════════════════════════════════════════════════════════════
    //  CONTEXT
    // ══════════════════════════════════════════════════════════════

    private sealed class PdfContext
    {
        public TerminationRecord Record { get; }
        public Employee Employee { get; }
        public Company Company { get; }

        public PdfContext(TerminationRecord record, Employee employee, Company company)
        {
            Record = record;
            Employee = employee;
            Company = company;
        }

        public string Fmt(Func<TerminationRecord, decimal> selector) =>
            selector(Record).ToString("N2", CultureInfo.InvariantCulture);

        public string Pct(decimal rate) =>
            (rate * 100).ToString("0.##", CultureInfo.InvariantCulture);

        public string ShortDate(DateOnly d) => d.ToString("dd/MMM/yy", new CultureInfo("es-NI"));

        private static readonly Dictionary<string, (string Symbol, string S, string P, string CS, string CP)> CurrencyMap = new()
        {
            ["NIC"] = ("C$", "Córdoba", "Córdobas", "Centavo", "Centavos"),
            ["HND"] = ("L", "Lempira", "Lempiras", "Centavo", "Centavos"),
            ["SLV"] = ("US$", "Dólar", "Dólares", "Centavo", "Centavos"),
            ["GTM"] = ("Q", "Quetzal", "Quetzales", "Centavo", "Centavos"),
            ["CRI"] = ("₡", "Colón", "Colones", "Céntimo", "Céntimos"),
            ["PAN"] = ("B/.", "Balboa", "Balboas", "Céntimo", "Céntimos"),
            ["USD"] = ("US$", "Dólar", "Dólares", "Centavo", "Centavos"),
        };

        private static readonly Dictionary<string, string> SsEntityMap = new()
        {
            ["NIC"] = "INSS", ["CRI"] = "CCSS", ["PAN"] = "CSS",
            ["HND"] = "IHSS", ["SLV"] = "ISSS", ["GTM"] = "IGSS",
        };

        public string CurrencySymbol =>
            CurrencyMap.TryGetValue(Record.CountryCode, out var c) ? c.Symbol : Record.Currency;
        public string CurrencySingular =>
            CurrencyMap.TryGetValue(Record.CountryCode, out var c) ? c.S : "Unidad";
        public string CurrencyPlural =>
            CurrencyMap.TryGetValue(Record.CountryCode, out var c) ? c.P : "Unidades";
        public string CentsSingular =>
            CurrencyMap.TryGetValue(Record.CountryCode, out var c) ? c.CS : "Centavo";
        public string CentsPlural =>
            CurrencyMap.TryGetValue(Record.CountryCode, out var c) ? c.CP : "Centavos";
        public string SsEntityName =>
            SsEntityMap.TryGetValue(Record.CountryCode, out var e) ? e : "Seguro Social";

        public string EmployeeName => $"{Employee.FirstName} {Employee.LastName}".Trim();
        public string IdentificationNumber => Employee.IdentificationNumber ?? "—";
        public string Position => Employee.Position ?? "—";
        public decimal HourlySalary => Record.DailySalary > 0 ? Record.DailySalary / 8m : 0m;

        public string SalaryTypeLabel => Employee.SalaryType?.ToLowerInvariant() switch
        {
            "monthly" => "Fijo",
            "variable" => "Variable",
            "daily" => "Fijo",
            "hourly" => "Variable",
            _ => "Fijo"
        };

        public string ReasonLabel => Record.Reason switch
        {
            TerminationReason.VoluntaryResignation => "Renuncia con 15 días",
            TerminationReason.ImmediateResignation => "Renuncia Inmediata",
            TerminationReason.JustifiedDismissal => "Despido",
            TerminationReason.UnjustifiedDismissal => "Art. 48 Código del Trabajo - Causa Justa (Autorizado por el MITRAB)",
            TerminationReason.MutualAgreement => "Mutuo Acuerdo",
            TerminationReason.EndOfContract => "Fin de Contrato",
            _ => Record.Reason.ToString()
        };

        private static readonly CultureInfo EsNi = new("es-NI");
        public string ElabDate => DateTime.Now.ToString("dddd, d 'de' MMMM 'de' yyyy", EsNi);
        public string HireDate => Record.HireDate.ToString("dddd, d 'de' MMMM 'de' yyyy", EsNi);
        public string TerminationDate => Record.TerminationDate.ToString("dddd, d 'de' MMMM 'de' yyyy", EsNi);

        public string CompanyLegalName => string.IsNullOrEmpty(Company.LegalName) ? Company.Name : Company.LegalName;

        public string NetAmountInWords => NumberToWordsHelper.ToCurrencyWords(
            Record.NetSettlement, CurrencySingular, CurrencyPlural, CentsSingular, CentsPlural);
    }
}
