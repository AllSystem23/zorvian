namespace Nexora.Application.DTOs.Common;

public sealed record ErrorResponse(
    string Code,
    string Message,
    object? Details = null,
    string? TraceId = null
);
