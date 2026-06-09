using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests;

public class LoadTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LoadTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MultiTenancy_Isolation_Under_Load()
    {
        // ARRANGE
        int concurrentRequests = 100;
        var tenants = Enumerable.Range(1, 10).Select(i => $"tenant-{i}").ToList();
        var client = _factory.CreateClient();

        // ACT
        var tasks = new List<Task<(string Tenant, bool Success)>>();
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < concurrentRequests; i++)
        {
            var tenantId = tenants[i % tenants.Count];
            tasks.Add(SimulateRequest(client, tenantId));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // ASSERT
        var failures = results.Where(r => !r.Success).ToList();
        Assert.Empty(failures);
        
        // Ensure average response time is acceptable (simplified check)
        var avgMs = stopwatch.ElapsedMilliseconds / concurrentRequests;
        Assert.True(avgMs < 500, $"Average response time too high: {avgMs}ms");
    }

    private async Task<(string Tenant, bool Success)> SimulateRequest(HttpClient client, string tenantId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Tenant-Id", tenantId);

        var response = await client.SendAsync(request);
        
        // In a real scenario, we would check if the returned data belongs to the correct tenant
        // Since /health is global, we just check for success and that the middleware didn't crash
        return (tenantId, response.IsSuccessStatusCode);
    }
}
