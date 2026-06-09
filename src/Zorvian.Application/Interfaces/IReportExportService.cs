using Zorvian.Application.DTOs.Report;

namespace Zorvian.Application.Interfaces;

public interface IReportExportService
{
    byte[] ExportToExcel(ReportResult result, string title);
    byte[] ExportToPdf(ReportResult result, string title);
}
