using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharpElement = AngleSharp.Dom.IElement;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Zorvian.Infrastructure.Services;

/// <summary>
/// Resuelve el subconjunto de CSS que usan las plantillas del motor documental
/// (<c>&lt;style&gt;</c> + atributos <c>style</c>) para que el PDF conserve la intención
/// tipográfica declarada: alineación justificada, sangría de primera línea, jerarquía
/// de títulos, márgenes, bordes, fondos y contenedores <c>display:flex</c>.
/// <para>
/// Motivo: <see cref="HtmlToQuestPdfConverter"/> elimina los bloques <c>&lt;style&gt;</c>
/// antes de mapear el DOM. Sin esta cascada, reglas como <c>p { text-align: justify }</c>
/// o <c>.body-text { text-indent: 40px }</c> se perdían y todos los párrafos salían
/// alineados a la izquierda (documentos legales sin justificar).
/// </para>
/// <para>
/// Soporta: selectores de tipo, clase, id, universal y combinador descendiente
/// (el hijo <c>&gt;</c> y los hermanos se aproximan a descendiente), herencia de las
/// propiedades tipográficas y prioridad inline &gt; id &gt; clase &gt; etiqueta &gt; orden.
/// Ignora pseudoclases/pseudoelementos, selectores de atributo y at-rules
/// (<c>@page</c>, <c>@media</c>, <c>@font-face</c>).
/// </para>
/// </summary>
public sealed class HtmlCssStylesheet
{
    private const float PxToPt = 0.75f;

