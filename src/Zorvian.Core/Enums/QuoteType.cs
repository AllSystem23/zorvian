namespace Zorvian.Core.Enums;

/// <summary>
/// Tipo de documento comercial. La cotización es una oferta sin compromiso;
/// la proforma es una pre-factura que exige pago anticipado antes de emitir la factura.
/// </summary>
public enum QuoteType
{
    Cotizacion = 0,
    Proforma = 1
}

public static class QuoteTypeExtensions
{
    /// <summary>Valor en minúsculas que viaja por la API y usa el frontend.</summary>
    public static string ToWire(this QuoteType type) =>
        type == QuoteType.Proforma ? "proforma" : "cotizacion";

    /// <summary>Convierte el valor de la API al enum (por defecto Cotización).</summary>
    public static QuoteType Parse(string? value) =>
        string.Equals((value ?? string.Empty).Trim(), "proforma", StringComparison.OrdinalIgnoreCase)
            ? QuoteType.Proforma
            : QuoteType.Cotizacion;
}
