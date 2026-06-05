using Zorvian.Application.DTOs.Auth;
using Zorvian.Application.DTOs.Mfa;

namespace Zorvian.Application.Interfaces;

public interface IMfaService
{
    Task<EnableMfaResponse> GenerateSecretAsync(Guid userId);
    Task<bool> VerifyAndEnableAsync(Guid userId, string code);
    Task<bool> DisableAsync(Guid userId, string password, string code);
    bool ValidateCode(string secretKey, string code);
    string GenerateMfaToken(Guid userId);
    Guid? ValidateMfaToken(string token);
}
