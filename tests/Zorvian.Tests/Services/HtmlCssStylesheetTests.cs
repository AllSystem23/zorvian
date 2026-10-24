using AngleSharp.Html.Parser;
using QuestPDF.Infrastructure;
using Xunit;
using Zorvian.Infrastructure.Services;

namespace Zorvian.Tests.Services;

/// <summary>
/// La cascada CSS es la que devuelve al PDF la intención tipográfica de las
/// plantillas (justificación, sangría, jerarquía). Antes de esta capa el conversor
/// borraba los bloques &lt;style&gt; y todos los documentos salían alineados a la
/// izquierda: estas pruebas fijan ese contrato.
/// </summary>
public sealed class HtmlCssStylesheetTests
{
    private static CssComputedStyle StyleOf(string html, string selector)
    {
        var sheet = HtmlCssStylesheet.Parse(html);
        var document = new HtmlParser().ParseDocument(html);
        var element = document.QuerySelector(selector);

        Assert.NotNull(element);
        return sheet.Compute(element);
    }

    private static string Document(string css, string body) =>
        $"<html><head><style>{css}</style></head><body>{body}</body></html>";

    private static string Hex(Color? color) => color?.ToString().ToUpperInvariant() ?? "<null>";

    [Fact]
    public void Default_Style_JustifiesBodyText()
    {
        var style = StyleOf(Document("", "<p id='target'>Texto</p>"), "#target");

        Assert.Equal("justify", style.TextAlign);
        Assert.Equal(HtmlCssStylesheetDefaults.BodyFontSizePt, style.FontSizePt);
        Assert.Equal(HtmlCssStylesheetDefaults.BodyLineHeight, style.LineHeight);
        Assert.Equal("#1A1A2E", Hex(style.Color));
    }

    [Fact]
    public void TagRule_JustifiesParagraphs_WhenBodyDeclaresAnotherAlignment()
    {
        var html = Document(
            "body { text-align: left; } p { text-align: justify; }",
            "<p id='target'>Prosa legal que debe quedar justificada.</p><span id='inline'>x</span>");

        Assert.Equal("justify", StyleOf(html, "#target").TextAlign);
        Assert.Equal("left", StyleOf(html, "#inline").TextAlign);
    }

    [Fact]
    public void BodyTextClass_AppliesIndentAndMargins()
    {
        var html = Document(
            ".body-text { text-align: justify; text-indent: 40px; margin: 12px 0; }",
            "<div class='body-text' id='target'>Cláusula primera.</div>");

        var style = StyleOf(html, "#target");

        Assert.Equal("justify", style.TextAlign);
        Assert.Equal(30f, style.TextIndentPt, 3);
        Assert.Equal(9f, style.MarginTopPt, 3);
        Assert.Equal(9f, style.MarginBottomPt, 3);
        Assert.Equal(0f, style.PaddingLeftPt, 3);
    }

    [Fact]
    public void TypographicProperties_AreInheritedFromAncestors()
    {
        var html = Document(
            ".header { text-align: center; font-size: 13px; } .header h1 { font-size: 20px; }",
            "<div class='header'><h1 id='title'><span id='accent'>Título</span></h1></div>");

        var title = StyleOf(html, "#title");
        Assert.Equal("center", title.TextAlign);
        Assert.Equal(15f, title.FontSizePt, 3);
        Assert.True(title.FontSizeDeclared);

        // El span no declara nada: hereda centrado y el tamaño del h1 (no el del div).
        var accent = StyleOf(html, "#accent");
        Assert.Equal("center", accent.TextAlign);
        Assert.Equal(15f, accent.FontSizePt, 3);
        Assert.False(accent.FontSizeDeclared);
    }

    [Fact]
    public void InlineStyle_WinsOverStylesheet()
    {
        var html = Document(
            ".body-text { text-align: justify; text-indent: 40px; }",
            "<p class='body-text' id='target' style='text-align:center; text-indent: 0;'>Firma</p>");

        var style = StyleOf(html, "#target");
        Assert.Equal("center", style.TextAlign);
        Assert.Equal(0f, style.TextIndentPt, 3);
    }

