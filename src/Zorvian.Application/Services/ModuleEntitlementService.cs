using Zorvian.Core.Enums;

namespace Zorvian.Application.Services;

public interface IModuleEntitlementService
{
    Task<bool> IsModuleEnabledAsync(string planId, ModuleType module);
    Task<IReadOnlySet<ModuleType>> GetEnabledModulesAsync(string planId);
}
