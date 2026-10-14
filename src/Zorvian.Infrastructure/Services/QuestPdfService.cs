using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Zorvian.Application.Interfaces;
using QUnit = QuestPDF.Infrastructure.Unit;

namespace Zorvian.Infrastructure.Services;

public sealed class QuestPdfService
{
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
    /// Renderiza HTML a PDF. Antes se intentaba DinkToPdf (wkhtmltopdf) con fallback a
    /// QuestPDF, pero DinkToPdf necesita libwkhtmltox.so, que el contenedor de Render no
    /// instala: el camino siempre lanzaba y caía al fallback. Además arrastra
    /// NETStandard.Library 1.6.0 y rompe el restore con --runtime linux-x64 (NU1605).
    /// </summary>
    public byte[] RenderHtmlToPdf(string htmlContent)
    {
        return RenderWithQuestPdf(htmlContent);
    }

    private byte[] RenderWithQuestPdf(string htmlContent)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, QUnit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Verdana));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "logo.png");
                        if (File.Exists(logoPath))
                        {
                            col.Item().Height(40).Image(logoPath);
                        }
                        else
                        {
                            col.Item().Text("ZORVIAN").FontSize(24).Bold().FontColor("#1e3a8a");
                        }
                        col.Item().Text("DOCENGINE / ENTERPRISE").LetterSpacing(0.2f).FontSize(9).FontColor(Colors.Grey.Medium);
                    });

                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text(DateTime.Now.ToString("dd MMMM yyyy")).FontSize(10);
                        col.Item().Text("Documento Certificado").FontSize(8).Italic().FontColor(Colors.Blue.Medium);
                    });
                });

                page.Content().PaddingVertical(1, QUnit.Centimetre).Column(col =>
                {
                    col.Item().PaddingBottom(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                    col.Item().PaddingTop(1, QUnit.Centimetre).Text(StripHtml(htmlContent)).LineHeight(1.5f);
                    col.Item().AlignCenter().PaddingTop(2, QUnit.Centimetre).Text("CONFIDENCIAL - ZORVIAN ERP")
                        .FontSize(30).Bold().FontColor(Colors.Grey.Lighten4);
                });

                page.Footer().Column(col =>
                {
                    col.Item().BorderTop(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text(x =>
                        {
                            x.Span("Generado por ").FontSize(8);
                            x.Span("Zorvian Documentary Engine").FontSize(8).Bold();
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

    private static string StripHtml(string html)
    {
        var doc = new System.Text.StringBuilder();
        var inTag = false;
        foreach (var c in html)
        {
            if (c == '<') inTag = true;
            else if (c == '>') inTag = false;
            else if (!inTag) doc.Append(c);
        }
        return System.Net.WebUtility.HtmlDecode(doc.ToString()).Trim();
    }
}

