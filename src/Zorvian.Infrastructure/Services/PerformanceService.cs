using Zorvian.Application.DTOs.Performance;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Zorvian.Core.Interfaces;

namespace Zorvian.Infrastructure.Services;

public sealed class PerformanceService
{
    private readonly ZorvianDbContext _db;
    private readonly ITenantContext _tenant;

    public PerformanceService(ZorvianDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<ObjectiveResponse> CreateObjectiveAsync(CreateObjectiveRequest request)
    {
        var objective = new Objective
        {
            Title = request.Title,
            Description = request.Description,
            EmployeeId = request.EmployeeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TenantId = _tenant.TenantId
        };
        _db.Objectives.Add(objective);
        await _db.SaveChangesAsync();
        return MapToResponse(objective);
    }

    public async Task<KeyResultResponse> AddKeyResultAsync(Guid objectiveId, CreateKeyResultRequest request)
    {
        var kr = new KeyResult
        {
            ObjectiveId = objectiveId,
            Title = request.Title,
            TargetValue = request.TargetValue,
            CurrentValue = 0,
            TenantId = _tenant.TenantId
        };
        _db.KeyResults.Add(kr);
        await _db.SaveChangesAsync();
        return MapKeyResultToResponse(kr);
    }

    public async Task<KeyResultResponse> UpdateKeyResultAsync(Guid krId, UpdateKeyResultRequest request)
    {
        var kr = await _db.KeyResults.FindAsync(krId);
        if (kr == null) throw new KeyNotFoundException("Key Result not found");

        kr.CurrentValue = request.CurrentValue;
        await _db.SaveChangesAsync();
        return MapKeyResultToResponse(kr);
    }

    public async Task<List<ObjectiveResponse>> GetObjectivesAsync(Guid employeeId)
    {
        // HasQueryFilter already handles TenantId + IsDeleted
        var objectives = await _db.Objectives
            .Include(o => o.KeyResults)
            .Where(o => o.EmployeeId == employeeId)
            .ToListAsync();
            
        return objectives.Select(MapToResponse).ToList();
    }

    private static ObjectiveResponse MapToResponse(Objective o) => new(
        o.Id, o.Title, o.Description, o.StartDate, o.EndDate, o.IsActive,
        o.KeyResults.Select(MapKeyResultToResponse).ToList()
    );

    private static KeyResultResponse MapKeyResultToResponse(KeyResult kr) => new(
        kr.Id, kr.Title, kr.TargetValue, kr.CurrentValue,
        kr.TargetValue > 0 ? (kr.CurrentValue / kr.TargetValue) * 100 : 0
    );
}