    private static readonly Regex StyleBlockRegex = new(
        @"<style\b[^>]*>(?<css>.*?)</style>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex CommentRegex = new(
        @"/\*.*?\*/",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex LengthRegex = new(
        @"^(?<value>-?\d+(?:\.\d+)?)\s*(?<unit>px|pt|cm|mm|in|em|rem|%)?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex BorderRegex = new(
        @"(?<width>\d+(?:\.\d+)?)\s*(?<unit>px|pt)?|(?<color>#[0-9a-f]{3,8}|rgba?\([^)]*\)|[a-z]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex CompoundTokenRegex = new(
        @"(?<tag>[a-zA-Z][\w-]*)|(?<class>\.[\w-]+)|(?<id>#[\w-]+)|(?<all>\*)",
        RegexOptions.Compiled);

    private static readonly Regex RgbRegex = new(
        @"rgba?\(\s*(?<r>\d+)\s*,\s*(?<g>\d+)\s*,\s*(?<b>\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Dictionary<string, Color> NamedColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["black"] = Colors.Black,
        ["white"] = Colors.White,
        ["red"] = Colors.Red.Medium,
        ["green"] = Colors.Green.Medium,
        ["blue"] = Colors.Blue.Medium,
        ["yellow"] = Colors.Yellow.Medium,
        ["orange"] = Colors.Orange.Medium,
        ["purple"] = Colors.Purple.Medium,
        ["pink"] = Colors.Pink.Medium,
        ["gray"] = Colors.Grey.Medium,
        ["grey"] = Colors.Grey.Medium,
        ["silver"] = Colors.Grey.Lighten1,
        ["maroon"] = Color.FromHex("#800000"),
        ["navy"] = Color.FromHex("#000080"),
        ["teal"] = Color.FromHex("#008080"),
        ["olive"] = Color.FromHex("#808000"),
        ["lime"] = Color.FromHex("#00FF00"),
        ["aqua"] = Color.FromHex("#00FFFF"),
        ["cyan"] = Color.FromHex("#00FFFF"),
        ["magenta"] = Color.FromHex("#FF00FF"),
        ["fuchsia"] = Color.FromHex("#FF00FF"),
        ["brown"] = Color.FromHex("#A52A2A"),
        ["gold"] = Color.FromHex("#FFD700"),
        ["darkblue"] = Color.FromHex("#00008B"),
        ["darkred"] = Color.FromHex("#8B0000"),
        ["darkgreen"] = Color.FromHex("#006400"),
        ["darkgray"] = Color.FromHex("#A9A9A9"),
        ["darkgrey"] = Color.FromHex("#A9A9A9"),
        ["lightgray"] = Color.FromHex("#D3D3D3"),
        ["lightgrey"] = Color.FromHex("#D3D3D3"),
        ["whitesmoke"] = Color.FromHex("#F5F5F5"),
        ["beige"] = Color.FromHex("#F5F5DC"),
        ["ivory"] = Color.FromHex("#FFFFF0"),
    };

    private readonly List<CssEntry> _entries;

    private HtmlCssStylesheet(List<CssEntry> entries) => _entries = entries;

    /// <summary>Hoja vacía: todo elemento recibe el estilo base profesional.</summary>
    public static HtmlCssStylesheet Empty { get; } = new(new List<CssEntry>());

    /// <summary>Extrae y compila las reglas de todos los bloques <c>&lt;style&gt;</c> del HTML.</summary>
    public static HtmlCssStylesheet Parse(string? html)
    {
        if (string.IsNullOrEmpty(html)) return Empty;

        var css = new StringBuilder();
        foreach (Match match in StyleBlockRegex.Matches(html))
            css.AppendLine(match.Groups["css"].Value);

        if (css.Length == 0) return Empty;

        var entries = new List<CssEntry>();
        foreach (var (selectorText, declarations) in EnumerateRules(css.ToString()))
        {
            foreach (var rawSelector in selectorText.Split(','))
            {
                var selector = CssSelector.TryCreate(rawSelector);
                if (selector is null) continue;

                entries.Add(new CssEntry
                {
                    Selector = selector,
                    Declarations = declarations,
                    Order = entries.Count,
                });
            }
        }

        if (entries.Count == 0) return Empty;

        // Cascada: menor prioridad primero para que la última asignación gane.
        entries.Sort((left, right) =>
            left.Selector.Specificity != right.Selector.Specificity
                ? left.Selector.Specificity.CompareTo(right.Selector.Specificity)
                : left.Order.CompareTo(right.Order));

        return new HtmlCssStylesheet(entries);
    }

    /// <summary>
    /// Calcula el estilo efectivo del elemento aplicando herencia CSS y cascada
    /// (reglas de la hoja y atributo <c>style</c> inline).
    /// </summary>
    public CssComputedStyle Compute(AngleSharpElement element)
    {
        var chain = new List<AngleSharpElement>();
        for (AngleSharpElement? current = element; current is not null; current = current.ParentElement)
            chain.Add(current);
        chain.Reverse();

        var style = CssComputedStyle.CreateDefault();
        foreach (var node in chain)
            style = Apply(style, node);

        return style;
    }

    private CssComputedStyle Apply(CssComputedStyle parent, AngleSharpElement element)
    {
        var style = parent.Inherit();

        foreach (var entry in _entries)
        {
            if (!entry.Selector.Matches(element)) continue;

            foreach (var declaration in entry.Declarations)
                ApplyDeclaration(style, declaration.Key, declaration.Value, element);
        }

        var inline = element.GetAttribute("style");
        if (!string.IsNullOrWhiteSpace(inline))
        {
            foreach (var declaration in ParseDeclarations(inline))
                ApplyDeclaration(style, declaration.Key, declaration.Value, element);
        }

        // line-height absoluto (px/pt) se resuelve contra el font-size EFECTIVO del
        // elemento, sin depender del orden en que aparezcan las declaraciones.
        if (style.LineHeightAbsolutePt is { } absoluteLineHeight)
        {
            style.LineHeight = Math.Clamp(
                absoluteLineHeight / Math.Max(style.FontSizePt, 1f), 1f, 2.2f);
        }

        return style;
    }

    private static void ApplyDeclaration(CssComputedStyle style, string name, string value, AngleSharpElement element)
    {
        switch (name)
        {
            case "text-align":
                style.TextAlign = NormalizeAlignment(value) ?? style.TextAlign;
                break;

            case "color":
                if (ParseColor(value) is { } color)
                {
                    style.Color = color;
                    style.ColorDeclared = true;
                }
                break;

            case "background":
            case "background-color":
                style.Background = ExtractBackgroundColor(value);
                break;

            case "font-size":
                if (ParseLength(value, style.FontSizePt) is > 0 and var fontSize)
                {
                    style.FontSizePt = fontSize;
                    style.FontSizeDeclared = true;
                }
                break;

            case "font-weight":
                style.Bold = IsBoldWeight(value);
                style.BoldDeclared = true;
                break;

            case "font-style":
                style.Italic = value.Contains("italic", StringComparison.OrdinalIgnoreCase)
                               || value.Contains("oblique", StringComparison.OrdinalIgnoreCase);
                style.ItalicDeclared = true;
                break;

            case "line-height":
                if (TryParseAbsoluteLineHeight(value, style.FontSizePt) is { } absoluteHeight)
                    style.LineHeightAbsolutePt = absoluteHeight;
                else
                    style.LineHeight = ParseLineHeightFactor(value) ?? style.LineHeight;
                break;

            case "letter-spacing":
                if (ParseLength(value, style.FontSizePt) is { } letterSpacing)
                    style.LetterSpacingPt = Math.Clamp(letterSpacing, -2f, 8f);
                break;

            case "text-indent":
                if (ParseLength(value, style.FontSizePt) is { } indent)
                    style.TextIndentPt = Math.Clamp(indent, 0f, 72f);
                break;

            case "margin":
                if (IsRootElement(element)) break;
                ApplyEdges(value, style.FontSizePt,
                    out var marginTop, out _, out var marginBottom, out _);
                style.MarginTopPt = marginTop;
                style.MarginBottomPt = marginBottom;
                break;

            case "margin-top":
                if (IsRootElement(element)) break;
                if (ParseLength(value, style.FontSizePt) is { } top) style.MarginTopPt = Math.Max(0, top);
                break;

            case "margin-bottom":
                if (IsRootElement(element)) break;
                if (ParseLength(value, style.FontSizePt) is { } bottom) style.MarginBottomPt = Math.Max(0, bottom);
                break;

            case "padding":
                if (IsRootElement(element)) break;
                ApplyEdges(value, style.FontSizePt,
                    out var paddingTop, out var paddingRight, out var paddingBottom, out var paddingLeft);
                style.PaddingTopPt = paddingTop;
                style.PaddingBottomPt = paddingBottom;
                style.PaddingLeftPt = paddingLeft;
                style.PaddingRightPt = paddingRight;
                break;

            case "padding-top":
                if (IsRootElement(element)) break;
                if (ParseLength(value, style.FontSizePt) is { } padTop) style.PaddingTopPt = Math.Max(0, padTop);
                break;

            case "padding-bottom":
                if (IsRootElement(element)) break;
                if (ParseLength(value, style.FontSizePt) is { } padBottom) style.PaddingBottomPt = Math.Max(0, padBottom);
                break;

            case "padding-left":
                if (ParseLength(value, style.FontSizePt) is { } padLeft) style.PaddingLeftPt = Math.Max(0, padLeft);
                break;

            case "padding-right":
                if (ParseLength(value, style.FontSizePt) is { } padRight) style.PaddingRightPt = Math.Max(0, padRight);
                break;

            case "border":
                {
                    var border = ParseBorder(value);
                    style.BorderTop = border;
                    style.BorderRight = border;
                    style.BorderBottom = border;
                    style.BorderLeft = border;
                    break;
                }

            case "border-top":
                style.BorderTop = ParseBorder(value);
                break;

            case "border-bottom":
                style.BorderBottom = ParseBorder(value);
                break;

            case "border-left":
                style.BorderLeft = ParseBorder(value);
                break;

            case "border-right":
                style.BorderRight = ParseBorder(value);
                break;

            case "border-radius":
                if (ParseLength(value, style.FontSizePt) is { } radius)
                    style.CornerRadiusPt = Math.Clamp(radius, 0f, 12f);
                break;

            case "min-height":
                if (ParseLength(value, style.FontSizePt) is { } minHeight)
                    style.MinHeightPt = Math.Clamp(minHeight, 0f, 60f);
                break;

            case "display":
                style.IsFlex = value.Contains("flex", StringComparison.OrdinalIgnoreCase);
                break;

            case "justify-content":
                style.JustifyContent = value.Trim().ToLowerInvariant();
                break;

            case "gap":
                if (ParseLength(value, style.FontSizePt) is { } gap)
                    style.GapPt = Math.Clamp(gap, 0f, 24f);
                break;

            case "flex":
                style.FlexGrow = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Any(part => float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var grow) && grow > 0);
                break;

            case "width":
                style.WidthPercent = ParseWidthPercent(value);
                break;
        }
    }

