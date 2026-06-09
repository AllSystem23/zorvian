using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Zorvian.Application.DTOs.Report;
using Zorvian.Application.Interfaces;

namespace Zorvian.Infrastructure.Services;

public sealed class ReportExportService : IReportExportService
{
    public ReportExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] ExportToExcel(ReportResult result, string title)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(title.Length > 31 ? title[..31] : title);

        for (int c = 0; c < result.Columns.Count; c++)
        {
            ws.Cell(1, c + 1).Value = result.Columns[c];
            ws.Cell(1, c + 1).Style.Font.Bold = true;
            ws.Cell(1, c + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        for (int r = 0; r < result.Rows.Count; r++)
        {
            var row = result.Rows[r];
            for (int c = 0; c < result.Columns.Count; c++)
            {
                var cell = ws.Cell(r + 2, c + 1);
                var colName = result.Columns[c];
                var val = row.GetValueOrDefault(colName);

                switch (val)
                {
                    case decimal d:
                        cell.Value = (double)d;
                        cell.Style.NumberFormat.Format = "#,##0.00";
                        break;
                    case int i:
                        cell.Value = i;
                        break;
                    case DateTime dt:
                        cell.Value = dt.ToString("dd/MM/yyyy HH:mm");
                        break;
                    case DateOnly doVal:
                        cell.Value = doVal.ToString("dd/MM/yyyy");
                        break;
                    case bool b:
                        cell.Value = b ? "Sí" : "No";
                        break;
                    default:
                        cell.Value = val?.ToString() ?? "";
                        break;
                }
            }
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportToPdf(ReportResult result, string title)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().Text(title).FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                    col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingBottom(10);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cd =>
                    {
                        var w = result.Columns.Count > 0 ? 100f / result.Columns.Count : 100f;
                        foreach (var _ in result.Columns)
                            cd.RelativeColumn(w);
                    });

                    table.Header(header =>
                    {
                        foreach (var col in result.Columns)
                        {
                            var cell = header.Cell();
                            cell.Padding(3).Text(col).Bold().FontSize(8);
                        }
                    });

                    foreach (var row in result.Rows)
                    {
                        foreach (var col in result.Columns)
                        {
                            var val = row.GetValueOrDefault(col);
                            table.Cell().Padding(2).Text(val?.ToString() ?? "").FontSize(7);
                        }
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Página ").FontSize(8);
                    x.CurrentPageNumber().FontSize(8);
                });
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return stream.ToArray();
    }
}
