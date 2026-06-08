using Microsoft.EntityFrameworkCore;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Services;

namespace Zorvian.Web.Jobs;

public sealed class ExpenseClassificationTrainingJob
{
    private readonly ZorvianDbContext _db;
    private readonly ExpenseClassificationService _mlService;

    public ExpenseClassificationTrainingJob(ZorvianDbContext db, ExpenseClassificationService mlService)
    {
        _db = db;
        _mlService = mlService;
    }

    public async Task RunAsync()
    {
        var details = await _db.AccountingEntryDetails
            .Where(ad => ad.Account.Type == AccountTypes.Expense
                && ad.AccountingEntry.Status == "posted"
                && !ad.IsDeleted)
            .Include(ad => ad.Account)
            .Include(ad => ad.AccountingEntry)
            .Take(5000)
            .ToListAsync();

        var trainingData = details
            .Where(ad => !string.IsNullOrWhiteSpace(ad.AccountingEntry.Description))
            .Select(ad => new Application.DTOs.ML.ExpenseClassificationData
            {
                Description = $"{ad.AccountingEntry.Description} {ad.Description ?? ""}".Trim(),
                Amount = (float)(ad.DebitAmount > 0 ? ad.DebitAmount : ad.CreditAmount),
                AccountId = ad.AccountId.ToString()
            })
            .ToList();

        if (trainingData.Count < 10) return;

        var uniqueAccounts = trainingData
            .Select(d => d.AccountId)
            .Distinct()
            .Select((id, idx) => new
            {
                Key = (uint)idx,
                AccountId = Guid.Parse(id),
                Info = details.First(ad => ad.AccountId.ToString() == id).Account
            })
            .ToDictionary(x => x.Key, x => new ExpenseClassificationService.AccountInfo(x.AccountId, x.Info.Code, x.Info.Name));

        _mlService.Train(trainingData, uniqueAccounts);
    }
}
