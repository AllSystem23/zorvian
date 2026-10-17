using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRankingsRlsPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rankings era la ÚNICA tabla multi-tenant del modelo ausente de las
            // listas de SECURITY_RLS.sql (bloques 1/2): no tenía ALTER estático,
            // política solo-tenant ni bypass SuperAdmin. En BDs existentes las
            // migraciones RLS históricas ya corrieron, así que las políticas
            // viajan en esta migración (mismo patrón que AddWarehouses).
            // Tiene columna BranchId → política COMBINADA tenant AND sucursal
            // (el OR de políticas por separado filtraría de más sin sucursal).
            migrationBuilder.Sql("""
                ALTER TABLE "Rankings" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "Rankings" FORCE ROW LEVEL SECURITY;

                -- La función la crea el script de arranque (SECURITY_RLS.sql), pero
                -- EnableRLS está backdatada a junio: en BDs cuya última migración
                -- aplicada es posterior, EF la salta y la función podría no existir.
                CREATE OR REPLACE FUNCTION is_super_admin()
                RETURNS BOOLEAN AS $fn$
                BEGIN
                    RETURN current_setting('app.is_super_admin', true)::boolean = true;
                END;
                $fn$ LANGUAGE plpgsql STABLE;

                DO $do$
                BEGIN
                    -- Limpia cualquier variante previa (incluidas las del bloque
                    -- dinámico 3 del script, que ya creaba la combinada).
                    EXECUTE format('DROP POLICY IF EXISTS %I ON %I', 'tenant_isolation_Rankings', 'Rankings');
                    EXECUTE format('DROP POLICY IF EXISTS %I ON %I', 'branch_scope_Rankings', 'Rankings');
                    EXECUTE format('DROP POLICY IF EXISTS %I ON %I', 'tenant_branch_scope_Rankings', 'Rankings');
                    EXECUTE format(
                        $$CREATE POLICY %I ON %I FOR ALL USING (
                            (
                                coalesce(current_setting('app.tenant_id', true), '') IN ('', '00000000-0000-0000-0000-000000000000')
                                OR "TenantId" = current_setting('app.tenant_id', true)
                            )
                            AND (
                                current_setting('app.branch_id', true) = ''
                                OR "BranchId"::text = current_setting('app.branch_id', true)
                            )
                        )$$,
                        'tenant_branch_scope_Rankings', 'Rankings'
                    );
                    EXECUTE format('DROP POLICY IF EXISTS %I ON %I', 'super_admin_bypass_Rankings', 'Rankings');
                    EXECUTE format(
                        $$CREATE POLICY %I ON %I FOR ALL USING (is_super_admin())$$,
                        'super_admin_bypass_Rankings', 'Rankings'
                    );
                END;
                $do$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS "super_admin_bypass_Rankings" ON "Rankings";
                DROP POLICY IF EXISTS "tenant_branch_scope_Rankings" ON "Rankings";
                DROP POLICY IF EXISTS "branch_scope_Rankings" ON "Rankings";
                DROP POLICY IF EXISTS "tenant_isolation_Rankings" ON "Rankings";
                """);
        }
    }
}
