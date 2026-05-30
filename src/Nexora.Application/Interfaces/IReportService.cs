using Nexora.Application.DTOs.Report;

namespace Nexora.Application.Interfaces;

public interface IReportService
{
    Task<byte[]> GenerateVacationReportAsync(int year);
    Task<byte[]> GeneratePermissionReportAsync(int year);
    Task<byte[]> GenerateAttendanceReportAsync(int year, int month);
    Task<byte[]> GenerateBalanceReportAsync();
}
