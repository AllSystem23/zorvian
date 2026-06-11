using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services.CommissionEngine;

public sealed class CommissionEngine
{
    private readonly RuleEvaluator _ruleEvaluator;
    private readonly CommissionCalculator _calculator;
    private readonly ICommissionDataSource _dataSource;
    private readonly ICommissionRepository _repo;

    public CommissionEngine(
        RuleEvaluator ruleEvaluator,
        CommissionCalculator calculator,
        ICommissionDataSource dataSource,
        ICommissionRepository repo)
    {
        _ruleEvaluator = ruleEvaluator;
        _calculator = calculator;
        _dataSource = dataSource;
        _repo = repo;
    }

    public async Task<List<CommissionRecord>> CalculateForPeriodAsync(
        Guid periodId, Guid companyId, string tenantId)
    {
        var records = new List<CommissionRecord>();
        var assignments = await _dataSource.GetActiveAssignmentsAsync(companyId);

        foreach (var assignment in assignments)
        {
            if (assignment.CommissionScheme is null) continue;
            var scheme = assignment.CommissionScheme;
            if (scheme.Status != "active") continue;

            var rules = await _repo.GetRulesBySchemeIdAsync(scheme.Id);

            var sales = await _dataSource.GetSalesByPeriodAsync(
                periodId, companyId, assignment.EmployeeId);
            var collections = await _dataSource.GetCollectionsByPeriodAsync(
                periodId, companyId, assignment.EmployeeId);

            foreach (var sale in sales)
            {
                ProcessSale(assignment, scheme, rules, sale, records, collections, companyId, tenantId);
            }

            if (scheme.CommissionType == "collection" && collections.Any())
            {
                foreach (var collection in collections)
                {
                    records.Add(CreateRecord(assignment, scheme, null,
                        collection.Amount, collection.Amount, periodId,
                        "collection", companyId, tenantId));
                }
            }
        }

        if (records.Any())
            await _repo.AddRecordsAsync(records);

        return records;
    }

    private void ProcessSale(
        CommissionAssignment assignment,
        CommissionScheme scheme,
        List<CommissionRule> rules,
        Sale sale,
        List<CommissionRecord> records,
        List<SalePayment> collections,
        Guid companyId,
        string tenantId)
    {
        var saleCollections = collections
            .Where(c => c.SaleId == sale.Id)
            .Sum(c => c.Amount);

        var profit = sale.Total;
        try
        {
            profit = sale.Details?.Sum(d => d.Subtotal - (d.UnitPrice * d.Quantity * 0.6m)) ?? sale.Total;
        }
        catch { }

        var productDetails = sale.Details?.ToList() ?? [];

        foreach (var rule in rules.OrderBy(r => r.Priority))
        {
            foreach (var detail in productDetails)
            {
                var product = detail.Product;
                var context = new RuleEvaluationContext
                {
                    EmployeeId = assignment.EmployeeId,
                    SaleId = sale.Id,
                    SaleType = sale.SaleType,
                    SaleAmount = detail.Subtotal,
                    CollectionAmount = saleCollections,
                    ProfitAmount = profit,
                    ProfitMargin = sale.Total > 0 ? profit / sale.Total : 0,
                    ProductLine = "",
                    ProductCategory = product?.Category?.Name ?? "",
                    Brand = product?.Brand?.Name ?? "",
                    BranchId = sale.BranchId.ToString(),
                    SaleDate = sale.SaleDate
                };

                if (!_ruleEvaluator.Evaluate(rule, context))
                    continue;

                var baseAmount = _ruleEvaluator.DetermineBaseAmount(context, rule);
                var commissionAmount = _ruleEvaluator.CalculateAmount(baseAmount, rule, _calculator);

                if (commissionAmount > 0)
                {
                    records.Add(new CommissionRecord
                    {
                        EmployeeId = assignment.EmployeeId,
                        CommissionAssignmentId = assignment.Id,
                        PayrollPeriodId = Guid.Empty,
                        SaleId = sale.Id,
                        SourceType = scheme.CommissionType,
                        BaseAmount = baseAmount,
                        Amount = commissionAmount,
                        Status = "calculated",
                        CommissionRuleId = rule.Id.ToString(),
                        TransactionDate = sale.SaleDate,
                        CompanyId = companyId,
                        TenantId = tenantId
                    });
                }
            }
        }
    }

    private static CommissionRecord CreateRecord(
        CommissionAssignment assignment,
        CommissionScheme scheme,
        Guid? saleId,
        decimal amount,
        decimal baseAmount,
        Guid periodId,
        string sourceType,
        Guid companyId,
        string tenantId)
    {
        return new CommissionRecord
        {
            EmployeeId = assignment.EmployeeId,
            CommissionAssignmentId = assignment.Id,
            PayrollPeriodId = periodId,
            SaleId = saleId,
            SourceType = sourceType,
            BaseAmount = baseAmount,
            Amount = amount,
            Status = "calculated",
            CompanyId = companyId,
            TenantId = tenantId,
            Description = $"Commission from {sourceType} ({scheme.Name})"
        };
    }
}
