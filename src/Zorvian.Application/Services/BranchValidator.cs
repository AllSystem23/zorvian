using Zorvian.Application.Interfaces;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

/// <summary>
/// Implementación por defecto de <see cref="IBranchValidator"/>.
/// La validación se hace contra BD: un BranchId que no pertenezca a la compañía
/// operativa (por query filters de tenant o por comparación directa de CompanyId)
/// se rechaza con una excepción explícita.
/// </summary>
public sealed class BranchValidator : IBranchValidator
{
    private readonly IBranchRepository _branchRepo;
    private readonly ITenantContext _tenant;

    public BranchValidator(IBranchRepository branchRepo, ITenantContext tenant)
    {
        _branchRepo = branchRepo;
        _tenant = tenant;
    }

    public async Task<Guid?> ResolveForWriteAsync(Guid? requestedBranchId)
    {
        var contextBranch = _tenant.ResolveBranchId();

        if (requestedBranchId is null || requestedBranchId == Guid.Empty)
            return contextBranch;

        var companyId = _tenant.RequireCompanyId();

        // SuperAdmin sin compañía seleccionada: sin referencia concreta que validar.
        if (companyId == Guid.Empty)
            return requestedBranchId.Value;

        var branch = await _branchRepo.GetByIdAsync(requestedBranchId.Value);
        if (branch is null || branch.CompanyId != companyId)
            throw new InvalidOperationException("La sucursal indicada no pertenece a la compañía seleccionada.");

        return branch.Id;
    }
}
