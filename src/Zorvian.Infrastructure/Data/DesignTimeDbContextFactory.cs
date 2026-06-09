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
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Zorvian.Web"))
            .AddJsonFile("appsettings.json")
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ZorvianDbContext>();
        optionsBuilder.UseNpgsql(config.GetConnectionString("ZorvianDb"));

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
