using Zorvian.Application.DTOs.Auth;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class AuthService
{
    private readonly IAuthRepository _authRepo;
    private readonly IFirebaseAuthService _firebase;
    private readonly IJwtService _jwt;
    private readonly IMfaService _mfa;
    private readonly ITenantContext _tenant;

    public AuthService(IAuthRepository authRepo, IFirebaseAuthService firebase, IJwtService jwt, IMfaService mfa, ITenantContext tenant)
    {
        _authRepo = authRepo;
        _firebase = firebase;
        _jwt = jwt;
        _mfa = mfa;
        _tenant = tenant;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var fbUser = await _firebase.VerifyIdTokenAsync(request.IdToken);
        if (fbUser is null) return null;

        var user = await _authRepo.GetUserByFirebaseUidAsync(fbUser.Uid);

        if (user is null)
        {
            user = await _authRepo.GetUserByEmailAsync(fbUser.Email ?? "");
            if (user is not null)
            {
                user.FirebaseUid = fbUser.Uid;
                if (!string.IsNullOrEmpty(fbUser.Name)) user.DisplayName = fbUser.Name;
                if (!string.IsNullOrEmpty(fbUser.Picture)) user.AvatarUrl = fbUser.Picture;
                await _authRepo.SaveChangesAsync();
            }
            else
            {
                user = new User
                {
                    FirebaseUid = fbUser.Uid,
                    Email = fbUser.Email,
                    DisplayName = fbUser.Name,
                    AvatarUrl = fbUser.Picture,
                    TenantId = _tenant.TenantId,
                };
                await _authRepo.AddUserAsync(user);
                await _authRepo.SaveChangesAsync();

                user = await _authRepo.GetUserByFirebaseUidAsync(fbUser.Uid);
                if (user is null) return null;
            }
        }

        return await GenerateAuthResponse(user, request.DeviceFingerprint);
    }

    public async Task<AuthResponse?> LoginWithPasswordAsync(LoginPasswordRequest request)
    {
        var result = await LoginWithPasswordStep1Async(request);
        return result?.AuthResponse;
    }

    public async Task<LoginStep1Result?> LoginWithPasswordStep1Async(LoginPasswordRequest request)
    {
        var fbUser = await _firebase.SignInWithPasswordAsync(request.Email, request.Password);

        User? user = null;

        if (fbUser is not null)
        {
            user = await _authRepo.GetUserByFirebaseUidAsync(fbUser.Uid);

            if (user is null)
            {
                user = await _authRepo.GetUserByEmailAsync(fbUser.Email ?? "");
                if (user is not null)
                {
                    user.FirebaseUid = fbUser.Uid;
                    if (!string.IsNullOrEmpty(fbUser.Name)) user.DisplayName = fbUser.Name;
                    if (!string.IsNullOrEmpty(fbUser.Picture)) user.AvatarUrl = fbUser.Picture;
                    await _authRepo.SaveChangesAsync();
                }
                else
                {
                    user = new User
                    {
                        FirebaseUid = fbUser.Uid,
                        Email = fbUser.Email,
                        DisplayName = fbUser.Name,
                        AvatarUrl = fbUser.Picture,
                        TenantId = _tenant.TenantId,
                    };
                    await _authRepo.AddUserAsync(user);
                    await _authRepo.SaveChangesAsync();

                    user = await _authRepo.GetUserByFirebaseUidAsync(fbUser.Uid);
                    if (user is null) return null;
                }
            }
        }
        else
        {
            user = await _authRepo.GetUserByEmailAsync(request.Email);
            if (user is null || user.PasswordHash is null) return null;

            if (!PasswordHelper.Verify(request.Password, user.PasswordHash)) return null;
        }

        if (user.IsMfaEnabled)
        {
            var mfaToken = _mfa.GenerateMfaToken(user.Id);
            return LoginStep1Result.MfaRequired(new MfaRequiredResponse(mfaToken));
        }

        var authResponse = await GenerateAuthResponse(user, request.DeviceFingerprint);
        return LoginStep1Result.Completed(authResponse);
    }

    public async Task<AuthResponse?> CompleteMfaLoginAsync(string mfaToken, string code, string? deviceFingerprint = null)
    {
        var userId = _mfa.ValidateMfaToken(mfaToken);
        if (userId is null) return null;

        var user = await _authRepo.GetUserWithRolesAsync(userId.Value);
        if (user is null || !user.IsMfaEnabled || string.IsNullOrEmpty(user.MfaSecretKey))
            return null;

        if (!_mfa.ValidateCode(user.MfaSecretKey, code))
            return null;

        return await GenerateAuthResponse(user, deviceFingerprint);
    }

    public async Task<UserInfo?> GetMeAsync(Guid userId)
    {
        var user = await _authRepo.GetUserWithRolesAsync(userId);
        if (user is null) return null;

        var primaryRole = user.UserRoles.FirstOrDefault()?.Role
            ?? new Role { Name = Core.Enums.RoleType.Employee, DisplayName = "Empleado" };

        return new UserInfo(
            user.Id.ToString(),
            user.Email,
            user.DisplayName,
            primaryRole.Name.ToString(),
            user.TenantId,
            user.EmployeeId?.ToString()
        );
    }

    private async Task<AuthResponse> GenerateAuthResponse(User user, string? deviceFingerprint = null)
    {
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
            DeviceFingerprint = deviceFingerprint,
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

        if (storedToken is null || storedToken.ExpiresAt < DateTime.UtcNow)
            return null;

        // Token reuse detection: if already revoked, revoke ALL tokens for this user
        if (storedToken.IsRevoked)
        {
            await _authRepo.RevokeAllUserTokensAsync(storedToken.UserId);
            await _authRepo.SaveChangesAsync();
            return null;
        }

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
            DeviceFingerprint = request.DeviceFingerprint,
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

    public async Task<bool> RevokeAllSessionsAsync(Guid userId)
    {
        await _authRepo.RevokeAllUserTokensAsync(userId);
        await _authRepo.SaveChangesAsync();
        return true;
    }
}
