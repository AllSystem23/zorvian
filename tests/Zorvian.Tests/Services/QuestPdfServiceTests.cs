using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Zorvian.Infrastructure.Services;

namespace Zorvian.Tests.Services;

/// <summary>
/// Pruebas de humo del pipeline HTML→QuestPDF. El detalle tipográfico lo cubre
/// <see cref="HtmlCssStylesheetTests"/>; aquí se garantiza que el documento real
/// (tablas, firmas en flex, listas, justificación) se renderiza sin excepciones y
/// produce un PDF válido.
/// </summary>
public sealed class QuestPdfServiceTests
{
    private readonly QuestPdfService _sut;

    public QuestPdfServiceTests()
    {
        _sut = new QuestPdfService(new Mock<ILogger<QuestPdfService>>().Object);
    }

    private static void AssertValidPdf(byte[] pdf)
    {
        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 1000, $"El PDF es sospechosamente pequeño ({pdf.Length} bytes).");
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        var tail = System.Text.Encoding.ASCII.GetString(pdf, pdf.Length - 16, 16);
        Assert.Contains("%%EOF", tail);
    }

    private const string LegalTemplate = """
        <!DOCTYPE html>
        <html><head><meta charset="UTF-8"><style>
          @page { margin: 2cm; }
          body { font-family: 'Segoe UI', Arial, sans-serif; margin: 40px; color: #1a1a2e; line-height: 1.7; text-align: justify; }
          p { text-align: justify; margin: 12px 0; }
          h2 { text-align: center; font-size: 18px; }
          .header { text-align: center; border-bottom: 3px solid #1A0A3E; padding-bottom: 20px; margin-bottom: 30px; }
          .header h1 { font-size: 22px; margin: 0; }
          .body-text { text-align: justify; text-indent: 40px; margin: 12px 0; }
          .closing { text-align: center; margin-top: 30px; font-style: italic; }
          .signature-area { margin-top: 90px; display: flex; justify-content: space-between; }
          .sig-block { text-align: center; width: 40%; }
          .sig-line { border-top: 1px solid #333; margin-top: 60px; padding-top: 6px; font-size: 11px; }
          table.items { width: 100%; border-collapse: collapse; margin: 20px 0; }
          table.items th { background: #1A0A3E; color: white; padding: 10px 12px; text-align: left; font-size: 13px; }
          table.items td { padding: 10px 12px; border-bottom: 1px solid #eee; font-size: 13px; }
          ul { margin: 8px 0 8px 18px; }
          .footer { margin-top: 40px; text-align: center; font-size: 10px; color: #888; border-top: 1px solid #ddd; }
        </style></head>
        <body>
          <div class="header">
            <h1>TIENDA BRIZUELA ROMERO #3</h1>
            <p>RUC 001-010190-0001X · Managua, Nicaragua</p>
          </div>

          <h2>Contrato de Trabajo</h2>

          <p class="body-text">Entre la empresa y el trabajador se celebra el presente contrato individual de
          trabajo, que se regirá por las disposiciones del Código del Trabajo y demás leyes aplicables. El trabajador
          prestará sus servicios en el cargo de Supervisor de Operaciones, con una jornada de lunes a sábado.</p>

          <p class="body-text">La remuneración mensual pactada se pagará por quincenas vencidas, y las deducciones
          legales de INSS e IR se aplicarán conforme a la tabla vigente. Ambas partes declaran conocer y aceptar las
          condiciones descritas en el presente instrumento.</p>

          <table class="items">
            <thead><tr><th>Descripción</th><th style="text-align:center;">Cant.</th><th style="text-align:right;">Subtotal</th></tr></thead>
            <tbody>
              <tr><td>Servicio de instalación y configuración</td><td style="text-align:center;">1</td><td style="text-align:right;">C$ 12,500.00</td></tr>
              <tr><td>Capacitación al personal (8 horas)</td><td style="text-align:center;">2</td><td style="text-align:right;">C$ 6,000.00</td></tr>
              <tr><td colspan="2" style="text-align:right;">Total</td><td style="text-align:right;">C$ 18,500.00</td></tr>
            </tbody>
          </table>

          <ul>
            <li>El trabajador se compromete a guardar confidencialidad.</li>
            <li>La empresa entregará los implementos necesarios.</li>
          </ul>

          <p class="closing">Se firma en la ciudad de Managua a los 24 días del mes de octubre de 2026.</p>

          <div class="signature-area">
            <div class="sig-block"><div class="sig-line">Empleador</div></div>
            <div class="sig-block"><div class="sig-line">Trabajador</div></div>
          </div>

          <div class="footer">Documento generado por el motor documental · Zorvian ERP</div>
        </body></html>
        """;

    [Fact]
    public void RenderHtmlToPdf_RealisticTemplate_ReturnsValidPdf()
    {
        var pdf = _sut.RenderHtmlToPdf(LegalTemplate);

        AssertValidPdf(pdf);
    }

    [Fact]
    public void RenderHtmlToPdf_IsDeterministicInLength()
    {
        var first = _sut.RenderHtmlToPdf(LegalTemplate);
        var second = _sut.RenderHtmlToPdf(LegalTemplate);

        // El mismo contenido debe producir un documento del mismo orden de tamaño
        // (sin depender de fuentes del sistema que cambien entre entornos).
        Assert.InRange(second.Length, first.Length * 0.9, first.Length * 1.1);
    }

    [Fact]
    public void RenderHtmlToPdf_EmptyContent_ReturnsFallbackPdf()
    {
        AssertValidPdf(_sut.RenderHtmlToPdf(string.Empty));
        AssertValidPdf(_sut.RenderHtmlToPdf("   "));
    }

    [Fact]
    public void RenderHtmlToPdf_LongContent_SpansMultiplePages()
    {
        var paragraphs = string.Concat(Enumerable.Repeat(
            "<p class=\"body-text\">Cláusula de prueba con suficiente texto para ocupar varias líneas y forzar " +
            "el salto de página del motor, verificando que las tablas y los bloques flex no rompan el layout.</p>",
            40));

        var pdf = _sut.RenderHtmlToPdf(
            "<html><head><style>p { text-align: justify; text-indent: 40px; margin: 12px 0; }</style></head>" +
            $"<body>{paragraphs}</body></html>");

        AssertValidPdf(pdf);
        Assert.True(pdf.Length > 4000, $"Se esperaba un documento multipágina ({pdf.Length} bytes).");
    }

    [Fact]
    public void RenderHtmlToPdf_TableWithoutHeaderAndWithNestedMarkup_DoesNotThrow()
    {
        const string html = """
            <html><body>
              <table>
                <tbody>
                  <tr><td><strong>Negrita</strong> y <em>cursiva</em> en una celda</td><td>2</td></tr>
                  <tr><td colspan="2"><span style="color:#7C4DFF;">Celda combinada</span></td></tr>
                </tbody>
              </table>
              <div style="display:flex; justify-content:space-between;">
                <div style="width:45%;">Izquierda</div>
                <div style="width:45%;">Derecha</div>
              </div>
            </body></html>
            """;

        AssertValidPdf(_sut.RenderHtmlToPdf(html));
    }
}
