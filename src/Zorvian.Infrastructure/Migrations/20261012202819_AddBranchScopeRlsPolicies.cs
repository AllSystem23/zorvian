using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <summary>
    /// Crea (idempotente) las políticas RLS por sucursal <c>branch_scope_%I</c> sobre toda tabla
    /// con columna <c>BranchId</c>. La migración <see cref="EnableRLS"/> (2026-06) ya aplica el
    /// script <c>docs/SECURITY_RLS.sql</c> completo, pero al ya estar desplegada en las BD
    /// existentes NO vuelve a ejecutarse: este bloque se añadió al script después, por lo que en
    /// entornos ya migrados las políticas NO existirían sin esta migración dedicada.
    /// <para>
    /// Semántica PARIDAD con el invariante del proyecto: la sucursal es filtro EXPLÍCITO opt-in.
    /// <c>app.branch_id = ''</c> (sin sucursal) → la política NO acota (company-wide); cualquier
    /// otro valor → solo filas de esa sucursal. RLS sigue inerte sin <c>FORCE</c> (la app conecta
    /// como owner); estas políticas quedan listas para el runbook no-owner.
    /// </para>
    /// </summary>
    public partial class AddBranchScopeRlsPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    tbl TEXT;
                BEGIN
                    FOR tbl IN
                        SELECT table_name
                        FROM information_schema.columns
                        WHERE column_name = 'BranchId' AND table_schema = 'public'
                    LOOP
                        EXECUTE format('DROP POLICY IF EXISTS branch_scope_%I ON %I', tbl, tbl);
                        EXECUTE format(
                            $$CREATE POLICY branch_scope_%I ON %I FOR ALL USING (
                                current_setting('app.branch_id', true) = ''
                                OR "BranchId"::text = current_setting('app.branch_id', true)
                            )$$,
                            tbl, tbl
                        );
                    END LOOP;
                END;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    tbl TEXT;
                BEGIN
                    FOR tbl IN
                        SELECT table_name
                        FROM information_schema.columns
                        WHERE column_name = 'BranchId' AND table_schema = 'public'
                    LOOP
                        EXECUTE format('DROP POLICY IF EXISTS branch_scope_%I ON %I', tbl, tbl);
                    END LOOP;
                END;
                $$;
                """);
        }
    }
}
