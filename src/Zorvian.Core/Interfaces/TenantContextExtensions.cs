namespace Zorvian.Core.Interfaces;

/// <summary>
/// Extensiones para resolver el CompanyId desde el TenantContext.
/// Maneja correctamente al SuperAdmin cuyo TenantId no es un GUID válido (ej: "superadmin").
/// </summary>
public static class TenantContextExtensions
{
    /// <summary>
    /// Resuelve el CompanyId para operaciones de LECTURA.
    /// Para SuperAdmin sin empresa seleccionada, devuelve Guid.Empty (EF Core query filters manejan el bypass).
    /// Para usuarios normales o SuperAdmin con empresa seleccionada, devuelve el GUID válido.
    /// </summary>
    public static Guid ResolveCompanyId(this ITenantContext tenant)
    {
        if (tenant.TenantId.TryGetCompanyId(out var id) && id != Guid.Empty)
            return id;

        if (tenant.IsSuperAdmin)
            return Guid.Empty;

        throw new InvalidOperationException("Tenant not configured. Switch to a company first.");
    }

    /// <summary>
    /// Resuelve el CompanyId para operaciones de ESCRITURA.
    /// Para SuperAdmin, devuelve Guid.Empty para que los servicios manejen
    /// el contexto según corresponda (ej: usar el CompanyId de la entidad padre).
    /// Para usuarios normales sin empresa, lanza excepción.
    /// </summary>
    public static Guid RequireCompanyId(this ITenantContext tenant)
    {
        if (tenant.TenantId.TryGetCompanyId(out var id) && id != Guid.Empty)
            return id;

        if (tenant.IsSuperAdmin)
            return Guid.Empty;

        throw new InvalidOperationException("Tenant not configured. Switch to a company first.");
    }

    /// <summary>
    /// Resuelve el CompanyId y lo convierte a string para consultas de tenant.
    /// Para SuperAdmin, devuelve null (seleccionar empresa primero).
    /// </summary>
    public static string? ResolveTenantIdString(this ITenantContext tenant)
    {
        if (tenant.TenantId.TryGetCompanyId(out var id) && id != Guid.Empty)
            return id.ToString();

        if (tenant.IsSuperAdmin)
            return null;

        throw new InvalidOperationException("Tenant not configured. Switch to a company first.");
    }

    /// <summary>
    /// Verifica si el usuario tiene un tenant/empresa válido configurado.
    /// </summary>
    public static bool HasValidCompany(this ITenantContext tenant)
    {
        return tenant.TenantId.TryGetCompanyId(out var id) && id != Guid.Empty;
    }
}
