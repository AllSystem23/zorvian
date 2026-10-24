using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Elements.Table;
using AngleSharpElement = AngleSharp.Dom.IElement;
using AngleSharpNode = AngleSharp.Dom.INode;
using AngleSharpText = AngleSharp.Dom.IText;

namespace Zorvian.Infrastructure.Services;

/// <summary>
/// Convierte el HTML renderizado por el motor documental (Liquid) a un documento
/// profesional de QuestPDF aplicando la cascada declarada en los bloques
/// <c>&lt;style&gt;</c> de la plantilla (ver <see cref="HtmlCssStylesheet"/>).
/// <para>
/// Preserva: alineación (incluida la justificación con sangría de primera línea),
/// negritas, cursivas, subrayados, tamaños y colores, encabezados jerárquicos,
/// tablas con cabecera repetida y celdas alineadas, listas, bordes, fondos y
/// bloques <c>display:flex</c> (firmas y tarjetas de metadatos).
/// </para>
/// </summary>
public sealed class HtmlToQuestPdfConverter
{
    private static readonly Regex _styleBlockRegex = new(
        @"<style\b[^>]*>.*?</style>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex _scriptBlockRegex = new(
        @"<script\b[^>]*>.*?</script>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex _commentRegex = new(
        @"<!--.*?-->",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private const float TableFontSize = 8.75f;
    private const float TableMinPaddingVertical = 3.5f;
    private const float TableMinPaddingHorizontal = 5f;
    private const float MaxBoxSpacingPt = 24f;
    private const float FlexDefaultSpacingPt = 12f;
    private const string BaseFontFamily = Fonts.Lato;

    private static readonly Color BrandNavy = Color.FromHex("#1A0A3E");
    private static readonly Color DefaultTextColor = Color.FromHex("#1A1A2E");
    private static readonly Color RuleColor = Colors.Grey.Lighten2;

    public static void RenderDocument(IContainer container, string html)
    {
        var body = ParseBody(html);
        if (body == null)
        {
            container.Text("Documento generado por el motor documental de Zorvian ERP.")
                .FontSize(HtmlCssStylesheetDefaults.BodyFontSizePt)
                .FontColor(DefaultTextColor);
            return;
        }

        var context = new RenderContext(HtmlCssStylesheet.Parse(html));

        container.Column(col =>
        {
            foreach (var child in body.ChildNodes)
                RenderNode(col, child, context);
        });
    }

    private static void RenderNode(ColumnDescriptor col, AngleSharpNode? node, RenderContext context)
    {
        switch (node)
        {
            case AngleSharpText text:
                if (string.IsNullOrWhiteSpace(text.TextContent)) return;
                col.Item().Element(item => RenderTextBlock(item, text, context));
                return;

            case AngleSharpElement element:
                RenderElement(col, element, context);
                return;
        }
    }

    private static void RenderTextBlock(IContainer container, AngleSharpNode source, RenderContext context)
    {
        var parent = source.ParentElement;
        var style = parent != null
            ? context.StyleOf(parent)
            : CssComputedStyle.CreateDefault();

        var fontSize = style.FontSizeDeclared
            ? style.FontSizePt
            : HtmlCssStylesheetDefaults.BodyFontSizePt;

        var box = ApplyBox(container, style);
        box.Text(text => ComposeText(
            text,
            source,
            context,
            CreateRunStyle(style, fontSize),
            style.TextAlign ?? "left",
            style.TextIndentPt));
    }

    private static void RenderElement(ColumnDescriptor col, AngleSharpElement element, RenderContext context)
    {
        var tag = NormalizedTag(element.TagName);

        switch (tag)
        {
            case "script":
            case "style":
            case "head":
            case "title":
            case "meta":
            case "link":
                return;

            case "br":
                col.Item().Height(4);
                return;

            case "hr":
                col.Item().PaddingVertical(6).LineHorizontal(0.8f).LineColor(RuleColor);
                return;

            case "table":
                col.Item().Element(item => RenderTable(item, element, context));
                return;

            case "ul":
            case "ol":
                col.Item().Element(item => RenderList(item, element, context, tag == "ol"));
                return;

            default:
                col.Item().Element(item => RenderBlock(item, element, context));
                return;
        }
    }

    private static void RenderBlock(IContainer container, AngleSharpElement element, RenderContext context)
    {
        var tag = NormalizedTag(element.TagName);
        var style = context.StyleOf(element);

        // Un título nunca se justifica: la justificación solo tiene sentido en prosa.
        var isHeading = IsHeading(tag);
        var box = ApplyBox(container, style);

        if (isHeading) box = box.EnsureSpace(48);

        if (style.IsFlex && element.Children.Length > 0)
        {
            RenderFlex(box, element, context, style);
            return;
        }

        if (HasBlockChildren(element))
        {
            box.Column(col =>
            {
                foreach (var child in element.ChildNodes)
                    RenderNode(col, child, context);
            });
            return;
        }

        if (!HasVisibleContent(element))
        {
            RenderEmptyBox(box, style);
            return;
        }

        var fontSize = style.FontSizeDeclared
            ? style.FontSizePt
            : HeadingSize(tag) ?? HtmlCssStylesheetDefaults.BodyFontSizePt;

        var runStyle = CreateRunStyle(style, fontSize, forceBold: isHeading, forceItalic: tag is "em" or "i");
        var alignment = isHeading ? LeftIfJustified(style.TextAlign) : style.TextAlign ?? "left";

        box.Text(text => ComposeText(text, element, context, runStyle, alignment, style.TextIndentPt));
    }

    private static void RenderFlex(
        IContainer container,
        AngleSharpElement element,
        RenderContext context,
        CssComputedStyle parentStyle)
    {
        var children = element.Children;
        var spaceBetween = string.Equals(parentStyle.JustifyContent, "space-between", StringComparison.OrdinalIgnoreCase);

        container.Row(row =>
        {
            row.Spacing(parentStyle.GapPt ?? FlexDefaultSpacingPt);

            for (var index = 0; index < children.Length; index++)
            {
                var child = children[index];
                var childStyle = context.StyleOf(child);

                if (spaceBetween && index > 0 && !WantsWidth(childStyle))
                    row.RelativeItem();

                if (childStyle.WidthPercent is { } width)
                    row.RelativeItem(width).Element(item => RenderBlock(item, child, context));
                else if (childStyle.FlexGrow || HasBlockChildren(child))
                    row.RelativeItem().Element(item => RenderBlock(item, child, context));
                else
                    row.AutoItem().Element(item => RenderBlock(item, child, context));
            }
        });
    }

    private static bool WantsWidth(CssComputedStyle style) =>
        style.WidthPercent is not null || style.FlexGrow;

    private static void RenderEmptyBox(IContainer box, CssComputedStyle style)
    {
        var height = style.MinHeightPt is > 0 ? style.MinHeightPt.Value : 1f;
        box.Height(height);
    }

    private static void RenderList(
        IContainer container,
        AngleSharpElement list,
        RenderContext context,
        bool ordered)
    {
        var style = context.StyleOf(list);
        var box = ApplyBox(container, style);

        box.Column(col =>
        {
            var index = 0;
            foreach (var item in list.Children)
            {
                if (NormalizedTag(item.TagName) != "li") continue;
                index++;

                var itemStyle = context.StyleOf(item);
                var marker = ordered ? $"{index}." : "•";
                var spacing = Math.Clamp(itemStyle.MarginTopPt, 1f, 6f);
                var fontSize = itemStyle.FontSizeDeclared
                    ? itemStyle.FontSizePt
                    : HtmlCssStylesheetDefaults.BodyFontSizePt;

                col.Item()
                    .PaddingLeft(12)
                    .PaddingTop(spacing)
                    .Row(row =>
                    {
                        row.ConstantItem(14).Text(marker)
                            .FontFamily(BaseFontFamily)
                            .FontSize(fontSize)
                            .FontColor(itemStyle.Color ?? DefaultTextColor)
                            .LineHeight(itemStyle.LineHeight ?? HtmlCssStylesheetDefaults.BodyLineHeight);

                        row.RelativeItem().Text(text => ComposeText(
                            text,
                            item,
                            context,
                            CreateRunStyle(itemStyle, fontSize),
                            LeftIfJustified(itemStyle.TextAlign),
                            itemStyle.TextIndentPt));
                    });
            }
        });
    }

    private static void RenderTable(IContainer container, AngleSharpElement table, RenderContext context)
    {
        var style = context.StyleOf(table);
        var rows = GetTableRows(table);
        if (rows.Count == 0) return;

        var columns = 0;
        foreach (var row in rows)
            columns = Math.Max(columns, CellsOf(row).Sum(CellSpan));
        if (columns == 0) return;

        var box = ApplyBox(container, style);
        var weights = ComputeColumnWeights(rows, columns);

        var headerRows = rows.Where(IsHeaderRow).ToList();
        var bodyRows = rows.Where(row => !headerRows.Contains(row)).ToList();

        box.Table(descriptor =>
        {
            descriptor.ColumnsDefinition(definition =>
            {
                for (var column = 0; column < columns; column++)
                    definition.RelativeColumn(weights[column]);
            });

            if (headerRows.Count > 0)
            {
                descriptor.Header(header =>
                {
                    var used = 0;
                    foreach (var cell in CellsOf(headerRows[0]))
                    {
                        var span = Math.Clamp(CellSpan(cell), 1, columns);
                        var cellDescriptor = header.Cell();
                        if (span > 1) cellDescriptor.ColumnSpan((uint)span);
                        RenderCell(cellDescriptor, cell, context, true);
                        used += span;
                    }

                    for (; used < columns; used++)
                        header.Cell().Padding(TableMinPaddingVertical).Text(string.Empty);
                });
            }

            foreach (var row in bodyRows)
            {
                var isHeader = IsHeaderRow(row);
                var used = 0;

                foreach (var cell in CellsOf(row))
                {
                    var span = Math.Clamp(CellSpan(cell), 1, columns);
                    var cellDescriptor = descriptor.Cell();
                    if (span > 1) cellDescriptor.ColumnSpan((uint)span);
                    RenderCell(cellDescriptor, cell, context, isHeader);
                    used += span;
                }

                for (; used < columns; used++)
                    descriptor.Cell().Padding(TableMinPaddingVertical).Text(string.Empty);
            }
        });
    }

    /// <summary>
    /// Reparte el ancho de las columnas según el contenido (descripción ancha,
    /// importes estrechos) en lugar de anchos fijos que desbordaban la caja de texto.
    /// </summary>
    private static float[] ComputeColumnWeights(List<AngleSharpElement> rows, int columns)
    {
        var weights = Enumerable.Repeat(1f, columns).ToArray();

        foreach (var row in rows)
        {
            var index = 0;
            foreach (var cell in CellsOf(row))
            {
                var span = Math.Clamp(CellSpan(cell), 1, columns);
                if (span == 1 && index < columns)
                {
                    var length = cell.TextContent?.Trim().Length ?? 0;
                    var weight = Math.Clamp(length / 12f, 1f, 4f);
                    if (weight > weights[index]) weights[index] = weight;
                }

                index += span;
            }
        }

        return weights;
    }

    private static void RenderCell(
        ITableCellContainer descriptor,
        AngleSharpElement cell,
        RenderContext context,
        bool isHeader)
    {
        var style = context.StyleOf(cell);
        IContainer container = descriptor;

        var background = style.Background;
        if (background is null && isHeader) background = BrandNavy;
        if (background is { } fill) container = container.Background(fill);
        if (style.CornerRadiusPt is > 0) container = container.CornerRadius(style.CornerRadiusPt.Value);

        if (style.BorderTop is { } top) container = container.BorderTop(top.WidthPt).BorderColor(top.Color);
        if (style.BorderRight is { } right) container = container.BorderRight(right.WidthPt).BorderColor(right.Color);
        if (style.BorderLeft is { } left) container = container.BorderLeft(left.WidthPt).BorderColor(left.Color);
        if (style.BorderBottom is { } bottom)
            container = container.BorderBottom(bottom.WidthPt).BorderColor(bottom.Color);
        else
            container = container.BorderBottom(0.4f).BorderColor(RuleColor);

        var paddingVertical = Math.Max(TableMinPaddingVertical,
            Math.Max(style.PaddingTopPt, style.PaddingBottomPt));
        var paddingHorizontal = Math.Max(TableMinPaddingHorizontal,
            Math.Max(style.PaddingLeftPt, style.PaddingRightPt));

        container = container
            .PaddingVertical(paddingVertical)
            .PaddingHorizontal(paddingHorizontal);

        if (string.IsNullOrWhiteSpace(cell.TextContent) && !HasVisibleContent(cell))
            return;

        var fontSize = style.FontSizeDeclared ? style.FontSizePt : TableFontSize;
        Color color;
        if (background is not null && !style.ColorDeclared) color = Colors.White;
        else color = style.Color ?? DefaultTextColor;        var bold = style.BoldDeclared ? style.Bold : isHeader || style.Bold;
        var runStyle = new RunStyle(bold, style.Italic, false, false, color, fontSize,
            style.LetterSpacingPt, Math.Clamp(style.LineHeight ?? 1.35f, 1f, 1.6f));

        // En celdas la justificación no aplica: columnas estrechas producen calles de aire.
        var alignment = style.TextAlign is "center" or "right" ? style.TextAlign : "left";

        container.Text(text => ComposeText(text, cell, context, runStyle, alignment, 0f));
    }

    private static void ComposeText(
        TextDescriptor text,
        AngleSharpNode source,
        RenderContext context,
        RunStyle baseStyle,
        string alignment,
        float firstLineIndentPt)
    {
        ApplyAlignment(text, alignment);

        if (firstLineIndentPt > 0.2f)
        {
            text.ParagraphFirstLineIndentation(firstLineIndentPt);
        }

        var runs = new List<TextRun>();
        CollectRuns(source, context, baseStyle, runs);
        TrimRuns(runs);

        foreach (var run in runs)
            AddRun(text, run);
    }

    private static void ApplyAlignment(TextDescriptor text, string? alignment)
    {
        switch (alignment)
        {
            case "center":
                text.AlignCenter();
                break;
            case "right":
                text.AlignRight();
                break;
            case "justify":
                text.Justify();
                break;
            default:
                text.AlignLeft();
                break;
        }
    }

    private static void CollectRuns(
        AngleSharpNode node,
        RenderContext context,
        RunStyle current,
        List<TextRun> runs)
    {
        foreach (var child in node.ChildNodes)
        {
            switch (child)
            {
                case AngleSharpText text:
                    var value = NormalizeWhitespace(text.Data);
                    if (value.Length > 0) runs.Add(new TextRun(value, current));
                    break;

                case AngleSharpElement element:
                    var tag = NormalizedTag(element.TagName);
                    if (tag == "br")
                    {
                        runs.Add(TextRun.LineBreak);
                        break;
                    }

                    if (tag is "script" or "style") break;

                    var style = context.StyleOf(element);
                    var next = current with
                    {
                        Bold = style.BoldDeclared
                            ? style.Bold
                            : current.Bold || tag is "strong" or "b",
                        Italic = style.ItalicDeclared
                            ? style.Italic
                            : current.Italic || tag is "em" or "i",
                        Underline = current.Underline || tag is "u" or "ins",
                        Strike = current.Strike || tag is "s" or "strike" or "del",
                        Color = style.ColorDeclared ? style.Color ?? current.Color : current.Color,
                        Size = style.FontSizeDeclared
                            ? style.FontSizePt
                            : tag switch
                            {
                                "small" => current.Size * 0.85f,
                                "sub" or "sup" => current.Size * 0.72f,
                                _ => current.Size,
                            },
                        LetterSpacing = style.LetterSpacingPt ?? current.LetterSpacing,
                        LineHeight = style.LineHeight ?? current.LineHeight,
                    };

                    CollectRuns(element, context, next, runs);
                    break;
            }
        }
    }

    private static void AddRun(TextDescriptor text, TextRun run)
    {
        if (run.IsLineBreak)
        {
            text.Line(string.Empty);
            return;
        }

        var span = text.Span(run.Text);
        var style = run.Style;

        if (style.Bold) span.Bold();
        if (style.Italic) span.Italic();
        if (style.Underline) span.Underline();
        if (style.Strike) span.Strikethrough();

        span.FontFamily(BaseFontFamily)
            .FontColor(style.Color)
            .FontSize(style.Size);

        if (style.LineHeight is { } lineHeight) span.LineHeight(lineHeight);
        if (style.LetterSpacing is { } letterSpacing) span.LetterSpacing(letterSpacing);
    }

    private static void TrimRuns(List<TextRun> runs)
    {
        for (var index = 0; index < runs.Count; index++)
        {
            var run = runs[index];
            if (run.IsLineBreak) continue;
            runs[index] = run with { Text = run.Text.TrimStart() };
            break;
        }

        for (var index = runs.Count - 1; index >= 0; index--)
        {
            var run = runs[index];
            if (run.IsLineBreak) continue;
            runs[index] = run with { Text = run.Text.TrimEnd() };
            break;
        }

        runs.RemoveAll(run => !run.IsLineBreak && run.Text.Length == 0);

        for (var index = 1; index < runs.Count; index++)
        {
            var previous = runs[index - 1];
            var current = runs[index];
            if (previous.IsLineBreak || current.IsLineBreak) continue;
            if (!previous.Text.EndsWith(' ') || !current.Text.StartsWith(' ')) continue;
            runs[index] = current with { Text = current.Text.TrimStart() };
        }
    }

    private static string NormalizeWhitespace(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        var builder = new StringBuilder(value.Length);
        var previousWasSpace = false;

        foreach (var character in value)
        {
            if (character is '\u00A0' or '\u2007' or '\u202F')
            {
                builder.Append(' ');
                previousWasSpace = true;
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                if (!previousWasSpace)
                {
                    builder.Append(' ');
                    previousWasSpace = true;
                }

                continue;
            }

            builder.Append(character);
            previousWasSpace = false;
        }

        return builder.ToString();
    }

    private static IContainer ApplyBox(IContainer container, CssComputedStyle style)
    {
        var box = container;

        if (style.Background is { } background) box = box.Background(background);
        if (style.CornerRadiusPt is > 0) box = box.CornerRadius(style.CornerRadiusPt.Value);

        var paddingTop = Math.Clamp(style.MarginTopPt + style.PaddingTopPt, 0f, MaxBoxSpacingPt);
        var paddingBottom = Math.Clamp(style.MarginBottomPt + style.PaddingBottomPt, 0f, MaxBoxSpacingPt);
        var paddingLeft = Math.Clamp(style.PaddingLeftPt, 0f, MaxBoxSpacingPt);
        var paddingRight = Math.Clamp(style.PaddingRightPt, 0f, MaxBoxSpacingPt);

        if (paddingTop > 0.05f) box = box.PaddingTop(paddingTop);
        if (paddingBottom > 0.05f) box = box.PaddingBottom(paddingBottom);
        if (paddingLeft > 0.05f) box = box.PaddingLeft(paddingLeft);
        if (paddingRight > 0.05f) box = box.PaddingRight(paddingRight);

        if (style.BorderTop is { } borderTop) box = box.BorderTop(borderTop.WidthPt).BorderColor(borderTop.Color);
        if (style.BorderRight is { } borderRight) box = box.BorderRight(borderRight.WidthPt).BorderColor(borderRight.Color);
        if (style.BorderBottom is { } borderBottom) box = box.BorderBottom(borderBottom.WidthPt).BorderColor(borderBottom.Color);
        if (style.BorderLeft is { } borderLeft) box = box.BorderLeft(borderLeft.WidthPt).BorderColor(borderLeft.Color);

        return box;
    }

    private static RunStyle CreateRunStyle(
        CssComputedStyle style,
        float fontSize,
        bool forceBold = false,
        bool forceItalic = false) =>
        new(
            style.Bold || forceBold,
            style.Italic || forceItalic,
            false,
            false,
            style.Color ?? DefaultTextColor,
            fontSize,
            style.LetterSpacingPt,
            Math.Clamp(style.LineHeight ?? HtmlCssStylesheetDefaults.BodyLineHeight, 1f, 2.2f));

    private static List<AngleSharpElement> GetTableRows(AngleSharpElement table)
    {
        var rows = new List<AngleSharpElement>();

        foreach (var child in table.Children)
        {
            var tag = NormalizedTag(child.TagName);
            if (tag == "tr")
            {
                rows.Add(child);
                continue;
            }

            if (tag is not ("thead" or "tbody" or "tfoot")) continue;

            foreach (var row in child.Children)
            {
                if (NormalizedTag(row.TagName) == "tr") rows.Add(row);
            }
        }

        return rows;
    }

    private static List<AngleSharpElement> CellsOf(AngleSharpElement row)
    {
        var cells = new List<AngleSharpElement>();
        foreach (var child in row.Children)
        {
            var tag = NormalizedTag(child.TagName);
            if (tag is "td" or "th") cells.Add(child);
        }

        return cells;
    }

    private static bool IsHeaderRow(AngleSharpElement row)
    {
        if (string.Equals(row.ParentElement?.LocalName, "thead", StringComparison.OrdinalIgnoreCase))
            return true;

        var cells = CellsOf(row);
        return cells.Count > 0 && cells.All(cell => NormalizedTag(cell.TagName) == "th");
    }

    private static int CellSpan(AngleSharpElement cell) =>
        int.TryParse(cell.GetAttribute("colspan"), out var span) && span > 0 ? span : 1;

    private static bool HasBlockChildren(AngleSharpElement element)
    {
        foreach (var child in element.Children)
        {
            if (IsBlockTag(NormalizedTag(child.TagName))) return true;
        }

        return false;
    }

    private static bool HasVisibleContent(AngleSharpElement element)
    {
        if (!string.IsNullOrWhiteSpace(element.TextContent)) return true;
        return element.QuerySelector("img, br, hr, table, ul, ol") is not null;
    }

    private static bool IsBlockTag(string tag) => tag is
        "p" or "div" or "section" or "article" or "header" or "footer" or "main" or "aside"
        or "blockquote" or "table" or "ul" or "ol" or "li" or "hr" or "figure" or "address"
        or "pre" or "form" or "fieldset" or "h1" or "h2" or "h3" or "h4" or "h5" or "h6";

    private static bool IsHeading(string tag) => tag is "h1" or "h2" or "h3" or "h4" or "h5" or "h6";

    private static float? HeadingSize(string tag) => tag switch
    {
        "h1" => 20f,
        "h2" => 16f,
        "h3" => 13.5f,
        "h4" => 12f,
        "h5" => 11f,
        "h6" => 10.5f,
        _ => null,
    };

    private static string LeftIfJustified(string? alignment) =>
        alignment is null or "justify" ? "left" : alignment;

    private static string NormalizedTag(string tag) =>
        tag.Trim().ToLowerInvariant().Replace("-", string.Empty);

    private static AngleSharpElement? ParseBody(string html)
    {
        if (string.IsNullOrEmpty(html)) return null;

        var cleaned = _styleBlockRegex.Replace(html, string.Empty);
        cleaned = _scriptBlockRegex.Replace(cleaned, string.Empty);
        cleaned = _commentRegex.Replace(cleaned, string.Empty);

        var parser = new HtmlParser();
        var document = parser.ParseDocument(cleaned);

        return document.Body;
    }

    private sealed class RenderContext
    {
        private readonly Dictionary<AngleSharpElement, CssComputedStyle> _cache = new();

        public RenderContext(HtmlCssStylesheet sheet) => Sheet = sheet;

        public HtmlCssStylesheet Sheet { get; }

        public CssComputedStyle StyleOf(AngleSharpElement element)
        {
            if (_cache.TryGetValue(element, out var cached)) return cached;

            var style = Sheet.Compute(element);
            _cache[element] = style;
            return style;
        }
    }

    private readonly record struct RunStyle(
        bool Bold,
        bool Italic,
        bool Underline,
        bool Strike,
        Color Color,
        float Size,
        float? LetterSpacing,
        float? LineHeight);

    private readonly record struct TextRun(string Text, RunStyle Style)
    {
        public static TextRun LineBreak => new("\n", default);

        public bool IsLineBreak => Text == "\n";
    }
}
