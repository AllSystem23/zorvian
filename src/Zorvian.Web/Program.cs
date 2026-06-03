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
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Identity;
using Zorvian.Infrastructure.Repositories;
using Zorvian.Infrastructure.Services;
using AutoMapper;
using Zorvian.Application.Mapping;
using Zorvian.Web.Hubs;
using Zorvian.Web.Jobs;
using Zorvian.Application.Jobs;
using Zorvian.Web.Middleware;
using Zorvian.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Firebase Admin SDK
var credPath = builder.Configuration["Firebase:CredentialsFilePath"] ?? "nexora-hr-firebase-admin.json";
var fbCredFile = Path.Combine(builder.Environment.ContentRootPath, credPath);
if (!File.Exists(fbCredFile))
    fbCredFile = Path.Combine("/etc/secrets", credPath);
if (File.Exists(fbCredFile))
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = CredentialFactory.FromFile<ServiceAccountCredential>(fbCredFile).ToGoogleCredential().CreateScoped(),
        ProjectId = builder.Configuration["Firebase:ProjectId"],
    });
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

// Database
builder.Services.AddDbContext<ZorvianDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ZorvianDb")));

// AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<MappingProfile>();
}, typeof(MappingProfile).Assembly);

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
builder.Services.AddScoped<IAchExportService, AchExportService>();
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
// DI - New Module: Multisucursal
builder.Services.AddScoped<IBranchRepository, BranchRepository>();
builder.Services.AddScoped<BranchService>();

// DI - New Module: Comercial
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<ClientService>();
builder.Services.AddScoped<IQuoteRepository, QuoteRepository>();
builder.Services.AddScoped<QuoteService>();
builder.Services.AddScoped<ISaleRepository, SaleRepository>();
builder.Services.AddScoped<SaleService>();

// DI - New Module: Inventario
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<IBrandRepository, BrandRepository>();
builder.Services.AddScoped<BrandService>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<SupplierService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<IInventoryMovementRepository, InventoryMovementRepository>();
builder.Services.AddScoped<InventoryMovementService>();

// DI - New Module: Créditos
builder.Services.AddScoped<ICreditRepository, CreditRepository>();
builder.Services.AddScoped<ICreditPaymentRepository, CreditPaymentRepository>();
builder.Services.AddScoped<ILateFeeRepository, LateFeeRepository>();
builder.Services.AddScoped<ICollectionActionRepository, CollectionActionRepository>();
builder.Services.AddScoped<CreditService>();

// DI - New Module: Caja
builder.Services.AddScoped<ICashRegisterRepository, CashRegisterRepository>();
builder.Services.AddScoped<ICashMovementRepository, CashMovementRepository>();
builder.Services.AddScoped<CashRegisterService>();

// DI - New Module: Garantías
builder.Services.AddScoped<IWarrantyRepository, WarrantyRepository>();
builder.Services.AddScoped<WarrantyService>();

builder.Services.AddScoped<IChatService>(sp => new ChatService(
    sp.GetRequiredService<ZorvianDbContext>(),
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
    options.AddPolicy("ZorvianCors", policy =>
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
        Title = "Zorvian ERP API",
        Version = "v1",
        Description = "API del sistema Zorvian ERP. Multi-tenant, con autenticación JWT + Firebase.",
        Contact = new OpenApiContact
        {
            Name = "Soporte Zorvian",
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

app.UseGlobalExceptionMiddleware();
app.UseCors("ZorvianCors");
app.UseRateLimitingMiddleware(maxRequests: 120, windowSeconds: 60);
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

// Recurring jobs using IRecurringJobManager from DI
var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();

recurringJobManager.AddOrUpdate<CheckInReminderJob>(
    "check-in-reminder",
    j => j.RunAsync(),
    "0 9 * * *"); // Every day at 09:00

recurringJobManager.AddOrUpdate<DocumentExpirationJob>(
    "document-expiration-check",
    j => j.RunAsync(),
    "0 8 * * *"); // Every day at 08:00

recurringJobManager.AddOrUpdate<AttendancePhotoCleanupJob>(
    "attendance-photo-cleanup",
    j => j.RunAsync(),
    "0 3 1 * *"); // 1st day of every month at 03:00

recurringJobManager.AddOrUpdate<AbsenteeismTrainingJob>(
    "absenteeism-model-training",
    j => j.RunAsync(),
    "0 2 * * 0"); // Every Sunday at 02:00

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", version = "1.0.0" }));

// Auto-migrate in production
if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ZorvianDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
