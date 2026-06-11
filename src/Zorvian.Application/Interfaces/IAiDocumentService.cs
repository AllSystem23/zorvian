namespace Zorvian.Application.Interfaces;

public interface IAiDocumentService
{
    Task<string> SummarizeDocumentAsync(string content);
    Task<string> AnalyzeRisksAsync(string content);
    Task<string> DetectOmissionsAsync(string module, string @event, string countryCode, List<string> existingDocumentNames);
}
