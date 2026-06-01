using FirebaseAdmin.Auth;
using Nexora.Application.Interfaces;

namespace Nexora.Infrastructure.Identity;

public sealed class FirebaseAuthService : IFirebaseAuthService
{
    public async Task<FirebaseUser?> VerifyIdTokenAsync(string idToken)
    {
        try
        {
            var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
            var uid = decoded.Uid;
            var email = decoded.Claims.TryGetValue("email", out var e) ? e?.ToString() : null;
            var name = decoded.Claims.TryGetValue("name", out var n) ? n?.ToString() : null;
            var picture = decoded.Claims.TryGetValue("picture", out var p) ? p?.ToString() : null;

            return new FirebaseUser(uid, email ?? "", name ?? "", picture);
        }
        catch
        {
            return null;
        }
    }

    public async Task<FirebaseUserCreated> CreateUserAsync(string email, string password, string displayName)
    {
        var args = new UserRecordArgs
        {
            Email = email,
            Password = password,
            DisplayName = displayName,
        };

        var record = await FirebaseAuth.DefaultInstance.CreateUserAsync(args);
        return new FirebaseUserCreated(record.Uid, record.Email);
    }

    public async Task<FirebaseUserCreated?> GetUserByEmailAsync(string email)
    {
        try
        {
            var record = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email);
            return new FirebaseUserCreated(record.Uid, record.Email);
        }
        catch
        {
            return null;
        }
    }
}
