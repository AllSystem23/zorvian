using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Condicional: en BDs donde esta migración se aplicó parcialmente (el
            // script idempotente de EF commitea por sentencia) el índice pudo ya
            // haberse borrado; en la vía normal (MigrateAsync, transaccional) es
            // equivalente al DropIndex generado.
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_Products_Code_BranchId";
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "FromWarehouseId",
                table: "StockTransfers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ToWarehouseId",
                table: "StockTransfers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "QuantityReceived",
                table: "StockTransferDetails",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                table: "Products",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Warehouses_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // ── Backfill (SQL crudo, idempotente) ─────────────────────────────
            // Las columnas nuevas se crearon NOT NULL DEFAULT GUID-cero: primero
            // se crea "Bodega Principal" por cada sucursal (incluso soft-deleteada,
            // para que ningún producto transferido quede sin FK válida) y luego se
            // apuntan Products/StockTransfers a esa bodega antes de crear los FKs
            // (un AddForeignKey valida las filas existentes).
            migrationBuilder.Sql("""
                INSERT INTO "Warehouses" (
                    "Id", "BranchId", "Name", "IsActive", "IsDefault",
                    "TenantId", "CompanyId", "CreatedAt", "CreatedBy", "IsDeleted")
                SELECT gen_random_uuid(), b."Id", 'Bodega Principal', TRUE, TRUE,
                       b."TenantId", b."CompanyId", NOW(), 'migration-backfill', FALSE
                FROM "Branches" b
                WHERE NOT EXISTS (
                    SELECT 1 FROM "Warehouses" w
                    WHERE w."BranchId" = b."Id" AND w."IsDefault");
                """);

            migrationBuilder.Sql("""
                UPDATE "Products" p
                SET "WarehouseId" = w."Id"
                FROM "Warehouses" w
                WHERE w."BranchId" = p."BranchId"
                  AND w."IsDefault" = TRUE
                  AND p."WarehouseId" = '00000000-0000-0000-0000-000000000000';
                """);

            migrationBuilder.Sql("""
                UPDATE "StockTransfers" t
                SET "FromWarehouseId" = w."Id"
                FROM "Warehouses" w
                WHERE w."BranchId" = t."FromBranchId"
                  AND w."IsDefault" = TRUE
                  AND t."FromWarehouseId" = '00000000-0000-0000-0000-000000000000';

                UPDATE "StockTransfers" t
                SET "ToWarehouseId" = w."Id"
                FROM "Warehouses" w
                WHERE w."BranchId" = t."ToBranchId"
                  AND w."IsDefault" = TRUE
                  AND t."ToWarehouseId" = '00000000-0000-0000-0000-000000000000';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_FromWarehouseId",
                table: "StockTransfers",
                column: "FromWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ToWarehouseId",
                table: "StockTransfers",
                column: "ToWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Code_WarehouseId",
                table: "Products",
                columns: new[] { "Code", "WarehouseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_WarehouseId",
                table: "Products",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_BranchId_Name",
                table: "Warehouses",
                columns: new[] { "BranchId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_TenantId_BranchId",
                table: "Warehouses",
                columns: new[] { "TenantId", "BranchId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Warehouses_WarehouseId",
                table: "Products",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransfers_Warehouses_FromWarehouseId",
                table: "StockTransfers",
                column: "FromWarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransfers_Warehouses_ToWarehouseId",
                table: "StockTransfers",
                column: "ToWarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // ── RLS de Warehouses (Phase 9) ──────────────────────────────────
            // Las migraciones RLS históricas ya corrieron en BDs existentes: las
            // políticas de la tabla nueva van AQUÍ además del script
            // docs/SECURITY_RLS.sql (que las recrea idempotentemente en cada
            // arranque). La política es la combinada tenant AND sucursal — misma
            // forma que ForceTenantRowLevelSecurity (bloque 3 del script). Ojo
            // dollar-quotes: bloque DO con $do$ y strings internos $$ (el patrón
            // DO $$ + $$ interno falla con "syntax error at or near CREATE").
            migrationBuilder.Sql("""
                ALTER TABLE "Warehouses" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "Warehouses" FORCE ROW LEVEL SECURITY;

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
                    EXECUTE format('DROP POLICY IF EXISTS %I ON %I', 'tenant_isolation_Warehouses', 'Warehouses');
                    EXECUTE format('DROP POLICY IF EXISTS %I ON %I', 'branch_scope_Warehouses', 'Warehouses');
                    EXECUTE format('DROP POLICY IF EXISTS %I ON %I', 'tenant_branch_scope_Warehouses', 'Warehouses');
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
                        'tenant_branch_scope_Warehouses', 'Warehouses'
                    );
                    EXECUTE format('DROP POLICY IF EXISTS %I ON %I', 'super_admin_bypass_Warehouses', 'Warehouses');
                    EXECUTE format(
                        $$CREATE POLICY %I ON %I FOR ALL USING (is_super_admin())$$,
                        'super_admin_bypass_Warehouses', 'Warehouses'
                    );
                END;
                $do$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Warehouses_WarehouseId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransfers_Warehouses_FromWarehouseId",
                table: "StockTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransfers_Warehouses_ToWarehouseId",
                table: "StockTransfers");

            migrationBuilder.DropTable(
                name: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_StockTransfers_FromWarehouseId",
                table: "StockTransfers");

            migrationBuilder.DropIndex(
                name: "IX_StockTransfers_ToWarehouseId",
                table: "StockTransfers");

            migrationBuilder.DropIndex(
                name: "IX_Products_Code_WarehouseId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_WarehouseId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "FromWarehouseId",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "ToWarehouseId",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "QuantityReceived",
                table: "StockTransferDetails");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "Products");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Code_BranchId",
                table: "Products",
                columns: new[] { "Code", "BranchId" },
                unique: true);
        }
    }
}
