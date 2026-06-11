namespace Zorvian.Core.Entities;

public sealed class ElectronicInvoice : BaseEntity
{
    public Guid SaleId { get; set; }
    public Sale? Sale { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string AuthorizationCode { get; set; } = string.Empty;
    public string AuthorizationDate { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public string? XmlContent { get; set; }
    public string? SignedXml { get; set; }
    public string? DgiResponse { get; set; }
    public string? ErrorMessage { get; set; }
    public int Attempts { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? AuthorizedAt { get; set; }
    public string? CancelReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? PdfUrl { get; set; }
}

public sealed class ElectronicInvoiceXml : BaseEntity
{
    public Guid ElectronicInvoiceId { get; set; }
    public ElectronicInvoice? ElectronicInvoice { get; set; }
    public string XmlType { get; set; } = string.Empty;
    public string XmlContent { get; set; } = string.Empty;
    public string? FileHash { get; set; }
    public long FileSizeBytes { get; set; }
}
