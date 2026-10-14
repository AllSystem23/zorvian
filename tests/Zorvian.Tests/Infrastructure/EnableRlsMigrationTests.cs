using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Zorvian.Infrastructure.Migrations;

namespace Zorvian.Tests.Infrastructure;

/// <summary>
/// La migración EnableRLS debe ser descubrible por EF (atributo [Migration])
/// y su script RLS embebido debe usar nombres de tabla REALES (snapshot EF +
/// create_fleet_tables.sql), nunca nombres inventados.
/// </summary>
public sealed class EnableRlsMigrationTests
{
    private static string ReadEmbeddedRlsScript()
    {
        var assembly = typeof(EnableRLS).Assembly;
        using var stream = assembly.GetManifestResourceStream("SecurityScripts.SECURITY_RLS.sql");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }

    [Fact]
    public void EnableRls_HasMigrationAttribute_WithExpectedId()
    {
        var attr = typeof(EnableRLS).GetCustomAttribute<MigrationAttribute>();
        Assert.NotNull(attr);
        Assert.Equal("20260617190000_EnableRLS", attr!.Id);
    }

    [Fact]
    public void EnableRls_Script_IsEmbedded()
    {
        var sql = ReadEmbeddedRlsScript();
        Assert.Contains("ENABLE ROW LEVEL SECURITY", sql);
        Assert.Contains("CREATE POLICY", sql);
    }

    [Fact]
    public void EnableRls_UsesRealFleetTableNames_NotInventedOnes()
    {
        var sql = ReadEmbeddedRlsScript();

        // Nombres reales (create_fleet_tables.sql / snapshot EF).
        Assert.Contains("'Vehicles'", sql);
        Assert.Contains("'Deliveries'", sql);
        Assert.Contains("'Drivers'", sql);
        Assert.Contains("'Routes'", sql);
        Assert.Contains("'RoutePoints'", sql);
        Assert.Contains("'DeliveryItems'", sql);
        Assert.Contains("'Trips'", sql);
        Assert.Contains("'GoalProgressEntries'", sql);

        // Nombres inventados que no existen en ninguna parte del esquema.
        Assert.DoesNotContain("'FleetVehicles'", sql);
        Assert.DoesNotContain("'FleetDeliveries'", sql);
        Assert.DoesNotContain("'FleetDrivers'", sql);
        Assert.DoesNotContain("'FleetRoutes'", sql);
        Assert.DoesNotContain("'FleetRoutePoints'", sql);
        Assert.DoesNotContain("'FleetDeliveryItems'", sql);
        Assert.DoesNotContain("'FleetTrips'", sql);
        Assert.DoesNotContain("'GoalAssignmentProgressEntries'", sql);
    }

    [Fact]
    public void EnableRls_ForcesRls_WithNoTenantContextEscape()
    {
        // Desde 2026-10-14 SÍ se fuerza RLS. Lo que hace seguro el forzado es el escape
        // de contexto ausente: con app.tenant_id vacío o GUID-cero las políticas no acotan,
        // que es la paridad con los HasQueryFilter de EF (login, arranque y jobs globales).
        var sql = ReadEmbeddedRlsScript();

        Assert.Contains("FORCE ROW LEVEL SECURITY;", sql);
        Assert.Contains("NOTE ON FORCE", sql);
        Assert.Contains("coalesce(current_setting('app.tenant_id', true), '') IN ('', '00000000-0000-0000-0000-000000000000')", sql);

        // El nombre de política se arma COMPLETO y se pasa como %I. 'tenant_isolation_%I'
        // produciría tenant_isolation_"Companies" (dos identificadores adyacentes) y PostgreSQL
        // responde 42601 syntax error at or near ""Companies"".
        Regex patronRoto = new(@"[A-Za-z_]+_%I");
        Assert.DoesNotMatch(patronRoto, sql);
        Assert.Contains("'tenant_isolation_' || tbl", sql);
        Assert.Contains("'super_admin_bypass_' || tbl", sql);
        Assert.Contains("'tenant_branch_scope_' || tbl", sql);
    }

    [Fact]
    public void EnableRls_BranchTables_DoNotKeepSeparateTenantPolicy()
    {
        // Verificación estática del bug de OR: si una tabla con BranchId conservara
        // tenant_isolation + branch_scope, la segunda pasaría siempre sin sucursal elegida.
        var sql = ReadEmbeddedRlsScript();

        var tenantIndex = sql.IndexOf("tenant_isolation_", StringComparison.Ordinal);
        var combinedIndex = sql.IndexOf("tenant_branch_scope_", StringComparison.Ordinal);
        Assert.True(tenantIndex >= 0 && combinedIndex > tenantIndex,
            "El bloque de políticas tenant debe ejecutarse antes del combinado para poder eliminarlas.");
    }

    [Fact]
    public void EnableRls_DollarQuotedStrings_ArePairedInsideDoBlocks()
    {
        // PostgreSQL NO anida dollar-quotes con la misma etiqueta. Dentro de un DO $do$...$do$
        // los strings de format() deben usar OTRA etiqueta ($$) y estar emparejados; si no, el
        // cuerpo del DO se cierra antes de tiempo y CREATE POLICY queda como SQL suelto
        // (42601: syntax error at or near "CREATE").
        var sql = ReadEmbeddedRlsScript();

        var regex = new Regex(@"DO (\$[A-Za-z_]*\$)(.*?)\1", RegexOptions.Singleline);
        var blocks = regex.Matches(sql);
        Assert.NotEmpty(blocks);

        foreach (Match block in blocks)
        {
            var body = block.Groups[2].Value;
            var innerOpens = Regex.Matches(body, @"\$\$").Count;

            // Dentro del bloque los $$ solo deben aparecer en parejas; uno suelto cerraría el
            // cuerpo del DO. Un número impar significa delimitador descasado.
            Assert.True(innerOpens % 2 == 0,
                $"Bloque DO sin delimitadores internos emparejados ({innerOpens} marcadores $$).");
        }
    }
}
