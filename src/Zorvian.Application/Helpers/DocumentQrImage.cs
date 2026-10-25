using QRCoder;

namespace Zorvian.Application.Helpers;

/// <summary>
/// QR generado en proceso para el bloque de auditoría de los documentos.
///
/// Antes la imagen se pedía a <c>https://api.qrserver.com</c> en el momento de
/// renderizar: el PDF dependía de un servicio de terceros y, si no había red o
/// el servicio caía, el documento salía sin QR. Ahora se genera con QRCoder y
/// se incrusta como data URI — sin llamadas de red y sin terceros.
///
/// Se usa <see cref="PngByteQRCode"/> a propósito: codifica el PNG en managed
/// code, sin tocar <c>System.Drawing</c> (que en Linux — Render/Docker —
/// lanzaría <c>PlatformNotSupportedException</c>).
/// </summary>
public static class DocumentQrImage
{
    /// <summary>
    /// <c>data:image/png;base64,…</c> del QR, o vacío si no se pudo generar.
    /// Un QR nunca debe tumbar la generación del documento.
    /// </summary>
    public static string TryGetDataUri(string? payload, int pixelsPerModule = 6)
    {
        if (string.IsNullOrWhiteSpace(payload)) return string.Empty;
        try
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data);
            return "data:image/png;base64," +
                   Convert.ToBase64String(png.GetGraphic(pixelsPerModule));
        }
        catch
        {
            // Mejor un documento sin QR que un documento que no se genera.
            return string.Empty;
        }
    }
}
