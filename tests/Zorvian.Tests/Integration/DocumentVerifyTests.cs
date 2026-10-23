using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Net;
using Xunit;
using Zorvian.Web;

namespace Zorvian.Tests.Integration;

public class DocumentVerifyTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DocumentVerifyTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing");
            b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Testing:MockExternalServices"] = "true",
                ["ConnectionStrings:ZorvianDb"] = "",
                ["Jwt:Secret"] = "SUPER_SECRET_KEY_FOR_TESTING_PURPOSES_ONLY_MIN_32_CHARS",
                ["Encryption:Key"] = "dGVzdC1lbmMta2V5LTMyLWJ5dGVzLTEyMzQ1Njc4OTA="
            }));
        });
    }

    [Fact]
    public async Task Verify_NonExistentId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/zorvian/v1/documents/verify/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
