using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using AngleSharpElement = AngleSharp.Dom.IElement;
using AngleSharpNode = AngleSharp.Dom.INode;
using AngleSharpText = AngleSharp.Dom.IText;

namespace Zorvian.Infrastructure.Services;

/// <summary>
/// Convierte el HTML renderizado por el motor documental (Liquid) a un documento
/// profesional de QuestPDF: respetando negritas, cursivas, titulos, tablas, listas,
/// alineaciones y saltos de seccion. Elimina el style y mapea el DOM a widgets.
/// </summary>
public sealed class HtmlToQuestPdfConverter
{
    private static readonly Regex _styleBlockRegex = new(
        @"<style\b[^>]*>.*?</style>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex _scriptBlockRegex = new(
        @"<script\b[^>]*>.*?</script>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private const float BodyFontSize = 10f;
    private const float BodyLineHeight = 1.55f;

    public static void RenderDocument(IContainer container, string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            container.Text("Documento generado por el motor documental de Zorvian ERP.").FontSize(BodyFontSize);
            return;
        }

        var body = ParseBody(html);
        if (body == null)
        {
            container.Text("Documento generado por el motor documental de Zorvian ERP.").FontSize(BodyFontSize);
            return;
        }

        container.Column(col =>
        {
            foreach (var child in body.ChildNodes)
            {
                RenderNode(col, child);
            }
        });
    }

    private static void RenderNode(ColumnDescriptor col, AngleSharpNode? node)
    {
        switch (node)
        {
            case AngleSharpText text:
                var t = text.TextContent?.Trim();
                if (!string.IsNullOrWhiteSpace(t))
                    col.Item().Text(t).FontSize(BodyFontSize).LineHeight(BodyLineHeight);
                break;
            case AngleSharpElement el:
                RenderEl(col, el);
                break;
        }
    }

    private static void RenderEl(ColumnDescriptor col, AngleSharpElement el)
    {
        var tag = NormalizedTag(el.TagName);

        switch (tag)
        {
            case "br":
                col.Item().Text("\n").FontSize(BodyFontSize);
                break;
            case "hr":
                col.Item().PaddingTop(8).PaddingBottom(8).BorderBottom(1f).BorderColor(Colors.Grey.Lighten2);
                break;
            case "h1": AddHeading(col, el, 20f); break;
            case "h2": AddHeading(col, el, 16f); break;
            case "h3": AddHeading(col, el, 13.5f); break;
            case "h4": AddHeading(col, el, 12f); break;
            case "h5": AddHeading(col, el, 11f); break;
            case "h6": AddHeading(col, el, 10.5f); break;
            case "p": AddParagraph(col, el); break;
            case "table": AddTable(col, el); break;
            case "ul":
            case "ol":
                AddList(col, el, tag == "ul");
                break;
            case "div":
            case "section":
            case "article":
                foreach (var child in el.ChildNodes)
                    RenderNode(col, child);
                break;
            default:
                if (el.TextContent != null && !string.IsNullOrWhiteSpace(el.TextContent.Trim()))
                    col.Item().Text(el.TextContent.Trim()).FontSize(BodyFontSize).LineHeight(BodyLineHeight);
                break;
        }
    }

    private static void AddHeading(ColumnDescriptor col, AngleSharpElement el, float fontSize)
    {
        var style = new TextStyle().FontSize(fontSize).Bold();
        var color = TryColorFromStyle(el.GetAttribute("style"));
        var align = GetAlignment(el);
        var text = el.TextContent?.Trim() ?? "";
        
        var item = col.Item().Text(text).Style(style).FontColor(color ?? Colors.Black);
        ApplyAlignment(item, align);
    }

    private static void AddParagraph(ColumnDescriptor col, AngleSharpElement el)
    {
        var style = new TextStyle().FontSize(BodyFontSize).LineHeight(BodyLineHeight);
        var color = TryColorFromStyle(el.GetAttribute("style"));
        var align = GetAlignment(el);
        var text = GetText(el);
        
        var item = col.Item().Text(text).Style(style).FontColor(color ?? Colors.Black);
        ApplyAlignment(item, align);
    }

    private static void ApplyAlignment(TextBlockDescriptor text, TextAlignment align)
    {
        switch (align)
        {
            case TextAlignment.Center:
                text.AlignCenter();
                break;
            case TextAlignment.Right:
                text.AlignRight();
                break;
            case TextAlignment.Justify:
                text.Justify();
                break;
            default:
                text.AlignLeft();
                break;
        }
    }

