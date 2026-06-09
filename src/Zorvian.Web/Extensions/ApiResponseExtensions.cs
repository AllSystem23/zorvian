using Microsoft.AspNetCore.Mvc;

namespace Zorvian.Web.Extensions;

/// <summary>
/// Standard API response wrapper for consistent response format.
/// </summary>
public static class ApiResponseExtensions
{
    public static IActionResult Success(this ControllerBase controller, object? data = null, string? message = null, int statusCode = 200)
    {
        var response = new ApiResponse
        {
            Success = true,
            Data = data,
            Message = message,
            StatusCode = statusCode,
            Timestamp = DateTime.UtcNow,
        };

        return new ObjectResult(response) { StatusCode = statusCode };
    }

    public static IActionResult Created(this ControllerBase controller, object data, string? message = null)
    {
        return controller.Success(data, message, StatusCodes.Status201Created);
    }

    public static IActionResult Error(this ControllerBase controller, string message, int statusCode = 400, object? details = null)
    {
        var response = new ApiResponse
        {
            Success = false,
            Message = message,
            StatusCode = statusCode,
            Timestamp = DateTime.UtcNow,
            Details = details,
        };

        return new ObjectResult(response) { StatusCode = statusCode };
    }

    public static IActionResult NotFound(this ControllerBase controller, string message = "Recurso no encontrado")
    {
        return controller.Error(message, StatusCodes.Status404NotFound);
    }

    public static IActionResult Unauthorized(this ControllerBase controller, string message = "No autorizado")
    {
        return controller.Error(message, StatusCodes.Status401Unauthorized);
    }

    public static IActionResult Forbidden(this ControllerBase controller, string message = "No tiene permiso")
    {
        return controller.Error(message, StatusCodes.Status403Forbidden);
    }
}

public class ApiResponse
{
    public bool Success { get; set; }
    public object? Data { get; set; }
    public string? Message { get; set; }
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; }
    public object? Details { get; set; }
}

/// <summary>
/// Paginated API response wrapper.
/// </summary>
public class PagedApiResponse<T>
{
    public bool Success { get; set; }
    public List<T>? Data { get; set; }
    public string? Message { get; set; }
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; }
    public PaginationMeta? Pagination { get; set; }
}

public class PaginationMeta
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;
}