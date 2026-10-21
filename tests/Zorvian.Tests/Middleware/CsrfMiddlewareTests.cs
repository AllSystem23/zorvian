using Microsoft.AspNetCore.Http;
using Zorvian.Web.Middleware;

namespace Zorvian.Tests.Middleware;

/// <summary>
/// El middleware anti-CSRF exige un header (o Authorization) en los verbos no
/// seguros. Los endpoints de credenciales anónimos NO pueden enviarlo (el
/// usuario todavía no tiene token), así que deben estar exentos.
/// </summary>
public sealed class CsrfMiddlewareTests
{
    private static async Task<(int Status, bool NextCalled)> RunAsync(
        string method, string path, bool withAuthorization = false, bool withApiKey = false)
    {
        var nextCalled = false;
        var middleware = new CsrfMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        if (withAuthorization)
            context.Request.Headers["Authorization"] = "Bearer token";
        if (withApiKey)
            context.Request.Headers["X-API-Key"] = "zorvian_key_123";

        await middleware.InvokeAsync(context);

        return (context.Response.StatusCode, nextCalled);
    }

    [Theory]
    [InlineData("/zorvian/v1/auth/forgot-password")]
    [InlineData("/zorvian/v1/auth/reset-password")]
    [InlineData("/zorvian/v1/auth/register")]
    [InlineData("/zorvian/v1/auth/request-access")]
    public async Task AnonymousPost_ToPublicAuthEndpoints_IsAllowed(string path)
    {
        var (status, nextCalled) = await RunAsync("POST", path);

        Assert.True(nextCalled);
        Assert.Equal(200, status);
    }

    [Theory]
    [InlineData("/zorvian/v1/auth/login")]
    [InlineData("/zorvian/v1/auth/login-password")]
    [InlineData("/zorvian/v1/auth/mfa/login")]
    [InlineData("/zorvian/v1/auth/refresh")]
    public async Task AnonymousPost_ToExistingPublicAuthEndpoints_IsAllowed(string path)
    {
        var (status, nextCalled) = await RunAsync("POST", path);

        Assert.True(nextCalled);
        Assert.Equal(200, status);
    }

    [Fact]
    public async Task AnonymousPost_ToProtectedEndpoint_IsBlocked()
    {
        var (status, nextCalled) = await RunAsync("POST", "/zorvian/v1/sales");

        Assert.False(nextCalled);
        Assert.Equal(403, status);
    }

    [Fact]
    public async Task Post_WithAuthorizationHeader_IsAllowed()
    {
        var (status, nextCalled) = await RunAsync("POST", "/zorvian/v1/sales", withAuthorization: true);

        Assert.True(nextCalled);
        Assert.Equal(200, status);
    }

    /// <summary>
    /// Las API Keys viajan en `X-API-Key`, NO en `Authorization`. Sin esta
    /// excepcion, todo POST/PUT/DELETE hecho con una API Key devolvia 403.
    /// </summary>
    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task UnsafeVerb_WithApiKeyHeader_IsAllowed(string method)
    {
        var (status, nextCalled) = await RunAsync(method, "/zorvian/v1/sales", withApiKey: true);

        Assert.True(nextCalled);
        Assert.Equal(200, status);
    }

    [Fact]
    public async Task Get_IsAlwaysAllowed()
    {
        var (status, nextCalled) = await RunAsync("GET", "/zorvian/v1/sales");

        Assert.True(nextCalled);
        Assert.Equal(200, status);
    }
}
