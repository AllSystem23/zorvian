using Microsoft.EntityFrameworkCore;
using Zorvian.Application.DTOs.ML;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Services;

public sealed class AccountingAssistantService
{
    private readonly ZorvianDbContext _db;
    private readonly ExpenseClassificationService _classifier;

    public AccountingAssistantService(ZorvianDbContext db, ExpenseClassificationService classifier)
    {
        _db = db;
        _classifier = classifier;
    }

    public async Task<AccountingAnomalyReportDto> DetectAnomaliesAsync(int daysBack = 30)
    {
        var since = DateTime.UtcNow.AddDays(-daysBack);

        // HasQueryFilter already handles TenantId and IsDeleted filtering
        var entries = await _db.AccountingEntries
            .Where(e => e.EntryDate >= since)
            .Include(e => e.Details)
            .ThenInclude(d => d.Account)
            .ToListAsync();

        var anomalies = new List<AccountingAnomalyDto>();

        foreach (var entry in entries)
        {
            if (Math.Abs(entry.TotalDebit - entry.TotalCredit) > 0.01m)
            {
                anomalies.Add(new AccountingAnomalyDto
                {
                    AccountingEntryId = entry.Id,
                    EntryNumber = entry.EntryNumber,
                    Description = entry.Description,
                    ReferenceType = entry.ReferenceType,
                    EntryDate = entry.EntryDate,
                    TotalDebit = entry.TotalDebit,
                    TotalCredit = entry.TotalCredit,
                    AnomalyType = "unbalanced_entry",
                    Detail = $"Total Débito ({entry.TotalDebit}) ≠ Total Crédito ({entry.TotalCredit})",
                    Severity = "critical"
                });
            }

            var revenueAccountsInExpenseEntries = entry.Details
                .Where(d => d.Account.Type == AccountTypes.Income && entry.ReferenceType == "Purchase")
                .ToList();
            foreach (var detail in revenueAccountsInExpenseEntries)
            {
                anomalies.Add(new AccountingAnomalyDto
                {
                    AccountingEntryId = entry.Id,
                    EntryNumber = entry.EntryNumber,
                    Description = entry.Description,
                    ReferenceType = entry.ReferenceType,
                    EntryDate = entry.EntryDate,
                    AnomalyType = "account_type_mismatch",
                    Detail = $"Cuenta de ingresos ({detail.Account.Code} - {detail.Account.Name}) usada en compra",
                    Severity = "warning"
                });
            }

            var expenseAccountsInSales = entry.Details
                .Where(d => d.Account.Type == AccountTypes.Expense && entry.ReferenceType == "Sale")
                .ToList();
            foreach (var detail in expenseAccountsInSales)
            {
                anomalies.Add(new AccountingAnomalyDto
                {
                    AccountingEntryId = entry.Id,
                    EntryNumber = entry.EntryNumber,
                    Description = entry.Description,
                    ReferenceType = entry.ReferenceType,
                    EntryDate = entry.EntryDate,
                    AnomalyType = "account_type_mismatch",
                    Detail = $"Cuenta de gastos ({detail.Account.Code} - {detail.Account.Name}) usada en venta",
                    Severity = "warning"
                });
            }
        }

        return new AccountingAnomalyReportDto
        {
            Anomalies = [.. anomalies.OrderByDescending(a => a.Severity).ThenBy(a => a.EntryDate)],
            TotalEntriesAnalyzed = entries.Count,
            AnomalyCount = anomalies.Count
        };
    }
}
