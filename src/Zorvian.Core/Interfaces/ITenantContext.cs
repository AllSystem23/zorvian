using Zorvian.Core.Entities;

namespace Zorvian.Core.Interfaces;

/// <summary>
/// Contexto de ejecución por request. Transporta el alcance (compañía + sucursal)
/// seleccionado por el usuario, resuelto de forma confiable en el servidor.
///
/// Semántica clave para SuperAdmin:
/// - Con compañía seleccionada  → <see cref="BypassTenantFilter"/> = false (las lecturas se restringen a esa compañía).
/// - Sin compañía seleccionada  → <see cref="BypassTenantFilter"/> = true  (acceso total, sólo para contextos admin).
/// </summary>
public interface ITenantContext
{
    TenantId TenantId { get; }

    bool IsSuperAdmin { get; }

    Guid? CurrentUserId { get; }

    Guid? CurrentEmployeeId { get; }

    /// <summary>Sucursal seleccionada por el usuario (null = todas / no aplica).</summary>
    Guid? CurrentBranchId { get; }

    /// <summary>Compañía concretamente seleccionada (null si no hay selección válida).</summary>
    Guid? SelectedCompanyId { get; }

    /// <summary>True cuando hay una compañía seleccionada (tenant es un GUID no vacío).</summary>
    bool HasCompanySelection { get; }

    /// <summary>
    /// True sólo cuando el filtro de tenant debe omitirse: SuperAdmin sin compañía seleccionada.
    /// Es lo único que habilita el bypass; un SuperAdmin con selección NO bypassa.
    /// </summary>
    bool BypassTenantFilter { get; }

    /// <summary>
    /// CompanyId efectivo para filtros de lectura.
    /// Devuelve la compañía seleccionada también para SuperAdmin (la selección manda).
    /// </summary>
    Guid? EffectiveCompanyId => SelectedCompanyId;

    string? GetUserIdentifier() => CurrentUserId?.ToString();
}

public interface ITenantContextWriter
{
    void SetTenantId(TenantId tenantId);
    void SetCurrentUser(Guid? userId, Guid? employeeId);
    void SetIsSuperAdmin(bool isSuperAdmin);
    void SetBranchId(Guid? branchId);
}
