using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Data;

/// <summary>
/// Seeds global CountryTaxConfig catalog with labor law defaults for all supported countries.
/// This is a global (non-tenant) table shared across all companies.
/// </summary>
public static class CountryTaxConfigSeeder
{
    public static async Task SeedAsync(ZorvianDbContext db, ILogger logger)
    {
        if (await db.CountryTaxConfigs.AnyAsync())
        {
            logger.LogInformation("CountryTaxConfig already seeded, skipping");
            return;
        }

        var configs = new List<CountryTaxConfig>
        {
            // ═══════════════════════════════════════════════════════════════
            // NICARAGUA — Decreto 06-2019 Reforma al Reglamento General de la Ley de Seguridad Social
            // Régimen Integral (empleo general): 7% emp / 22.5% pat (≥50) or 21.5% pat (<50)
            // Régimen IVM (Invalidez, Vejez y Muerte): 5% emp / 16.5% pat (≥50) or 15.5% pat (<50)
            // ═══════════════════════════════════════════════════════════════
            new()
            {
                CountryCode = "NIC",
                CountryName = "Nicaragua",
                Currency = "NIO",
                // Legacy/fallback defaults (Región Integral, ≥50)
                InssEmployeeRate = 0.07m,
                InssEmployeeMax = 84764.00m,
                InssEmployerRate = 0.225m,
                InssEmployerMax = 84764.00m,
                OtherEmployerRate = 0.02m,
                OtherEmployerName = "INATEC",
                // Regímenes INSS (Decreto 06-2019)
                InssIntegralEmployeeRate = 0.07m,
                InssIntegralEmployerRate = 0.225m,       // ≥50 empleados
                InssIntegralEmployerRateSmall = 0.215m,   // <50 empleados
                InssIvmEmployeeRate = 0.05m,
                InssIvmEmployerRate = 0.165m,             // ≥50 empleados
                InssIvmEmployerRateSmall = 0.155m,        // <50 empleados
                InssSmallEmployerThreshold = 50,
                // IR
                IrExemptAmount = 154524.00m,
                IrTableJson = "[{\"min\":0,\"max\":100000,\"rate\":0},{\"min\":100001,\"max\":200000,\"rate\":0.15},{\"min\":200001,\"max\":350000,\"rate\":0.20},{\"min\":350001,\"max\":500000,\"rate\":0.25},{\"min\":500001,\"max\":9999999,\"rate\":0.30}]",
                // Prestaciones laborales
                VacationDaysPerYear = 30, // Art. 78 CT
                ChristmasBonusPercentage = 0.0833m, // 1/12 por mes
                IndemnityDaysPerYear = 30, // Art. 45 CT: 1 mes (30 días) por año de servicio
                MaxIndemnityYears = 12, // Máximo 12 meses (Art. 45 CT)
                // Fórmula escalonada NIC (Art. 45 CT):
                // 1-3 años: 30 días/año | 4-6 años: 20 días/año | 7+: tope 150 días
                IndemnityTiersJson = "[{\"upToYears\":3,\"daysPerYear\":30},{\"upToYears\":6,\"daysPerYear\":20}]",
                MaxIndemnityDays = 150, // Tope: 5 meses (150 días)
                HasThirteenthMonth = true,
                HasFourteenthMonth = false,
                // Aguinaldo (Art. 93 CT): siempre desde 1/dic del año anterior
                AguinaldoPeriodStartMonth = 12,
                AguinaldoPeriodStartDay = 1,
                DefaultFiscalStartMonth = 1,
                IsActive = true,
            },

            // ═══════════════════════════════════════════════════════════════
            // COSTA RICA — CCSS (Caja Costarricense de Seguro Social)
            // Régimen Semi Integral: 10.83% emp / 26.83% pat
            // Régimen IVM: 5.58% emp / 18.58% pat
            // ═══════════════════════════════════════════════════════════════
            new()
            {
                CountryCode = "CRI",
                CountryName = "Costa Rica",
                Currency = "CRC",
                InssEmployeeRate = 0.1083m,
                InssEmployeeMax = 863083.00m,
                InssEmployerRate = 0.2683m,
                InssEmployerMax = 863083.00m,
                OtherEmployerRate = 0,
                OtherEmployerName = null,
                // Regímenes CCSS
                InssIntegralEmployeeRate = 0.1083m,     // Semi Integral
                InssIntegralEmployerRate = 0.2683m,
                InssIntegralEmployerRateSmall = 0.2683m, // CR no distingue por tamaño
                InssIvmEmployeeRate = 0.0558m,
                InssIvmEmployerRate = 0.1858m,
                InssIvmEmployerRateSmall = 0.1858m,
                InssSmallEmployerThreshold = 50,
                IrExemptAmount = 932000.00m,
                IrTableJson = "[{\"min\":0,\"max\":932000,\"rate\":0},{\"min\":932001,\"max\":1379000,\"rate\":0.10},{\"min\":1379001,\"max\":2425000,\"rate\":0.15},{\"min\":2425001,\"max\":4850000,\"rate\":0.20},{\"min\":4850001,\"max\":9999999,\"rate\":0.25}]",
                VacationDaysPerYear = 12,
                ChristmasBonusPercentage = 0.0833m,
                IndemnityDaysPerYear = 24,
                MaxIndemnityYears = 12,
                HasThirteenthMonth = true,
                HasFourteenthMonth = true,
                DefaultFiscalStartMonth = 1,
                IsActive = true,
            },

            // ═══════════════════════════════════════════════════════════════
            // PANAMÁ — CSS (Caja de Seguro Social)
            // Régimen Integral: 9.75% emp / 12.25% pat (+ 1.5% riesgos + 0.56% educación)
            // No distingue por tamaño de empresa
            // ═══════════════════════════════════════════════════════════════
            new()
            {
                CountryCode = "PAN",
                CountryName = "Panamá",
                Currency = "USD",
                InssEmployeeRate = 0.0975m,
                InssEmployeeMax = 7000.00m,
                InssEmployerRate = 0.1325m,
                InssEmployerMax = 7000.00m,
                OtherEmployerRate = 0.015m,
                OtherEmployerName = "Seguro Educativo",
                InssIntegralEmployeeRate = 0.0975m,
                InssIntegralEmployerRate = 0.1225m,
                InssIntegralEmployerRateSmall = 0.1225m,
                InssIvmEmployeeRate = 0m,    // Panamá no tiene régimen IVM separado
                InssIvmEmployerRate = 0m,
                InssIvmEmployerRateSmall = 0m,
                InssSmallEmployerThreshold = 50,
                IrExemptAmount = 11000.00m,
                IrTableJson = "[{\"min\":0,\"max\":11000,\"rate\":0},{\"min\":11001,\"max\":50000,\"rate\":0.15},{\"min\":50001,\"max\":9999999,\"rate\":0.25}]",
                VacationDaysPerYear = 30,
                ChristmasBonusPercentage = 0.25m,
                IndemnityDaysPerYear = 3,
                MaxIndemnityYears = 10,
                HasThirteenthMonth = true,
                HasFourteenthMonth = false,
                DefaultFiscalStartMonth = 1,
                IsActive = true,
            },

            // ═══════════════════════════════════════════════════════════════
            // HONDURAS — IHSS (Instituto Hondureño de Seguridad Social)
            // Régimen Contributivo: 5% emp / 12.5% pat (+ 1.5% RAP)
            // ═══════════════════════════════════════════════════════════════
            new()
            {
                CountryCode = "HND",
                CountryName = "Honduras",
                Currency = "HNL",
                InssEmployeeRate = 0.05m,
                InssEmployeeMax = 50333.00m,
                InssEmployerRate = 0.125m,
                InssEmployerMax = 50333.00m,
                OtherEmployerRate = 0.015m,
                OtherEmployerName = "RAP (FONDO VIVIENDA)",
                InssIntegralEmployeeRate = 0.05m,
                InssIntegralEmployerRate = 0.125m,
                InssIntegralEmployerRateSmall = 0.125m,
                InssIvmEmployeeRate = 0m,    // Honduras no distingue régimen IVM
                InssIvmEmployerRate = 0m,
                InssIvmEmployerRateSmall = 0m,
                InssSmallEmployerThreshold = 50,
                IrExemptAmount = 19445.00m,
                IrTableJson = "[{\"min\":0,\"max\":19445,\"rate\":0},{\"min\":19446,\"max\":30000,\"rate\":0.15},{\"min\":30001,\"max\":71000,\"rate\":0.20},{\"min\":71001,\"max\":200000,\"rate\":0.25},{\"min\":200001,\"max\":9999999,\"rate\":0.30}]",
                VacationDaysPerYear = 12,
                ChristmasBonusPercentage = 0.0833m,
                IndemnityDaysPerYear = 10,
                MaxIndemnityYears = 10,
                HasThirteenthMonth = true,
                HasFourteenthMonth = false,
                DefaultFiscalStartMonth = 1,
                IsActive = true,
            },

            // ═══════════════════════════════════════════════════════════════
            // EL SALVADOR — ISSS + AFP (Administradora de Fondos de Pensiones)
            // ISSS: 3% emp / 7.5% pat
            // AFP: 6.25% emp / 8.75% pat
            // ═══════════════════════════════════════════════════════════════
            new()
            {
                CountryCode = "SLV",
                CountryName = "El Salvador",
                Currency = "USD",
                InssEmployeeRate = 0.03m,
                InssEmployeeMax = 1000.00m,
                InssEmployerRate = 0.175m,
                InssEmployerMax = 1000.00m,
                OtherEmployerRate = 0.01m,
                OtherEmployerName = "INSAFORP",
                InssIntegralEmployeeRate = 0.03m,     // ISSS
                InssIntegralEmployerRate = 0.075m,     // ISSS patronal
                InssIntegralEmployerRateSmall = 0.075m,
                InssIvmEmployeeRate = 0.0625m,         // AFP
                InssIvmEmployerRate = 0.0875m,         // AFP patronal
                InssIvmEmployerRateSmall = 0.0875m,
                InssSmallEmployerThreshold = 50,
                IrExemptAmount = 555.16m,
                IrTableJson = "[{\"min\":0,\"max\":555.16,\"rate\":0},{\"min\":555.17,\"max\":916.34,\"rate\":0.10},{\"min\":916.35,\"max\":2278.96,\"rate\":0.20},{\"min\":2278.97,\"max\":9999999,\"rate\":0.30}]",
                VacationDaysPerYear = 15,
                ChristmasBonusPercentage = 0.0833m,
                IndemnityDaysPerYear = 15,
                MaxIndemnityYears = 15,
                HasThirteenthMonth = true,
                HasFourteenthMonth = false,
                DefaultFiscalStartMonth = 1,
                IsActive = true,
            },

            // ═══════════════════════════════════════════════════════════════
            // GUATEMALA — IGSS (Instituto Guatemalteco de Seguridad Social)
            // Régimen General: 4.83% emp / 12.67% pat (+ INTECAP 0.5% + IRTRA 1.5%)
            // ═══════════════════════════════════════════════════════════════
            new()
            {
                CountryCode = "GTM",
                CountryName = "Guatemala",
                Currency = "GTQ",
                InssEmployeeRate = 0.0483m,
                InssEmployeeMax = 5562.91m,
                InssEmployerRate = 0.1267m,
                InssEmployerMax = 5562.91m,
                OtherEmployerRate = 0.02m,
                OtherEmployerName = "INTECAP + IRTRA",
                InssIntegralEmployeeRate = 0.0483m,
                InssIntegralEmployerRate = 0.1267m,
                InssIntegralEmployerRateSmall = 0.1267m,
                InssIvmEmployeeRate = 0m,    // Guatemala no tiene régimen IVM separado
                InssIvmEmployerRate = 0m,
                InssIvmEmployerRateSmall = 0m,
                InssSmallEmployerThreshold = 50,
                IrExemptAmount = 34202.16m,
                IrTableJson = "[{\"min\":0,\"max\":34202.16,\"rate\":0},{\"min\":34202.17,\"max\":45602.88,\"rate\":0.05},{\"min\":45602.89,\"max\":86254.92,\"rate\":0.10},{\"min\":86254.93,\"max\":164517.00,\"rate\":0.20},{\"min\":164517.01,\"max\":9999999,\"rate\":0.35}]",
                VacationDaysPerYear = 15,
                ChristmasBonusPercentage = 0.0833m,
                IndemnityDaysPerYear = 0,
                MaxIndemnityYears = 0,
                HasThirteenthMonth = true,
                HasFourteenthMonth = false,
                DefaultFiscalStartMonth = 1,
                IsActive = true,
            },

            // ═══════════════════════════════════════════════════════════════
            // UNITED KINGDOM — HMRC fiscal year (April to March)
            // ═══════════════════════════════════════════════════════════════
            new()
            {
                CountryCode = "GBR",
                CountryName = "United Kingdom",
                Currency = "GBP",
                IrExemptAmount = 0m,
                IrTableJson = "[]",
                VacationDaysPerYear = 28,
                ChristmasBonusPercentage = 0m,
                HasThirteenthMonth = false,
                DefaultFiscalStartMonth = 4,
                IsActive = true,
            },

            // ═══════════════════════════════════════════════════════════════
            // INDIA — Central Board of Direct Taxes fiscal year (April to March)
            // ═══════════════════════════════════════════════════════════════
            new()
            {
                CountryCode = "IND",
                CountryName = "India",
                Currency = "INR",
                IrExemptAmount = 0m,
                IrTableJson = "[]",
                VacationDaysPerYear = 24,
                ChristmasBonusPercentage = 0m,
                HasThirteenthMonth = false,
                DefaultFiscalStartMonth = 4,
                IsActive = true,
            },

            // ═══════════════════════════════════════════════════════════════
            // UNITED STATES — Calendar year (January to December)
            // Companies can override via CompanySettings.FiscalYearStartMonth
            // ═══════════════════════════════════════════════════════════════
            new()
            {
                CountryCode = "USA",
                CountryName = "United States",
                Currency = "USD",
                IrExemptAmount = 0m,
                IrTableJson = "[]",
                VacationDaysPerYear = 0,
                ChristmasBonusPercentage = 0m,
                HasThirteenthMonth = false,
                DefaultFiscalStartMonth = 1,
                IsActive = true,
            },
        };

        db.CountryTaxConfigs.AddRange(configs);
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} CountryTaxConfig records", configs.Count);
    }
}