    [Fact]
    public void Specificity_IdBeatsClass_AndLaterRuleWinsOnTie()
    {
        var html = Document(
            "#title { color: #ff0000; } .title { color: #00ff00; } p { color: #0000ff; }",
            "<p class='title' id='title'>x</p>");

        Assert.Equal("#FF0000", Hex(StyleOf(html, "#title").Color));

        var tie = Document(
            ".first { color: #112233; } .second { color: #445566; }",
            "<span class='first second' id='target'>x</span>");

        Assert.Equal("#445566", Hex(StyleOf(tie, "#target").Color));
    }

    [Fact]
    public void MediaQueriesAndPseudos_AreIgnored()
    {
        var html = Document(
            "@media print { p { text-align: center; } } " +
            "p::before { content: 'x'; text-align: right; } " +
            "p:hover { text-align: right; } " +
            "p { text-align: justify; }",
            "<p id='target'>x</p>");

        Assert.Equal("justify", StyleOf(html, "#target").TextAlign);
    }

    [Fact]
    public void TableHeaderRule_ProducesHeaderLookAndCellSpacing()
    {
        var html = Document(
            "table.items th { background: #1A0A3E; color: white; padding: 10px 12px; text-align: left; font-size: 13px; }",
            "<table class='items'><thead><tr><th id='target'>Descripción</th></tr></thead><tbody><tr><td>x</td></tr></tbody></table>");

        var style = StyleOf(html, "#target");

        Assert.Equal("#1A0A3E", Hex(style.Background));
        Assert.True(style.ColorDeclared);
        Assert.Equal("#FFFFFF", Hex(style.Color));
        Assert.Equal(9.75f, style.FontSizePt, 3);
        Assert.Equal("left", style.TextAlign);
        Assert.Equal(7.5f, style.PaddingTopPt, 3);
        Assert.Equal(7.5f, style.PaddingBottomPt, 3);
        Assert.Equal(9f, style.PaddingLeftPt, 3);
        Assert.Equal(9f, style.PaddingRightPt, 3);
    }

    [Fact]
    public void DescendantSelector_MatchesNestedCells()
    {
        var html = Document(
            "table.items td { padding: 10px 12px; border-bottom: 1px solid #eee; font-size: 13px; }",
            "<table class='items'><tbody><tr><td id='target'>Fila</td></tr></tbody></table>");

        var style = StyleOf(html, "#target");

        Assert.Equal(9.75f, style.FontSizePt, 3);
        Assert.NotNull(style.BorderBottom);
        Assert.Equal(0.75f, style.BorderBottom!.Value.WidthPt, 3);
    }

    [Fact]
    public void BorderShorthand_IsParsedWithWidthInPointsAndColor()
    {
        var html = Document(
            ".header { border-bottom: 3px solid #1A0A3E; padding-bottom: 20px; text-align: center; }",
            "<div class='header' id='target'>Empresa</div>");

        var style = StyleOf(html, "#target");

        Assert.Equal("center", style.TextAlign);
        Assert.NotNull(style.BorderBottom);
        Assert.Equal(2.25f, style.BorderBottom!.Value.WidthPt, 3);
        Assert.Equal("#1A0A3E", Hex(style.BorderBottom.Value.Color));
        Assert.Equal(15f, style.PaddingBottomPt, 3);
        Assert.Null(style.BorderTop);
    }

    [Fact]
    public void FlexContainers_ExposeLayoutHints()
    {
        var html = Document(
            ".sig-area { display: flex; justify-content: space-between; margin-top: 80px; gap: 12px; } " +
            ".sig-block { width: 40%; text-align: center; }",
            "<div class='sig-area' id='area'><div class='sig-block' id='block'>Firma</div></div>");

        var area = StyleOf(html, "#area");
        Assert.True(area.IsFlex);
        Assert.Equal("space-between", area.JustifyContent);
        Assert.Equal(60f, area.MarginTopPt, 3);
        Assert.Equal(9.0, (double)area.GapPt!.Value, 3);

        var block = StyleOf(html, "#block");
        Assert.Equal(40.0, (double)block.WidthPercent!.Value, 3);
        Assert.Equal("center", block.TextAlign);
    }

