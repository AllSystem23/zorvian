namespace Zorvian.Application.Interfaces;

public interface IAchExportService
{
    Task<byte[]> GenerateAchFileAsync(Guid payrollRunId);
    string FileName { get; }
}
