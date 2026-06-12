using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Tests.Integration;

public class RLSIsolationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RLSIsolationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RLS_ShouldIsolateDataByTenant()
    {
        var tenantA = Guid.NewGuid().ToString();
        var tenantB = Guid.NewGuid().ToString();

        // 1. Setup initial data
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ZorvianDbContext>();
            dbContext.Companies.Add(new Company { Id = Guid.NewGuid(), Name = "Company A", TenantId = tenantA });
            dbContext.Companies.Add(new Company { Id = Guid.NewGuid(), Name = "Company B", TenantId = tenantB });
            await dbContext.SaveChangesAsync();
        }

        // 2. Test Tenant A
        using (var scope = _factory.Services.CreateScope())
        {
            var tenantWriter = scope.ServiceProvider.GetRequiredService<ITenantContextWriter>();
            tenantWriter.SetTenantId(TenantId.FromString(tenantA));
            
            var dbContext = scope.ServiceProvider.GetRequiredService<ZorvianDbContext>();
            var companies = await dbContext.Companies.ToListAsync();

            Assert.Single(companies);
            Assert.Equal("Company A", companies.First().Name);
        }

        // 3. Test Tenant B
        using (var scope = _factory.Services.CreateScope())
        {
            var tenantWriter = scope.ServiceProvider.GetRequiredService<ITenantContextWriter>();
            tenantWriter.SetTenantId(TenantId.FromString(tenantB));
            
            var dbContext = scope.ServiceProvider.GetRequiredService<ZorvianDbContext>();
            var companies = await dbContext.Companies.ToListAsync();

            Assert.Single(companies);
            Assert.Equal("Company B", companies.First().Name);
        }
    }
}
