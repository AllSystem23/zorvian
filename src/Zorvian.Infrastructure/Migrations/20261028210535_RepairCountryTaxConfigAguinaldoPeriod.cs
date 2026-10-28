using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RepairCountryTaxConfigAguinaldoPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Período de aguinaldo inválido (0 o fuera de rango): lo dejaba en cero el
            // formulario viejo del catálogo fiscal y rompía el cálculo de la liquidación
            // y el contexto del motor documental con
            // "Year, Month, and Day parameters describe an un-representable DateTime."
            // (TerminationService.ResolveAguinaldoStartDate ahora también se protege al
            // leer; esta migración repara los datos para que el catálogo muestre el valor
            // real y el próximo guardado no sea rechazado por la validación de rango).
            migrationBuilder.Sql(@"
UPDATE ""CountryTaxConfigs""
SET ""AguinaldoPeriodStartMonth"" = 12,
    ""AguinaldoPeriodStartDay"" = 1
WHERE ""AguinaldoPeriodStartMonth"" < 1 OR ""AguinaldoPeriodStartMonth"" > 12
   OR ""AguinaldoPeriodStartDay"" < 1 OR ""AguinaldoPeriodStartDay"" > 31;

UPDATE ""CountryTaxConfigs""
SET ""AguinaldoPeriodStartDay"" = 1
WHERE ""AguinaldoPeriodStartMonth"" BETWEEN 1 AND 12
  AND ((""AguinaldoPeriodStartMonth"" IN (4, 6, 9, 11) AND ""AguinaldoPeriodStartDay"" > 30)
    OR (""AguinaldoPeriodStartMonth"" = 2 AND ""AguinaldoPeriodStartDay"" > 29));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reparación de datos: no hay reversa (el valor 0 original ya no es
            // recuperable ni sería válido).
        }
    }
}
