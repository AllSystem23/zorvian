using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Zorvian.Web.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt {Path}", context.Request.Path);
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await WriteResponse(context, new { message = "No tiene permiso para realizar esta acción." });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found {Path}", context.Request.Path);
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            await WriteResponse(context, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation at {Path}: {Message}", context.Request.Path, ex.Message);
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await WriteResponse(context, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument at {Path}: {Message}", context.Request.Path, ex.Message);
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await WriteResponse(context, new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => char.ToLowerInvariant(g.Key[0]) + g.Key[1..],
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
            _logger.LogWarning(ex, "Validation failed at {Path}", context.Request.Path);
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await WriteResponse(context, new { message = "Validación fallida", details = errors });
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Request cancelled/timeout at {Path}", context.Request.Path);
            context.Response.StatusCode = (int)HttpStatusCode.GatewayTimeout;
            await WriteResponse(context, new { message = "La operación tardó demasiado. Intente de nuevo." });
        }
        catch (OperationCanceledException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.GatewayTimeout;
            await WriteResponse(context, new { message = "Operación cancelada." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception processing {Method} {Path}. TraceId: {TraceId}",
                context.Request.Method, context.Request.Path, context.TraceIdentifier);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await WriteResponse(context, new
            {
                message = "Error interno del servidor. Intente más tarde.",
                detail = $"{ex.GetType().Name}: {ex.Message}",
                traceId = context.TraceIdentifier
            });
        }
    }

    private static async Task WriteResponse(HttpContext context, object body)
    {
        context.Response.ContentType = "application/json";
        var json = JsonSerializer.Serialize(new
        {
            error = body,
            statusCode = context.Response.StatusCode,
            timestamp = DateTime.UtcNow,
        }, JsonOptions);
        await context.Response.WriteAsync(json);
    }
}

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
