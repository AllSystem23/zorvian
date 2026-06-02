using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Services;

public sealed class AchExportService : IAchExportService
{
    private readonly ZorvianDbContext _db;
    private string _fileName = string.Empty;

    public string FileName => _fileName;

    public AchExportService(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<byte[]> GenerateAchFileAsync(Guid payrollRunId)
    {
        var run = await _db.PayrollRuns
            .Include(r => r.PayrollPeriod)
            .Include(r => r.Details!)
                .ThenInclude(d => d.Employee)
            .FirstOrDefaultAsync(r => r.Id == payrollRunId);

        if (run is null)
            throw new InvalidOperationException("Corrida de nómina no encontrada");

        if (run.Status != "approved")
            throw new InvalidOperationException("La nómina debe estar aprobada para exportar ACH");

        var period = run.PayrollPeriod!;
        var company = await _db.Companies.FirstOrDefaultAsync();
        var companyName = company?.Name ?? "Zorvian";
        var paymentDate = period.PaymentDate.ToString("yyyy-MM-dd");

        _fileName = $"ACH_{companyName.Replace(" ", "_")}_{period.Year}_{period.Month:D2}_{period.PeriodNumber}_{DateTime.UtcNow:yyyyMMdd}.csv";

        var sb = new StringBuilder();

        // Header
        sb.AppendLine(CultureInfo.InvariantCulture, $"COMPANY,{companyName},TOTAL_EMPLOYEES,{run.EmployeeCount},TOTAL_NET_PAY,{run.TotalNetPay:F2},PAYMENT_DATE,{paymentDate}");
        sb.AppendLine("EMPLOYEE_CODE,EMPLOYEE_NAME,BANK_NAME,ACCOUNT_NUMBER,ACCOUNT_TYPE,NET_PAY");

        // Details
        foreach (var detail in run.Details!.OrderBy(d => d.Employee?.LastName))
        {
            var emp = detail.Employee;
            var name = emp is null ? "N/A" : $"{emp.LastName} {emp.FirstName}";
            var code = emp?.EmployeeCode ?? "";
            var bankName = emp?.BankName ?? "";
            var accountNumber = emp?.BankAccountNumber ?? "";
            var accountType = emp?.BankAccountType ?? "";
            var netPay = detail.NetPay.ToString("F2", CultureInfo.InvariantCulture);

            sb.AppendLine(CultureInfo.InvariantCulture, $"{code},{name},{bankName},{accountNumber},{accountType},{netPay}");
        }

        // Trailer
        var total = run.Details!.Sum(d => d.NetPay);
        sb.AppendLine(CultureInfo.InvariantCulture, $"TRAILER,TOTAL_RECORDS,{run.Details!.Count},TOTAL_AMOUNT,{total:F2}");

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }
}
