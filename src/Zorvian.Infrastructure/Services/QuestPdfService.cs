using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QUnit = QuestPDF.Infrastructure.Unit;

namespace Zorvian.Infrastructure.Services;

/// <summary>
/// Renderiza el HTML del motor documental a un PDF con presentación corporativa:
/// cabecera con identidad, marca de agua de confidencialidad, pie paginado y
/// tipografía única (Lato, incluida en QuestPDF) disponible también en contenedores
/// sin fuentes del sistema.
/// </summary>
public sealed class QuestPdfService
{
    private static readonly Color BrandNavy = Color.FromHex("#1A0A3E");
    private static readonly Color BrandViolet = Color.FromHex("#7C4DFF");
    private static readonly Color Divider = Color.FromHex("#E4E0EF");
    private static readonly Color Muted = Color.FromHex("#6B6785");
    private static readonly Color Watermark = Color.FromARGB(14, 0, 0, 0);
    private static readonly string[] SpanishMonths =
    {
        "enero", "febrero", "marzo", "abril", "mayo", "junio",
        "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre",
    };

    private readonly ILogger<QuestPdfService> _logger;

    static QuestPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public QuestPdfService(ILogger<QuestPdfService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Renderiza HTML a PDF profesional usando el conversor HTML→QuestPDF.
    /// Respetando negritas, cursivas, titulos, tablas, listas, alineaciones y saltos de seccion.
    /// </summary>
    public byte[] RenderHtmlToPdf(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            _logger.LogWarning(
                "RenderHtmlToPdf recibió contenido HTML vacío; se generará el documento de respaldo.");
        }

        var pdf = RenderWithQuestPdf(htmlContent);

        _logger.LogDebug("PDF generado correctamente ({SizeKb} KB).", pdf.Length / 1024d);

        return pdf;
    }

    private static byte[] RenderWithQuestPdf(string htmlContent)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, QUnit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x
                    .FontSize(10.5f)
                    .FontFamily(Fonts.Lato)
                    .FontColor("#1A1A2E")
                    .LineHeight(1.5f));

                page.Background()
                    .AlignCenter().AlignMiddle()
                    .Rotate(-30)
                    .Text("CONFIDENCIAL")
                    .FontSize(72).Bold().LetterSpacing(8).FontColor(Watermark);

                page.Header().Element(header =>
                {
                    header
                        .BorderBottom(0.8f).BorderColor(Divider)
                        .PaddingBottom(8)
                        .Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                var logoPath = System.IO.Path.Combine(
                                    System.IO.Directory.GetCurrentDirectory(),
                                    "wwwroot", "assets", "logo.png");

                                if (System.IO.File.Exists(logoPath))
                                    col.Item().Height(38).Image(logoPath);
                                else
                                    col.Item().Text("ZORVIAN").FontSize(22).Bold().FontColor(BrandNavy);

                                col.Item()
                                    .PaddingTop(2)
                                    .Text("SISTEMA DE GESTIÓN DOCUMENTAL EMPRESARIAL")
                                    .LetterSpacing(1.1f).FontSize(7).FontColor(Muted);
                            });

                            row.ConstantItem(180).AlignRight().Column(col =>
                            {
                                col.Item()
                                    .Text(FormatSpanishDate(System.DateTime.Now))
                                    .FontSize(9).FontColor(Muted);

                                col.Item()
                                    .PaddingTop(2)
                                    .Text("DOCUMENTO CONFIDENCIAL")
                                    .Bold().LetterSpacing(0.6f).FontSize(7).FontColor(BrandViolet);
                            });
                        });
                });

                page.Content().PaddingVertical(0.9f, QUnit.Centimetre).Element(content =>
                {
                    HtmlToQuestPdfConverter.RenderDocument(content, htmlContent);
                });

                page.Footer().Element(footer =>
                {
                    footer
                        .BorderTop(0.5f).BorderColor(Divider)
                        .PaddingTop(6)
                        .Row(row =>
                        {
                            row.RelativeItem().Text(text =>
                            {
                                text.Span("Generado por ").FontSize(7.5f).FontColor(Muted);
                                text.Span("Zorvian ERP · Motor Documental")
                                    .FontSize(7.5f).SemiBold().FontColor(BrandNavy);
                            });

                            row.ConstantItem(120).AlignRight().Text(text =>
                            {
                                text.Span("Página ").FontSize(7.5f).FontColor(Muted);
                                text.CurrentPageNumber().FontSize(7.5f).SemiBold().FontColor(BrandNavy);
                                text.Span(" de ").FontSize(7.5f).FontColor(Muted);
                                text.TotalPages().FontSize(7.5f).SemiBold().FontColor(BrandNavy);
                            });
                        });
                });
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return stream.ToArray();
    }

    private static string FormatSpanishDate(System.DateTime date) =>
        $"{date.Day} de {SpanishMonths[date.Month - 1]} de {date.Year}";
}
