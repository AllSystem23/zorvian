using Microsoft.Extensions.Logging;
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
    private readonly IEmailService _email;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IAuthRepository authRepo, IFirebaseAuthService firebase, IJwtService jwt, IMfaService mfa, ITenantContext tenant, IEmailService email, ILogger<AuthService> logger)
    {
        _authRepo = authRepo;
        _firebase = firebase;
        _jwt = jwt;
        _mfa = mfa;
        _tenant = tenant;
        _email = email;
        _logger = logger;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var fbUser = await _firebase.VerifyIdTokenAsync(request.IdToken);
        if (fbUser is null) return null;

        var user = await _authRepo.GetUserByFirebaseUidAsync(fbUser.Uid);

        if (user is null)
        {
            var email = fbUser.Email ?? "";

            user = await _authRepo.GetUserByEmailAsync(email ?? "");
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
                    Email = email ?? string.Empty,
                    DisplayName = fbUser.Name ?? string.Empty,
                    AvatarUrl = fbUser.Picture ?? string.Empty,
                    TenantId = _tenant.TenantId ?? throw new InvalidOperationException("TenantId missing"),
                };
                await _authRepo.AddUserAsync(user);
                await _authRepo.SaveChangesAsync();

                await AddTenantReferenceAsync(user);

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
        try
        {
            var fbUser = await _firebase.SignInWithPasswordAsync(request.Email, request.Password);

            User? user = null;

            if (fbUser is not null)
            {
                user = await _authRepo.GetUserByFirebaseUidAsync(fbUser.Uid);

                if (user is null)
                {
                    var email = fbUser.Email;
                    if (string.IsNullOrEmpty(email)) email = request.Email;

                    user = await _authRepo.GetUserByEmailAsync(email ?? "");
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
                            Email = email ?? string.Empty,
                            DisplayName = fbUser.Name ?? string.Empty,
                            AvatarUrl = fbUser.Picture ?? string.Empty,
                            TenantId = _tenant.TenantId ?? throw new InvalidOperationException("TenantId missing"),
                        };
                        await _authRepo.AddUserAsync(user);
                        await _authRepo.SaveChangesAsync();

                        await AddTenantReferenceAsync(user);

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
        catch (Exception ex)
        {
            var innerMsg = ex.InnerException != null ? $" | Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}" : " | No inner exception";
            _logger.LogError(ex, "LoginWithPasswordStep1Async failed for {Email}. Exception: {Type} {Message}{Inner}", request.Email, ex.GetType().Name, ex.Message, innerMsg);
            throw new InvalidOperationException($"Login failed: {ex.GetType().Name}: {ex.Message}{innerMsg}", ex);
        }
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

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        var user = await _authRepo.GetUserByEmailAsync(email);
        if (user is null)
        {
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
            return true;
        }

        await _email.SendEmailAsync(
            email,
            "Recuperación de Contraseña — Zorvian ERP",
            $"""
            <div style="font-family: 'Inter', sans-serif; max-width: 480px; margin: 0 auto;">
                <div style="background: linear-gradient(135deg, #1A0A3E, #7C4DFF); padding: 32px; border-radius: 16px 16px 0 0; text-align: center;">
                    <h1 style="color: #fff; margin: 0; font-size: 24px;">Recuperación de Contraseña</h1>
                </div>
                <div style="background: #fff; padding: 32px; border-radius: 0 0 16px 16px; color: #333;">
                    <p>Hemos recibido una solicitud para restablecer tu contraseña de <strong>Zorvian ERP</strong>.</p>
                    <p>Tu nombre de usuario registrado es:</p>
                    <div style="background: #f5f5f5; padding: 12px 16px; border-radius: 8px; text-align: center; font-size: 18px; font-weight: bold;">
                        {email}
                    </div>
                    <p style="margin-top: 24px;">Para continuar con el restablecimiento, contacta a tu administrador del sistema o utiliza la opción "Olvidé mi contraseña" en la pantalla de inicio de sesión.</p>
                    <hr style="border: none; border-top: 1px solid #eee; margin: 24px 0;" />
                    <p style="color: #aaa; font-size: 12px; text-align: center;">Si no solicitaste este cambio, ignora este mensaje.</p>
                    <p style="color: #aaa; font-size: 12px; text-align: center;">© 2026 Zorvian ERP — Todos los derechos reservados.</p>
                </div>
            </div>
            """
        );

        _logger.LogInformation("Password reset email sent to: {Email}", email);
        return true;
    }

    private async Task AddTenantReferenceAsync(User user)
    {
        var exists = await _authRepo.UserHasTenantAccessAsync(user.Id, user.TenantId);
        if (!exists)
        {
            await _authRepo.AddUserTenantAsync(new UserTenant
            {
                UserId = user.Id,
                TenantId = user.TenantId,
                IsActive = true,
            });
            await _authRepo.SaveChangesAsync();
        }
    }

    public async Task<List<TenantInfoResponse>> GetUserTenantsAsync(Guid userId)
    {
        var userTenants = await _authRepo.GetUserTenantsAsync(userId);
        var tenantIds = userTenants.Select(ut => ut.TenantId).ToList();

        var companies = await _authRepo.GetCompaniesByTenantIdsAsync(tenantIds);

        var currentTenantId = _tenant.TenantId.ToString();

        return userTenants.Select(ut =>
        {
            var company = companies.FirstOrDefault(c => c.TenantId == ut.TenantId);
            return new TenantInfoResponse(
                ut.TenantId,
                company?.Name ?? "Empresa",
                ut.TenantId == currentTenantId
            );
        }).ToList();
    }

    public async Task<AuthResponse?> SwitchTenantAsync(Guid userId, string newTenantId)
    {
        var hasAccess = await _authRepo.UserHasTenantAccessAsync(userId, newTenantId);
        if (!hasAccess) return null;

        var user = await _authRepo.GetUserWithRolesAsync(userId);
        if (user is null) return null;

        user.LastLoginAt = DateTime.UtcNow;
        user.TenantId = newTenantId;
        await _authRepo.SaveChangesAsync();

        var primaryRole = user.UserRoles.FirstOrDefault()?.Role
            ?? new Role { Name = Core.Enums.RoleType.Employee, DisplayName = "Empleado" };

        var (accessToken, refreshToken, expiresIn) = _jwt.GenerateTokens(user, primaryRole, newTenantId);

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };
        await _authRepo.AddRefreshTokenAsync(refreshTokenEntity);
        await _authRepo.SaveChangesAsync();

        return new AuthResponse(
            accessToken, refreshToken, expiresIn,
            new UserInfo(
                user.Id.ToString(), user.Email, user.DisplayName,
                primaryRole.Name.ToString(), newTenantId, user.EmployeeId?.ToString()
            )
        );
    }
}
