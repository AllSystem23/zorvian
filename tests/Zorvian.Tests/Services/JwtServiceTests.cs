using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using Zorvian.Core.Entities;
using Zorvian.Core.Enums;
using Zorvian.Infrastructure.Identity;

namespace Zorvian.Tests.Services;

public sealed class JwtServiceTests
{
    [Fact]
    public void GenerateAccessToken_IncludesAllUserRoles()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-secret-key-for-ci-minimum-32-chars!!",
                ["Jwt:ExpirationMinutes"] = "60",
            })
            .Build();
        var sut = new JwtService(config);
        var superAdminRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = RoleType.SuperAdmin,
            DisplayName = "Super Admin",
        };
        var companyAdminRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = RoleType.CompanyAdmin,
            DisplayName = "Admin",
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirebaseUid = "fb-uid",
            Email = "super@zorvian.app",
            DisplayName = "Super Admin",
            UserRoles =
            {
                new UserRole { RoleId = superAdminRole.Id, Role = superAdminRole },
                new UserRole { RoleId = companyAdminRole.Id, Role = companyAdminRole },
            },
        };

        var token = sut.GenerateTokens(user, superAdminRole, "superadmin").accessToken;
        var claims = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.ToList();
        var roles = claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToHashSet();
        var shortRoles = claims.Where(c => c.Type == "role").Select(c => c.Value).ToHashSet();

        Assert.Contains("SuperAdmin", roles);
        Assert.Contains("CompanyAdmin", roles);
        Assert.Contains("SuperAdmin", shortRoles);
        Assert.Contains("CompanyAdmin", shortRoles);
    }
}
