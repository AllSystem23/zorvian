using Zorvian.Core.Entities;

namespace Zorvian.Core.Interfaces;

/// <summary>
/// Extensiones para resolver el alcance (compañía + sucursal) desde el TenantContext.
/// Maneja correctamente al SuperAdmin cuyo TenantId puede no ser un GUID válido (ej: "superadmin").
/// </summary>
public static class TenantContextExtensions
{
    /// <summary>
    /// Compañía seleccionada, con fallback al propio TenantId cuando el contexto no expone
    /// SelectedCompanyId (ej: mocks en tests que sólo stubbean TenantId).
    /// En el contexto real SelectedCompanyId se deriva de TenantId, así que el resultado es idéntico.
    /// </summary>
    private static Guid? ResolveSelectionOrTenant(ITenantContext tenant)
    {
        if (tenant.SelectedCompanyId is { } id && id != Guid.Empty) return id;

        var tenantId = tenant.TenantId;
        if (tenantId is not null && tenantId.TryGetCompanyId(out var parsed) && parsed != Guid.Empty)
            return parsed;

        return null;
    }

    /// <summary>
    /// Resuelve el CompanyId para operaciones de LECTURA.
    /// Devuelve la compañía seleccionada (también para SuperAdmin).
    /// Si no hay selección y es SuperAdmin, devuelve Guid.Empty (los query filters manejan el bypass).
    /// </summary>
    public static Guid ResolveCompanyId(this ITenantContext tenant)
    {
        if (ResolveSelectionOrTenant(tenant) is { } id) return id;

        if (tenant.IsSuperAdmin)
            return Guid.Empty;

        throw new InvalidOperationException("Tenant not configured. Switch to a company first.");
    }

    /// <summary>
    /// Resuelve el CompanyId para operaciones de ESCRITURA.
    /// Devuelve la compañía seleccionada; para SuperAdmin sin selección, Guid.Empty
    /// para que los servicios usen el CompanyId de la entidad padre.
    /// </summary>
    public static Guid RequireCompanyId(this ITenantContext tenant)
    {
        if (ResolveSelectionOrTenant(tenant) is { } id) return id;

        if (tenant.IsSuperAdmin)
            return Guid.Empty;

        throw new InvalidOperationException("Tenant not configured. Switch to a company first.");
    }

    /// <summary>
    /// Resuelve el CompanyId como string para comparar contra el campo TenantId (string) de las entidades.
    /// </summary>
    public static string? ResolveTenantIdString(this ITenantContext tenant)
    {
        if (tenant.SelectedCompanyId is { } id) return id.ToString();

        if (tenant.IsSuperAdmin)
            return null;

        throw new InvalidOperationException("Tenant not configured. Switch to a company first.");
    }

    /// <summary>
    /// CompanyId que debe usarse para filtrar lecturas (EF query filters y SQL crudo).
    /// </summary>
    public static Guid ResolveFilterCompanyId(this ITenantContext tenant)
        => tenant.SelectedCompanyId ?? Guid.Empty;

    /// <summary>
    /// Sucursal seleccionada para lecturas/escrituras. null = todas las sucursales / no aplica.
    /// </summary>
    public static Guid? ResolveBranchId(this ITenantContext tenant)
        => tenant.CurrentBranchId is { } b && b != Guid.Empty ? b : null;

    /// <summary>
    /// Verifica si el usuario tiene un tenant/empresa válido configurado.
    /// </summary>
    public static bool HasValidCompany(this ITenantContext tenant)
        => tenant.HasCompanySelection;

    // NOTA: ValidateBranchOwnership (no-op, 0 referencias) fue eliminada en la
    // remediación multi-tenant 2026-10: daba falsa sensación de validación.
    // La validación real de sucursal contra BD vive en
    // IBranchValidator.ResolveForWriteAsync (BranchValidator) y en
    // TenantMiddleware.ResolveBranchAsync (header X-Branch-Id).
}
