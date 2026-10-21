using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

public sealed class PasswordResetServiceTests
{
    private readonly Mock<IPasswordResetTokenRepository> _tokens = new();
    private readonly Mock<IAuthRepository> _authRepo = new();
    private readonly Mock<IFirebaseAuthService> _firebase = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<IAppLinks> _links = new();
    private readonly Mock<ILogger<PasswordResetService>> _logger = new();
    private readonly PasswordResetService _sut;

    private string? _rawToken;

    public PasswordResetServiceTests()
    {
        _links.Setup(l => l.PasswordResetTokenMinutes).Returns(30);
        _links.Setup(l => l.BuildResetPasswordUrl(It.IsAny<string>()))
            .Callback<string>(t => _rawToken = t)
            .Returns((string t) => $"https://app.test/reset-password?token={t}");

        _sut = new PasswordResetService(
            _tokens.Object,
            _authRepo.Object,
            _firebase.Object,
            _email.Object,
            _links.Object,
            _logger.Object);
    }

    private static string Sha256Hex(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static User BuildUser() => new()
    {
        Id = Guid.NewGuid(),
        FirebaseUid = "fb-uid-reset",
        Email = "user@zorvian.app",
        DisplayName = "Usuario",
        PasswordHash = PasswordHelper.Hash("OldPass1")
    };

    [Fact]
    public async Task RequestResetAsync_UnknownEmail_ReturnsTrue_AndSendsNothing()
    {
        _authRepo.Setup(r => r.GetUserByEmailIgnoreCaseAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var result = await _sut.RequestResetAsync("nadie@zorvian.app");

        Assert.True(result);
        _tokens.Verify(t => t.AddAsync(It.IsAny<PasswordResetToken>()), Times.Never);
        _email.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task RequestResetAsync_EmptyEmail_ReturnsFalse()
    {
        var result = await _sut.RequestResetAsync("   ");

        Assert.False(result);
        _tokens.Verify(t => t.AddAsync(It.IsAny<PasswordResetToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestResetAsync_KnownEmail_StoresOnlyTheHash_AndSendsTheLink()
    {
        var user = BuildUser();
        _authRepo.Setup(r => r.GetUserByEmailIgnoreCaseAsync("user@zorvian.app")).ReturnsAsync(user);

        PasswordResetToken? stored = null;
        _tokens.Setup(t => t.AddAsync(It.IsAny<PasswordResetToken>()))
            .Callback<PasswordResetToken>(t => stored = t)
            .Returns(Task.CompletedTask);

        var result = await _sut.RequestResetAsync("user@zorvian.app");

        Assert.True(result);
        Assert.NotNull(stored);
        Assert.Equal(user.Id, stored!.UserId);
        Assert.True(stored.ExpiresAt > DateTime.UtcNow);
        // El token en claro NUNCA se persiste: solo su hash SHA-256.
        Assert.Equal(64, stored.TokenHash.Length);
        Assert.NotEqual(_rawToken, stored.TokenHash);
        Assert.Equal(Sha256Hex(_rawToken!), stored.TokenHash);

        _email.Verify(e => e.SendEmailAsync(
            "user@zorvian.app",
            It.Is<string>(s => s.Contains("Recuperación")),
            It.Is<string>(b => b.Contains("https://app.test/reset-password?token=")),
            It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task RequestResetAsync_EmailFailure_StillReturnsTrue_NoEnumerationLeak()
    {
        _authRepo.Setup(r => r.GetUserByEmailIgnoreCaseAsync(It.IsAny<string>())).ReturnsAsync(BuildUser());
        _email.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ThrowsAsync(new InvalidOperationException("EmailSettings:From is missing"));

        var result = await _sut.RequestResetAsync("user@zorvian.app");

        Assert.True(result);
    }

    [Fact]
    public async Task ResetAsync_UnknownToken_ReturnsError()
    {
        _tokens.Setup(t => t.GetByHashAsync(It.IsAny<string>())).ReturnsAsync((PasswordResetToken?)null);

        var (success, error) = await _sut.ResetAsync("token-fake", "Nueva123");

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task ResetAsync_ExpiredToken_ReturnsError()
    {
        var token = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Email = "user@zorvian.app",
            TokenHash = Sha256Hex("raw"),
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };
        _tokens.Setup(t => t.GetByHashAsync(It.IsAny<string>())).ReturnsAsync(token);

        var (success, error) = await _sut.ResetAsync("raw", "Nueva123");

        Assert.False(success);
        Assert.Contains("expir", error!.ToLowerInvariant());
    }

    [Fact]
    public async Task ResetAsync_AlreadyUsedToken_ReturnsError()
    {
        var token = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Email = "user@zorvian.app",
            TokenHash = Sha256Hex("raw"),
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            IsUsed = true
        };
        _tokens.Setup(t => t.GetByHashAsync(It.IsAny<string>())).ReturnsAsync(token);

        var (success, error) = await _sut.ResetAsync("raw", "Nueva123");

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task ResetAsync_ValidToken_UpdatesPassword_MarksTokenUsed_AndRevokesSessions()
    {
        var user = BuildUser();
        var token = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Email = user.Email,
            TokenHash = Sha256Hex("raw"),
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        };

        _tokens.Setup(t => t.GetByHashAsync(It.IsAny<string>())).ReturnsAsync(token);
        _authRepo.Setup(r => r.GetUserWithRolesAsync(user.Id)).ReturnsAsync(user);

        var (success, error) = await _sut.ResetAsync("raw", "Nueva123");

        Assert.True(success);
        Assert.Null(error);
        Assert.True(token.IsUsed);
        Assert.NotNull(token.UsedAt);
        Assert.NotEqual(PasswordHelper.Hash("Nueva123"), user.PasswordHash);
        Assert.True(PasswordHelper.Verify("Nueva123", user.PasswordHash!));
        _firebase.Verify(f => f.UpdatePasswordAsync(user.FirebaseUid!, "Nueva123"), Times.Once);
        _authRepo.Verify(r => r.RevokeAllUserTokensAsync(user.Id, It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ResetAsync_FirebaseFailure_ReturnsError_AndKeepsTheOldPassword()
    {
        var user = BuildUser();
        var oldHash = user.PasswordHash;
        var token = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Email = user.Email,
            TokenHash = Sha256Hex("raw"),
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        };

        _tokens.Setup(t => t.GetByHashAsync(It.IsAny<string>())).ReturnsAsync(token);
        _authRepo.Setup(r => r.GetUserWithRolesAsync(user.Id)).ReturnsAsync(user);
        _firebase.Setup(f => f.UpdatePasswordAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("firebase down"));

        var (success, error) = await _sut.ResetAsync("raw", "Nueva123");

        Assert.False(success);
        Assert.NotNull(error);
        Assert.Equal(oldHash, user.PasswordHash);
        Assert.False(token.IsUsed);
    }
}

public sealed class PasswordPolicyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Empty_ReturnsError(string? password)
    {
        Assert.NotNull(PasswordPolicy.Validate(password));
    }

    [Fact]
    public void Validate_ShorterThanMinimum_ReturnsError()
    {
        var error = PasswordPolicy.Validate("Ab1");

        Assert.NotNull(error);
        Assert.Contains("8", error);
    }

    [Fact]
    public void Validate_WithoutUppercase_ReturnsError()
    {
        var error = PasswordPolicy.Validate("abcdefg1");

        Assert.NotNull(error);
        Assert.Contains("mayúscula", error);
    }

    [Fact]
    public void Validate_WithoutDigit_ReturnsError()
    {
        var error = PasswordPolicy.Validate("Abcdefgh");

        Assert.NotNull(error);
        Assert.Contains("número", error);
    }

    [Fact]
    public void Validate_StrongPassword_ReturnsNull()
    {
        Assert.Null(PasswordPolicy.Validate("Zorvian1"));
    }
}
