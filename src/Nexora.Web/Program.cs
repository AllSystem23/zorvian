using System.Text;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
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
using Nexora.Web.Middleware;

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
            Credential = GoogleCredential.FromFile(fullPath),
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

// DI - Repositories
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();

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
    c.SwaggerDoc("v1", new() { Title = "Nexora API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
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
app.UseAuthentication();
app.UseAuthorization();
app.UseTenantMiddleware();
app.MapControllers();

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
