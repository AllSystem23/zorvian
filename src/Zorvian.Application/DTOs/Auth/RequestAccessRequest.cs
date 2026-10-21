namespace Zorvian.Application.DTOs.Auth;

/// <summary>
/// Body de <c>POST zorvian/v1/auth/request-access</c> (modo "Solicitar Acceso"
/// del registro público).
/// </summary>
public sealed record RequestAccessRequest(
    string FullName,
    string Email,
    string? Company,
    string? Phone,
    string? Message
);
