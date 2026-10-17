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

    [Fact]
    public void EnableRls_CoversPurchaseRequestAndStockTransferTables()
    {
        // Tanda 4: las 4 tablas nuevas necesitan política tenant (+ bypass SuperAdmin).
        // Las de sucursal (PurchaseRequest*) tienen columna BranchId, así que el bloque
        // dinámico 3 además las reescribe con la política combinada tenant AND sucursal.
        var sql = ReadEmbeddedRlsScript();

        Assert.Contains("ALTER TABLE IF EXISTS \"PurchaseRequests\" ENABLE ROW LEVEL SECURITY;", sql);
        Assert.Contains("ALTER TABLE IF EXISTS \"PurchaseRequestDetails\" ENABLE ROW LEVEL SECURITY;", sql);
        Assert.Contains("ALTER TABLE IF EXISTS \"StockTransfers\" ENABLE ROW LEVEL SECURITY;", sql);
        Assert.Contains("ALTER TABLE IF EXISTS \"StockTransferDetails\" ENABLE ROW LEVEL SECURITY;", sql);

        Assert.Contains("'PurchaseRequests','PurchaseRequestDetails',", sql);
        Assert.Contains("'StockTransfers','StockTransferDetails'", sql);
    }

    [Fact]
    public void EnableRls_CoversWarehousesTable()
    {
        // Bodegas (2026-10-16): tabla nueva con columna BranchId — el ALTER estático
        // la habilita, los arrays de los bloques 1/2 le dan política tenant + bypass
        // y el bloque dinámico 3 la reescribe con la combinada tenant AND sucursal.
        var sql = ReadEmbeddedRlsScript();

        Assert.Contains("ALTER TABLE IF EXISTS \"Warehouses\" ENABLE ROW LEVEL SECURITY;", sql);
        Assert.Contains("'Warehouses'", sql);
    }

    [Fact]
    public void AddWarehousesMigration_CreatesCombinedPolicy_WithPairedDollarQuotes()
    {
        // Las políticas de la tabla nueva viajan en la propia migración (las RLS
        // históricas ya corrieron en BDs existentes). Regresión del bug de
        // dollar-quotes: DO $$ + $$ interno cierra el cuerpo antes de tiempo
        // (42601 syntax error at or near "CREATE") — verificado en PG16 real.
        var sql = ReadMigrationSourceFile("20261016210153_AddWarehouses.cs");

        Assert.Contains("tenant_branch_scope_Warehouses", sql);
        Assert.Contains("super_admin_bypass_Warehouses", sql);
        Assert.Contains("is_super_admin()", sql);
        Assert.Contains("FORCE ROW LEVEL SECURITY", sql);

        var regex = new Regex(@"DO (\$[A-Za-z_]*\$)(.*?)\1", RegexOptions.Singleline);
        var blocks = regex.Matches(sql);
        Assert.True(blocks.Count > 0,
            "la migración debe contener el bloque DO de políticas");

        foreach (Match block in blocks)
        {
            var body = block.Groups[2].Value;
            var innerOpens = Regex.Matches(body, @"\$\$").Count;
            Assert.True(innerOpens % 2 == 0,
                $"Bloque DO de AddWarehouses con delimitadores $$ descasados ({innerOpens}).");
        }
    }

    [Fact]
    public void EnableRls_CoversEmploymentPeriodsTable()
    {
        // Historial de contratos (2026-10-16): tabla nueva SIN BranchId — el
        // ALTER estático la habilita y los arrays de los bloques 1/2 le dan
        // política solo-tenant + bypass SuperAdmin (el bloque 3 no aplica).
        var sql = ReadEmbeddedRlsScript();

        Assert.Contains("ALTER TABLE IF EXISTS \"EmploymentPeriods\" ENABLE ROW LEVEL SECURITY;", sql);
        Assert.Contains("'EmploymentPeriods'", sql);
    }

    [Fact]
    public void AddEmploymentPeriodsMigration_CreatesPolicies_AndBackfillsExistingEmployees()
    {
        // Las políticas de la tabla nueva viajan en la propia migración (las RLS
        // históricas ya corrieron en BDs existentes). Regresión del bug de
        // dollar-quotes: DO $$ + $$ interno cierra el cuerpo antes de tiempo
        // (42601 syntax error at or near "CREATE").
        var sql = ReadMigrationSourceFile("20261016224911_AddEmploymentPeriods.cs");

        Assert.Contains("tenant_isolation_EmploymentPeriods", sql);
        Assert.Contains("super_admin_bypass_EmploymentPeriods", sql);
        Assert.Contains("is_super_admin()", sql);
        Assert.Contains("FORCE ROW LEVEL SECURITY", sql);

        // Backfill: cada empleado existente abre su período 1 (los terminados
        // quedan cerrados con su fecha de salida).
        Assert.Contains("INSERT INTO \"EmploymentPeriods\"", sql);
        Assert.Contains("NOT EXISTS", sql);
        Assert.Contains("'closed' ELSE 'open'", sql);

        var regex = new Regex(@"DO (\$[A-Za-z_]*\$)(.*?)\1", RegexOptions.Singleline);
        var blocks = regex.Matches(sql);
        Assert.True(blocks.Count > 0,
            "la migración debe contener el bloque DO de políticas");

        foreach (Match block in blocks)
        {
            var body = block.Groups[2].Value;
            var innerOpens = Regex.Matches(body, @"\$\$").Count;
            Assert.True(innerOpens % 2 == 0,
                $"Bloque DO de AddEmploymentPeriods con delimitadores $$ descasados ({innerOpens}).");
        }
    }

    [Fact]
    public void EnableRls_CoversServiceProviderPaymentTables()
    {
        // Pago a prestadores (2026-10-17): 3 tablas nuevas SIN BranchId — el
        // ALTER estático las habilita y los arrays de los bloques 1/2 les dan
        // política solo-tenant + bypass SuperAdmin (el bloque 3 no aplica).
        var sql = ReadEmbeddedRlsScript();

        Assert.Contains("ALTER TABLE IF EXISTS \"ServiceProviderPayments\" ENABLE ROW LEVEL SECURITY;", sql);
        Assert.Contains("ALTER TABLE IF EXISTS \"ServiceProviderPaymentLines\" ENABLE ROW LEVEL SECURITY;", sql);
        Assert.Contains("ALTER TABLE IF EXISTS \"ServiceProviderPaymentDeductions\" ENABLE ROW LEVEL SECURITY;", sql);
        Assert.Contains("'ServiceProviderPayments','ServiceProviderPaymentLines','ServiceProviderPaymentDeductions'", sql);
    }

    [Fact]
    public void AddServiceProviderPaymentsMigration_CreatesPolicies_AndSequence()
    {
        // Las políticas de las tablas nuevas viajan en la propia migración (las RLS
        // históricas ya corrieron en BDs existentes). Regresión del bug de
        // dollar-quotes: DO $$ + $$ interno cierra el cuerpo antes de tiempo
        // (42601 syntax error at or near "CREATE"). Además crea la secuencia
        // de numeración PP-YYYYMMDD-NNNN.
        var sql = ReadMigrationSourceFile("20261017151536_AddServiceProviderPayments.cs");

        Assert.Contains("tenant_isolation_ServiceProviderPayments", sql);
        Assert.Contains("super_admin_bypass_ServiceProviderPayments", sql);
        Assert.Contains("tenant_isolation_ServiceProviderPaymentLines", sql);
        Assert.Contains("tenant_isolation_ServiceProviderPaymentDeductions", sql);
        Assert.Contains("is_super_admin()", sql);
        Assert.Contains("FORCE ROW LEVEL SECURITY", sql);
        Assert.Contains("CREATE SEQUENCE IF NOT EXISTS seq_service_provider_payment_number", sql);
        Assert.Contains("DROP SEQUENCE IF EXISTS seq_service_provider_payment_number", sql);

        var regex = new Regex(@"DO (\$[A-Za-z_]*\$)(.*?)\1", RegexOptions.Singleline);
        var blocks = regex.Matches(sql);
        Assert.True(blocks.Count > 0,
            "la migración debe contener el bloque DO de políticas");

        foreach (Match block in blocks)
        {
            var body = block.Groups[2].Value;
            var innerOpens = Regex.Matches(body, @"\$\$").Count;
            Assert.True(innerOpens % 2 == 0,
                $"Bloque DO de AddServiceProviderPayments con delimitadores $$ descasados ({innerOpens}).");
        }
    }

    [Fact]
    public void EnableRls_CoversRankingsTable()
    {
        // Fase D (2026-10-17): Rankings era la ÚNICA tabla multi-tenant del modelo
        // ausente de las listas del script (sin ALTER estático, sin política tenant
        // ni bypass SuperAdmin). Tiene columna BranchId → el bloque dinámico 3 la
        // reescribe con la política combinada tenant AND sucursal.
        var sql = ReadEmbeddedRlsScript();

        Assert.Contains("ALTER TABLE IF EXISTS \"Rankings\" ENABLE ROW LEVEL SECURITY;", sql);
        Assert.Contains("'KpiDefinitions','KpiRecords','Rankings','Budgets',", sql);
    }

    [Fact]
    public void AddRankingsRlsPoliciesMigration_CreatesCombinedPolicies()
    {
        // Las políticas viajan en la propia migración (EnableRLS está backdatada y
        // ya corrió en BDs existentes). Regresión del bug de dollar-quotes: DO $do$
        // + $$ interno cierra el cuerpo antes de tiempo (42601 at or near "CREATE").
        var sql = ReadMigrationSourceFile("20261017214954_AddRankingsRlsPolicies.cs");

        Assert.Contains("tenant_branch_scope_Rankings", sql);
        Assert.Contains("super_admin_bypass_Rankings", sql);
        Assert.Contains("is_super_admin()", sql);
        Assert.Contains("FORCE ROW LEVEL SECURITY", sql);
        // Con BranchId la política debe ser la COMBINADA: tenant_isolation sola
        // combinada por OR con branch_scope dejaría ver otros tenants sin sucursal.
        Assert.Contains("\"BranchId\"::text = current_setting('app.branch_id', true)", sql);

        var regex = new Regex(@"DO (\$[A-Za-z_]*\$)(.*?)\1", RegexOptions.Singleline);
        var blocks = regex.Matches(sql);
        Assert.True(blocks.Count > 0,
            "la migración debe contener el bloque DO de políticas");

        foreach (Match block in blocks)
        {
            var body = block.Groups[2].Value;
            var innerOpens = Regex.Matches(body, @"\$\$").Count;
            Assert.True(innerOpens % 2 == 0,
                $"Bloque DO de AddRankingsRlsPolicies con delimitadores $$ descasados ({innerOpens}).");
        }
    }

    private static string ReadMigrationSourceFile(string fileName)
    {
        // Los tests corren desde bin/Debug/net9.0: se sube hasta encontrar la raíz del repo.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Zorvian.Infrastructure", "Migrations", fileName);
            if (File.Exists(candidate)) return File.ReadAllText(candidate);
            dir = dir.Parent!;
        }
        throw new FileNotFoundException($"No se encontró {fileName} subiendo desde {AppContext.BaseDirectory}");
    }
}
