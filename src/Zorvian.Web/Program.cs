using FluentValidation;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Zorvian.Web.Extensions;
using Zorvian.Web.Middleware;
using Zorvian.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──
builder.Host.UseZorvianSerilog(builder.Configuration);

var mockExternal = builder.Configuration.GetValue<bool>("Testing:MockExternalServices");

// ── Firebase ──
builder.Services.AddZorvianFirebase(builder.Configuration, mockExternal);

// ── Response Compression ──
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// ── Controllers, OpenAPI ──
builder.Services.AddScoped<Zorvian.Web.Filters.ValidationFilter>();
builder.Services.AddControllers(options =>
{
    options.Filters.AddService<Zorvian.Web.Filters.ValidationFilter>();
    options.Filters.Add<Zorvian.Web.Filters.ModuleEnforcementFilter>();
}).AddJsonOptions(options =>
{
    // Global converter: auto-convert incoming DateTime with Kind=Unspecified to Utc
    // to prevent Npgsql "Cannot write DateTime with Kind=Unspecified" errors.
    options.JsonSerializerOptions.Converters.Add(new Zorvian.Web.Json.UtcDateTimeJsonConverter());
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

// ── Message Bus (MassTransit + RabbitMQ) ──
builder.Services.AddZorvianMessageBus(builder.Configuration, mockExternal);

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

// ── Redis Distributed Cache ──
var redisConnection = builder.Configuration["Redis:Connection"];
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = builder.Configuration["Redis:InstanceName"] ?? "zorvian_";
    });
    builder.Services.AddSingleton<ICacheService, RedisCacheService>();
    builder.Services.AddHealthChecks().AddRedis(redisConnection, name: "redis");
    Console.WriteLine("[Startup] Redis distributed cache enabled");
}
else
{
    builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
    Console.WriteLine("[Startup] In-memory cache (Redis not configured)");
}

// ── OpenTelemetry ──
builder.Services.AddZorvianOpenTelemetry(builder.Configuration);

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
    var authLimit = builder.Configuration.GetValue<int?>("RateLimiting:AuthLimit") ?? 10;
    var authWindowMinutes = builder.Configuration.GetValue<int?>("RateLimiting:AuthWindowMinutes") ?? 5;
    var authOtherLimit = builder.Configuration.GetValue<int?>("RateLimiting:AuthOtherLimit") ?? 30;
    var authOtherWindowMinutes = builder.Configuration.GetValue<int?>("RateLimiting:AuthOtherWindowMinutes") ?? 5;
    app.UseRateLimitingMiddleware(
        maxRequests: permitLimit,
        windowSeconds: windowMinutes * 60,
        authLimit: authLimit,
        authWindowSeconds: authWindowMinutes * 60,
        authOtherLimit: authOtherLimit,
        authOtherWindowSeconds: authOtherWindowMinutes * 60);
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

