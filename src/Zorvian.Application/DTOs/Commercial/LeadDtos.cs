using Zorvian.Core.Enums;

namespace Zorvian.Application.DTOs.Commercial;

public record CreateLeadRequest(
    string FirstName,
    string LastName,
    string? CompanyName,
    string? JobTitle,
    string? Email,
    string? Phone,
    string? WhatsApp,
    string? City,
    string CountryCode,
    string? Source,
    string? InterestLevel,
    string? Notes
);

public record UpdateLeadRequest(
    string FirstName,
    string LastName,
    string? CompanyName,
    string? JobTitle,
    string? Email,
    string? Phone,
    string? WhatsApp,
    string? City,
    string CountryCode,
    string? Source,
    string? InterestLevel,
    LeadStatus Status,
    string? Notes,
    Guid? AssignedToId
);

public record LeadFilterRequest(
    string? Search,
    string? Status,
    int Page = 1,
    int PageSize = 20
);

public record LeadResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string? CompanyName,
    string? JobTitle,
    string? Email,
    string? Phone,
    string? WhatsApp,
    string? City,
    string CountryCode,
    string? Source,
    string? InterestLevel,
    string Status,
    Guid? AssignedToId,
    string? Notes,
    DateTime CreatedAt
);
