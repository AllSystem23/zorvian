using System.Text;

namespace Zorvian.Application.Helpers;

/// <summary>
/// Converts decimal amounts to Spanish words (cantidad en letras).
/// Used for liquidation documents, invoices, and legal PDFs.
/// Format: "Veinte Mil Setecientos Ochenta y Seis Córdobas con Setenta y Ocho Centavos"
/// </summary>
public static class NumberToWordsHelper
{
    private static readonly string[] Units =
        ["", "uno", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve",
         "diez", "once", "doce", "trece", "catorce", "quince", "dieciséis", "diecisiete",
         "dieciocho", "diecinueve"];

    private static readonly string[] Tens =
        ["", "", "veinte", "treinta", "cuarenta", "cincuenta", "sesenta",
         "setenta", "ochenta", "noventa"];

    private static readonly string[] Hundreds =
        ["", "ciento", "doscientos", "trescientos", "cuatrocientos", "quinientos",
         "seiscientos", "setecientos", "ochocientos", "novecientos"];

    /// <summary>
    /// Converts a decimal to Spanish words with currency labels.
    /// </summary>
    /// <param name="amount">The amount to convert (e.g. 20786.78m)</param>
    /// <param name="currencySingular">Currency singular (e.g. "Córdoba")</param>
    /// <param name="currencyPlural">Currency plural (e.g. "Córdobas")</param>
    /// <param name="centsSingular">Cents singular (e.g. "Centavo")</param>
    /// <param name="centsPlural">Cents plural (e.g. "Centavos")</param>
    /// <returns>Full string in Spanish words</returns>
    public static string ToCurrencyWords(
        decimal amount,
        string currencySingular = "Córdoba",
        string currencyPlural = "Córdobas",
        string centsSingular = "Centavo",
        string centsPlural = "Centavos")
    {
        var whole = (long)Math.Floor(amount);
        var cents = (int)Math.Round((amount - whole) * 100);

        if (cents == 100) { whole += 1; cents = 0; }

        var sb = new StringBuilder();

        if (whole == 0 && cents == 0)
        {
            sb.Append("Cero ");
            sb.Append(currencyPlural);
            return sb.ToString();
        }

        if (whole > 0)
        {
            sb.Append(ConvertWholeNumber(whole));
            sb.Append(' ');
            sb.Append(whole == 1 ? currencySingular : currencyPlural);
        }

        if (cents > 0)
        {
            if (whole > 0) sb.Append(" con ");
            sb.Append(ConvertWholeNumber(cents));
            sb.Append(' ');
            sb.Append(cents == 1 ? centsSingular : centsPlural);
        }

        return sb.ToString();
    }

    private static string ConvertWholeNumber(long n)
    {
        if (n == 0) return "cero";
        if (n == 100) return "cien";

        var sb = new StringBuilder();

        if (n >= 1_000_000_000)
        {
            var billons = (int)(n / 1_000_000_000);
            sb.Append(billons == 1 ? "un mil millones" : $"{ConvertThreeDigits(billons)} mil millones");
            n %= 1_000_000_000;
            if (n > 0) sb.Append(' ');
        }

        if (n >= 1_000_000)
        {
            var millions = (int)(n / 1_000_000);
            sb.Append(millions == 1 ? "un millón" : $"{ConvertThreeDigits(millions)} millones");
            n %= 1_000_000;
            if (n > 0) sb.Append(' ');
        }

        if (n >= 1_000)
        {
            var thousands = (int)(n / 1_000);
            sb.Append(thousands == 1 ? "mil" : $"{ConvertThreeDigits(thousands)} mil");
            n %= 1_000;
            if (n > 0) sb.Append(' ');
        }

        if (n > 0)
        {
            sb.Append(ConvertThreeDigits((int)n));
        }

        return sb.ToString().Trim();
    }

    private static string ConvertThreeDigits(int n)
    {
        if (n == 0) return "";
        if (n == 100) return "cien";

        var sb = new StringBuilder();
        var h = n / 100;
        var remainder = n % 100;

        if (h > 0)
        {
            sb.Append(Hundreds[h]);
            if (remainder > 0) sb.Append(' ');
        }

        if (remainder > 0)
        {
            if (remainder < 20)
            {
                sb.Append(Units[remainder]);
            }
            else
            {
                var t = remainder / 10;
                var u = remainder % 10;
                sb.Append(Tens[t]);
                if (u > 0)
                {
                    sb.Append(" y ");
                    sb.Append(Units[u]);
                }
            }
        }

        return sb.ToString();
    }
}
