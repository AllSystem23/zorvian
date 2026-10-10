using Microsoft.EntityFrameworkCore.Migrations;

namespace Zorvian.Infrastructure.Migrations;

/// <summary>
/// Aplica Row Level Security (ENABLE + policies tenant/super-admin + índices)
/// usando el script embebido <c>docs/SECURITY_RLS.sql</c>. Idempotente.
/// <para>
/// <c>FORCE ROW LEVEL SECURITY</c> NO se aplica a propósito: la app se conecta
/// como owner de las tablas (Neon <c>neondb_owner</c>) y el owner bypasea RLS
/// sin FORCE — es lo que mantiene hoy vivos los flujos de auth (login corre
/// con la GUC de tenant en cero). Activar FORCE exige un rol no-owner dedicado,
/// excluir las tablas de auth de las policies y re-ejecutar
/// <c>docs/test_rls_staging.sql</c>. Detalle en el encabezado del script.
/// </para>
/// </summary>
[Migration("20260617190000_EnableRLS")]
public partial class EnableRLS : Migration
{
    private const string EmbeddedScriptName = "SecurityScripts.SECURITY_RLS.sql";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(ReadRlsScript());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Recorre TODAS las tablas públicas: deshabilita RLS y elimina las
        // policies creadas por este script y por el bloque runtime de
        // Program.cs (tenant_isolation_policy), para que un re-aplicar
        // posterior empiece limpio.
        migrationBuilder.Sql("""
            DO $$
            DECLARE r RECORD;
            BEGIN
                FOR r IN SELECT tablename FROM pg_tables WHERE schemaname = 'public' LOOP
                    EXECUTE format('ALTER TABLE %I DISABLE ROW LEVEL SECURITY', r.tablename);
                    EXECUTE format('DROP POLICY IF EXISTS tenant_isolation_policy ON %I', r.tablename);
                    EXECUTE format('DROP POLICY IF EXISTS tenant_isolation_%I ON %I', r.tablename, r.tablename);
                    EXECUTE format('DROP POLICY IF EXISTS super_admin_bypass_%I ON %I', r.tablename, r.tablename);
                END LOOP;
            END $$;
            DROP FUNCTION IF EXISTS is_super_admin();
            """);
    }

    private static string ReadRlsScript()
    {
        var assembly = typeof(EnableRLS).Assembly;

        // 1. Recurso embebido (funciona en Render/Docker: no depende del CWD).
        using (var stream = assembly.GetManifestResourceStream(EmbeddedScriptName))
        {
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
        }

        // 2. Fallback local: docs/ en el directorio base o en el repo root.
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "docs", "SECURITY_RLS.sql"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "SECURITY_RLS.sql")),
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return File.ReadAllText(path);
        }

        throw new InvalidOperationException(
            $"RLS script no encontrado: recurso embebido '{EmbeddedScriptName}' ausente " +
            "y sin docs/SECURITY_RLS.sql en disco.");
    }
}
