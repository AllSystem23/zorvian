using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Zorvian.Application.Services;
using Zorvian.Application.Interfaces;
using Zorvian.Web.Authorization;
using Zorvian.Web.Controllers;

namespace Zorvian.Tests.Integration;

public class CommissionEventsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<ICommissionService> _mockCommissionService;

    public CommissionEventsControllerTests(WebApplicationFactory<Program> factory)
    {
        _mockCommissionService = new Mock<ICommissionService>(MockBehavior.Loose);
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Testing:MockExternalServices", "true");
            builder.ConfigureTestServices(services =>
            {
                // Eliminar el servicio real si existe
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICommissionService));
                if (descriptor != null) services.Remove(descriptor);
                
                services.AddScoped(_ => _mockCommissionService.Object);
                
                // Añadir esquema de autenticación de pruebas
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
            });
        });
    }

    [Fact]
    public async Task HandleEvent_ShouldReturnOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test");
        
        var request = new CommissionEventRequest("test", "id", "sp", 100m, "USD", DateTime.UtcNow, new());
        _mockCommissionService.Setup(s => s.ProcessEventAsync(It.IsAny<CommissionEventRequest>())).Returns(Task.CompletedTask);

        // Act
        var response = await client.PostAsJsonAsync("/zorvian/v1/commissions/events", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _mockCommissionService.Verify(s => s.ProcessEventAsync(It.IsAny<CommissionEventRequest>()), Times.Once);
    }
}

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) 
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "Test user"),
            new Claim("permission", Permissions.CommissionWrite)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
