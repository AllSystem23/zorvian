using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OtpNet;
using Zorvian.Application.DTOs.Mfa;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Interfaces;

namespace Zorvian.Infrastructure.Services;

public sealed class MfaService : IMfaService
{
    private readonly IAuthRepository _authRepo;
    private readonly ITenantContext _tenant;
    private readonly IConfiguration _config;

    public MfaService(IAuthRepository authRepo, ITenantContext tenant, IConfiguration config)
    {
        _authRepo = authRepo;
        _tenant = tenant;
        _config = config;
    }

    public async Task<EnableMfaResponse> GenerateSecretAsync(Guid userId)
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        var secret = Base32Encoding.ToString(key);
        var issuer = "Zorvian ERP";
        var user = await _authRepo.GetUserWithRolesAsync(userId);
        var email = user?.Email ?? "user";
        var uri = new OtpUri(OtpType.Totp, secret, email, issuer).ToString();

        var dbUser = await _authRepo.GetUserWithRolesAsync(userId);
        if (dbUser is not null)
        {
            dbUser.MfaSecretKey = secret;
            await _authRepo.SaveChangesAsync();
        }

        return new EnableMfaResponse(secret, uri);
    }

    public async Task<bool> VerifyAndEnableAsync(Guid userId, string code)
    {
        var user = await _authRepo.GetUserWithRolesAsync(userId);
        if (user is null || string.IsNullOrEmpty(user.MfaSecretKey))
            return false;

        if (!ValidateCode(user.MfaSecretKey, code))
            return false;

        user.IsMfaEnabled = true;
        await _authRepo.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DisableAsync(Guid userId, string password, string code)
    {
        var user = await _authRepo.GetUserWithRolesAsync(userId);
        if (user is null || !user.IsMfaEnabled)
            return false;

        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            if (!PasswordHelper.Verify(password, user.PasswordHash))
                return false;
        }

        if (!ValidateCode(user.MfaSecretKey!, code))
            return false;

        user.IsMfaEnabled = false;
        user.MfaSecretKey = null;
        await _authRepo.SaveChangesAsync();
        return true;
    }

    public bool ValidateCode(string secretKey, string code)
    {
        var key = Base32Encoding.ToBytes(secretKey);
        var totp = new Totp(key, step: 30, mode: OtpHashMode.Sha256, totpSize: 6);
        return totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
    }

    public string GenerateMfaToken(Guid userId)
    {
        var jwtSecret = _config["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT Secret not configured");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "nexora",
            audience: "nexora-api",
            claims:
            [
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new("mfa_partial", "true"),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Guid? ValidateMfaToken(string token)
    {
        var jwtSecret = _config["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT Secret not configured");

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var result = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "nexora",
                ValidateAudience = true,
                ValidAudience = "nexora-api",
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew = TimeSpan.Zero,
            }, out _);

            var mfaPartial = result.FindFirst("mfa_partial")?.Value;
            if (mfaPartial != "true") return null;

            var sub = result.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (Guid.TryParse(sub, out var userId)) return userId;
        }
        catch
        {
            return null;
        }

        return null;
    }
}
