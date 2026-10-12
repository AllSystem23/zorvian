using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Services;
using Zorvian.Core.Enums;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Services;

public sealed class ModuleEntitlementService : IModuleEntitlementService
{
    private readonly ZorvianDbContext _db;

    public ModuleEntitlementService(ZorvianDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Si el plan no tiene ninguna configuración (p. ej. tenants creados antes
    /// de habilitar el licenciamiento, o entornos sin seed), se considera
    /// permisivo para no romper instalaciones existentes. Sólo cuando el plan
    /// tiene mappings se aplica el filtro estricto.
    /// </summary>
    public async Task<bool> IsModuleEnabledAsync(string planId, ModuleType module)
    {
        var rows = await _db.PlanModules
            .AsNoTracking()
            .Where(pm => pm.PlanId == planId)
            .Select(pm => new { pm.Module, pm.IsEnabled })
            .ToListAsync();

        if (rows.Count == 0)
            return true;

        return rows.Any(pm => pm.Module == module && pm.IsEnabled);
    }

    public async Task<IReadOnlySet<ModuleType>> GetEnabledModulesAsync(string planId)
    {
        var rows = await _db.PlanModules
            .AsNoTracking()
            .Where(pm => pm.PlanId == planId)
            .Select(pm => new { pm.Module, pm.IsEnabled })
            .ToListAsync();

        if (rows.Count == 0)
            return Enum.GetValues<ModuleType>().ToHashSet();

        return rows.Where(pm => pm.IsEnabled).Select(pm => pm.Module).ToHashSet();
    }
}