    [Fact]
    public void LineHeight_AcceptsFactorAndAbsoluteValue()
    {
        var html = Document(
            "body { line-height: 1.7; } .tight { line-height: 26px; font-size: 13px; }",
            "<p id='body'>x</p><p class='tight' id='tight'>x</p>");

        Assert.Equal(1.7, (double)StyleOf(html, "#body").LineHeight!.Value, 3);
        // 26px = 19.5pt contra un font-size efectivo de 13px (9.75pt) → factor 2.0,
        // sin importar que line-height se declare antes o después del font-size.
        Assert.Equal(2.0, (double)StyleOf(html, "#tight").LineHeight!.Value, 3);
    }

    [Fact]
    public void FontWeightAndStyle_AreDetected()
    {
        var html = Document(
            ".bold { font-weight: 700; } .italic { font-style: italic; } .normal { font-weight: normal; }",
            "<span class='bold' id='bold'>x</span><span class='italic' id='italic'>x</span>" +
            "<span class='bold normal' id='normal'>x</span>");

        Assert.True(StyleOf(html, "#bold").Bold);
        Assert.True(StyleOf(html, "#bold").BoldDeclared);
        Assert.True(StyleOf(html, "#italic").Italic);
        Assert.False(StyleOf(html, "#normal").Bold);
    }

    [Fact]
    public void Colors_SupportHexRgbAndNames()
    {
        var html = Document(
            ".hex { color: #7c4dff; } .rgb { color: rgb(26, 10, 62); } .named { color: navy; } " +
            ".bg { background-color: whitesmoke; }",
            "<span class='hex' id='hex'>x</span><span class='rgb' id='rgb'>x</span>" +
            "<span class='named' id='named'>x</span><span class='bg' id='bg'>x</span>");

        Assert.Equal("#7C4DFF", Hex(StyleOf(html, "#hex").Color));
        Assert.Equal("#1A0A3E", Hex(StyleOf(html, "#rgb").Color));
        Assert.Equal("#000080", Hex(StyleOf(html, "#named").Color));
        Assert.Equal("#F5F5F5", Hex(StyleOf(html, "#bg").Background));
    }

    [Fact]
    public void MarginShorthand_SupportsFourValues()
    {
        var html = Document(
            ".box { margin: 10px 20px 30px 40px; }",
            "<div class='box' id='target'>x</div>");

        var style = StyleOf(html, "#target");
        Assert.Equal(7.5f, style.MarginTopPt, 3);
        Assert.Equal(22.5f, style.MarginBottomPt, 3);
    }

    [Fact]
    public void RootMargins_AreIgnoredToKeepThePageBox()
    {
        var html = Document(
            "body { margin: 40px; } p { margin: 0; }",
            "<p id='target'>x</p>");

        var body = StyleOf(html, "body");
        Assert.Equal(0f, body.MarginTopPt, 3);
        Assert.Equal(0f, body.MarginBottomPt, 3);
    }

    [Fact]
    public void LetterSpacing_IsConvertedToPoints()
    {
        var html = Document(
            ".kicker { letter-spacing: 2px; }",
            "<span class='kicker' id='target'>x</span>");

        Assert.Equal(1.5f, StyleOf(html, "#target").LetterSpacingPt!.Value, 3);
    }

    [Fact]
    public void EmptyMarkup_FallsBackToProfessionalDefaults()
    {
        foreach (var sheet in new[] { HtmlCssStylesheet.Empty, HtmlCssStylesheet.Parse(null), HtmlCssStylesheet.Parse("   ") })
        {
            var document = new HtmlParser().ParseDocument("<html><body><p id='target'>x</p></body></html>");
            var style = sheet.Compute(document.QuerySelector("#target")!);

            Assert.Equal("justify", style.TextAlign);
            Assert.Equal(10f, style.FontSizePt, 3);
        }
    }
}
