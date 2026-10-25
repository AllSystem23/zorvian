using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ISignatureService
{
    /// <summary>
    /// Firma el documento: fija la huella SHA-256 de la última versión en
    /// <c>SignatureData</c> y marca la firma y el documento como firmados.
    /// </summary>
    /// <param name="documentId">Documento a firmar.</param>
    /// <param name="signerRole">Rol del firmante; si se omite se reutiliza el de la fila pendiente.</param>
    /// <param name="notes">Observaciones opcionales de la firma.</param>
    /// <param name="signerId">Usuario que firma (claim del JWT).</param>
    /// <param name="ipAddress">IP desde la que se firma, para auditoría.</param>
    Task<DocumentSignature> SignDocumentAsync(
        Guid documentId,
        string signerRole,
        string? notes,
        string? signerId = null,
        string? ipAddress = null);

    Task<List<DocumentSignature>> GetSignaturesByDocumentAsync(Guid documentId);
}
