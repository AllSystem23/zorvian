namespace Zorvian.Core.Authorization;

/// <summary>
/// Catálogo maestro de permisos granulares (código, etiqueta en español, grupo).
/// Es la fuente única para la UI de administración de la matriz rol→permisos.
/// Los códigos deben coincidir 1:1 con las constantes de
/// `Zorvian.Web.Authorization.Permissions` (verificado por test de consistencia).
/// </summary>
public static class PermissionCatalog
{
    public sealed record PermissionDefinition(string Code, string Label, string Group, bool IsSuperAdminOnly = false);

    /// <summary>
    /// Permisos que solo SuperAdmin puede otorgar/quitar (evita que un
    /// CompanyAdmin se otorgue privilegios de plataforma).
    /// </summary>
    public static readonly IReadOnlySet<string> SuperAdminOnly = new HashSet<string>(StringComparer.Ordinal)
    {
        "company.manage", "role.manage", "apikey.manage",
    };

    public static readonly IReadOnlyList<PermissionDefinition> All = new List<PermissionDefinition>
    {
        // Usuarios y seguridad
        new("user.read", "Ver usuarios", "Usuarios y Seguridad"),
        new("user.write", "Crear/editar usuarios", "Usuarios y Seguridad"),
        new("user.manage", "Gestionar usuarios (rol, activar/desactivar)", "Usuarios y Seguridad"),
        new("apikey.manage", "Gestionar API Keys", "Usuarios y Seguridad", IsSuperAdminOnly: true),
        new("company.manage", "Gestionar compañías", "Usuarios y Seguridad", IsSuperAdminOnly: true),
        new("role.manage", "Gestionar roles", "Usuarios y Seguridad", IsSuperAdminOnly: true),
        new("audit.read", "Ver logs de auditoría", "Usuarios y Seguridad"),
        new("settings.read", "Ver configuración", "Usuarios y Seguridad"),
        new("settings.write", "Modificar configuración", "Usuarios y Seguridad"),

        // Capital humano
        new("employee.read", "Ver empleados", "Capital Humano"),
        new("employee.write", "Crear/editar empleados", "Capital Humano"),
        new("employee.delete", "Eliminar empleados", "Capital Humano"),
        new("employee.import", "Importar empleados", "Capital Humano"),
        new("payroll.read", "Ver nómina", "Capital Humano"),
        new("payroll.write", "Editar nómina", "Capital Humano"),
        new("payroll.process", "Procesar planillas", "Capital Humano"),
        new("commission.read", "Ver comisiones", "Capital Humano"),
        new("commission.write", "Editar comisiones", "Capital Humano"),
        new("commission.process", "Procesar comisiones", "Capital Humano"),
        new("goal.read", "Ver metas", "Capital Humano"),
        new("goal.write", "Editar metas", "Capital Humano"),
        new("goal.evaluate", "Evaluar metas", "Capital Humano"),
        new("kpi.read", "Ver KPIs", "Capital Humano"),
        new("kpi.write", "Editar KPIs", "Capital Humano"),

        // Ventas y clientes
        new("sale.read", "Ver ventas", "Ventas y Clientes"),
        new("sale.write", "Registrar ventas", "Ventas y Clientes"),
        new("client.read", "Ver clientes", "Ventas y Clientes"),
        new("client.write", "Crear/editar clientes", "Ventas y Clientes"),
        new("credit.read", "Ver créditos", "Ventas y Clientes"),
        new("credit.write", "Gestionar créditos y cobros", "Ventas y Clientes"),

        // Compras y proveedores
        new("purchase.read", "Ver compras", "Compras y Proveedores"),
        new("purchase.write", "Registrar compras", "Compras y Proveedores"),
        new("provider.read", "Ver proveedores", "Compras y Proveedores"),
        new("provider.write", "Crear/editar proveedores", "Compras y Proveedores"),

        // Inventario
        new("inventory.read", "Ver inventario", "Inventario"),
        new("inventory.write", "Modificar inventario", "Inventario"),
        new("warranty.read", "Ver garantías", "Inventario"),
        new("warranty.write", "Gestionar garantías", "Inventario"),
        new("warranty.view_profitability", "Ver rentabilidad de garantías", "Inventario"),

        // Finanzas
        new("accounting.read", "Ver contabilidad", "Finanzas"),
        new("accounting.write", "Registrar asientos contables", "Finanzas"),
        new("cash.read", "Ver caja", "Finanzas"),
        new("cash.write", "Operar caja", "Finanzas"),
        new("fixedasset.read", "Ver activos fijos", "Finanzas"),
        new("fixedasset.write", "Gestionar activos fijos", "Finanzas"),

        // Documentos y reportes
        new("document.read", "Ver documentos", "Documentos y Reportes"),
        new("document.write", "Generar documentos", "Documentos y Reportes"),
        new("report.read", "Ver reportes", "Documentos y Reportes"),

        // Flota
        new("fleet.read", "Ver flota", "Flota"),
        new("fleet.write", "Gestionar operaciones de flota", "Flota"),
        new("fleet.delete", "Eliminar registros de flota", "Flota"),
        new("fleet.config", "Configurar catálogos de flota", "Flota"),
    };

    public static IReadOnlyList<string> Codes { get; } =
        All.Select(p => p.Code).ToList();
}
