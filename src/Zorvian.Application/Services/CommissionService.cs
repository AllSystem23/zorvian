using AutoMapper;
using Microsoft.Extensions.Logging;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services.CommissionEngine;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class CommissionService : ICommissionService
{
    private readonly ICommissionRepository _repo;
    private readonly ISaleRepository _saleRepo;
    private readonly IMapper _mapper;
    private readonly ITenantContext _tenant;
    private readonly CommissionEngine.CommissionEngine _engine;
    private readonly IWebhookService _webhook;
    private readonly ILogger<CommissionService> _logger;
    private readonly RuleEvaluator _ruleEvaluator;
    private readonly CommissionCalculator _calculator;
    private readonly ITenantContextWriter _tenantWriter;

    public CommissionService(
        ICommissionRepository repo,
        ISaleRepository saleRepo,
        IMapper mapper,
        ITenantContext tenant,
        CommissionEngine.CommissionEngine engine,
        IWebhookService webhook,
        ILogger<CommissionService> logger,
        RuleEvaluator ruleEvaluator,
        CommissionCalculator calculator,
        ITenantContextWriter tenantWriter)
    {
        _repo = repo;
        _saleRepo = saleRepo;
        _mapper = mapper;
        _tenant = tenant;
        _engine = engine;
        _webhook = webhook;
        _logger = logger;
        _ruleEvaluator = ruleEvaluator;
        _calculator = calculator;
        _tenantWriter = tenantWriter;
    }

    public async Task<List<CommissionScheme>> GetSchemesAsync() =>
        await _repo.GetSchemesAsync();

    public async Task<CommissionScheme?> GetSchemeByIdAsync(Guid id) =>
        await _repo.GetSchemeByIdAsync(id);

    public async Task<CommissionScheme> CreateSchemeAsync(CommissionScheme scheme)
    {
        scheme.TenantId = _tenant.TenantId.Value.ToString();
        await _repo.AddSchemeAsync(scheme);
        return scheme;
    }

    public async Task<CommissionScheme?> UpdateSchemeAsync(Guid id, CommissionScheme scheme)
    {
        var existing = await _repo.GetSchemeByIdAsync(id);
        if (existing is null) return null;
        _mapper.Map(scheme, existing);
        await _repo.UpdateSchemeAsync(existing);
        return existing;
    }

    public async Task<bool> DeleteSchemeAsync(Guid id)
    {
        var existing = await _repo.GetSchemeByIdAsync(id);
        if (existing is null) return false;
        await _repo.DeleteSchemeAsync(id);
        return true;
    }

    public async Task<List<CommissionRule>> GetRulesBySchemeAsync(Guid schemeId) =>
        await _repo.GetRulesBySchemeIdAsync(schemeId);

    public async Task<CommissionRule> AddRuleAsync(CommissionRule rule)
    {
        rule.TenantId = _tenant.TenantId.Value.ToString();
        await _repo.AddRuleAsync(rule);
        return rule;
    }

    public async Task UpdateRuleAsync(CommissionRule rule) =>
        await _repo.UpdateRuleAsync(rule);

    public async Task<bool> DeleteRuleAsync(Guid ruleId)
    {
        await _repo.DeleteRuleAsync(ruleId);
        return true;
    }

    public async Task<List<CommissionAssignment>> GetAssignmentsBySchemeAsync(Guid schemeId) =>
        await _repo.GetAssignmentsBySchemeIdAsync(schemeId);

    public async Task<List<CommissionAssignment>> GetAssignmentsByEmployeeAsync(Guid employeeId) =>
        await _repo.GetAssignmentsByEmployeeIdAsync(employeeId);

    public async Task<CommissionAssignment> AssignEmployeeAsync(CommissionAssignment assignment)
    {
        assignment.TenantId = _tenant.TenantId.Value.ToString();
        await _repo.AddAssignmentAsync(assignment);
        return assignment;
    }

    public async Task<bool> UnassignEmployeeAsync(Guid assignmentId)
    {
        var existing = await _repo.GetAssignmentsBySchemeIdAsync(assignmentId);
        if (existing.FirstOrDefault() is null) return false;
        await _repo.DeleteSchemeAsync(assignmentId);
        return true;
    }

    public async Task<List<CommissionRecord>> CalculateCommissionsAsync(Guid periodId)
    {
        if (!Guid.TryParse(_tenant.TenantId?.ToString(), out var companyId))
            throw new InvalidOperationException("Tenant not configured");
        var tenantId = _tenant.TenantId.Value.ToString();

        var records = await _engine.CalculateForPeriodAsync(periodId, companyId, tenantId);

        await _webhook.PublishAsync(tenantId, "commission.calculated", new
        {
            PeriodId = periodId,
            RecordCount = records.Count,
            TotalCommissions = records.Sum(r => r.Amount)
        });

        return records;
    }

    public async Task<bool> ApproveCommissionAsync(Guid recordId)
    {
        var record = await _repo.GetRecordByIdAsync(recordId);
        if (record is null) return false;

        record.Status = "approved";
        await _repo.UpdateRecordAsync(record);
        await _repo.SaveChangesAsync();

        await _webhook.PublishAsync(_tenant.TenantId.Value.ToString(), "commission.approved", new
        {
            RecordId = recordId,
            EmployeeId = record.EmployeeId,
            Amount = record.Amount
        });

        return true;
    }

    public async Task<List<CommissionRecord>> GetCommissionRecordsByPeriodAsync(Guid periodId)
    {
        if (!Guid.TryParse(_tenant.TenantId?.ToString(), out var companyId))
            throw new InvalidOperationException("Tenant not configured");
        return await _repo.GetRecordsByPeriodAsync(periodId, companyId);
    }

    public async Task ProcessCommissionForSaleAsync(Guid saleId, Guid companyId, Guid employeeId, DateTime saleDate, string saleType)
    {
        var tenantId = companyId.ToString();

        // Set tenant context explicitly — this method runs in background (Hangfire) outside HTTP scope
        _tenantWriter.SetTenantId(tenantId);
        _logger.LogDebug("Set tenant context to {TenantId} for commission processing of SaleId={SaleId}", tenantId, saleId);

        // ── Step 1: Load the full Sale from DB with Details + Product costs ──
        var sale = await _saleRepo.GetByIdAsync(saleId);
        if (sale is null)
        {
            _logger.LogWarning("SaleId={SaleId} not found — aborting commission processing", saleId);
            return;
        }

        // ── Step 2: Calculate real profit using Product.CostPrice ──
        // Profit = Total sale amount - Cost of Goods Sold
        // Cost of Goods Sold = Σ(Quantity × Product.CostPrice) per detail
        var totalCost = sale.Details?.Sum(d => d.Quantity * (d.Product?.CostPrice ?? 0)) ?? 0m;
        var profitAmount = sale.Total - totalCost;
        var profitMargin = sale.Total > 0 ? profitAmount / sale.Total : 0;

        _logger.LogDebug(
            "SaleId={SaleId}: Total={Total:F2}, COGS={TotalCost:F2}, Profit={Profit:F2}, Margin={Margin:P2}",
            saleId, sale.Total, totalCost, profitAmount, profitMargin);

        // ── Step 3: Get active commission assignments for the employee ──
        var assignments = await _repo.GetAssignmentsByEmployeeIdAsync(employeeId);
        var activeAssignments = assignments
            .Where(a => a.IsActive
                && a.CommissionScheme?.Status == "active"
                && a.EffectiveDate <= DateOnly.FromDateTime(DateTime.UtcNow)
                && (!a.ExpirationDate.HasValue || a.ExpirationDate >= DateOnly.FromDateTime(DateTime.UtcNow)))
            .ToList();

        if (activeAssignments.Count == 0)
        {
            _logger.LogDebug(
                "No active commission assignments for EmployeeId={EmployeeId}, SaleId={SaleId}",
                employeeId, saleId);
            return;
        }

        _logger.LogInformation(
            "Processing commissions for SaleId={SaleId}, EmployeeId={EmployeeId}, Total={Total}, Profit={Profit}, Assignments={Count}",
            saleId, employeeId, sale.Total, profitAmount, activeAssignments.Count);

        var records = new List<CommissionRecord>();

        foreach (var assignment in activeAssignments)
        {
            var scheme = assignment.CommissionScheme!;
            var rules = await _repo.GetRulesBySchemeIdAsync(scheme.Id);

            if (rules.Count == 0)
            {
                _logger.LogDebug(
                    "Scheme {SchemeId} ({SchemeName}) has no rules — skipping",
                    scheme.Id, scheme.Name);
                continue;
            }

            // Evaluate rules per-detail for maximum granularity (same pattern as CommissionEngine.ProcessSale)
            // When details exist, each line item is evaluated individually with its own profit calculation.
            if (sale.Details?.Count > 0)
            {
                foreach (var detail in sale.Details)
                {
                    if (detail.Product is null) continue;

                    var detailProfit = detail.Subtotal - (detail.Quantity * detail.Product.CostPrice);

                    foreach (var rule in rules.OrderBy(r => r.Priority))
                    {
                        var context = new RuleEvaluationContext
                        {
                            EmployeeId = employeeId,
                            SaleId = saleId,
                            SaleType = saleType,
                            SaleAmount = detail.Subtotal,
                            CollectionAmount = 0,
                            ProfitAmount = detailProfit,
                            ProfitMargin = detail.Subtotal > 0 ? detailProfit / detail.Subtotal : 0,
                            ProductLine = detail.Product.Category?.Name ?? string.Empty,
                            ProductCategory = detail.Product.Category?.Name ?? string.Empty,
                            Brand = detail.Product.Brand?.Name ?? string.Empty,
                            BranchId = sale.BranchId.ToString(),
                            SaleDate = saleDate
                        };

                        await ProcessRule(assignment, scheme, rule, context, records, tenantId, saleId, saleDate);
                    }
                }
            }
            else
            {
                // Fallback: no details loaded — use sale-level values
                foreach (var rule in rules.OrderBy(r => r.Priority))
                {
                    var context = new RuleEvaluationContext
                    {
                        EmployeeId = employeeId,
                        SaleId = saleId,
                        SaleType = saleType,
                        SaleAmount = sale.Total,
                        CollectionAmount = 0,
                        ProfitAmount = profitAmount,
                        ProfitMargin = profitMargin,
                        ProductLine = string.Empty,
                        ProductCategory = string.Empty,
                        Brand = string.Empty,
                        BranchId = sale.BranchId.ToString(),
                        SaleDate = saleDate
                    };

                    await ProcessRule(assignment, scheme, rule, context, records, tenantId, saleId, saleDate);
                }
            }
            }

        if (records.Count > 0)
        {
            await _repo.AddRecordsAsync(records);
            await _repo.SaveChangesAsync();

            _logger.LogInformation(
                "Commissions processed for SaleId={SaleId}: {Count} records created, Total={TotalAmount:F2}",
                saleId, records.Count, records.Sum(r => r.Amount));
        }
        else
        {
            _logger.LogDebug(
                "No commission records created for SaleId={SaleId} — rules may require more conditions (e.g., product category, branch)",
                saleId);
        }
    }

    private Task ProcessRule(
        CommissionAssignment assignment,
        CommissionScheme scheme,
        CommissionRule rule,
        RuleEvaluationContext context,
        List<CommissionRecord> records,
        string tenantId,
        Guid saleId,
        DateTime saleDate)
    {
        if (!_ruleEvaluator.Evaluate(rule, context))
        {
            _logger.LogDebug(
                "Rule {RuleId} (Priority={Priority}) did not match for SaleId={SaleId}",
                rule.Id, rule.Priority, saleId);
            return Task.CompletedTask;
        }

        var baseAmount = _ruleEvaluator.DetermineBaseAmount(context, rule);
        var commissionAmount = _ruleEvaluator.CalculateAmount(baseAmount, rule, _calculator);

        if (commissionAmount <= 0) return Task.CompletedTask;

        records.Add(new CommissionRecord
        {
            EmployeeId = assignment.EmployeeId,
            CommissionAssignmentId = assignment.Id,
            PayrollPeriodId = Guid.Empty,
            SaleId = saleId,
            SourceType = "sale",
            BaseAmount = baseAmount,
            Amount = commissionAmount,
            Status = "calculated",
            CommissionRuleId = rule.Id.ToString(),
            TransactionDate = saleDate,
            Description = $"Comisión automática — {scheme.Name}: ${commissionAmount:F2}",
            TenantId = tenantId
        });

        _logger.LogDebug(
            "CommissionRecord created: EmployeeId={EmployeeId}, Scheme={Scheme}, Amount={Amount}, Rule={RuleId}",
            assignment.EmployeeId, scheme.Name, commissionAmount, rule.Id);

        return Task.CompletedTask;
    }

    public async Task<int> ClawbackCommissionsBySaleAsync(Guid saleId, Guid companyId)
    {
        var tenantId = companyId.ToString();

        // Set tenant context explicitly — runs in background (Hangfire)
        _tenantWriter.SetTenantId(tenantId);

        var records = await _repo.GetRecordsBySaleIdAsync(saleId);

        // Only claw back records that are still "calculated" or "approved"
        var clawable = records.Where(r => r.Status is "calculated" or "approved").ToList();

        if (clawable.Count == 0)
        {
            _logger.LogInformation(
                "No clawable commission records found for SaleId={SaleId} — all already clawed back or finalized",
                saleId);
            return 0;
        }

        foreach (var record in clawable)
        {
            record.Status = "clawed_back";
            await _repo.UpdateRecordAsync(record);
        }

        await _repo.SaveChangesAsync();

        // Publish webhook event for each clawed-back record
        foreach (var record in clawable)
        {
            await _webhook.PublishAsync(tenantId, "commission.clawback", new
            {
                RecordId = record.Id,
                EmployeeId = record.EmployeeId,
                SaleId = saleId,
                Amount = -record.Amount,
                OriginalAmount = record.Amount,
                Status = record.Status
            });
        }

        _logger.LogInformation(
            "Clawed back {Count} commission records for SaleId={SaleId}, TotalAmount={Total:F2}",
            clawable.Count, saleId, clawable.Sum(r => r.Amount));

        return clawable.Count;
    }

    public async Task<bool> ClawbackCommissionAsync(Guid recordId)
    {
        var record = await _repo.GetRecordByIdAsync(recordId);
        if (record is null || record.Status == "clawed_back") return false;

        record.Status = "clawed_back";
        await _repo.UpdateRecordAsync(record);
        await _repo.SaveChangesAsync();

        await _webhook.PublishAsync(_tenant.TenantId.Value.ToString(), "commission.clawback", new
        {
            RecordId = recordId,
            EmployeeId = record.EmployeeId,
            Amount = -record.Amount,
            OriginalAmount = record.Amount
        });

        return true;
    }

    public Task ProcessEventAsync(CommissionEventRequest request)
    {
        // Lógica de procesamiento de eventos CRM
        // Ejemplo: Buscar comisiones activas para el colaborador y actualizar métricas
        var tenantId = _tenant.TenantId.Value.ToString();
        // Implementar la lógica específica según el tipo de evento
        return Task.CompletedTask;
    }

    public async Task ProcessSaleAsync(CommissionSaleRequest request)
    {
        // Webhook/Cron-driven commission processing for external integrations
        var tenantId = _tenant.TenantId?.ToString() ?? string.Empty;
        if (!Guid.TryParse(tenantId, out var companyId))
            throw new InvalidOperationException("Tenant not configured");

        if (!Guid.TryParse(request.SaleId, out var saleId) ||
            !Guid.TryParse(request.SalespersonId, out var employeeId))
        {
            _logger.LogWarning("Invalid SaleId or SalespersonId in CommissionSaleRequest");
            return;
        }

        await ProcessCommissionForSaleAsync(
            saleId, companyId, employeeId,
            request.InvoiceDate,
            string.Empty);
    }
}

public record CommissionEventRequest(
    string EventType,
    string SourceId,
    string SalespersonId,
    decimal Amount,
    string Currency,
    DateTime Timestamp,
    Dictionary<string, string> Metadata
);

public record CommissionSaleRequest(
    string SaleId,
    string InvoiceId,
    string SalespersonId,
    DateTime InvoiceDate,
    decimal InvoiceAmount,
    string Currency,
    decimal ProfitAmount,
    decimal ProfitMargin,
    List<SaleProduct> Products,
    string BranchId,
    string ClientId
);

public record SaleProduct(
    string ProductId,
    string ProductName,
    string ProductLine,
    string Category,
    string Brand,
    int Quantity,
    decimal UnitPrice,
    decimal Cost
);
