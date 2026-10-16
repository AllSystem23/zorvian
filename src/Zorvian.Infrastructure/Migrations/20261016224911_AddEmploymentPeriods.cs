using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmploymentPeriods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmploymentPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodNumber = table.Column<int>(type: "integer", nullable: false),
                    HireDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TerminationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TerminationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BaseSalary = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Position = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "open"),
                    TerminationRecordId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmploymentPeriods_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentPeriods_EmployeeId_PeriodNumber",
                table: "EmploymentPeriods",
                columns: new[] { "EmployeeId", "PeriodNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentPeriods_EmployeeId_Status",
                table: "EmploymentPeriods",
                columns: new[] { "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentPeriods_TenantId_EmployeeId",
                table: "EmploymentPeriods",
                columns: new[] { "TenantId", "EmployeeId" });

            // ── Backfill: los empleados existentes abren su período 1 ──
            // Idempotente (NOT EXISTS); los terminados quedan cerrados con su
            // fecha/motivo de salida para que el historial sea completo desde
            // el primer arranque post-migración. gen_random_uuid() es nativo
            // desde PG13.
            migrationBuilder.Sql("""
                INSERT INTO "EmploymentPeriods" (
                    "Id", "EmployeeId", "PeriodNumber", "HireDate", "TerminationDate",
                    "TerminationReason", "BaseSalary", "Position", "Status",
                    "TerminationRecordId", "Notes", "TenantId", "CompanyId",
                    "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted", "DeletedAt")
                SELECT
                    gen_random_uuid(), e."Id", 1, e."HireDate", e."TerminationDate",
                    e."TerminationReason", e."Salary", e."Position",
                    CASE WHEN e."TerminationDate" IS NOT NULL THEN 'closed' ELSE 'open' END,
                    NULL, 'Backfill automático (migración AddEmploymentPeriods)',
                    e."TenantId", e."CompanyId", NOW(), 'system', NULL, NULL, false, NULL
                FROM "Employees" e
                WHERE NOT e."IsDeleted"
                  AND NOT EXISTS (
                      SELECT 1 FROM "EmploymentPeriods" p WHERE p."EmployeeId" = e."Id");
                """);

            // ── RLS: EmploymentPeriods no tiene BranchId → política solo
            // tenant (tenant_isolation_*) + bypass SuperAdmin, con escape de
            // contexto ausente. Mismo patrón dollar-quotes que AddWarehouses
            // ($do$ externo + $$ internos; DO $$ + $$ interno falla siempre).
            migrationBuilder.Sql("""
                ALTER TABLE "EmploymentPeriods" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "EmploymentPeriods" FORCE ROW LEVEL SECURITY;

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
                    EXECUTE format('DROP POLICY IF EXISTS %I ON %I', 'tenant_isolation_EmploymentPeriods', 'EmploymentPeriods');
                    EXECUTE format(
                        $$CREATE POLICY %I ON %I FOR ALL USING (
                            coalesce(current_setting('app.tenant_id', true), '') IN ('', '00000000-0000-0000-0000-000000000000')
                            OR "TenantId" = current_setting('app.tenant_id', true)
                        )$$,
                        'tenant_isolation_EmploymentPeriods', 'EmploymentPeriods'
                    );
                    EXECUTE format('DROP POLICY IF EXISTS %I ON %I', 'super_admin_bypass_EmploymentPeriods', 'EmploymentPeriods');
                    EXECUTE format(
                        $$CREATE POLICY %I ON %I FOR ALL USING (is_super_admin())$$,
                        'super_admin_bypass_EmploymentPeriods', 'EmploymentPeriods'
                    );
                END;
                $do$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmploymentPeriods");
        }
    }
}
