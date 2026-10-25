using System.Security.Cryptography;
using System.Text;

namespace Zorvian.Application.Helpers;

/// <summary>
/// Huella SHA-256 (hex) del HTML renderizado de una versión de documento.
///
/// Es el valor que se persiste en <c>DocumentSignature.SignatureData</c> al
/// firmar y que el endpoint público de verificación recalcula para detectar si
/// el contenido fue alterado después de la firma. Firma y verificación DEBEN
/// usar este mismo helper o las huellas no coincidirían.
/// </summary>
public static class DocumentContentHasher
{
    public static string Sha256Hex(string? content) =>
        Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(content ?? string.Empty)));
}
