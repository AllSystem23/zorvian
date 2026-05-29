namespace Nexora.Application.Interfaces;

public interface IFirebaseAuthService
{
    Task<FirebaseUser?> VerifyIdTokenAsync(string idToken);
}

public sealed record FirebaseUser(string Uid, string Email, string Name, string? Picture);
