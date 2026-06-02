using Zorvian.Application.DTOs.Report;

namespace Zorvian.Application.Interfaces;

public interface IReportService
{
    Task<byte[]> GenerateVacationReportAsync(int year);
    Task<byte[]> GeneratePermissionReportAsync(int year);
    Task<byte[]> GenerateAttendanceReportAsync(int year, int month);
    Task<byte[]> GenerateBalanceReportAsync();
}
