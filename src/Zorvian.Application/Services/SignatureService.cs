using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services;

/// <summary>
/// Servicio de firma simplificado: carga certificado desde appsettings y
/// devuelve estructura de firma. La computación real del hash y persistencia
/// la realiza el controller o el repositorio inyectado.
/// </summary>
public sealed class SignatureService : ISignatureService
{
    private readonly IConfiguration _configuration;
    private readonly Lazy<X509Certificate2> _certificate;

    public SignatureService(IConfiguration configuration)
    {
        _configuration = configuration;
        _certificate = new Lazy<X509Certificate2>(LoadCertificate);
    }

    private X509Certificate2 LoadCertificate()
    {
        var b64Cert = _configuration["Certificate:PfxBase64"];
        var certPassword = _configuration["Certificate:Password"] ?? "zorvian-erp";

        if (!string.IsNullOrEmpty(b64Cert))
        {
            try
            {
                var certBytes = Convert.FromBase64String(b64Cert);
                return new X509Certificate2(certBytes, certPassword);
            }
            catch { }
        }

        var certPath = _configuration["Certificate:PfxPath"];
        if (!string.IsNullOrEmpty(certPath) && System.IO.File.Exists(certPath))
        {
            return new X509Certificate2(certPath, certPassword);
        }

        throw new InvalidOperationException(
            "Certificate not configured. Set Certificate:PfxBase64 and Certificate:Password in appsettings, " +
            "or Certificate:PfxPath pointing to a .pfx file, or ensure the secret is injected in production.");
    }

    public async Task<DocumentSignature> SignDocumentAsync(
        Guid documentId,
        string signerRole,
        string? notes,
        string? signerId = null,
        string? ipAddress = null)
    {
        return new DocumentSignature
        {
            DocumentId = documentId,
            SignerRole = signerRole,
            SignerType = "user",
            SignerId = signerId ?? "system",
            IPAddress = ipAddress,
            SignatureToken = Guid.NewGuid().ToString().Replace("-", ""),
            SignatureData = "",
            Status = "pending",
            SignedAt = null,
            Notes = notes
        };
    }

    public async Task<List<DocumentSignature>> GetSignaturesByDocumentAsync(Guid documentId)
    {
        return new List<DocumentSignature>();
    }
}