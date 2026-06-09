namespace Zorvian.Application.DTOs.Accounting;

public record FinancialAssistantFeedbackRequest(
    string Query,
    string Response,
    bool IsHelpful,
    string? Comments
);