    private static string GetText(AngleSharpElement el)
    {
        var sb = new StringBuilder();
        foreach (var n in el.ChildNodes)
        {
            if (n is AngleSharpText text)
            {
                var t = text.TextContent;
                if (string.IsNullOrWhiteSpace(t)) continue;
                if (sb.Length > 0 && !char.IsWhiteSpace(sb[sb.Length - 1]) && !t.StartsWith(" ")) sb.Append(' ');
                sb.Append(t.Trim());
            }
            else if (n is AngleSharpElement me && IsInline(me.TagName))
            {
                var inner = GetText(me);
                if (!string.IsNullOrWhiteSpace(inner))
                {
                    if (sb.Length > 0 && !char.IsWhiteSpace(sb[sb.Length - 1]) && !inner.StartsWith(" ")) sb.Append(' ');
                    sb.Append(inner);
                }
            }
        }
        return sb.ToString().Trim();
    }

    private static bool IsInline(string tag) =>
        tag is "SPAN" or "STRONG" or "B" or "EM" or "I" or "U" or "S" or "A" or "SUB" or "SUP";

    private enum TextAlignment { Left, Center, Right, Justify }

    private static TextAlignment GetAlignment(AngleSharpElement el)
    {
        var s = el.GetAttribute("style")?.ToLowerInvariant();
        if (s != null && s.Contains("text-align:center")) return TextAlignment.Center;
        if (s != null && s.Contains("text-align:right")) return TextAlignment.Right;
        if (s != null && s.Contains("text-align:justify")) return TextAlignment.Justify;
        return TextAlignment.Left;
    }

    private static Color? TryColorFromStyle(string? style)
    {
        if (string.IsNullOrEmpty(style)) return null;
        var m = Regex.Match(style, @"color\s*:\s*([^;]+)", RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var c = m.Groups[1].Value.Trim();
            return ParseColor(c);
        }
        return null;
    }

    private static Color? ParseColor(string c)
    {
        c = c.Trim().TrimEnd(';').ToLowerInvariant();

        // Hex: #rgb / #rrggbb / #aarrggbb
        if (c.StartsWith("#"))
        {
            return Color.FromHex(c);
        }

        // rgb(r, g, b)
        var rgb = Regex.Match(c, @"rgb\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)");
        if (rgb.Success &&
            byte.TryParse(rgb.Groups[1].Value, out var r3) &&
            byte.TryParse(rgb.Groups[2].Value, out var g3) &&
            byte.TryParse(rgb.Groups[3].Value, out var b3))
            return Color.FromRGB(r3, g3, b3);

        // Named colors
        return NamedColor(c);
    }

    private static Color? NamedColor(string name)
    {
        switch (name)
        {
            case "red": return Colors.Red.Medium;
            case "green": return Colors.Green.Medium;
            case "blue": return Colors.Blue.Medium;
            case "orange": return Colors.Orange.Medium;
            case "purple": return Colors.Purple.Medium;
            case "pink": return Colors.Pink.Medium;
            case "yellow": return Colors.Yellow.Medium;
            case "black": return Colors.Black;
            case "gray":
            case "grey": return Colors.Grey.Medium;
            case "white": return Colors.White;
            default: return null;
        }
    }

    private static string NormalizedTag(string tag) =>
        tag?.Trim().ToLowerInvariant().Replace("-", "") ?? "div";

    private static void AddTable(ColumnDescriptor col, AngleSharpElement table)
    {
        foreach (var row in table.QuerySelectorAll("tr"))
        {
            var cells = new List<string>();
            foreach (var td in row.QuerySelectorAll("td, th"))
                cells.Add(td.TextContent?.Trim() ?? "");

            if (cells.All(string.IsNullOrWhiteSpace)) continue;

            col.Item().PaddingTop(2).Row(rowR =>
            {
                foreach (var cell in cells)
                {
                    rowR.ConstantItem(Math.Max(50, Math.Min(180, cell.Length * 2.6f)))
                        .Text(cell).FontSize(8.5f);
                }
            });
        }
    }

    private static void AddList(ColumnDescriptor col, AngleSharpElement list, bool ordered)
    {
        var idx = 0;
        foreach (var li in list.QuerySelectorAll("li"))
        {
            idx++;
            var text = li.TextContent?.Trim() ?? "";
            if (string.IsNullOrEmpty(text)) continue;
            var prefix = ordered ? $"{idx}." : "•";
            col.Item().PaddingTop(1).Text($"{prefix} {text}").FontSize(9.5f);
        }
    }

    private static AngleSharpElement? ParseBody(string html)
    {
        var cleaned = _styleBlockRegex.Replace(html, string.Empty);
        cleaned = _scriptBlockRegex.Replace(cleaned, string.Empty);
        var parser = new HtmlParser();
        var doc = parser.ParseDocument(cleaned);
        var body = doc.Body;
        return body;
    }
}
