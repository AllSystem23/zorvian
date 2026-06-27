using AutoMapper;
using Zorvian.Application.DTOs.Provider;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class ProviderService
{
    private readonly IProviderRepository _repo;
    private readonly IMapper _mapper;
    private readonly ITenantContext _tenant;
    private readonly IApprovalEngine? _approvalEngine;

    public ProviderService(IProviderRepository repo, IMapper mapper, ITenantContext tenant, IApprovalEngine? approvalEngine = null)
    {
        _repo = repo;
        _mapper = mapper;
        _tenant = tenant;
        _approvalEngine = approvalEngine;
    }

    public async Task<List<ServiceProvider>> GetProvidersAsync() =>
        await _repo.GetProvidersAsync();

    public async Task<ServiceProvider?> GetProviderByIdAsync(Guid id) =>
        await _repo.GetProviderByIdAsync(id);

    public async Task<ServiceProvider> CreateProviderAsync(ServiceProvider provider)
    {
        provider.TenantId = _tenant.TenantId.Value.ToString();
        await _repo.AddProviderAsync(provider);
        return provider;
    }

    public async Task<ServiceProvider?> UpdateProviderAsync(Guid id, ServiceProvider provider)
    {
        var existing = await _repo.GetProviderByIdAsync(id);
        if (existing is null) return null;
        _mapper.Map(provider, existing);
        await _repo.UpdateProviderAsync(existing);
        return existing;
    }

    public async Task<bool> DeleteProviderAsync(Guid id)
    {
        var existing = await _repo.GetProviderByIdAsync(id);
        if (existing is null) return false;
        await _repo.DeleteProviderAsync(id);
        return true;
    }

    public async Task<List<ServiceContract>> GetContractsByProviderAsync(Guid providerId) =>
        await _repo.GetContractsByProviderIdAsync(providerId);

    public async Task<List<ServiceContract>> GetAllContractsAsync() =>
        await _repo.GetAllContractsAsync();

    public async Task<List<PaymentMilestone>> GetMilestonesByContractAsync(Guid contractId) =>
        await _repo.GetMilestonesByContractIdAsync(contractId);

    public async Task<ServiceContract?> GetContractByIdAsync(Guid id) =>
        await _repo.GetContractByIdAsync(id);

    public async Task<ServiceContract> CreateContractAsync(ServiceContract contract)
    {
        contract.TenantId = _tenant.TenantId.Value.ToString();
        await _repo.AddContractAsync(contract);
        return contract;
    }

    public async Task<ServiceContract?> UpdateContractAsync(Guid id, ServiceContract contract)
    {
        var existing = await _repo.GetContractByIdAsync(id);
        if (existing is null) return null;
        _mapper.Map(contract, existing);
        await _repo.UpdateContractAsync(existing);
        return existing;
    }

    public async Task<bool> DeleteContractAsync(Guid id)
    {
        var existing = await _repo.GetContractByIdAsync(id);
        if (existing is null) return false;
        await _repo.DeleteContractAsync(id);
        return true;
    }

    public async Task<PaymentMilestone> CreateMilestoneAsync(PaymentMilestone milestone)
    {
        milestone.TenantId = _tenant.TenantId.Value.ToString();
        await _repo.AddMilestoneAsync(milestone);
        return milestone;
    }

    public async Task<PaymentMilestone?> UpdateMilestoneAsync(Guid id, PaymentMilestone milestone)
    {
        var existing = (await _repo.GetMilestonesByContractIdAsync(milestone.ServiceContractId))
            .FirstOrDefault(m => m.Id == id);
        if (existing is null) return null;
        _mapper.Map(milestone, existing);
        await _repo.UpdateMilestoneAsync(existing);
        return existing;
    }

    public async Task<bool> ApproveMilestoneAsync(Guid milestoneId)
    {
        var milestone = await _repo.GetMilestoneByIdAsync(milestoneId);
        if (milestone is null) return false;
        milestone.Status = "approved";
        await _repo.UpdateMilestoneAsync(milestone);
        return true;
    }

    public async Task<bool> CompleteMilestoneAsync(Guid milestoneId)
    {
        var milestone = await _repo.GetMilestoneByIdAsync(milestoneId);
        if (milestone is null) return false;
        milestone.Status = "completed";
        milestone.CompletionDate = DateOnly.FromDateTime(DateTime.UtcNow);
        await _repo.UpdateMilestoneAsync(milestone);
        return true;
    }

    public async Task<bool> DeleteMilestoneAsync(Guid milestoneId)
    {
        await _repo.DeleteMilestoneAsync(milestoneId);
        return true;
    }

    public async Task<ProviderInvoice> RegisterInvoiceAsync(ProviderInvoice invoice)
    {
        invoice.NetAmount = invoice.InvoiceAmount - invoice.WithholdingAmount;
        invoice.TenantId = _tenant.TenantId.Value.ToString();
        await _repo.AddInvoiceAsync(invoice);
        return invoice;
    }

    public async Task<List<ProviderInvoice>> GetAllInvoicesAsync() =>
        await _repo.GetAllInvoicesAsync();

    public async Task<ProviderInvoice?> ProgramPaymentAsync(Guid invoiceId, DateOnly paymentDate, string? paymentReference)
    {
        var invoice = await _repo.GetInvoiceByIdAsync(invoiceId);
        if (invoice is null) return null;

        invoice.Status = "scheduled";
        invoice.PaymentDate = paymentDate;
        invoice.PaymentReference = paymentReference;
        await _repo.UpdateInvoiceAsync(invoice);
        return invoice;
    }

    // ── Dashboard ──

    public async Task<ProviderDashboardDto> GetDashboardAsync(string? countryCode = null)
    {
        var providers = countryCode is not null
            ? await _repo.GetProvidersByCountryAsync(countryCode)
            : await _repo.GetProvidersAsync();
        var contracts = countryCode is not null
            ? await _repo.GetContractsByCountryAsync(countryCode)
            : await _repo.GetAllContractsAsync();
        var invoices = countryCode is not null
            ? await _repo.GetInvoicesByCountryAsync(countryCode)
            : await _repo.GetAllInvoicesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var allMilestones = contracts.SelectMany(c => c.Milestones).ToList();

        var overdueMilestones = allMilestones
            .Count(m => m.Status != "completed" && m.Status != "cancelled" && m.EstimatedDate < today);

        var pendingInvoices = invoices.Where(i => i.Status == "received" || i.Status == "verified").ToList();

        var recentContracts = contracts
            .OrderByDescending(c => c.CreatedAt)
            .Take(5)
            .Select(c => new ProviderContractSummary(
                c.Id, c.ContractNumber, c.ContractName,
                c.ServiceProvider?.BusinessName ?? "",
                c.TotalContractAmount, c.Status, c.StartDate, c.EndDate
            )).ToList();

        var milestonesByStatus = allMilestones
            .GroupBy(m => m.Status)
            .Select(g => new ProviderMilestoneByStatus(g.Key, g.Count()))
            .ToList();

        var topProviders = providers
            .Where(p => p.Contracts.Any())
            .Select(p => new ProviderTopProvider(
                p.Id, p.BusinessName,
                p.Contracts.Count,
                p.Contracts.Sum(c => c.TotalContractAmount)
            ))
            .OrderByDescending(t => t.TotalValue)
            .Take(5)
            .ToList();

        return new ProviderDashboardDto(
            TotalProviders: providers.Count,
            ActiveProviders: providers.Count(p => p.Status == "active"),
            TotalContracts: contracts.Count,
            ActiveContracts: contracts.Count(c => c.Status == "active"),
            TotalContractValue: contracts.Sum(c => c.TotalContractAmount),
            PendingMilestones: allMilestones.Count(m => m.Status == "pending" || m.Status == "in_progress"),
            OverdueMilestones: overdueMilestones,
            CompletedMilestones: allMilestones.Count(m => m.Status == "completed" || m.Status == "approved" || m.Status == "paid"),
            PendingInvoices: pendingInvoices.Count,
            PendingInvoiceAmount: pendingInvoices.Sum(i => i.NetAmount),
            RecentContracts: recentContracts,
            MilestonesByStatus: milestonesByStatus,
            TopProviders: topProviders
        );
    }

    // ── Approval-aware milestone completion ──

    public async Task<(bool success, Guid? approvalRequestId)> CompleteMilestoneWithApprovalAsync(
        Guid milestoneId, string requestedBy)
    {
        var milestone = await _repo.GetMilestoneByIdAsync(milestoneId);
        if (milestone is null) return (false, null);

        if (_approvalEngine is not null)
        {
            var evaluation = await _approvalEngine.EvaluateAsync(
                module: "providers",
                eventType: "milestone_completion",
                referenceId: milestoneId,
                amount: milestone.Amount,
                requestedBy: requestedBy
            );

            if (evaluation.RequiresApproval)
            {
                milestone.Status = "pending_approval";
                await _repo.UpdateMilestoneAsync(milestone);
                return (true, evaluation.ApprovalRequestId);
            }
        }

        milestone.Status = "completed";
        milestone.CompletionDate = DateOnly.FromDateTime(DateTime.UtcNow);
        await _repo.UpdateMilestoneAsync(milestone);
        return (true, null);
    }

    // ── Rankings ──

    public async Task<ProviderRankingDetailDto> GetRankingsAsync(string? countryCode = null)
    {
        var providers = countryCode is not null
            ? await _repo.GetProvidersByCountryAsync(countryCode)
            : await _repo.GetProvidersAsync();
        var contracts = countryCode is not null
            ? await _repo.GetContractsByCountryAsync(countryCode)
            : await _repo.GetAllContractsAsync();
        var allInvoices = countryCode is not null
            ? await _repo.GetInvoicesByCountryAsync(countryCode)
            : await _repo.GetAllInvoicesAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var invoiceLookup = allInvoices
            .GroupBy(i => i.PaymentMilestoneId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rankings = providers.Select(p =>
        {
            var providerContracts = contracts.Where(c => c.ServiceProviderId == p.Id).ToList();
            var allMilestones = providerContracts.SelectMany(c => c.Milestones).ToList();
            var completedMilestones = allMilestones
                .Where(m => m.Status is "completed" or "approved" or "paid").ToList();
            var onTimeMilestones = completedMilestones
                .Count(m => m.CompletionDate.HasValue && m.CompletionDate.Value <= m.EstimatedDate);

            var onTimeDeliveryScore = completedMilestones.Count > 0
                ? (decimal)onTimeMilestones / completedMilestones.Count * 100
                : 0m;

            var completedContracts = providerContracts.Count(c => c.Status == "completed");
            var contractCompletionScore = providerContracts.Count > 0
                ? (decimal)completedContracts / providerContracts.Count * 100
                : 0m;

            var providerInvoices = providerContracts
                .SelectMany(c => c.Milestones)
                .SelectMany(m => invoiceLookup.TryGetValue(m.Id, out var inv) ? inv : [])
                .ToList();
            var approvedInvoices = providerInvoices.Count(i => i.Status is "approved" or "paid" or "scheduled");
            var invoiceAccuracyScore = providerInvoices.Count > 0
                ? (decimal)approvedInvoices / providerInvoices.Count * 100
                : 0m;

            var overallScore = onTimeDeliveryScore * 0.4m + contractCompletionScore * 0.3m + invoiceAccuracyScore * 0.3m;

            var employeeName = p.Employee != null
                ? $"{p.Employee.FirstName} {p.Employee.LastName}"
                : "—";

            return new ProviderRankingDto(
                ProviderId: p.Id,
                BusinessName: p.BusinessName,
                EmployeeName: employeeName,
                Rank: 0,
                OverallScore: Math.Round(overallScore, 1),
                OnTimeDeliveryScore: Math.Round(onTimeDeliveryScore, 1),
                ContractCompletionScore: Math.Round(contractCompletionScore, 1),
                InvoiceAccuracyScore: Math.Round(invoiceAccuracyScore, 1),
                TotalContracts: providerContracts.Count,
                CompletedContracts: completedContracts,
                TotalMilestones: completedMilestones.Count,
                OnTimeMilestones: onTimeMilestones,
                TotalInvoices: providerInvoices.Count,
                AccurateInvoices: approvedInvoices,
                Trend: "stable"
            );
        })
        .OrderByDescending(r => r.OverallScore)
        .Select((r, i) => r with { Rank = i + 1 })
        .ToList();

        var avgScore = rankings.Count > 0
            ? rankings.Average(r => r.OverallScore)
            : 0m;

        return new ProviderRankingDetailDto(
            Rankings: rankings,
            AverageOverallScore: Math.Round(avgScore, 1),
            TotalProviders: providers.Count,
            GeneratedAt: DateTime.UtcNow
        );
    }

    public async Task<ProviderRankingHistoryDto> GetRankingHistoryAsync(string? countryCode = null)
    {
        var providers = countryCode is not null
            ? await _repo.GetProvidersByCountryAsync(countryCode)
            : await _repo.GetProvidersAsync();
        var contracts = countryCode is not null
            ? await _repo.GetContractsByCountryAsync(countryCode)
            : await _repo.GetAllContractsAsync();
        var allInvoices = countryCode is not null
            ? await _repo.GetInvoicesByCountryAsync(countryCode)
            : await _repo.GetAllInvoicesAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var invoiceLookup = allInvoices
            .GroupBy(i => i.PaymentMilestoneId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var months = Enumerable.Range(0, 6)
            .Select(i => today.AddMonths(-i))
            .OrderBy(d => d)
            .ToList();

        var seriesList = new List<ProviderRankingHistorySeries>();

        foreach (var provider in providers.Where(p => p.Contracts.Any()))
        {
            var providerContracts = contracts.Where(c => c.ServiceProviderId == provider.Id).ToList();
            var points = new List<ProviderRankingHistoryPoint>();

            foreach (var monthEnd in months)
            {
                var cutoff = DateOnly.FromDateTime(new DateTime(monthEnd.Year, monthEnd.Month, DateTime.DaysInMonth(monthEnd.Year, monthEnd.Month)));
                var monthLabel = monthEnd.ToString("yyyy-MM");

                var contractsAtMonth = providerContracts.Where(c => c.StartDate <= cutoff).ToList();
                var allMilestones = contractsAtMonth.SelectMany(c => c.Milestones).ToList();
                var completedMilestones = allMilestones
                    .Where(m => m.Status is "completed" or "approved" or "paid")
                    .Where(m => m.CompletionDate.HasValue && m.CompletionDate.Value <= cutoff)
                    .ToList();
                var onTimeMilestones = completedMilestones
                    .Count(m => m.CompletionDate!.Value <= m.EstimatedDate);

                var onTimeScore = completedMilestones.Count > 0
                    ? (decimal)onTimeMilestones / completedMilestones.Count * 100 : 0m;

                var completedContracts = contractsAtMonth.Count(c => c.Status == "completed" && c.EndDate.HasValue && c.EndDate.Value <= cutoff);
                var completionScore = contractsAtMonth.Count > 0
                    ? (decimal)completedContracts / contractsAtMonth.Count * 100 : 0m;

                var providerInvoices = contractsAtMonth
                    .SelectMany(c => c.Milestones)
                    .SelectMany(m => invoiceLookup.TryGetValue(m.Id, out var inv) ? inv : [])
                    .ToList();
                var approvedInvoices = providerInvoices.Count(i => i.Status is "approved" or "paid" or "scheduled");
                var accuracyScore = providerInvoices.Count > 0
                    ? (decimal)approvedInvoices / providerInvoices.Count * 100 : 0m;

                var overall = Math.Round(onTimeScore * 0.4m + completionScore * 0.3m + accuracyScore * 0.3m, 1);

                points.Add(new ProviderRankingHistoryPoint(monthLabel, overall,
                    Math.Round(onTimeScore, 1), Math.Round(completionScore, 1), Math.Round(accuracyScore, 1)));
            }

            seriesList.Add(new ProviderRankingHistorySeries(
                provider.Id, provider.BusinessName, points));
        }

        var allPoints = seriesList.SelectMany(s => s.Points).ToList();
        return new ProviderRankingHistoryDto(seriesList, allPoints, DateTime.UtcNow);
    }

    // ── Notifications ──

    public async Task<List<ProviderNotificationDto>> GetNotificationsAsync(string? countryCode = null)
    {
        var contracts = countryCode is not null
            ? await _repo.GetContractsByCountryAsync(countryCode)
            : await _repo.GetAllContractsAsync();
        var invoices = countryCode is not null
            ? await _repo.GetInvoicesByCountryAsync(countryCode)
            : await _repo.GetAllInvoicesAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var notifications = new List<ProviderNotificationDto>();

        foreach (var contract in contracts)
        {
            if (contract.Status == "active" && contract.EndDate.HasValue)
            {
                var daysUntilEnd = contract.EndDate.Value.DayNumber - today.DayNumber;
                if (daysUntilEnd <= 0)
                {
                    notifications.Add(new ProviderNotificationDto(
                        Guid.NewGuid(), "contract_expired",
                        $"Contrato '{contract.ContractNumber}' vencido",
                        $"El contrato con {contract.ServiceProvider?.BusinessName ?? ""} venció el {contract.EndDate:dd/MM/yyyy}",
                        "high", contract.Id, "contract"
                    ));
                }
                else if (daysUntilEnd <= 30)
                {
                    notifications.Add(new ProviderNotificationDto(
                        Guid.NewGuid(), "contract_expiring",
                        $"Contrato '{contract.ContractNumber}' vence en {daysUntilEnd} días",
                        $"El contrato con {contract.ServiceProvider?.BusinessName ?? ""} vence el {contract.EndDate:dd/MM/yyyy}",
                        daysUntilEnd <= 7 ? "high" : "medium", contract.Id, "contract"
                    ));
                }
            }

            foreach (var milestone in contract.Milestones)
            {
                if (milestone.Status is not ("completed" or "approved" or "paid" or "cancelled"))
                {
                    var daysUntilDue = milestone.EstimatedDate.DayNumber - today.DayNumber;
                    if (daysUntilDue < 0)
                    {
                        notifications.Add(new ProviderNotificationDto(
                            Guid.NewGuid(), "milestone_overdue",
                            $"Hito '{milestone.Name}' vencido",
                            $"El hito del contrato '{contract.ContractNumber}' venció hace {Math.Abs(daysUntilDue)} días",
                            "high", milestone.Id, "milestone"
                        ));
                    }
                    else if (daysUntilDue <= 7)
                    {
                        notifications.Add(new ProviderNotificationDto(
                            Guid.NewGuid(), "milestone_due_soon",
                            $"Hito '{milestone.Name}' vence en {daysUntilDue} días",
                            $"El hito del contrato '{contract.ContractNumber}' vence el {milestone.EstimatedDate:dd/MM/yyyy}",
                            "medium", milestone.Id, "milestone"
                        ));
                    }
                }
            }
        }

        foreach (var invoice in invoices.Where(i => i.Status == "received"))
        {
            notifications.Add(new ProviderNotificationDto(
                Guid.NewGuid(), "invoice_pending",
                $"Factura '{invoice.InvoiceNumber}' pendiente de verificación",
                $"Factura por {invoice.NetAmount:N2} {invoice.Currency} registrada y pendiente",
                "low", invoice.Id, "invoice"
            ));
        }

        return notifications.OrderByDescending(n => n.Severity switch { "high" => 3, "medium" => 2, _ => 1 }).ToList();
    }
}
