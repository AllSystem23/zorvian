using Nexora.Application.DTOs.Auth;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;

namespace Nexora.Application.Services;

public sealed class AuthService
{
    private readonly IAuthRepository _authRepo;
    private readonly IFirebaseAuthService _firebase;
    private readonly IJwtService _jwt;

    public AuthService(IAuthRepository authRepo, IFirebaseAuthService firebase, IJwtService jwt)
    {
        _authRepo = authRepo;
        _firebase = firebase;
        _jwt = jwt;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var fbUser = await _firebase.VerifyIdTokenAsync(request.IdToken);
        if (fbUser is null) return null;

        var user = await _authRepo.GetUserByFirebaseUidAsync(fbUser.Uid);

        if (user is null)
        {
            user = new User
            {
                FirebaseUid = fbUser.Uid,
                Email = fbUser.Email,
                DisplayName = fbUser.Name,
                AvatarUrl = fbUser.Picture,
            };
            await _authRepo.AddUserAsync(user);
            await _authRepo.SaveChangesAsync();

            user = await _authRepo.GetUserByFirebaseUidAsync(fbUser.Uid);
            if (user is null) return null;
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _authRepo.SaveChangesAsync();

        var primaryRole = user.UserRoles.FirstOrDefault()?.Role
            ?? new Role { Name = Core.Enums.RoleType.Employee, DisplayName = "Empleado" };

        var tenantId = user.TenantId;
        var (accessToken, refreshToken, expiresIn) = _jwt.GenerateTokens(user, primaryRole, tenantId);

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };
        await _authRepo.AddRefreshTokenAsync(refreshTokenEntity);
        await _authRepo.SaveChangesAsync();

        return new AuthResponse(
            accessToken,
            refreshToken,
            expiresIn,
            new UserInfo(
                user.Id.ToString(),
                user.Email,
                user.DisplayName,
                primaryRole.Name.ToString(),
                tenantId,
                user.EmployeeId?.ToString()
            )
        );
    }

    public async Task<AuthResponse?> LoginWithPasswordAsync(LoginPasswordRequest request)
    {
        var fbUser = await _firebase.SignInWithPasswordAsync(request.Email, request.Password);
        if (fbUser is null) return null;

        var user = await _authRepo.GetUserByFirebaseUidAsync(fbUser.Uid);

        if (user is null)
        {
            user = new User
            {
                FirebaseUid = fbUser.Uid,
                Email = fbUser.Email,
                DisplayName = fbUser.Name,
                AvatarUrl = fbUser.Picture,
            };
            await _authRepo.AddUserAsync(user);
            await _authRepo.SaveChangesAsync();

            user = await _authRepo.GetUserByFirebaseUidAsync(fbUser.Uid);
            if (user is null) return null;
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _authRepo.SaveChangesAsync();

        var primaryRole = user.UserRoles.FirstOrDefault()?.Role
            ?? new Role { Name = Core.Enums.RoleType.Employee, DisplayName = "Empleado" };

        var tenantId = user.TenantId;
        var (accessToken, refreshToken, expiresIn) = _jwt.GenerateTokens(user, primaryRole, tenantId);

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };
        await _authRepo.AddRefreshTokenAsync(refreshTokenEntity);
        await _authRepo.SaveChangesAsync();

        return new AuthResponse(
            accessToken,
            refreshToken,
            expiresIn,
            new UserInfo(
                user.Id.ToString(),
                user.Email,
                user.DisplayName,
                primaryRole.Name.ToString(),
                tenantId,
                user.EmployeeId?.ToString()
            )
        );
    }

    public async Task<AuthResponse?> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var storedToken = await _authRepo.GetRefreshTokenAsync(request.RefreshToken);

        if (storedToken is null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
            return null;

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;

        var user = storedToken.User;
        var primaryRole = user.UserRoles.FirstOrDefault()?.Role
            ?? new Role { Name = Core.Enums.RoleType.Employee, DisplayName = "Empleado" };

        var tenantId = user.TenantId;
        var (accessToken, newRefreshToken, expiresIn) = _jwt.GenerateTokens(user, primaryRole, tenantId);

        storedToken.ReplacedByToken = newRefreshToken;

        var newRefreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };
        await _authRepo.AddRefreshTokenAsync(newRefreshTokenEntity);
        await _authRepo.SaveChangesAsync();

        return new AuthResponse(
            accessToken,
            newRefreshToken,
            expiresIn,
            new UserInfo(
                user.Id.ToString(),
                user.Email,
                user.DisplayName,
                primaryRole.Name.ToString(),
                tenantId,
                user.EmployeeId?.ToString()
            )
        );
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        var storedToken = await _authRepo.GetRefreshTokenAsync(refreshToken);
        if (storedToken is null) return false;

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        await _authRepo.SaveChangesAsync();
        return true;
    }
}
