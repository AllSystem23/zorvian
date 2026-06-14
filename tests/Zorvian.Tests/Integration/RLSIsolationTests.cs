using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Tests.Integration;

/// <summary>
/// RLS isolation tests using InMemory database.
/// 
/// NOTE: EF Core InMemory store does NOT share data across scopes created
/// from WebApplicationFactory.Services.CreateScope(). Therefore all operations
/// (insert + query with different tenant contexts) must occur within a single
/// scope. This still validates the HasQueryFilter tenant isolation logic.
/// 
/// In production, PostgreSQL RLS policies provide cross-request isolation via
/// TenantSessionInterceptor which sets session-level tenant context variables.
/// </summary>
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

        using (var scope = _factory.Services.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var db = sp.GetRequiredService<ZorvianDbContext>();
            var tenantWriter = sp.GetRequiredService<ITenantContextWriter>();

            // Insert two companies with different tenant IDs
            db.Companies.Add(new Company { Id = Guid.NewGuid(), Name = "Company A", TenantId = tenantA });
            db.Companies.Add(new Company { Id = Guid.NewGuid(), Name = "Company B", TenantId = tenantB });
            await db.SaveChangesAsync();

            // Clear change tracker to force queries from store
            db.ChangeTracker.Clear();

            // Act — switch to tenant A
            tenantWriter.SetTenantId(TenantId.FromString(tenantA));
            var companiesA = await db.Companies.ToListAsync();

            // Assert — only Company A visible
            Assert.Single(companiesA);
            Assert.Equal("Company A", companiesA.First().Name);

            // Clear again for next query
            db.ChangeTracker.Clear();

            // Act — switch to tenant B
            tenantWriter.SetTenantId(TenantId.FromString(tenantB));
            var companiesB = await db.Companies.ToListAsync();

            // Assert — only Company B visible
            Assert.Single(companiesB);
            Assert.Equal("Company B", companiesB.First().Name);
        }
    }

    [Fact]
    public async Task RLS_IsSuperAdmin_SeesAllCompanies()
    {
        var tenantA = Guid.NewGuid().ToString();
        var tenantB = Guid.NewGuid().ToString();

        using (var scope = _factory.Services.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var db = sp.GetRequiredService<ZorvianDbContext>();
            var tenantWriter = sp.GetRequiredService<ITenantContextWriter>();

            db.Companies.Add(new Company { Id = Guid.NewGuid(), Name = "Company A", TenantId = tenantA });
            db.Companies.Add(new Company { Id = Guid.NewGuid(), Name = "Company B", TenantId = tenantB });
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();

            // Act — set IsSuperAdmin = true
            tenantWriter.SetIsSuperAdmin(true);
            var all = await db.Companies.ToListAsync();

            // Assert — super admin sees both companies
            Assert.Equal(2, all.Count);
        }
    }
}
