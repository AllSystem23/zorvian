using System.Text.Json;
using Zorvian.Application.DTOs.Report;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public interface ICustomReportService
{
    Task<List<CustomReportResponse>> GetAllAsync(string? userId = null);
    Task<CustomReportResponse?> GetByIdAsync(Guid id);
    Task<CustomReportResponse> CreateAsync(CreateCustomReportRequest request, string userId);
    Task<CustomReportResponse?> UpdateAsync(Guid id, UpdateCustomReportRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<ReportResult> ExecuteAsync(Guid reportId);
    Task<ReportResult> ExecuteAdHocAsync(string module, CreateCustomReportRequest request);
}

public sealed class CustomReportService : ICustomReportService
{
    private readonly ICustomReportRepository _repo;
    private readonly DynamicReportEngine _engine;
    private readonly ITenantContext _tenant;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public CustomReportService(ICustomReportRepository repo, DynamicReportEngine engine, ITenantContext tenant)
    {
        _repo = repo;
        _engine = engine;
        _tenant = tenant;
    }

    private Guid CompanyId =>
        Guid.TryParse(_tenant.TenantId, out var id) ? id : throw new InvalidOperationException("Invalid tenant");

    public async Task<List<CustomReportResponse>> GetAllAsync(string? userId = null)
    {
        var reports = await _repo.GetAllAsync(CompanyId);
        if (userId is not null && Guid.TryParse(userId, out var userGuid))
        {
            reports = reports.Where(r => r.IsPublic || r.CreatedByUserId == userGuid).ToList();
        }
        return reports.Select(ToResponse).ToList();
    }

    public async Task<CustomReportResponse?> GetByIdAsync(Guid id)
    {
        var report = await _repo.GetByIdAsync(id);
        return report is null ? null : ToResponse(report);
    }

    public async Task<CustomReportResponse> CreateAsync(CreateCustomReportRequest request, string userId)
    {
        var userGuid = Guid.TryParse(userId, out var id) ? id : CompanyId;

        var entity = new CustomReport
        {
            Name = request.Name,
            Description = request.Description,
            Module = request.Module.ToLowerInvariant(),
            FieldsJson = JsonSerializer.Serialize(request.Fields, JsonOpts),
            FiltersJson = JsonSerializer.Serialize(request.Filters, JsonOpts),
            GroupByField = request.GroupByField,
            SortByField = request.SortByField,
            SortOrder = request.SortOrder,
            IsPublic = request.IsPublic,
            CreatedByUserId = userGuid,
            CompanyId = CompanyId,
        };
        var created = await _repo.AddAsync(entity);
        return ToResponse(created);
    }

    public async Task<CustomReportResponse?> UpdateAsync(Guid id, UpdateCustomReportRequest request)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.Module = request.Module.ToLowerInvariant();
        entity.FieldsJson = JsonSerializer.Serialize(request.Fields, JsonOpts);
        entity.FiltersJson = JsonSerializer.Serialize(request.Filters, JsonOpts);
        entity.GroupByField = request.GroupByField;
        entity.SortByField = request.SortByField;
        entity.SortOrder = request.SortOrder;
        entity.IsPublic = request.IsPublic;

        var updated = await _repo.UpdateAsync(entity);
        return ToResponse(updated!);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _repo.DeleteAsync(id);
    }

    public async Task<ReportResult> ExecuteAsync(Guid reportId)
    {
        var report = await _repo.GetByIdAsync(reportId);
        if (report is null) throw new KeyNotFoundException("Report not found");

        var fields = JsonSerializer.Deserialize<List<ReportField>>(report.FieldsJson, JsonOpts) ?? [];
        var filters = JsonSerializer.Deserialize<List<ReportFilter>>(report.FiltersJson, JsonOpts) ?? [];

        return await _engine.ExecuteAsync(
            report.Module, fields, filters,
            report.GroupByField, report.SortByField, report.SortOrder,
            CompanyId);
    }

    public async Task<ReportResult> ExecuteAdHocAsync(string module, CreateCustomReportRequest request)
    {
        return await _engine.ExecuteAsync(
            module, request.Fields, request.Filters,
            request.GroupByField, request.SortByField, request.SortOrder,
            CompanyId);
    }

    private CustomReportResponse ToResponse(CustomReport r)
    {
        var fields = JsonSerializer.Deserialize<List<ReportField>>(r.FieldsJson, JsonOpts) ?? [];
        var filters = JsonSerializer.Deserialize<List<ReportFilter>>(r.FiltersJson, JsonOpts) ?? [];

        return new CustomReportResponse(
            r.Id, r.Name, r.Description, r.Module,
            fields, filters,
            r.GroupByField, r.SortByField, r.SortOrder,
            r.IsPublic, r.CreatedByUserId, r.CreatedAt
        );
    }
}
