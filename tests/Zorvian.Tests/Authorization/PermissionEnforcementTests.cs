using System.Reflection;
using Xunit;
using Zorvian.Core.Authorization;
using Zorvian.Web.Authorization;
using Zorvian.Web.Controllers;

namespace Zorvian.Tests.Authorization;

/// <summary>
/// El catálogo `PermissionCatalog` es la fuente de verdad de los permisos
/// granulares: ningún controller puede exigir un permiso que no exista en el
/// catálogo, y ningún permiso del catálogo puede quedarse sin ningún endpoint
/// que lo exija (sería un toggle decorativo en la matriz de roles).
/// </summary>
public sealed class PermissionEnforcementTests
{
    private static readonly Assembly WebAssembly = typeof(RolesController).Assembly;

    private static List<string> EnforcedCodes()
    {
        var codes = new List<string>();

        foreach (var type in WebAssembly.GetTypes()
                     .Where(t => t.Namespace?.StartsWith("Zorvian.Web.Controllers") == true))
        {
            codes.AddRange(AttributesOf(type));
            foreach (var method in type.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                codes.AddRange(AttributesOf(method));
            }
        }

        return codes;
    }

    private static IEnumerable<string> AttributesOf(MemberInfo member) =>
        member.GetCustomAttributes<RequirePermissionAttribute>().Select(a => a.Permission);

    [Fact]
    public void EveryRequiredPermissionExistsInCatalog()
    {
        var enforced = EnforcedCodes();

        // Guarda anti-falso-positivo: si la reflexión dejara de encontrar
        // atributos, los dos tests pasarían trivialmente.
        Assert.True(enforced.Count >= 100,
            $"La reflexión solo encontró {enforced.Count} usos de [RequirePermission].");

        var unknown = enforced
            .Where(code => !PermissionCatalog.Codes.Contains(code))
            .Distinct()
            .ToList();

        Assert.True(unknown.Count == 0,
            $"Permisos exigidos por un controller que no están en el catálogo: {string.Join(", ", unknown)}");
    }

    [Fact]
    public void EveryCatalogPermissionIsEnforcedBySomeController()
    {
        var enforced = EnforcedCodes().ToHashSet();

        var unused = PermissionCatalog.Codes
            .Where(code => !enforced.Contains(code))
            .ToList();

        Assert.True(unused.Count == 0,
            "Permisos del catálogo sin ningún endpoint que los exija " +
            $"(toggles que no hacen nada en la matriz de roles): {string.Join(", ", unused)}");
    }

    /// <summary>
    /// `BranchesController` es el caso especial documentado: la LECTURA es de
    /// cualquier usuario autenticado de la compañía porque alimenta el selector
    /// de sucursales del cabecero (todas las páginas); la ESCRITURA es
    /// configuración de la compañía → `settings.write`. Antes TODAS las acciones
    /// exigían `company.manage` (permiso de plataforma, solo SuperAdmin), así que
    /// el `GET` devolvía 403 al resto de roles y el selector quedaba vacío en
    /// silencio (`headerBranchListProvider` traga el error).
    /// </summary>
    [Fact]
    public void BranchesController_ReadsAreOpen_WritesRequireSettingsWrite()
    {
        var expectations = new (string Name, bool IsWrite)[]
        {
            (nameof(BranchesController.GetAll), false),
            (nameof(BranchesController.GetById), false),
            (nameof(BranchesController.Create), true),
            (nameof(BranchesController.Update), true),
            (nameof(BranchesController.Delete), true),
        };

        foreach (var (name, isWrite) in expectations)
        {
            var method = typeof(BranchesController).GetMethod(name)!;
            var permissions = method.GetCustomAttributes<RequirePermissionAttribute>()
                .Select(a => a.Permission)
                .ToList();

            if (isWrite)
            {
                Assert.Equal(new[] { Permissions.SettingsWrite }, permissions);
            }
            else
            {
                Assert.True(permissions.Count == 0,
                    $"{name} debe quedar abierto a cualquier autenticado " +
                    $"(selector de sucursales), pero exige: {string.Join(", ", permissions)}");
            }
        }
    }
}
