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
            new()
            {
                CountryCode = "NIC",
                CountryName = "Nicaragua",
                Currency = "NIO",
                InssEmployeeRate = 0.07m,
                InssEmployeeMax = 84764.00m,
                InssEmployerRate = 0.225m,
                InssEmployerMax = 84764.00m,
                OtherEmployerRate = 0.02m,
                OtherEmployerName = "INATEC",
                IrExemptAmount = 154524.00m,
                IrTableJson = "[{\"min\":0,\"max\":100000,\"rate\":0},{\"min\":100001,\"max\":200000,\"rate\":0.15},{\"min\":200001,\"max\":350000,\"rate\":0.20},{\"min\":350001,\"max\":500000,\"rate\":0.25},{\"min\":500001,\"max\":9999999,\"rate\":0.30}]",
                VacationDaysPerYear = 15,
                ChristmasBonusPercentage = 0.0833m,
                IndemnityDaysPerYear = 12,
                MaxIndemnityYears = 12,
                HasThirteenthMonth = true,
                HasFourteenthMonth = false,
                IsActive = true,
            },
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
                IrExemptAmount = 932000.00m,
                IrTableJson = "[{\"min\":0,\"max\":932000,\"rate\":0},{\"min\":932001,\"max\":1379000,\"rate\":0.10},{\"min\":1379001,\"max\":2425000,\"rate\":0.15},{\"min\":2425001,\"max\":4850000,\"rate\":0.20},{\"min\":4850001,\"max\":9999999,\"rate\":0.25}]",
                VacationDaysPerYear = 12,
                ChristmasBonusPercentage = 0.0833m,
                IndemnityDaysPerYear = 24,
                MaxIndemnityYears = 12,
                HasThirteenthMonth = true,
                HasFourteenthMonth = true,
                IsActive = true,
            },
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
                IrExemptAmount = 11000.00m,
                IrTableJson = "[{\"min\":0,\"max\":11000,\"rate\":0},{\"min\":11001,\"max\":50000,\"rate\":0.15},{\"min\":50001,\"max\":9999999,\"rate\":0.25}]",
                VacationDaysPerYear = 30,
                ChristmasBonusPercentage = 0.25m,
                IndemnityDaysPerYear = 3,
                MaxIndemnityYears = 10,
                HasThirteenthMonth = true,
                HasFourteenthMonth = false,
                IsActive = true,
            },
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
                IrExemptAmount = 19445.00m,
                IrTableJson = "[{\"min\":0,\"max\":19445,\"rate\":0},{\"min\":19446,\"max\":30000,\"rate\":0.15},{\"min\":30001,\"max\":71000,\"rate\":0.20},{\"min\":71001,\"max\":200000,\"rate\":0.25},{\"min\":200001,\"max\":9999999,\"rate\":0.30}]",
                VacationDaysPerYear = 12,
                ChristmasBonusPercentage = 0.0833m,
                IndemnityDaysPerYear = 10,
                MaxIndemnityYears = 10,
                HasThirteenthMonth = true,
                HasFourteenthMonth = false,
                IsActive = true,
            },
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
                IrExemptAmount = 555.16m,
                IrTableJson = "[{\"min\":0,\"max\":555.16,\"rate\":0},{\"min\":555.17,\"max\":916.34,\"rate\":0.10},{\"min\":916.35,\"max\":2278.96,\"rate\":0.20},{\"min\":2278.97,\"max\":9999999,\"rate\":0.30}]",
                VacationDaysPerYear = 15,
                ChristmasBonusPercentage = 0.0833m,
                IndemnityDaysPerYear = 15,
                MaxIndemnityYears = 15,
                HasThirteenthMonth = true,
                HasFourteenthMonth = false,
                IsActive = true,
            },
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
                IrExemptAmount = 34202.16m,
                IrTableJson = "[{\"min\":0,\"max\":34202.16,\"rate\":0},{\"min\":34202.17,\"max\":45602.88,\"rate\":0.05},{\"min\":45602.89,\"max\":86254.92,\"rate\":0.10},{\"min\":86254.93,\"max\":164517.00,\"rate\":0.20},{\"min\":164517.01,\"max\":9999999,\"rate\":0.35}]",
                VacationDaysPerYear = 15,
                ChristmasBonusPercentage = 0.0833m,
                IndemnityDaysPerYear = 0,
                MaxIndemnityYears = 0,
                HasThirteenthMonth = true,
                HasFourteenthMonth = false,
                IsActive = true,
            },
        };

        db.CountryTaxConfigs.AddRange(configs);
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} CountryTaxConfig records for Central American countries", configs.Count);
    }
}
