using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <summary>
    /// Habilita los AJUSTES FISCALES POR COMPAÑÍA sin cambiar el esquema: la tabla ya
    /// tenía <c>TenantId</c>/<c>CompanyId</c>. Las filas del catálogo global pasan a
    /// llevar el tenant centinela <c>00000000-0000-0000-0000-000000000000</c> y cada
    /// compañía puede crear su propia fila por país (su tenant real), que la resolución
    /// de <c>ICountryTaxConfigRepository.GetByCountryCodeAsync</c> prefiere.
    /// </summary>
    public partial class AddCountryTaxConfigCompanyOverrides : Migration
    {
        private const string GlobalTenant = "00000000-0000-0000-0000-000000000000";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Antes de este cambio todo el catálogo era global, pero las filas creadas
            // desde el API quedaron con el TenantId del SuperAdmin que las guardó (lo
            // estampa TenantAuditInterceptor). Con la resolución nueva, esas filas
            // habrían pasado a ser el "ajuste propio" de UNA compañía y habrían
            // desaparecido del catálogo. Se normalizan al tenant global (idempotente).
            migrationBuilder.Sql(
                $@"UPDATE ""CountryTaxConfigs""
                   SET ""TenantId"" = '{GlobalTenant}',
                       ""CompanyId"" = '00000000-0000-0000-0000-000000000000'
                   WHERE ""TenantId"" IS DISTINCT FROM '{GlobalTenant}'
                      OR ""CompanyId"" IS DISTINCT FROM '00000000-0000-0000-0000-000000000000';");

            // La resolución filtra por (código de país, tenant): índice de apoyo.
            // Va en SQL crudo para no alterar el modelo EF (índice no expresado en el snapshot).
            migrationBuilder.Sql(
                @"CREATE INDEX IF NOT EXISTS ""IX_CountryTaxConfigs_CountryCode_TenantId""
                  ON ""CountryTaxConfigs"" (""CountryCode"", ""TenantId"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"DROP INDEX IF EXISTS ""IX_CountryTaxConfigs_CountryCode_TenantId"";");
        }
    }
}
