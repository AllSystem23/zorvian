using AutoMapper;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services.CommissionEngine;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class CommissionService : ICommissionService
{
    private readonly ICommissionRepository _repo;
    private readonly IMapper _mapper;
    private readonly ITenantContext _tenant;
    private readonly CommissionEngine.CommissionEngine _engine;
    private readonly IWebhookService _webhook;

    public CommissionService(
        ICommissionRepository repo,
        IMapper mapper,
        ITenantContext tenant,
        CommissionEngine.CommissionEngine engine,
        IWebhookService webhook)
    {
        _repo = repo;
        _mapper = mapper;
        _tenant = tenant;
        _engine = engine;
        _webhook = webhook;
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

    public async Task ProcessSaleForCommissionAsync(Guid saleId)
    {
        var companyId = Guid.Parse(_tenant.TenantId.Value.ToString());
        var tenantId = _tenant.TenantId.Value.ToString();

        var assignments = await _repo.GetAssignmentsByEmployeeIdAsync(Guid.Empty);
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

    public async Task ProcessEventAsync(CommissionEventRequest request)
    {
        // Lógica de procesamiento de eventos CRM
        // Ejemplo: Buscar comisiones activas para el colaborador y actualizar métricas
        var tenantId = _tenant.TenantId.Value.ToString();
        // Implementar la lógica específica según el tipo de evento
        await Task.CompletedTask;
    }

    public async Task ProcessSaleAsync(CommissionSaleRequest request)
    {
        // Lógica de procesamiento de ventas
        // Ejemplo: Invocar al CommissionEngine para calcular comisiones basadas en la venta
        var tenantId = _tenant.TenantId?.ToString() ?? string.Empty;
        if (!Guid.TryParse(tenantId, out var companyId))
            throw new InvalidOperationException("Tenant not configured");
        
        // Implementar lógica: obtener vendedores, calcular comisiones, guardar registros
        await Task.CompletedTask;
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