    private static bool IsRootElement(AngleSharpElement element) =>
        element.LocalName is "html" or "body";

    private static string? NormalizeAlignment(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "justify" => "justify",
            "center" => "center",
            "right" or "end" => "right",
            "left" or "start" => "left",
            _ => null,
        };
    }

    private static bool IsBoldWeight(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized is "bold" or "bolder") return true;
        return float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var weight)
               && weight >= 600;
    }

    private static float? TryParseAbsoluteLineHeight(string value, float fontSizePt)
    {
        var normalized = value.Trim();
        if (!normalized.EndsWith("px", StringComparison.OrdinalIgnoreCase)
            && !normalized.EndsWith("pt", StringComparison.OrdinalIgnoreCase)) return null;

        var absolute = ParseLength(normalized, fontSizePt);
        return absolute is > 0 ? absolute : null;
    }

    private static float? ParseLineHeightFactor(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized is "normal" or "inherit" or "initial" or "unset") return null;

        if (!float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var factor))
            return null;

        return Math.Clamp(factor, 1f, 2.2f);
    }

    private static float? ParseLength(string? value, float baseFontSizePt)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var match = LengthRegex.Match(value.Trim());
        if (!match.Success) return null;
        if (!float.TryParse(match.Groups["value"].Value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out var number)) return null;

        return match.Groups["unit"].Value.ToLowerInvariant() switch
        {
            "pt" => number,
            "px" => number * PxToPt,
            "cm" => number * 28.3465f,
            "mm" => number * 2.83465f,
            "in" => number * 72f,
            "em" or "rem" => number * baseFontSizePt,
            "%" => number * baseFontSizePt / 100f,
            _ => number * PxToPt,
        };
    }

    private static float? ParseWidthPercent(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        if (!normalized.EndsWith('%')) return null;
        if (!float.TryParse(normalized.TrimEnd('%'), NumberStyles.Float,
                CultureInfo.InvariantCulture, out var percent)) return null;
        return Math.Clamp(percent, 1f, 100f);
    }

    private static void ApplyEdges(
        string value,
        float baseFontSizePt,
        out float top,
        out float right,
        out float bottom,
        out float left)
    {
        top = right = bottom = left = 0;

        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        var values = new float[4];
        for (var i = 0; i < parts.Length && i < 4; i++)
            values[i] = Math.Max(0, ParseLength(parts[i], baseFontSizePt) ?? 0);

        switch (parts.Length)
        {
            case 1:
                top = right = bottom = left = values[0];
                break;
            case 2:
                top = bottom = values[0];
                right = left = values[1];
                break;
            case 3:
                top = values[0];
                right = left = values[1];
                bottom = values[2];
                break;
            default:
                top = values[0];
                right = values[1];
                bottom = values[2];
                left = values[3];
                break;
        }
    }

    private static CssBorder? ParseBorder(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length == 0) return null;
        if (normalized.Contains("none", StringComparison.Ordinal)) return null;

        var width = 1f * PxToPt;
        Color? color = null;
        var sawWidth = false;

        foreach (Match match in BorderRegex.Matches(normalized))
        {
            if (match.Groups["color"].Success)
            {
                color ??= ParseColor(match.Groups["color"].Value);
                continue;
            }

            if (!match.Groups["width"].Success || sawWidth) continue;

            var unit = match.Groups["unit"].Value.ToLowerInvariant();
            if (unit.Length > 0 && unit is not ("px" or "pt")) continue;

            if (float.TryParse(match.Groups["width"].Value, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out var number))
            {
                width = unit == "pt" ? number : number * PxToPt;
                sawWidth = true;
            }
        }

        return new CssBorder(Math.Clamp(width, 0.2f, 4f), color ?? Colors.Grey.Medium);
    }

    private static Color? ExtractBackgroundColor(string value)
    {
        var normalized = value.Trim();
        if (normalized.Length == 0) return null;
        if (normalized.Contains("url(", StringComparison.OrdinalIgnoreCase)) return null;

        var direct = ParseColor(normalized);
        if (direct is not null) return direct;

        foreach (var token in normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            // "background: #fafafa none repeat" o "background: linear-gradient(...) #fff"
            var parsed = ParseColor(token);
            if (parsed is not null) return parsed;
        }

        return null;
    }

    /// <summary>Convierte un token CSS de color a <see cref="Color"/> (hex, rgb/rgba o nombre).</summary>
    public static Color? ParseColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var normalized = value.Trim().TrimEnd(';').ToLowerInvariant();
        if (normalized is "transparent" or "inherit" or "initial" or "none" or "unset" or "currentcolor")
            return null;

        if (normalized.StartsWith("rgb", StringComparison.Ordinal))
        {
            var rgb = RgbRegex.Match(normalized);
            if (!rgb.Success) return null;
            if (!byte.TryParse(rgb.Groups["r"].Value, out var red)
                || !byte.TryParse(rgb.Groups["g"].Value, out var green)
                || !byte.TryParse(rgb.Groups["b"].Value, out var blue)) return null;
            return Color.FromRGB(red, green, blue);
        }

        if (normalized.StartsWith('#'))
        {
            var hex = normalized[1..];
            if (hex.Length == 3)
                hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
            if (hex.Length is 6 or 8 && hex.All(Uri.IsHexDigit))
                return Color.FromHex($"#{hex}");
            return null;
        }

        if (NamedColors.TryGetValue(normalized, out var named)) return named;
        return null;
    }

    private static IEnumerable<(string Selector, List<KeyValuePair<string, string>> Declarations)> EnumerateRules(string css)
    {
        var clean = CommentRegex.Replace(css, string.Empty);
        var index = 0;

        while (index < clean.Length)
        {
            var open = clean.IndexOf('{', index);
            if (open < 0) yield break;

            var selector = clean[index..open].Trim();
            var depth = 0;
            var close = -1;
            for (var i = open; i < clean.Length; i++)
            {
                if (clean[i] == '{') depth++;
                else if (clean[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        close = i;
                        break;
                    }
                }
            }

            if (close < 0) yield break;

            var body = clean[(open + 1)..close];
            index = close + 1;

            if (selector.Length == 0 || selector.StartsWith('@')) continue;

            var declarations = ParseDeclarations(body);
            if (declarations.Count > 0)
                yield return (selector, declarations);
        }
    }

    private static List<KeyValuePair<string, string>> ParseDeclarations(string body)
    {
        var declarations = new List<KeyValuePair<string, string>>();

        foreach (var part in body.Split(';'))
        {
            var colon = part.IndexOf(':');
            if (colon <= 0) continue;

            var name = part[..colon].Trim().ToLowerInvariant();
            var value = part[(colon + 1)..].Trim();
            if (name.Length == 0 || value.Length == 0) continue;
            if (name.StartsWith("--", StringComparison.Ordinal)) continue;
            if (value.Contains("url(", StringComparison.OrdinalIgnoreCase)) continue;

            declarations.Add(new KeyValuePair<string, string>(name, value));
        }

        return declarations;
    }

    private sealed class CssEntry
    {
        public CssSelector Selector { get; init; } = null!;
        public List<KeyValuePair<string, string>> Declarations { get; init; } = null!;
        public int Order { get; init; }
    }

    private sealed class CssSelector
    {
        private readonly CompoundSelector[] _compounds;

        private CssSelector(CompoundSelector[] compounds, int specificity)
        {
            _compounds = compounds;
            Specificity = specificity;
        }

        public int Specificity { get; }

        public static CssSelector? TryCreate(string rawSelector)
        {
            var text = rawSelector.Trim();
            if (text.Length == 0) return null;

            // Pseudoclases/pseudoelementos, atributos y selectores funcionales: fuera de alcance.
            if (text.Contains(':') || text.Contains('[') || text.Contains('(')) return null;

            // Combinadores hijo/hermano → descendiente (aproximación suficiente para las plantillas).
            text = text.Replace('>', ' ').Replace('+', ' ').Replace('~', ' ');

            var parts = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return null;

            var compounds = new List<CompoundSelector>(parts.Length);
            var specificity = 0;

            foreach (var part in parts)
            {
                if (CompoundSelector.TryCreate(part) is not { } compound) return null;
                compounds.Add(compound);
                specificity += compound.Specificity;
            }

            return new CssSelector(compounds.ToArray(), specificity);
        }

        public bool Matches(AngleSharpElement element)
        {
            if (_compounds.Length == 0) return false;
            if (!_compounds[^1].Matches(element)) return false;

            var current = element;
            for (var i = _compounds.Length - 2; i >= 0; i--)
            {
                var ancestor = current.ParentElement;
                var found = false;

                while (ancestor is not null)
                {
                    if (_compounds[i].Matches(ancestor))
                    {
                        current = ancestor;
                        found = true;
                        break;
                    }

                    ancestor = ancestor.ParentElement;
                }

                if (!found) return false;
            }

            return true;
        }
    }

    private readonly record struct CompoundSelector(
        string? Tag,
        IReadOnlyList<string> Classes,
        IReadOnlyList<string> Ids)
    {
        public int Specificity => Ids.Count * 100 + Classes.Count * 10 + (Tag is null ? 0 : 1);

        public static CompoundSelector? TryCreate(string token)
        {
            var matches = CompoundTokenRegex.Matches(token);
            if (matches.Count == 0) return null;

            string? tag = null;
            var classes = new List<string>();
            var ids = new List<string>();

            foreach (Match match in matches)
            {
                if (match.Groups["tag"].Success)
                    tag = match.Groups["tag"].Value.ToLowerInvariant();
                else if (match.Groups["class"].Success)
                    classes.Add(match.Groups["class"].Value[1..]);
                else if (match.Groups["id"].Success)
                    ids.Add(match.Groups["id"].Value[1..]);
            }

            return new CompoundSelector(tag, classes, ids);
        }

        public bool Matches(AngleSharpElement element)
        {
            if (Tag is not null && !string.Equals(element.LocalName, Tag, StringComparison.OrdinalIgnoreCase))
                return false;

            foreach (var id in Ids)
            {
                if (!string.Equals(element.Id, id, StringComparison.Ordinal)) return false;
            }

            foreach (var className in Classes)
            {
                if (!element.ClassList.Contains(className)) return false;
            }

            return true;
        }
    }
}

