using FluentValidation;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Zorvian.Web.Extensions;
using Zorvian.Web.Middleware;
using Zorvian.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

var mockExternal = builder.Configuration.GetValue<bool>("Testing:MockExternalServices");

// ── Firebase ──
builder.Services.AddZorvianFirebase(builder.Configuration, mockExternal);

// ── Response Compression ──
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// ── Controllers, OpenAPI ──
builder.Services.AddScoped<Zorvian.Web.Filters.ValidationFilter<object>>();
builder.Services.AddControllers(options =>
{
    options.Filters.AddService<Zorvian.Web.Filters.ValidationFilter<object>>();
});
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

// ── Database ──
builder.Services.AddZorvianDatabase(builder.Configuration, mockExternal);

// ── AutoMapper ──
builder.Services.AddZorvianAutoMapper();

// ── Infrastructure (Interceptors, Identity, Services) ──
builder.Services.AddZorvianInfrastructure();

// ── Repositories ──
builder.Services.AddZorvianRepositories();

// ── Application Services ──
builder.Services.AddZorvianApplicationServices();

// ── Jobs ──
builder.Services.AddZorvianJobs();

// ── Authentication ──
builder.Services.AddZorvianAuthentication(builder.Configuration);

// ── SignalR ──
builder.Services.AddSignalR();

// ── Hangfire ──
builder.Services.AddZorvianHangfire(builder.Configuration, mockExternal);

// ── FluentValidation ──
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ── Polly Resilience (Circuit Breaker + Retry) ──
builder.Services.AddZorvianResilience();

// ── Health Checks ──
builder.Services.AddZorvianHealthChecks(builder.Configuration, mockExternal);

// ── Anti-CSRF ──
builder.Services.AddAntiforgery();

// ── CORS ──
builder.Services.AddZorvianCors(builder.Configuration);

// ── Swagger ──
builder.Services.AddZorvianSwagger();

// ══════════════════════════════════════════════
// Build
// ══════════════════════════════════════════════
var app = builder.Build();

// ── Validate critical secrets ──
if (!mockExternal)
{
    var secrets = new[]
    {
        ("Jwt:Secret", "JWT signing key"),
        ("Encryption:Key", "AES-256-GCM encryption key"),
        ("ConnectionStrings:ZorvianDb", "Database connection string"),
        ("Firebase:ProjectId", "Firebase project ID"),
    };

    var missing = secrets
        .Select(s => (key: s.Item1, label: s.Item2, value: builder.Configuration[s.Item1]))
        .Where(s => string.IsNullOrWhiteSpace(s.value))
        .ToList();

    if (missing.Count > 0)
    {
        var missingStr = string.Join(", ", missing.Select(s => $"{s.label} ({s.key})"));
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError("Startup aborted — missing required secrets: {Missing}", missingStr);

        if (!app.Environment.IsDevelopment())
            throw new InvalidOperationException($"Missing required secrets: {missingStr}");
    }
}

// ── Pipeline ──
app.UseCors("ZorvianCors");

var enableSwagger = builder.Configuration.GetValue<bool>("Features:Swagger");
if (app.Environment.IsDevelopment() || enableSwagger)
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCorrelationId();
app.UseSecurityHeaders();
app.UseGlobalExceptionMiddleware();
app.UseRequestLogging();
var rateLimitingEnabled = builder.Configuration.GetValue<bool>("RateLimiting:Enabled");
if (rateLimitingEnabled)
{
    var permitLimit = builder.Configuration.GetValue<int>("RateLimiting:PermitLimit");
    var windowMinutes = builder.Configuration.GetValue<int>("RateLimiting:WindowMinutes");
    app.UseRateLimitingMiddleware(maxRequests: permitLimit, windowSeconds: windowMinutes * 60);
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseResponseCompression();
app.UseStaticFiles();
app.UseCsrfProtection();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthentication();
app.UseTenantMiddleware();
app.UseAuthorization();

// ── Ensure Fleet tables exist (always, not just in Production) ──
if (!mockExternal)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<Zorvian.Infrastructure.Data.ZorvianDbContext>();
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    try
    {
        var assembly = typeof(Program).Assembly;
        var stream = assembly.GetManifestResourceStream("FleetScripts.create_fleet_tables.sql");

        if (stream != null)
        {
            using var reader = new StreamReader(stream);
            var fleetSql = await reader.ReadToEndAsync();
            await db.Database.ExecuteSqlRawAsync(fleetSql);
            logger.LogInformation("Fleet tables ensured via embedded SQL script");
        }
        else
        {
            logger.LogWarning("Fleet SQL embedded resource not found. Available resources: {Resources}",
                string.Join(", ", assembly.GetManifestResourceNames()));
        }
    }
    catch (Exception fleetEx)
    {
        logger.LogError(fleetEx, "Failed to apply Fleet SQL script");
    }

    // Seed catalog data if tables are empty
    await Zorvian.Infrastructure.Data.DocumentTemplateSeeder.SeedAsync(db, logger);
    await Zorvian.Infrastructure.Data.FleetCatalogSeeder.SeedAsync(db, logger);
    await Zorvian.Infrastructure.Data.CountryTaxConfigSeeder.SeedAsync(db, logger);
}

