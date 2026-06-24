using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Infrastructure.Data;

/// <summary>
/// Seeds Fleet catalog data (brands, vehicle types, fuel types, license categories) when tables are empty.
/// Also handles migration of legacy license categories (without CountryCode) to per-country data.
/// </summary>
public static class FleetCatalogSeeder
{
    private const string TenantId = "SYSTEM";

    public static async Task SeedAsync(ZorvianDbContext db, ILogger logger)
    {
        // ── Vehicle Brands ──
        if (!await db.VehicleBrands.IgnoreQueryFilters().AnyAsync())
        {
            logger.LogInformation("Seeding VehicleBrands...");
            var now = DateTime.UtcNow;
            db.VehicleBrands.AddRange(
                BuildBrand("Toyota", "Vehículos Toyota", now),
                BuildBrand("Ford", "Vehículos Ford", now),
                BuildBrand("Chevrolet", "Vehículos Chevrolet (GM)", now),
                BuildBrand("Hyundai", "Vehículos Hyundai", now),
                BuildBrand("Kia", "Vehículos Kia", now),
                BuildBrand("Nissan", "Vehículos Nissan", now),
                BuildBrand("Honda", "Vehículos Honda", now),
                BuildBrand("Mitsubishi", "Vehículos Mitsubishi", now),
                BuildBrand("Mercedes-Benz", "Vehículos Mercedes-Benz", now),
                BuildBrand("BMW", "Vehículos BMW", now),
                BuildBrand("Isuzu", "Vehículos Isuzu (comercial)", now),
                BuildBrand("Hino", "Vehículos Hino (camiones)", now),
                BuildBrand("Volkswagen", "Vehículos Volkswagen", now),
                BuildBrand("Mazda", "Vehículos Mazda", now),
                BuildBrand("Suzuki", "Vehículos Suzuki", now)
            );
        }

        // ── Vehicle Types ──
        if (!await db.VehicleTypes.IgnoreQueryFilters().AnyAsync())
        {
            logger.LogInformation("Seeding VehicleTypes...");
            var now = DateTime.UtcNow;
            db.VehicleTypes.AddRange(
                BuildVehicleType("Sedán", "Automóvil sedan de 4 puertas", now),
                BuildVehicleType("Camioneta", "Camioneta tipo SUV o pickup", now),
                BuildVehicleType("SUV", "Vehículo todoterreno tipo SUV", now),
                BuildVehicleType("Pickup", "Camión pickup de carga ligera", now),
                BuildVehicleType("Furgoneta", "Van o furgoneta de pasajeros/carga", now),
                BuildVehicleType("Camión", "Vehículo de carga pesada", now),
                BuildVehicleType("Moto", "Motocicleta", now),
                BuildVehicleType("Autobús", "Vehículo de transporte de pasajeros", now),
                BuildVehicleType("Cabezal", "Tractor de cabezal para remolques", now),
                BuildVehicleType("Remolque", "Remolque o semirremolque", now),
                BuildVehicleType("Maquinaria", "Maquinaria de construcción o agrícola", now),
                BuildVehicleType("Eléctrico", "Vehículo 100% eléctrico", now),
                BuildVehicleType("Híbrido", "Vehículo híbrido gasolina-eléctrico", now)
            );
        }

        // ── Fuel Types ──
        if (!await db.FuelTypes.IgnoreQueryFilters().AnyAsync())
        {
            logger.LogInformation("Seeding FuelTypes...");
            var now = DateTime.UtcNow;
            db.FuelTypes.AddRange(
                BuildFuelType("Gasolina", "Gasolina regular o premium", now),
                BuildFuelType("Diésel", "Diésel regular o ultra", now),
                BuildFuelType("Eléctrico", "Carga eléctrica (batería)", now),
                BuildFuelType("Gas (GLP/GNV)", "Gas licuado de petróleo o gas natural vehicular", now),
                BuildFuelType("Híbrido", "Combinación gasolina + eléctrico", now),
                BuildFuelType("Gasolina + Gas", "Dual: gasolina y gas GLP/GNV", now),
                BuildFuelType("Diésel + Gas", "Dual: diésel y gas GNV", now),
                BuildFuelType("Etanol", "Combustible a base de etanol", now),
                BuildFuelType("Hidrógeno", "Celda de combustible de hidrógeno", now)
            );
        }

        // ── Driver License Categories (per country) ──
        // Handle 3 scenarios:
        // 1. Table empty → seed all 38 categories
        // 2. Table has items WITHOUT CountryCode (legacy) → delete & re-seed
        // 3. Table has items WITH CountryCode → already migrated, skip
        var hasAny = await db.DriverLicenseCategories.IgnoreQueryFilters().AnyAsync();
        if (hasAny)
        {
            // Check if any items lack CountryCode (legacy data)
            var legacyCount = await db.DriverLicenseCategories.IgnoreQueryFilters()
                .CountAsync(c => c.CountryCode == "" || c.CountryCode == null);
            if (legacyCount > 0)
            {
                logger.LogWarning("Migrating {Count} legacy DriverLicenseCategories (no CountryCode) to per-country data...", legacyCount);
                var legacy = await db.DriverLicenseCategories.IgnoreQueryFilters().ToListAsync();
                db.DriverLicenseCategories.RemoveRange(legacy);
                await db.SaveChangesAsync(); // Save deletion before re-seeding
                hasAny = false; // Force re-seed below
            }
        }

        if (!hasAny)
        {
            logger.LogInformation("Seeding DriverLicenseCategories per country (38 categories across 6 countries)...");
            var now = DateTime.UtcNow;
            db.DriverLicenseCategories.AddRange(
                // ── 🇳🇮 Nicaragua (MOT) ──
                BuildLicenseCategory("A1", "Motocicleta hasta 125cc", "NIC", now),
                BuildLicenseCategory("A2", "Motocicleta más de 125cc", "NIC", now),
                BuildLicenseCategory("B1", "Vehículo particular hasta 3,500 kg", "NIC", now),
                BuildLicenseCategory("B2", "Vehículo particular hasta 3,500 kg con remolque", "NIC", now),
                BuildLicenseCategory("C1", "Vehículo de servicio público hasta 3,500 kg", "NIC", now),
                BuildLicenseCategory("C2", "Vehículo de servicio público más de 3,500 kg", "NIC", now),
                BuildLicenseCategory("D1", "Autobús hasta 30 pasajeros", "NIC", now),
                BuildLicenseCategory("D2", "Autobús más de 30 pasajeros", "NIC", now),
                BuildLicenseCategory("E", "Vehículo articulado (cabezal + remolque)", "NIC", now),
                BuildLicenseCategory("F", "Maquinaria especial (grúa, retroexcavadora, etc.)", "NIC", now),

                // ── 🇨🇷 Costa Rica (MTT) ──
                BuildLicenseCategory("A1", "Motocicleta hasta 125cc", "CRI", now),
                BuildLicenseCategory("A2", "Motocicleta más de 125cc", "CRI", now),
                BuildLicenseCategory("B1", "Vehículo particular hasta 3,500 kg", "CRI", now),
                BuildLicenseCategory("B2", "Vehículo particular hasta 3,500 kg con remolque", "CRI", now),
                BuildLicenseCategory("C1", "Vehículo de servicio público hasta 3,500 kg", "CRI", now),
                BuildLicenseCategory("C2", "Vehículo de servicio público más de 3,500 kg", "CRI", now),

                // ── 🇬🇹 Guatemala (MT) ──
                BuildLicenseCategory("A1", "Motocicleta hasta 125cc", "GTM", now),
                BuildLicenseCategory("A2", "Motocicleta más de 125cc", "GTM", now),
                BuildLicenseCategory("B1", "Vehículo particular hasta 3,500 kg", "GTM", now),
                BuildLicenseCategory("B2", "Vehículo particular hasta 3,500 kg con remolque", "GTM", now),
                BuildLicenseCategory("C1", "Vehículo de servicio público hasta 3,500 kg", "GTM", now),
                BuildLicenseCategory("C2", "Vehículo de servicio público más de 3,500 kg", "GTM", now),
                BuildLicenseCategory("D1", "Autobús hasta 30 pasajeros", "GTM", now),
                BuildLicenseCategory("D2", "Autobús más de 30 pasajeros", "GTM", now),
                BuildLicenseCategory("E", "Vehículo articulado (cabezal + remolque)", "GTM", now),
                BuildLicenseCategory("F", "Maquinaria especial (grúa, retroexcavadora, etc.)", "GTM", now),

                // ── 🇭🇳 Honduras (SCT) ──
                BuildLicenseCategory("A1", "Motocicleta hasta 125cc", "HND", now),
                BuildLicenseCategory("A2", "Motocicleta más de 125cc", "HND", now),
                BuildLicenseCategory("B1", "Vehículo particular hasta 3,500 kg", "HND", now),
                BuildLicenseCategory("B2", "Vehículo particular hasta 3,500 kg con remolque", "HND", now),
                BuildLicenseCategory("C1", "Vehículo de servicio público", "HND", now),

                // ── 🇸🇻 El Salvador (MOP) ──
                BuildLicenseCategory("A1", "Motocicleta hasta 125cc", "SLV", now),
                BuildLicenseCategory("A2", "Motocicleta más de 125cc", "SLV", now),
                BuildLicenseCategory("B1", "Vehículo particular hasta 3,500 kg", "SLV", now),
                BuildLicenseCategory("B2", "Vehículo particular hasta 3,500 kg con remolque", "SLV", now),
                BuildLicenseCategory("C1", "Vehículo de servicio público", "SLV", now),

                // ── 🇵🇦 Panamá (ATTT) ──
                BuildLicenseCategory("A1", "Motocicleta", "PAN", now),
                BuildLicenseCategory("B1", "Vehículo particular hasta 3,500 kg", "PAN", now),
                BuildLicenseCategory("B2", "Vehículo particular con remolque", "PAN", now),
                BuildLicenseCategory("C1", "Vehículo de servicio público", "PAN", now)
            );
        }

        var saved = await db.SaveChangesAsync();
        if (saved > 0)
            logger.LogInformation("Fleet catalog seeded successfully ({Count} rows).", saved);
    }

    private static VehicleBrand BuildBrand(string name, string? description, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = TenantId,
        CreatedAt = now,
        CreatedBy = "system",
        Name = name,
        Description = description,
        IsActive = true
    };

    private static VehicleType BuildVehicleType(string name, string? description, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = TenantId,
        CreatedAt = now,
        CreatedBy = "system",
        Name = name,
        Description = description,
        IsActive = true
    };

    private static FuelType BuildFuelType(string name, string? description, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = TenantId,
        CreatedAt = now,
        CreatedBy = "system",
        Name = name,
        Description = description,
        IsActive = true
    };

    private static DriverLicenseCategory BuildLicenseCategory(string name, string? description, string countryCode, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = TenantId,
        CreatedAt = now,
        CreatedBy = "system",
        Name = name,
        Description = description,
        CountryCode = countryCode,
        IsActive = true
    };
}