/// <summary>Borde resuelto desde CSS (ancho en puntos y color).</summary>
public readonly record struct CssBorder(float WidthPt, Color Color);

/// <summary>
/// Estilo efectivo de un elemento tras aplicar herencia y cascada.
/// Las propiedades tipográficas se heredan; las de caja (márgenes, bordes, fondo,
/// flex) se reinician en cada elemento, como en CSS.
/// </summary>
public sealed class CssComputedStyle
{
    public string? TextAlign { get; set; }
    public Color? Color { get; set; }
    public bool ColorDeclared { get; set; }
    public float FontSizePt { get; set; }
    public bool FontSizeDeclared { get; set; }
    public bool Bold { get; set; }
    public bool BoldDeclared { get; set; }
    public bool Italic { get; set; }
    public bool ItalicDeclared { get; set; }
    public float? LineHeight { get; set; }

    /// <summary>
    /// line-height declarado en unidades absolutas (px/pt). Se resuelve a factor
    /// contra el font-size efectivo al terminar la cascada del elemento.
    /// </summary>
    public float? LineHeightAbsolutePt { get; set; }
    public float TextIndentPt { get; set; }
    public float? LetterSpacingPt { get; set; }

    public float MarginTopPt { get; set; }
    public float MarginBottomPt { get; set; }
    public float PaddingTopPt { get; set; }
    public float PaddingBottomPt { get; set; }
    public float PaddingLeftPt { get; set; }
    public float PaddingRightPt { get; set; }

