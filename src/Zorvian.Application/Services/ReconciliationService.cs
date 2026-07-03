using AutoMapper;
using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class ReconciliationService
{
    private readonly IReconciliationRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public ReconciliationService(IReconciliationRepository repo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo;
        _tenant = tenant;
        _mapper = mapper;
    }

    private Guid RequireCompanyId()
    {
        if (_tenant.TenantId.Value == Guid.Empty)
            throw new InvalidOperationException("Seleccione una empresa antes de continuar.");
        return _tenant.TenantId.Value;
    }

    public async Task<ReconciliationResponse?> GetByIdAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;

        var details = entity.Details?.Select(d => new ReconciliationDetailResponse(
            d.Id, d.Reference, d.Amount, d.TransactionType, d.TransactionDate,
            d.Description, d.SourceType, d.MatchStatus, d.Notes
        )).ToList() ?? [];

        return new ReconciliationResponse(
            entity.Id, entity.BankAccountId, entity.BankAccount?.Bank?.Name ?? "",
            entity.BankAccount?.AccountNumber ?? "", entity.DateFrom, entity.DateTo,
            entity.ReconciledAt, entity.Status, entity.TotalTransactions,
            entity.MatchedCount, entity.UnmatchedCount, entity.TotalDebit,
            entity.TotalCredit, entity.Difference, entity.FileName, entity.Notes,
            entity.CreatedAt, details
        );
    }

    public async Task<PagedResult<ReconciliationListResponse>> GetFilteredAsync(ReconciliationFilterRequest filter)
    {
        var companyId = RequireCompanyId();
        var page = filter.Page ?? 1;
        var pageSize = filter.PageSize ?? 20;

        var items = await _repo.GetFilteredAsync(filter.BankAccountId, filter.Status, filter.DateFrom, filter.DateTo, companyId, page, pageSize);
        var total = await _repo.GetFilteredCountAsync(filter.BankAccountId, filter.Status, filter.DateFrom, filter.DateTo, companyId);

        var mapped = items.Select(i => new ReconciliationListResponse(
            i.Id, i.BankAccount?.AccountNumber ?? "", i.DateFrom, i.DateTo,
            i.Status, i.TotalTransactions, i.MatchedCount, i.UnmatchedCount,
            i.Difference, i.CreatedAt
        )).ToList();

        return new PagedResult<ReconciliationListResponse>(mapped, total, page, pageSize);
    }

    public async Task<ReconciliationResponse> CreateAsync(CreateReconciliationRequest request)
    {
        var companyId = RequireCompanyId();
        var entity = new Reconciliation
        {
            BankAccountId = request.BankAccountId,
            DateFrom = request.DateFrom,
            DateTo = request.DateTo,
            Notes = request.Notes,
            Status = "draft",
            TenantId = _tenant.TenantId.ToString(),
            CompanyId = companyId,
        };

        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();

        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<ReconciliationResponse?> UpdateAsync(Guid id, UpdateReconciliationRequest request)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;

        if (request.Status is not null) entity.Status = request.Status;
        if (request.Notes is not null) entity.Notes = request.Notes;

        if (request.Status == "completed" && entity.ReconciledAt is null)
        {
            entity.ReconciledAt = DateTime.UtcNow;
            entity.ReconciledBy = _tenant.CurrentUserId?.ToString();
        }

        await _repo.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return false;

        await _repo.DeleteAsync(entity);
        await _repo.SaveChangesAsync();
        return true;
    }

    public async Task<(int Imported, int Failed, string Message)> ImportBankStatementAsync(Guid reconciliationId, Stream fileStream, string fileName)
    {
        var reconciliation = await _repo.GetByIdAsync(reconciliationId)
            ?? throw new KeyNotFoundException("Reconciliation not found");

        var details = new List<ReconciliationDetail>();
        int failed = 0;

        using var reader = new StreamReader(fileStream);
        bool isHeader = true;

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Skip CSV header
            if (isHeader) { isHeader = false; continue; }

            var parts = line.Split(',');
            if (parts.Length < 3) { failed++; continue; }

            try
            {
                var detail = new ReconciliationDetail
                {
                    ReconciliationId = reconciliationId,
                    Reference = parts[0].Trim(),
                    Amount = decimal.TryParse(parts[1].Trim(), out var amt) ? amt : 0,
                    TransactionType = parts[2].Trim().ToLower() == "credit" ? "credit" : "debit",
                    TransactionDate = DateOnly.TryParse(parts.Length > 3 ? parts[3].Trim() : "", out var dt) ? dt : DateOnly.FromDateTime(DateTime.UtcNow),
                    Description = parts.Length > 4 ? parts[4].Trim() : null,
                    SourceType = "bank_statement",
                    MatchStatus = "unmatched",
                    TenantId = _tenant.TenantId.ToString(),
                    CompanyId = reconciliation.CompanyId,
                };
                details.Add(detail);
            }
            catch { failed++; }
        }

        if (details.Count == 0 && failed > 0)
            return (0, failed, "Error al procesar el archivo. Verifique el formato CSV (Reference,Amount,Type,Date,Description).");

        await _repo.AddDetailsBulkAsync(details);
        reconciliation.TotalTransactions = (await _repo.GetDetailsByReconciliationIdAsync(reconciliationId)).Count;
        reconciliation.UnmatchedCount = reconciliation.TotalTransactions;
        reconciliation.FileName = fileName;
        reconciliation.Status = "in_progress";
        await _repo.SaveChangesAsync();

        return (details.Count, failed, $"Importación completada: {details.Count} transacciones importadas, {failed} fallidas.");
    }

    public async Task<int> RunAutoMatchingAsync(Guid reconciliationId)
    {
        var details = await _repo.GetDetailsByReconciliationIdAsync(reconciliationId);
        var unmatched = details.Where(d => d.MatchStatus == "unmatched").ToList();

        // Group by reference and amount for automatic matching
        var matchedCount = 0;
        var matchedGroups = unmatched
            .GroupBy(d => new { d.Reference, d.Amount, d.TransactionType })
            .Where(g => g.Count() >= 2);

        foreach (var group in matchedGroups)
        {
            var items = group.ToList();
            var bankItems = items.Where(i => i.SourceType == "bank_statement").ToList();
            var systemItems = items.Where(i => i.SourceType == "system").ToList();

            foreach (var bankItem in bankItems)
            {
                var match = systemItems.FirstOrDefault(s => s.MatchStatus == "unmatched");
                if (match is null) break;

                bankItem.MatchStatus = "matched";
                bankItem.MatchedDetailId = match.Id;
                match.MatchStatus = "matched";
                match.MatchedDetailId = bankItem.Id;
                matchedCount++;
            }
        }

        var reconciliation = await _repo.GetByIdAsync(reconciliationId);
        if (reconciliation is not null)
        {
            var allDetails = await _repo.GetDetailsByReconciliationIdAsync(reconciliationId);
            reconciliation.MatchedCount = allDetails.Count(d => d.MatchStatus == "matched");
            reconciliation.UnmatchedCount = allDetails.Count(d => d.MatchStatus == "unmatched");
            reconciliation.TotalDebit = allDetails.Where(d => d.TransactionType == "debit").Sum(d => d.Amount);
            reconciliation.TotalCredit = allDetails.Where(d => d.TransactionType == "credit").Sum(d => d.Amount);
            reconciliation.Difference = reconciliation.TotalDebit - reconciliation.TotalCredit;
            await _repo.SaveChangesAsync();
        }

        return matchedCount;
    }

    public async Task<bool> ManualMatchAsync(Guid reconciliationId, Guid bankDetailId, Guid systemDetailId)
    {
        var details = await _repo.GetDetailsByReconciliationIdAsync(reconciliationId);
        var bankDetail = details.FirstOrDefault(d => d.Id == bankDetailId && d.SourceType == "bank_statement");
        var systemDetail = details.FirstOrDefault(d => d.Id == systemDetailId && d.SourceType == "system");

        if (bankDetail is null || systemDetail is null) return false;

        bankDetail.MatchStatus = "matched";
        bankDetail.MatchedDetailId = systemDetailId;
        systemDetail.MatchStatus = "matched";
        systemDetail.MatchedDetailId = bankDetailId;

        await _repo.SaveChangesAsync();
        return true;
    }
}
