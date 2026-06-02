using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Infrastructure.Identity;

public sealed class JwtService : IJwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    public (string accessToken, string refreshToken, int expiresIn) GenerateTokens(
        User user, Role role, string tenantId)
    {
        var accessToken = GenerateAccessToken(user, role, tenantId);
        var refreshToken = GenerateRefreshToken();
        var expiresIn = int.Parse(_config["Jwt:ExpirationMinutes"] ?? "60");

        return (accessToken, refreshToken, expiresIn * 60);
    }

    private string GenerateAccessToken(User user, Role role, string tenantId)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured")));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("firebase_uid", user.FirebaseUid),
            new(ClaimTypes.Email, user.Email),
            new("tenant_id", tenantId),
            new("employee_id", user.EmployeeId?.ToString() ?? ""),
            new(ClaimTypes.Role, role.Name.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var permission in role.RolePermissions)
        {
            claims.Add(new Claim("permission", permission.PermissionCode));
        }

        var token = new JwtSecurityToken(
            issuer: "nexora",
            audience: "nexora-api",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                int.Parse(_config["Jwt:ExpirationMinutes"] ?? "60")),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public bool ValidateRefreshToken(string token)
    {
        // Basic length validation; actual validation against DB
        return !string.IsNullOrEmpty(token) && token.Length >= 32;
    }
}
