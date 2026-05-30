using System.Reflection;
using System.Text;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Nexora.Application.Interfaces;
using Nexora.Application.Services;
using Nexora.Core.Interfaces;
using Nexora.Infrastructure.Data;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Repositories;
using Nexora.Infrastructure.Services;
using Nexora.Web.Hubs;
using Nexora.Web.Jobs;
using Nexora.Application.Jobs;
using Nexora.Web.Middleware;
using Nexora.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Firebase Admin SDK
var credPath = builder.Configuration["Firebase:CredentialsFilePath"];
if (!string.IsNullOrEmpty(credPath))
{
    var fullPath = Path.Combine(builder.Environment.ContentRootPath, credPath);
    if (File.Exists(fullPath))
    {
        FirebaseApp.Create(new AppOptions
        {
            Credential = CredentialFactory.FromFile<ServiceAccountCredential>(fullPath).ToGoogleCredential().CreateScoped(),
            ProjectId = builder.Configuration["Firebase:ProjectId"],
        });
    }
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

// Database
builder.Services.AddDbContext<NexoraDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("NexoraDb")));

// DI - Infrastructure
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddSingleton<IFirebaseAuthService, FirebaseAuthService>();
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();

// DI - Application
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CompanyService>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddScoped<DepartmentService>();
builder.Services.AddScoped<VacationService>();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddScoped<IVacationRepository, VacationRepository>();
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<SeedService>();
builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
builder.Services.AddScoped<IReportService, ReportService>();
var storageBucket = builder.Configuration["Firebase:StorageBucket"]
    ?? "nexora-hr.firebasestorage.app";
builder.Services.AddScoped<IDocumentStorageService>(_ =>
    new FirebaseStorageService(storageBucket));

// DI - Repositories
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<AttendanceService>();
builder.Services.AddScoped<IPayrollRepository, PayrollRepository>();
builder.Services.AddScoped<PayrollService>();
builder.Services.AddScoped<IWebhookService, WebhookService>();
builder.Services.AddScoped<ApiKeyService>();
builder.Services.AddScoped<IJobScheduler, HangfireJobScheduler>();
builder.Services.AddScoped<IOcrService, OcrService>();
builder.Services.AddScoped<IEmbeddingService>(sp => new EmbeddingService(
    builder.Configuration["GoogleAi:ProjectId"]!,
    builder.Configuration["GoogleAi:Location"]!,
    "text-embedding-004"));
builder.Services.AddScoped<IPolicyRepository, PolicyRepository>();
builder.Services.AddScoped<IPolicyService, PolicyService>();
builder.Services.AddScoped<PerformanceService>();
builder.Services.AddScoped<IVacationRecommendationService, VacationRecommendationService>();
builder.Services.AddScoped<IChatService>(sp => new ChatService(
    sp.GetRequiredService<NexoraDbContext>(),
    sp.GetRequiredService<IEmbeddingService>(),
    builder.Configuration["GoogleAi:ProjectId"]!,
    builder.Configuration["GoogleAi:Location"]!));
builder.Services.AddSingleton<AbsenteeismPredictionService>();
builder.Services.AddScoped<OcrProcessingJob>();
builder.Services.AddHttpClient(); // For fetching document streams

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("JWT Secret not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "nexora",
            ValidAudience = "nexora-api",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero,
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// SignalR
builder.Services.AddSignalR();

// Notifications
builder.Services.AddScoped<SignalRNotificationService>();
builder.Services.AddScoped<IFCMNotificationService, FCMNotificationService>();
builder.Services.AddScoped<INotificationService, CombinedNotificationService>();

// Hangfire
builder.Services.AddHangfire(c => c.UseMemoryStorage());
builder.Services.AddHangfireServer();
builder.Services.AddScoped<CheckInReminderJob>();
builder.Services.AddScoped<DocumentExpirationJob>();
builder.Services.AddScoped<AttendancePhotoCleanupJob>();
builder.Services.AddScoped<AbsenteeismTrainingJob>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("NexoraCors", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["*"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Nexora HR API",
        Version = "v1",
        Description = "API REST para el sistema de gestión de recursos humanos Nexora. Multi-tenant, con autenticación JWT + Firebase.",
        Contact = new OpenApiContact
        {
            Name = "Soporte Nexora",
            Email = "soporte@nexora.app",
        },
        License = new OpenApiLicense
        {
            Name = "Uso interno",
        },
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new() { Id = "Bearer", Type = ReferenceType.SecurityScheme }
            },
            Array.Empty<string>()
        }
    });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("NexoraCors");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseTenantMiddleware();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapControllers();

// Hangfire dashboard (dev only)
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard();
}

// Recurring job: check-in reminder at 9 AM
RecurringJob.AddOrUpdate<CheckInReminderJob>(
    "check-in-reminder",
    j => j.RunAsync(),
    "0 9 * * *"); // Every day at 09:00

RecurringJob.AddOrUpdate<DocumentExpirationJob>(
    "document-expiration-check",
    j => j.RunAsync(),
    "0 8 * * *"); // Every day at 08:00

RecurringJob.AddOrUpdate<AttendancePhotoCleanupJob>(
    "attendance-photo-cleanup",
    j => j.RunAsync(),
    "0 3 1 * *"); // 1st day of every month at 03:00

RecurringJob.AddOrUpdate<AbsenteeismTrainingJob>(
    "absenteeism-model-training",
    j => j.RunAsync(),
    "0 2 * * 0"); // Every Sunday at 02:00

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", version = "1.0.0" }));

// Auto-migrate in production
if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<NexoraDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
