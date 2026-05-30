using Nexora.Application.DTOs.Employee;

namespace Nexora.Application.Interfaces;

public interface IExcelImportService
{
    Task<ExcelImportResult> ImportAsync(Stream excelStream);
}

public sealed record ExcelImportResult(
    int Imported,
    int Failed,
    List<string> Errors
);
