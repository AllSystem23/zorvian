using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Zorvian.Application.DTOs.Fleet;

namespace Zorvian.Infrastructure.Services;

/// <summary>
/// Generates PDF reports with visual chart representations for fleet data.
/// Uses QuestPDF fluent API to create bar charts, KPI cards, and data tables.
/// </summary>
public sealed class FleetPdfChartService
{
    static FleetPdfChartService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateExpenseByAccountPdf(ExpenseByAccountReport report, DateTime? startDate, DateTime? endDate)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                // ── Header ──
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("ZORVIAN ERP").FontSize(20).Bold().FontColor("#1A0A3E");
                        col.Item().Text("Gastos por Cuenta Contable — Reporte con Gráficos").FontSize(12).FontColor("#7C4DFF");
                    });

                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        var dateRange = startDate != null && endDate != null
                            ? $"{startDate:dd/MM/yyyy} — {endDate:dd/MM/yyyy}"
                            : "Todo el período";
                        col.Item().Text(dateRange).FontSize(10).FontColor(Colors.Grey.Medium);
                        col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });

                // ── Content ──
                page.Content().PaddingVertical(0.5f, Unit.Centimetre).Column(col =>
                {
                    // KPI Summary Row
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(kpi =>
                        {
                            kpi.Item().Text("Total Gastos").FontSize(9).FontColor(Colors.Grey.Medium);
                            kpi.Item().Text($"C$ {report.GrandTotal:N2}").FontSize(16).Bold().FontColor("#FF6D00");
                            kpi.Item().Text($"{report.TotalExpenses} gastos").FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                        row.ConstantItem(10);
                        row.RelativeItem().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(kpi =>
                        {
                            kpi.Item().Text("Aprobados").FontSize(9).FontColor(Colors.Grey.Medium);
                            kpi.Item().Text($"{report.TotalApproved}").FontSize(16).Bold().FontColor("#2EE59D");
                            kpi.Item().Text($"{(report.TotalExpenses > 0 ? report.TotalApproved * 100 / report.TotalExpenses : 0)}% del total").FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                        row.ConstantItem(10);
                        row.RelativeItem().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(kpi =>
                        {
                            kpi.Item().Text("Pendientes").FontSize(9).FontColor(Colors.Grey.Medium);
                            kpi.Item().Text($"{report.TotalPending}").FontSize(16).Bold().FontColor("#FFD54F");
                            kpi.Item().Text($"{(report.TotalExpenses > 0 ? report.TotalPending * 100 / report.TotalExpenses : 0)}% del total").FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                        row.ConstantItem(10);
                        row.RelativeItem().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(kpi =>
                        {
                            var accounts = report.Accounts.Count;
                            kpi.Item().Text("Cuentas Contables").FontSize(9).FontColor(Colors.Grey.Medium);
                            kpi.Item().Text($"{accounts}").FontSize(16).Bold().FontColor("#7C4DFF");
                            kpi.Item().Text("con gastos clasificados").FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                    });

                    col.Item().PaddingVertical(10);

                    // Bar Chart Section
                    if (report.Accounts.Count > 0)
                    {
                        col.Item().Text("Distribución por Cuenta Contable").FontSize(13).Bold().FontColor("#1A0A3E");
                        col.Item().PaddingBottom(8);

                        var maxAmount = report.Accounts.Max(a => a.TotalAmount);

                        foreach (var account in report.Accounts)
                        {
                            var fraction = maxAmount > 0 ? (double)(account.TotalAmount / maxAmount) : 0;
                            var pctLabel = report.GrandTotal > 0
                                ? $"{account.TotalAmount / report.GrandTotal * 100:N1}%"
                                : "0%";

                            col.Item().Row(row =>
                            {
                                // Account label (fixed width)
                                row.ConstantItem(140).Column(c =>
                                {
                                    var label = account.AccountCode;
                                    if (!string.IsNullOrEmpty(account.AccountName))
                                        label = account.AccountCode.Length > 10
                                            ? account.AccountCode[..10]
                                            : account.AccountCode;
                                    c.Item().Text(label).FontSize(9).Bold().FontColor("#1A0A3E");
                                });

                                // Bar (flexible)
                                row.RelativeItem().Height(22).Row(barRow =>
                                {
                                    var barWidth = (float)Math.Max(0.01, fraction);
                                    var emptyWidth = (float)Math.Max(0.01, 1 - fraction);
                                    barRow.RelativeItem(barWidth).Background("#FF6D00");
                                    barRow.RelativeItem(emptyWidth).Background(Colors.Grey.Lighten4);
                                    barRow.ConstantItem(10);
                                    barRow.ConstantItem(70).AlignRight().Text($"C$ {account.TotalAmount:N0}").FontSize(8).FontColor(Colors.Grey.Darken1);
                                    barRow.ConstantItem(5);
                                    barRow.ConstantItem(45).AlignRight().Text(pctLabel).FontSize(8).Bold().FontColor("#FF6D00");
                                });
                            });

                            col.Item().PaddingBottom(4);

                            // Sub-labels (approved/pending counts)
                            col.Item().PaddingLeft(150).Text(text =>
                            {
                                text.Span($"{account.ApprovedCount} aprobados  ").FontSize(7).FontColor("#2EE59D");
                                text.Span($"{account.PendingCount} pendientes  ").FontSize(7).FontColor("#FFD54F");
                                text.Span(account.AccountName).FontSize(7).FontColor(Colors.Grey.Medium);
                            });

                            col.Item().PaddingBottom(6);
                        }
                    }

                    // Watermark
                    col.Item().AlignCenter().PaddingTop(1, Unit.Centimetre)
                        .Text("CONFIDENCIAL — ZORVIAN ERP")
                        .FontSize(24).Bold().FontColor(Colors.Grey.Lighten4);
                });

                // ── Footer ──
                page.Footer().Column(col =>
                {
                    col.Item().BorderTop(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text(x =>
                        {
                            x.Span("Generado por ").FontSize(8);
                            x.Span("Zorvian ERP").FontSize(8).Bold();
                        });
                        row.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Página ").FontSize(8);
                            x.CurrentPageNumber().FontSize(8);
                            x.Span(" de ").FontSize(8);
                            x.TotalPages().FontSize(8);
                        });
                    });
                });
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return stream.ToArray();
    }
}
