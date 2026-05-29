using Nexora.Core.Entities;

namespace Nexora.Application.Interfaces;

public interface IAuthRepository
{
    Task<User?> GetUserByFirebaseUidAsync(string firebaseUid);
    Task<User?> GetUserWithRolesAsync(Guid userId);
    Task AddUserAsync(User user);
    Task AddRefreshTokenAsync(RefreshToken refreshToken);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task SaveChangesAsync();
}
