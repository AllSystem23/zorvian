using System.Reflection;
using System.Text;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
using Zorvian.Application.Services.PayrollStrategies;
using Zorvian.Application.Services.DepreciationCalculators;

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
builder.Services.AddDbContext<ZorvianDbContext>((sp, options) =>
{
    var interceptor = sp.GetRequiredService<Zorvian.Infrastructure.Data.AuditInterceptor>();
    options.UseNpgsql(builder.Configuration.GetConnectionString("ZorvianDb"))
           .AddInterceptors(interceptor)
           .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
});

// AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<MappingProfile>();
}, typeof(MappingProfile).Assembly);

// DI - Infrastructure
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
builder.Services.AddScoped<ITenantContextWriter>(sp => sp.GetRequiredService<TenantContext>());
builder.Services.AddScoped<Zorvian.Infrastructure.Data.AuditInterceptor>();
builder.Services.AddSingleton<IFirebaseAuthService, FirebaseAuthService>();
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();

// DI - Application
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IMfaService, Zorvian.Infrastructure.Services.MfaService>();
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

builder.Services.AddScoped<IPayrollCalculationStrategy, NicaraguaCalculationStrategy>();
builder.Services.AddScoped<PayrollCalculationFactory>();
builder.Services.AddScoped<EmployeeLoanService>();
builder.Services.AddScoped<SalaryAdvanceService>();
builder.Services.AddScoped<WageGarnishmentService>();
builder.Services.AddScoped<TerminationService>();

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
builder.Services.AddScoped<IPayrollConceptRepository, PayrollConceptRepository>();
builder.Services.AddScoped<IBankAccountRepository, BankAccountRepository>();
builder.Services.AddScoped<ISickLeaveRepository, SickLeaveRepository>();
builder.Services.AddScoped<ITerminationRepository, TerminationRepository>();
builder.Services.AddScoped<BankAccountService>();
builder.Services.AddScoped<SickLeaveService>();
builder.Services.AddScoped<TerminationService>();
builder.Services.AddScoped<IReconciliationService, ReconciliationService>();
builder.Services.AddScoped<IEmployeeLoanRepository, EmployeeLoanRepository>();
builder.Services.AddScoped<ISalaryAdvanceRepository, SalaryAdvanceRepository>();
builder.Services.AddScoped<IWageGarnishmentRepository, WageGarnishmentRepository>();
builder.Services.AddScoped<IBenefitProvisionRepository, BenefitProvisionRepository>();
builder.Services.AddScoped<ICommissionRecordRepository, CommissionRecordRepository>();
builder.Services.AddScoped<IBonusRecordRepository, BonusRecordRepository>();
builder.Services.AddScoped<ICountryTaxConfigRepository, CountryTaxConfigRepository>();
builder.Services.AddScoped<PayrollService>(sp => new PayrollService(
    sp.GetRequiredService<IPayrollRepository>(),
    sp.GetRequiredService<IEmployeeRepository>(),
    sp.GetRequiredService<ITenantContext>(),
    sp.GetRequiredService<IWebhookService>(),
    sp.GetRequiredService<IAchExportService>(),
    sp.GetRequiredService<AutoAccountingService>(),
    sp.GetRequiredService<ICountryTaxConfigRepository>(),
    sp.GetRequiredService<ICompanyRepository>(),
    sp.GetRequiredService<IOvertimeRecordRepository>(),
    sp.GetRequiredService<ICommissionRecordRepository>(),
    sp.GetRequiredService<IBonusRecordRepository>(),
    sp.GetRequiredService<IPayrollConceptRepository>(),
    sp.GetRequiredService<IEmployeeLoanRepository>(),
    sp.GetRequiredService<ISalaryAdvanceRepository>(),
    sp.GetRequiredService<IWageGarnishmentRepository>(),
    sp.GetRequiredService<IBenefitProvisionRepository>(),
    sp.GetRequiredService<IAuditLogRepository>(),
    sp.GetRequiredService<PayrollCalculationFactory>(),
    sp.GetRequiredService<IBankTransferService>(),
    sp.GetRequiredService<ISickLeaveRepository>()));
builder.Services.AddScoped<PayrollConceptService>();
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
    builder.Services.AddScoped<IPurchaseRepository, PurchaseRepository>();
    builder.Services.AddScoped<PurchaseService>();
    builder.Services.AddScoped<ISupplierPaymentRepository, SupplierPaymentRepository>();
    builder.Services.AddScoped<ISupplierCreditNoteRepository, SupplierCreditNoteRepository>();
    builder.Services.AddScoped<IWithholdingRepository, WithholdingRepository>();
    builder.Services.AddScoped<SupplierPaymentService>();
    builder.Services.AddScoped<SupplierCreditNoteService>();

