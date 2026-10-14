using Microsoft.AspNetCore.Http;
using Zorvian.Web.Middleware;

namespace Zorvian.Tests.Middleware;

public sealed class RateLimitingMiddlewareTests
{
    private static RateLimitingMiddleware CreateMiddleware(int maxRequests = 5, int windowSeconds = 60, int authLimit = 5, int authWindowSeconds = 900, int authOtherLimit = 30, int authOtherWindowSeconds = 300)
    {
        return new RateLimitingMiddleware(async ctx =>
        {
            await Task.CompletedTask;
        }, maxRequests, windowSeconds, authLimit, authWindowSeconds, authOtherLimit, authOtherWindowSeconds);
    }

    private static (RateLimitingMiddleware, DefaultHttpContext) CreatePair(int maxRequests = 5, int windowSeconds = 60)
    {
        return (CreateMiddleware(maxRequests, windowSeconds), NewContext("192.168.1.1", "/"));
    }

    private static DefaultHttpContext NewContext(string ip, string path)
    {
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ip);
        ctx.Request.Path = path;
        return ctx;
    }

    [Fact]
    public async Task HealthEndpoint_AlwaysPasses()
    {
        var (middleware, ctx) = CreatePair(1, 60);
        ctx.Request.Path = "/health";

        await middleware.InvokeAsync(ctx);

        Assert.NotEqual(429, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task UnderLimit_ReturnsSuccess()
    {
        var (middleware, ctx) = CreatePair(5, 60);
        ctx.Request.Path = "/api/v1/test";

        for (var i = 0; i < 5; i++)
        {
            var freshCtx = new DefaultHttpContext();
            freshCtx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");
            freshCtx.Request.Path = "/api/v1/test";
            await middleware.InvokeAsync(freshCtx);
            Assert.NotEqual(429, freshCtx.Response.StatusCode);
        }
    }

    [Fact]
    public async Task OverLimit_Returns429()
    {
        var (middleware, ctx) = CreatePair(2, 60);
        ctx.Request.Path = "/api/v1/test";

        for (var i = 0; i < 2; i++)
        {
            var passCtx = new DefaultHttpContext();
            passCtx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.2");
            passCtx.Request.Path = "/api/v1/test";
            await middleware.InvokeAsync(passCtx);
        }

        var failCtx = new DefaultHttpContext();
        failCtx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.2");
        failCtx.Request.Path = "/api/v1/test";
        await middleware.InvokeAsync(failCtx);

        Assert.Equal(429, failCtx.Response.StatusCode);
    }

    [Fact]
    public async Task DifferentClients_HaveIndependentCounters()
    {
        var (middleware, _) = CreatePair(1, 60);

        var client1 = new DefaultHttpContext();
        client1.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.1");
        client1.Request.Path = "/api/v1/test";

        var client2 = new DefaultHttpContext();
        client2.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.2");
        client2.Request.Path = "/api/v1/test";

        await middleware.InvokeAsync(client1);
        Assert.NotEqual(429, client1.Response.StatusCode);

        await middleware.InvokeAsync(client2);
        Assert.NotEqual(429, client2.Response.StatusCode);

        await middleware.InvokeAsync(client1);
        Assert.Equal(429, client1.Response.StatusCode);
    }

    [Fact]
    public async Task AuthEndpoint_HasStricterLimit()
    {
        var (middleware, _) = CreatePair(100, 60);

        for (var i = 0; i < 5; i++)
        {
            var ctx = new DefaultHttpContext();
            ctx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.99");
            ctx.Request.Path = "/api/v1/auth/login";
            await middleware.InvokeAsync(ctx);
        }

        var blockedCtx = new DefaultHttpContext();
        blockedCtx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.99");
        blockedCtx.Request.Path = "/api/v1/auth/login";
        await middleware.InvokeAsync(blockedCtx);

        Assert.Equal(429, blockedCtx.Response.StatusCode);
    }

    [Fact]
    public async Task FixedWindow_ResetsAfterWindowElapses()
    {
        var shortWindow = 1;
        var (middleware, _) = CreatePair(1, shortWindow);

        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.50");
        ctx.Request.Path = "/api/v1/test";
        await middleware.InvokeAsync(ctx);
        Assert.NotEqual(429, ctx.Response.StatusCode);

        var blockedCtx = new DefaultHttpContext();
        blockedCtx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.50");
        blockedCtx.Request.Path = "/api/v1/test";
        await middleware.InvokeAsync(blockedCtx);
        Assert.Equal(429, blockedCtx.Response.StatusCode);

        await Task.Delay(1100);

        var resetCtx = new DefaultHttpContext();
        resetCtx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.50");
        resetCtx.Request.Path = "/api/v1/test";
        await middleware.InvokeAsync(resetCtx);
        Assert.NotEqual(429, resetCtx.Response.StatusCode);
    }

    [Fact]
    public async Task ReturnsRetryAfterHeader()
    {
        var (middleware, _) = CreatePair(1, 30);

        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.77");
        ctx.Request.Path = "/api/v1/test";
        await middleware.InvokeAsync(ctx);

        var blockedCtx = new DefaultHttpContext();
        blockedCtx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.77");
        blockedCtx.Request.Path = "/api/v1/test";
        await middleware.InvokeAsync(blockedCtx);

        Assert.Equal(429, blockedCtx.Response.StatusCode);
        Assert.True(blockedCtx.Response.Headers.TryGetValue("Retry-After", out _));
    }

    [Fact]
    public async Task CredentialEndpoints_HaveIndependentBudgets()
    {
        // Exhaust /auth/login; /auth/refresh must still work (users behind a NAT
        // share an IP, so a single global auth bucket would lock everyone out).
        var middleware = CreateMiddleware(maxRequests: 100, authLimit: 5);

        const string ip = "10.1.0.5";
        for (var i = 0; i < 5; i++)
        {
            var ctx = NewContext(ip, "/zorvian/v1/auth/login");
            await middleware.InvokeAsync(ctx);
            Assert.NotEqual(429, ctx.Response.StatusCode);
        }

        var loginBlocked = NewContext(ip, "/zorvian/v1/auth/login");
        await middleware.InvokeAsync(loginBlocked);
        Assert.Equal(429, loginBlocked.Response.StatusCode);

        var refreshOk = NewContext(ip, "/zorvian/v1/auth/refresh");
        await middleware.InvokeAsync(refreshOk);
        Assert.NotEqual(429, refreshOk.Response.StatusCode);
    }

    [Fact]
    public async Task OtherAuthEndpoints_UseLooserSharedBudget()
    {
        var middleware = CreateMiddleware(maxRequests: 100, authOtherLimit: 3);

        const string ip = "10.1.0.6";
        for (var i = 0; i < 3; i++)
        {
            var ctx = NewContext(ip, "/zorvian/v1/auth/me");
            await middleware.InvokeAsync(ctx);
            Assert.NotEqual(429, ctx.Response.StatusCode);
        }

        var blocked = NewContext(ip, "/zorvian/v1/auth/me");
        await middleware.InvokeAsync(blocked);
        Assert.Equal(429, blocked.Response.StatusCode);

        var logoutBlocked = NewContext(ip, "/zorvian/v1/auth/logout");
        await middleware.InvokeAsync(logoutBlocked);
        Assert.Equal(429, logoutBlocked.Response.StatusCode);
    }

    [Fact]
    public async Task MfaVerify_IsTreatedAsCredentialEndpoint()
    {
        var middleware = CreateMiddleware(maxRequests: 100, authLimit: 2);

        const string ip = "10.1.0.7";
        for (var i = 0; i < 2; i++)
        {
            var ctx = NewContext(ip, "/zorvian/v1/auth/mfa/verify");
            await middleware.InvokeAsync(ctx);
            Assert.NotEqual(429, ctx.Response.StatusCode);
        }

        var blocked = NewContext(ip, "/zorvian/v1/auth/mfa/verify");
        await middleware.InvokeAsync(blocked);
        Assert.Equal(429, blocked.Response.StatusCode);
    }
}