// ── Endpoints ──
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapControllers();

// ── Health Check Endpoints ──
app.MapHealthChecks("/health", new() { ResponseWriter = async (context, report) =>
{
    context.Response.ContentType = "application/json";
    var result = new
    {
        status = report.Status.ToString(),
        version = "1.1.0-prod", // Updated version for production
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            duration = e.Value.Duration.TotalMilliseconds,
            description = e.Value.Description
        }),
        totalDuration = report.TotalDuration.TotalMilliseconds
    };
    await System.Text.Json.JsonSerializer.SerializeAsync(context.Response.Body, result);
}});

app.MapHealthChecks("/health/ready", new() { Predicate = check => check.Tags.Contains("ready") });
app.MapHealthChecks("/health/live", new() { Predicate = _ => false });

// ── Hangfire Dashboard ──
if (app.Environment.IsDevelopment() && !mockExternal)
{
    app.UseHangfireDashboard();
}

// ── Recurring Jobs ──
if (!mockExternal)
{
    var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();

    recurringJobManager.AddOrUpdate<Zorvian.Web.Jobs.CheckInReminderJob>("check-in-reminder", j => j.RunAsync(), "0 9 * * *");
    recurringJobManager.AddOrUpdate<Zorvian.Web.Jobs.DocumentExpirationJob>("document-expiration-check", j => j.RunAsync(), "0 8 * * *");
    recurringJobManager.AddOrUpdate<Zorvian.Web.Jobs.AttendancePhotoCleanupJob>("attendance-photo-cleanup", j => j.RunAsync(), "0 3 1 * *");
    recurringJobManager.AddOrUpdate<Zorvian.Web.Jobs.AbsenteeismTrainingJob>("absenteeism-model-training", j => j.RunAsync(), "0 2 * * 0");
    recurringJobManager.AddOrUpdate<Zorvian.Web.Jobs.SalesPredictionTrainingJob>("sales-prediction-model-training", j => j.RunAsync(), "0 3 * * 0");
    recurringJobManager.AddOrUpdate<Zorvian.Web.Jobs.ExpenseClassificationTrainingJob>("expense-classification-training", j => j.RunAsync(), "0 4 * * 0");
    recurringJobManager.AddOrUpdate<Zorvian.Web.Jobs.AuditLogCleanupJob>("audit-log-cleanup", j => j.RunAsync(), "0 3 1 * *");
    recurringJobManager.AddOrUpdate<Zorvian.Web.Jobs.DatabaseBackupJob>("database-backup", j => j.RunAsync(), "0 2 * * *");
    recurringJobManager.AddOrUpdate<Zorvian.Application.Jobs.VacationAutomatedJob>("vacation-accrual", j => j.RunAsync(), "0 0 1 * *");
    recurringJobManager.AddOrUpdate<Zorvian.Web.Jobs.FleetAlertJob>("fleet-alert-check", j => j.RunAsync(), "0 */6 * * *");
    recurringJobManager.AddOrUpdate<Zorvian.Web.Jobs.FleetExpenseNotificationJob>("fleet-expense-notification", j => j.RunAsync(), "0 8 * * *");
}

// ── Auto-migrate in Production ──
if (app.Environment.IsProduction() && !mockExternal)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<Zorvian.Infrastructure.Data.ZorvianDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var pending = await db.Database.GetPendingMigrationsAsync();
        var pendingList = pending.ToList();

        if (pendingList.Count > 0)
        {
            logger.LogInformation("Applying {Count} pending migration(s): {Migrations}", pendingList.Count, string.Join(", ", pendingList));
            await db.Database.MigrateAsync();
            
            // ── Activate Row Level Security (RLS) in Neon/Postgres ──
            await db.Database.ExecuteSqlRawAsync(@"
                DO $$ 
                DECLARE 
                    r RECORD;
                BEGIN
                    FOR r IN (SELECT table_name FROM information_schema.columns WHERE column_name = 'TenantId' AND table_schema = 'public') 
                    LOOP
                        EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', r.table_name);
                        EXECUTE format('DROP POLICY IF EXISTS tenant_isolation_policy ON %I', r.table_name);
                        EXECUTE format('CREATE POLICY tenant_isolation_policy ON %I USING (""TenantId"" = current_setting(''app.tenant_id'') OR current_setting(''app.is_super_admin'') = ''true'')', r.table_name);
                    END LOOP;
                END $$;
            ");

            logger.LogInformation("Migration(s) and RLS policies applied successfully");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Migration failed");
    }
}

app.Run();

public partial class Program { }