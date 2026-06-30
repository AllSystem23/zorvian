using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zorvian.Core.Entities;

namespace Zorvian.Infrastructure.Data;

/// <summary>
/// Seeds the default subscription plans into the database on first run.
/// Plans are a global catalog (not tenant-scoped).
/// </summary>
public static class SubscriptionPlanSeeder
{
    public static async Task SeedAsync(ZorvianDbContext db, Microsoft.Extensions.Logging.ILogger logger)
    {
        if (await db.SubscriptionPlans.AnyAsync())
        {
            logger.LogInformation("SubscriptionPlan table already seeded, skipping.");
            return;
        }

        logger.LogInformation("Seeding default subscription plans...");

        var plans = new[]
        {
            new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                PlanId = "starter",
                Name = "Starter",
                Price = 0m,
                Period = "Para siempre",
                MaxEmployees = 10,
                IsPopular = false,
                ShortDescription = "Hasta 10 empleados, mÃ³dulos bÃ¡sicos",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            },
            new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                PlanId = "professional",
                Name = "Professional",
                Price = 49m,
                Period = "/mes",
                MaxEmployees = 100,
                IsPopular = true,
                ShortDescription = "Hasta 100 empleados, todos los mÃ³dulos",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            },
            new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                PlanId = "enterprise",
                Name = "Enterprise",
                Price = 199m,
                Period = "/mes",
                MaxEmployees = 9999,
                IsPopular = false,
                ShortDescription = "Empleados ilimitados, IA + API",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            },
        };

        await db.SubscriptionPlans.AddRangeAsync(plans);
        await db.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} subscription plans.", plans.Length);
    }
}
