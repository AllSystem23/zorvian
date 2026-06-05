using System.Globalization;
using Zorvian.Application.Interfaces;

namespace Zorvian.Infrastructure.Services;

public sealed class ReconciliationService : IReconciliationService
{
    private readonly IPayrollRepository _repo;

    public ReconciliationService(IPayrollRepository repo) => _repo = repo;

    public async Task<(int Reconciled, int Failed, string Message)> ProcessBankResponseFileAsync(Stream fileStream)
    {
        int reconciled = 0;
        int failed = 0;

        using var reader = new StreamReader(fileStream);
        // Expecting a CSV: PaymentReference,Status,ErrorMessage
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(',');
            if (parts.Length < 2) continue;

            var reference = parts[0].Trim();
            var status = parts[1].Trim().ToLower();

            var detail = await _repo.GetDetailByReferenceAsync(reference);
            if (detail == null)
            {
                failed++;
                continue;
            }

            detail.PaymentStatus = status == "ok" ? "paid" : "failed";
            if (parts.Length > 2) detail.Details = parts[2].Trim();
            
            await _repo.UpdateDetailAsync(detail);
            reconciled++;
        }

        await _repo.SaveChangesAsync();
        return (reconciled, failed, $"Reconciliation completed: {reconciled} reconciled, {failed} failed.");
    }
}
