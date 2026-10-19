namespace Zorvian.Core.Enums;

public enum RoleType
{
    SuperAdmin,
    CompanyAdmin,
    Rrhh,
    Supervisor,
    Employee,
    Accountant,
    Dispatcher,

    /// <summary>
    /// Rol PERSONALIZADO de la compañía (creado desde la matriz de permisos).
    /// Comparte el valor de enum para todos los roles a medida: su identidad es
    /// el `Id`/`DisplayName`, no un `RoleType` propio. Al no ser un rol del
    /// sistema, su acceso lo deciden sus PERMISOS granulares (el frontend no lo
    /// reconoce en las listas de roles por ruta/menú).
    /// </summary>
    Custom
}
