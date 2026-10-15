namespace Zorvian.Application.DTOs.Employee;

public record CreateCertificateRequestDto(string CertificateType, string? Reason);

public record CertificateRequestResponse(
    Guid Id,
    string CertificateType,
    string? Reason,
    string Status,
    string? RejectionReason,
    Guid? GeneratedDocumentId,
    DateTime RequestedAt,
    DateTime? ProcessedAt,
    string EmployeeName,
    string EmployeeIdentification
);
