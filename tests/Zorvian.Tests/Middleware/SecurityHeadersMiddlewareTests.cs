using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Zorvian.Web.Middleware;

namespace Zorvian.Tests.Middleware;

public sealed class SecurityHeadersMiddlewareTests
{
    private static async Task<HttpContext> InvokeMiddleware()
    {
        var httpContext = new DefaultHttpContext();
        var wasCalled = false;

        var logger = new Mock<ILogger<SecurityHeadersMiddleware>>();
        var middleware = new SecurityHeadersMiddleware(async ctx =>
        {
            wasCalled = true;
            await Task.CompletedTask;
        }, logger.Object);

        await middleware.InvokeAsync(httpContext);

        Assert.True(wasCalled, "Next delegate was not called");
        return httpContext;
    }

    [Fact]
    public async Task Sets_XContentTypeOptions_NoSniff()
    {
        var ctx = await InvokeMiddleware();
        Assert.Equal("nosniff", ctx.Response.Headers["X-Content-Type-Options"]);
    }

    [Fact]
    public async Task Sets_XFrameOptions_Deny()
    {
        var ctx = await InvokeMiddleware();
        Assert.Equal("DENY", ctx.Response.Headers["X-Frame-Options"]);
    }

    [Fact]
    public async Task Sets_ReferrerPolicy_StrictOrigin()
    {
        var ctx = await InvokeMiddleware();
        Assert.Equal("strict-origin-when-cross-origin", ctx.Response.Headers["Referrer-Policy"]);
    }

    [Fact]
    public async Task Sets_XPermittedCrossDomainPolicies_None()
    {
        var ctx = await InvokeMiddleware();
        Assert.Equal("none", ctx.Response.Headers["X-Permitted-Cross-Domain-Policies"]);
    }

    [Fact]
    public async Task Sets_CrossOriginResourcePolicy_SameOrigin()
    {
        var ctx = await InvokeMiddleware();
        Assert.Equal("same-origin", ctx.Response.Headers["Cross-Origin-Resource-Policy"]);
    }

    [Fact]
    public async Task AllFiveHeadersAreSet()
    {
        var ctx = await InvokeMiddleware();
        Assert.True(ctx.Response.Headers.Count >= 5);
    }

    [Fact]
    public async Task HeadersAreSetBeforeNextDelegate()
    {
        var httpContext = new DefaultHttpContext();
        string? headerValue = null;

        var middleware = new SecurityHeadersMiddleware(ctx =>
        {
            headerValue = ctx.Response.Headers["X-Frame-Options"];
            return Task.CompletedTask;
        }, new Mock<ILogger<SecurityHeadersMiddleware>>().Object);

        await middleware.InvokeAsync(httpContext);

        Assert.Equal("DENY", headerValue);
    }

    [Fact]
    public async Task ResponsesAreNotCooked()
    {
        var ctx = await InvokeMiddleware();
        Assert.NotNull(ctx.Response);
        Assert.False(ctx.Response.HasStarted);
    }
}