// DI - New Module: Business Intelligence
builder.Services.AddScoped<BiService>();

// DI - New Module: Activos Fijos
builder.Services.AddScoped<IFixedAssetRepository, FixedAssetRepository>();
builder.Services.AddScoped<IFixedAssetCategoryRepository, FixedAssetCategoryRepository>();
builder.Services.AddScoped<IDepreciationEntryRepository, DepreciationEntryRepository>();
builder.Services.AddScoped<IAssetRevaluationRepository, AssetRevaluationRepository>();
builder.Services.AddScoped<IAssetDisposalRepository, AssetDisposalRepository>();
builder.Services.AddScoped<IAssetMaintenanceRepository, AssetMaintenanceRepository>();
builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddScoped<FixedAssetService>();
builder.Services.AddSingleton<DepreciationCalculatorFactory>();

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

// DI - New Module: Contabilidad
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IAccountingEntryRepository, AccountingEntryRepository>();
builder.Services.AddScoped<IAccountingPeriodRepository, AccountingPeriodRepository>();
builder.Services.AddScoped<IAccountLinkRepository, AccountLinkRepository>();
builder.Services.AddScoped<IAccountingRuleRepository, AccountingRuleRepository>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<AccountingEntryService>();
builder.Services.AddScoped<AccountingPeriodService>();
builder.Services.AddScoped<AccountLinkService>();
builder.Services.AddScoped<AutoAccountingService>(sp => new AutoAccountingService(
    sp.GetRequiredService<IAccountingEntryRepository>(),
    sp.GetRequiredService<IAccountingPeriodRepository>(),
    sp.GetRequiredService<IAccountLinkRepository>(),
    sp.GetRequiredService<IAccountingRuleRepository>(),
    sp.GetRequiredService<IAccountRepository>(),
    sp.GetRequiredService<ITenantContext>(),
    sp.GetRequiredService<IPayrollRepository>(),
    sp.GetRequiredService<ICashMovementRepository>()));
builder.Services.AddScoped<IAutoAccountingService>(sp => sp.GetRequiredService<AutoAccountingService>());
builder.Services.AddScoped<FinancialReportService>();

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
builder.Services.AddHangfire((sp, config) =>
{
    var connectionString = builder.Configuration.GetConnectionString("ZorvianDb");
    config.UsePostgreSqlStorage(connectionString);
});
builder.Services.AddHangfireServer();
builder.Services.AddScoped<CheckInReminderJob>();
builder.Services.AddScoped<DocumentExpirationJob>();
builder.Services.AddScoped<AttendancePhotoCleanupJob>();
builder.Services.AddScoped<AbsenteeismTrainingJob>();
builder.Services.AddScoped<AuditLogCleanupJob>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("ZorvianCors", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        policy.SetIsOriginAllowed(origin =>
            {
                if (origin.StartsWith("http://localhost:") || origin.StartsWith("https://localhost:") ||
                    origin.StartsWith("http://127.0.0.1:") || origin.StartsWith("https://127.0.0.1:"))
                    return true;
                return allowedOrigins.Contains(origin);
            })
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

// THE SOLUTION: CORS MUST BE FIRST
app.UseCors("ZorvianCors");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSecurityHeaders();
app.UseGlobalExceptionMiddleware();
app.UseRateLimitingMiddleware(maxRequests: 120, windowSeconds: 60);
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthentication();
app.UseTenantMiddleware();
app.UseAuthorization();
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

recurringJobManager.AddOrUpdate<AuditLogCleanupJob>(
    "audit-log-cleanup",
    j => j.RunAsync(),
    "0 3 1 * *"); // 1st day of every month at 03:00

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", version = "1.0.0" }));

// Auto-migrate in production with guardrails
if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ZorvianDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var pending = await db.Database.GetPendingMigrationsAsync();
        var pendingList = pending.ToList();

        if (pendingList.Count > 0)
        {
            logger.LogInformation("Applying {Count} pending migration(s): {Migrations}",
                pendingList.Count, string.Join(", ", pendingList));
            await db.Database.MigrateAsync();
            logger.LogInformation("Migration(s) applied successfully");
        }
        else
        {
            logger.LogInformation("No pending migrations");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Migration failed — application will start but database may be out of date");
    }
}

app.Run();
