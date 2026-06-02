namespace Zorvian.Application.DTOs.Common;

public sealed record PagedResult<T>(
    List<T> Items,
    int Total,
    int Page,
    int PageSize
);
