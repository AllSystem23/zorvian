namespace Nexora.Application.Interfaces;

public interface IFirebaseAuthService
{
    Task<FirebaseUser?> VerifyIdTokenAsync(string idToken);
    Task<FirebaseUserCreated> CreateUserAsync(string email, string password, string displayName);
    Task<FirebaseUserCreated?> GetUserByEmailAsync(string email);
}

public sealed record FirebaseUser(string Uid, string Email, string Name, string? Picture);
public sealed record FirebaseUserCreated(string Uid, string Email);
