using Microsoft.Extensions.Caching.Memory;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services.GoalEngine;

public sealed class GoalEngine
{
    private readonly IGoalRepository _repo;
    private readonly IGoalEvaluator _evaluator;
    private readonly IMemoryCache _cache;
    private readonly ITenantContextWriter _tenantWriter;

    public GoalEngine(IGoalRepository repo, IGoalEvaluator evaluator, IMemoryCache cache, ITenantContextWriter tenantWriter)
    {
        _repo = repo;
        _evaluator = evaluator;
        _cache = cache;
        _tenantWriter = tenantWriter;
    }

    public async Task ProcessProgressAsync(Guid assignmentId, decimal newProgress)
    {
        var assignment = await _repo.GetGoalAssignmentByIdAsync(assignmentId);
        if (assignment == null) return;

        // Corre en Hangfire (sin request HTTP): adopta el tenant de la asignación
        // para que el resto de lecturas y el GoalProgress persistido queden en la
        // compañía correcta (el repo ya cargó por bypass con Id conocido).
        if (!string.IsNullOrEmpty(assignment.TenantId))
            _tenantWriter.SetTenantId(TenantId.FromString(assignment.TenantId));

        var cacheKey = $"GoalDef_{assignment.GoalDefinitionId}";
        if (!_cache.TryGetValue(cacheKey, out GoalDefinition? definition))
        {
            definition = await _repo.GetGoalDefinitionByIdAsync(assignment.GoalDefinitionId);
            if (definition != null)
            {
                _cache.Set(cacheKey, definition, TimeSpan.FromMinutes(30));
            }
        }

        if (definition == null) return;

        var result = await _evaluator.EvaluateAsync(assignment, definition, newProgress);
        
        await _repo.AddGoalProgressAsync(new GoalProgress
        {
            GoalAssignmentId = assignment.Id,
            CurrentValue = result.Value,
            CompliancePercentage = assignment.TargetValue > 0 ? (newProgress / assignment.TargetValue) * 100 : 0,
            EvaluationDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PeriodKey = DateTime.UtcNow.ToString("yyyy-MM")
        });
    }
}
