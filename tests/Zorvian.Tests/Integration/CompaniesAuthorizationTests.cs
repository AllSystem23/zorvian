using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Zorvian.Web;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Testing:MockExternalServices"] = "true",
                ["ConnectionStrings:ZorvianDb"] = "",
                ["Jwt:Secret"] = "SUPER_SECRET_KEY_FOR_TESTING_PURPOSES_ONLY_MIN_32_CHARS"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Opcional: overrides adicionales aquí
        });
    }
}

public class CompaniesAuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CompaniesAuthorizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCurrent_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/zorvian/v1/companies/current");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.Unauthorized,
            $"Expected Unauthorized (401) but got {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
    }
}
