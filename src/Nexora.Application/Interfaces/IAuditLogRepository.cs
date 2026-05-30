using Nexora.Core.Entities;

namespace Nexora.Application.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log);
    Task<List<AuditLog>> GetFilteredAsync(string? entityName, string? action, DateTime? from, DateTime? to, int page, int pageSize);
    Task<int> GetFilteredCountAsync(string? entityName, string? action, DateTime? from, DateTime? to);
}