// ── Auto-migrate + seed in non-test environments ──
if (!mockExternal)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<Zorvian.Infrastructure.Data.ZorvianDbContext>();
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    // 1. Apply pending EF Core migrations so tables exist before seeding
    //    Lock asesor de Postgres: si Render corre >1 instancia, evita que dos
    //    procesos apliquen la misma migración a la vez.
    try
    {
        var applyMigrations = builder.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true);
        if (!applyMigrations)
        {
            logger.LogInformation("Database:ApplyMigrationsOnStartup=false — skipping startup migration");
        }
        else
        {
            var pending = await db.Database.GetPendingMigrationsAsync();
            var pendingList = pending.ToList();
            if (pendingList.Count > 0)
            {
                var isPostgres = db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

                Npgsql.NpgsqlConnection? lockConnection = null;
                if (isPostgres)
                {
                    // Lock a nivel de sesión sobre una conexión dedicada: se mantiene
                    // mientras migramos y se libera al disposing (fin del scope).
                    lockConnection = new Npgsql.NpgsqlConnection(db.Database.GetConnectionString());
                    await lockConnection.OpenAsync();
                    await using var lockCmd = lockConnection.CreateCommand();
                    lockCmd.CommandText = "SELECT pg_advisory_lock(742139851);";
                    await lockCmd.ExecuteNonQueryAsync();
                }

            try
            {
                logger.LogInformation("Applying {Count} pending migration(s): {Migrations}",
                    pendingList.Count, string.Join(", ", pendingList));
                await db.Database.MigrateAsync();

                // Activate Row Level Security (RLS) in Neon/Postgres
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
            finally
            {
                if (lockConnection is not null)
                {
                    try
                    {
                        await using var unlockCmd = lockConnection.CreateCommand();
                        unlockCmd.CommandText = "SELECT pg_advisory_unlock(742139851);";
                        await unlockCmd.ExecuteNonQueryAsync();
                    }
                    catch (Exception unlockEx)
                    {
                        logger.LogWarning(unlockEx, "Could not release advisory lock explicitly (connection close releases it)");
                    }
                    await lockConnection.DisposeAsync();
                }
            }
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Migration failed — app may still start if DB is already compatible");
    }

    // 2. Ensure Fleet tables exist via embedded SQL
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

    // 3. Seed catalog data if tables are empty. Un fallo (p. ej. Neon con su
    //    límite de 1024 MB excedido: cualquier escritura devuelve 53100) NO debe
    //    tumbar el arranque — los seeders son idempotentes y se reintentan en el
    //    próximo deploy; sin este guard el servicio entraba en crash-loop.
    //    Con la base llena se omiten los seeders restantes (fallarían igual).
    var storageFull = false;

    async Task SeedSafelyAsync(string seeder, Func<Task> run)
    {
        if (storageFull)
            return;

        try
        {
            await run();
        }
        catch (Exception seedEx) when (Zorvian.Infrastructure.Data.PostgresStorageErrors.IsStorageFull(seedEx))
        {
            storageFull = true;
            logger.LogCritical(seedEx,
                "{Seeder} failed: la base de datos alcanzó su límite de almacenamiento (Neon 53100). " +
                "Se omiten los seeders restantes de este arranque; libere espacio con " +
                "POST /zorvian/v1/system/db-storage/purge o siguiendo docs/DB_STORAGE_RECOVERY.md",
                seeder);
        }
        catch (Exception seedEx)
        {
            logger.LogError(seedEx,
                "{Seeder} failed — app continues without refreshing seed data", seeder);
        }
    }

    await SeedSafelyAsync(nameof(Zorvian.Infrastructure.Data.DocumentTemplateSeeder),
        () => Zorvian.Infrastructure.Data.DocumentTemplateSeeder.SeedAsync(db, logger));
    await SeedSafelyAsync(nameof(Zorvian.Infrastructure.Data.FleetCatalogSeeder),
        () => Zorvian.Infrastructure.Data.FleetCatalogSeeder.SeedAsync(db, logger));
    await SeedSafelyAsync(nameof(Zorvian.Infrastructure.Data.CountryTaxConfigSeeder),
        () => Zorvian.Infrastructure.Data.CountryTaxConfigSeeder.SeedAsync(db, logger));
    await SeedSafelyAsync(nameof(Zorvian.Infrastructure.Data.SubscriptionPlanSeeder),
        () => Zorvian.Infrastructure.Data.SubscriptionPlanSeeder.SeedAsync(db, logger));

    // 4. Upgrade automático del plan contable + enlaces de cuentas (ensure-missing) para
    //    TODAS las compañías: los asientos de Nómina y Pago a Prestadores quedan operativos
    //    sin configuración manual del contador. Idempotente; un fallo no detiene el arranque.
    //    Se omite si la base ya se quedó sin espacio (el intento solo repetiría el 53100).
    if (!storageFull)
    {
        try
        {
            var seedService = scope.ServiceProvider.GetRequiredService<Zorvian.Infrastructure.Services.SeedService>();
            await seedService.EnsureAccountsAndLinksAsync();
        }
        catch (Exception seedEx)
        {
            logger.LogError(seedEx, "Account chart/link auto-upgrade failed — app continues");
        }
    }
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
    recurringJobManager.AddOrUpdate<Zorvian.Web.Jobs.HealthCheckJob>("health-check", j => j.RunAsync(), "0 * * * *");
    recurringJobManager.AddOrUpdate<Zorvian.Application.Jobs.WarrantySlaMonitorJob>("warranty-sla-monitor", j => j.RunAsync(), "0 */4 * * *");
    recurringJobManager.AddOrUpdate<Zorvian.Web.Jobs.InvitationCleanupJob>("invitation-cleanup", j => j.RunAsync(), "0 4 * * *");
    // Red de seguridad de logos: borra objetos bajo `companies/` que ninguna
    // compañía referencia (con 24 h de gracia). Los borrados normales ocurren
    // en línea al reemplazar o quitar el logo.
    recurringJobManager.AddOrUpdate<Zorvian.Web.Jobs.CompanyLogoOrphanSweepJob>("company-logo-orphan-sweep", j => j.RunAsync(), "0 5 * * 0");
    // Backfill del HTML de DocumentVersions a Storage (libera la BD sin borrar nada).
    // Lotes de 50 cada 10 min; si un lote sale completo se encadena solo hasta drenar.
    recurringJobManager.AddOrUpdate<Zorvian.Web.Jobs.DocumentVersionContentSweepJob>("document-version-content-sweep", j => j.RunAsync(), "*/10 * * * *");
    // Retención de las tablas de alto volumen (telemetría GPS, logs de webhooks,
    // historial de entidades, journal de sync, tokens vencidos) + VACUUM ANALYZE.
    // Neon corta las escrituras al llegar a 1024 MB: sin esta purga esas tablas
    // crecen para siempre. Los AuditLogs los purga audit-log-cleanup (6 meses).
    recurringJobManager.AddOrUpdate<Zorvian.Web.Jobs.DataRetentionJob>("data-retention", j => j.RunAsync(), "30 1 * * *");
}

app.Run();

public partial class Program { }