    public Color? Background { get; set; }
    public CssBorder? BorderTop { get; set; }
    public CssBorder? BorderRight { get; set; }
    public CssBorder? BorderBottom { get; set; }
    public CssBorder? BorderLeft { get; set; }
    public float? CornerRadiusPt { get; set; }
    public float? MinHeightPt { get; set; }

    public bool IsFlex { get; set; }
    public bool FlexGrow { get; set; }
    public string? JustifyContent { get; set; }
    public float? GapPt { get; set; }
    public float? WidthPercent { get; set; }

    /// <summary>
    /// Línea base profesional: cuerpo de 10 pt, interlineado 1.55, texto justificado
    /// y tinta azul-navy de la marca. Las reglas del documento la sobrescriben.
    /// </summary>
    public static CssComputedStyle CreateDefault() => new()
    {
        TextAlign = "justify",
        Color = QuestPDF.Infrastructure.Color.FromHex("#1A1A2E"),
        FontSizePt = HtmlCssStylesheetDefaults.BodyFontSizePt,
        LineHeight = HtmlCssStylesheetDefaults.BodyLineHeight,
    };

    /// <summary>Copia solo lo heredable (CSS) y reinicia las propiedades de caja.</summary>
    public CssComputedStyle Inherit() => new()
    {
        TextAlign = TextAlign,
        Color = Color,
        FontSizePt = FontSizePt,
        Bold = Bold,
        Italic = Italic,
        LineHeight = LineHeight,
        TextIndentPt = TextIndentPt,
        LetterSpacingPt = LetterSpacingPt,
    };
}

/// <summary>Constantes tipográficas compartidas por la cascada y el conversor.</summary>
public static class HtmlCssStylesheetDefaults
{
    public const float BodyFontSizePt = 10f;
    public const float BodyLineHeight = 1.55f;
}
