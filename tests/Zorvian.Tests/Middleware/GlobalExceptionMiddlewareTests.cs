using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Zorvian.Web.Middleware;

namespace Zorvian.Tests.Middleware;

/// <summary>
/// El incidente de Neon (<c>53100: could not extend file because project size limit
/// (1024 MB) has been exceeded</c>) llegaba al cliente como <c>500 Error interno del
/// servidor</c>, sin pistas de que el problema era falta de espacio. Ahora se traduce a
/// <c>503</c> con un mensaje accionable y <c>Retry-After</c>.
/// </summary>
public sealed class GlobalExceptionMiddlewareTests
{
    private static PostgresException StorageFull() => new(
        "could not extend file because project size limit (1024 MB) has been exceeded",
        "ERROR",
        "ERROR",
        "53100",
        detail: "This limit is defined externally by the project size limit, and internally by neon.max_cluster_size GUC");

    private static async Task<(HttpContext Context, JsonElement Body)> InvokeAsync(Exception thrown)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/zorvian/v1/sales";
        context.Response.Body = new MemoryStream();

        var middleware = new GlobalExceptionMiddleware(
            _ => throw thrown,
            NullLogger<GlobalExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var raw = await reader.ReadToEndAsync();
        var json = JsonDocument.Parse(raw).RootElement.Clone();
        return (context, json);
    }

    [Fact]
    public async Task DatabaseStorageFull_Returns503WithActionableMessage()
    {
        var (context, body) = await InvokeAsync(
            new DbUpdateException("An error occurred while saving the entity changes.", StorageFull()));

        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, context.Response.StatusCode);
        Assert.Equal("120", context.Response.Headers["Retry-After"].ToString());

        var detail = body.GetProperty("detail").GetString();
        Assert.Contains("límite de almacenamiento", detail);
        Assert.DoesNotContain("Error interno del servidor", detail);

        // El ProblemDetails sigue exponiendo status/instance para el frontend.
        Assert.Equal(503, body.GetProperty("status").GetInt32());
        Assert.Equal("/zorvian/v1/sales", body.GetProperty("instance").GetString());
    }

    [Fact]
    public async Task DatabaseStorageFull_NotWrappedInDbUpdateException_AlsoReturns503()
    {
        var (context, body) = await InvokeAsync(StorageFull());

        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, context.Response.StatusCode);
        Assert.Contains("límite de almacenamiento", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task OtherDatabaseError_StillReturns500()
    {
        var duplicateKey = new PostgresException(
            "duplicate key value violates unique constraint", "ERROR", "ERROR", "23505");

        var (context, body) = await InvokeAsync(new DbUpdateException("save failed", duplicateKey));

        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        Assert.Equal("Error interno del servidor. Intente más tarde.", body.GetProperty("detail").GetString());
        Assert.Equal(string.Empty, context.Response.Headers["Retry-After"].ToString());
    }

    [Fact]
    public async Task GenericException_StillReturns500WithTraceId()
    {
        var (context, body) = await InvokeAsync(new Exception("boom"));

        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("traceId").GetString()));
    }
}
