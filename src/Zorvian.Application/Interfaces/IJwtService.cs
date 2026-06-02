using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IJwtService
{
    (string accessToken, string refreshToken, int expiresIn) GenerateTokens(User user, Role role, string tenantId);
    string GenerateRefreshToken();
    bool ValidateRefreshToken(string token);
}
