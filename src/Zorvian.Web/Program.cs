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
builder.Services.AddControllers(options =>
{
    // Register global validation filter
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
app.UseRateLimitingMiddleware(maxRequests: 120, windowSeconds: 60);

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
        version = "1.0.0",
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
            logger.LogInformation("Migration(s) applied successfully");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Migration failed");
    }
}

app.Run();

public partial class Program { }