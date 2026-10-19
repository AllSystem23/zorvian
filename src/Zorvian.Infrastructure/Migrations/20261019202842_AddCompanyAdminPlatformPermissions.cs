using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <summary>
    /// Backfill de datos: el rol de sistema `CompanyAdmin` administra los roles
    /// y las API keys de SU compañía, así que necesita `role.manage` y
    /// `apikey.manage` (antes se aplicaban por rol hardcodeado en los
    /// controllers, no por permiso). Va en migración —y no en un "ensure-missing"
    /// de arranque— para no resucitar una revocación deliberada del SuperAdmin.
    /// `SeedService` ya trae el set nuevo para las compañías creadas después.
    /// </summary>
    public partial class AddCompanyAdminPlatformPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO ""RolePermissions"" (""RoleId"", ""PermissionCode"")
                SELECT r.""Id"", p.code
                FROM ""Roles"" r
                CROSS JOIN (VALUES ('role.manage'), ('apikey.manage')) AS p(code)
                WHERE r.""IsSystem"" = true
                  AND r.""Name"" = 'CompanyAdmin'
                  AND r.""IsDeleted"" = false
                  AND NOT EXISTS (
                      SELECT 1 FROM ""RolePermissions"" rp
                      WHERE rp.""RoleId"" = r.""Id"" AND rp.""PermissionCode"" = p.code
                  );");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No se revierte: borrar estos grants podría quitar permisos que el
            // SuperAdmin haya ajustado después de aplicar la migración.
        }
    }
}
