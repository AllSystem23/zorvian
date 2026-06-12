using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IAuthRepository
{
    Task<User?> GetUserByFirebaseUidAsync(string firebaseUid);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserWithRolesAsync(Guid userId);
    Task AddUserAsync(User user);
    Task AddRefreshTokenAsync(RefreshToken refreshToken);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task<List<RefreshToken>> GetActiveRefreshTokensAsync(Guid userId);
    Task RevokeAllUserTokensAsync(Guid userId, string? excludeToken = null);

    Task<List<UserTenant>> GetUserTenantsAsync(Guid userId);
    Task AddUserTenantAsync(UserTenant userTenant);
    Task<bool> UserHasTenantAccessAsync(Guid userId, string tenantId);
    Task<List<Company>> GetCompaniesByTenantIdsAsync(List<string> tenantIds);

    Task SaveChangesAsync();
}
