using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Infrastructure.Data;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ZorvianDbContext>
{
    public ZorvianDbContext CreateDbContext(string[] args)
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Zorvian.Web"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{env}.json", optional: true)
            .Build();

        var connectionString = config.GetConnectionString("ZorvianDb")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__ZorvianDb")
            ?? throw new InvalidOperationException(
                $"No connection string found. Set 'ConnectionStrings:ZorvianDb' in appsettings.json " +
                $"or the environment variable 'ConnectionStrings__ZorvianDb' (ASPNETCORE_ENVIRONMENT={env}).");

        var optionsBuilder = new DbContextOptionsBuilder<ZorvianDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ZorvianDbContext(optionsBuilder.Options, new DesignTimeTenantContext());
    }
}

public sealed class DesignTimeTenantContext : ITenantContext
{
    public TenantId TenantId { get; set; } = new(Guid.NewGuid());
    public bool IsSuperAdmin => false;
    public Guid? CurrentUserId => null;
    public Guid? CurrentEmployeeId => null;
}
