using System.Net.Http.Json;
using System.Text.Json;
using FirebaseAdmin.Auth;
using Microsoft.Extensions.Configuration;
using Zorvian.Application.Interfaces;

namespace Zorvian.Infrastructure.Identity;

public sealed class FirebaseAuthService : IFirebaseAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _webApiKey;

    public FirebaseAuthService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _webApiKey = configuration["Firebase:WebApiKey"] ?? throw new InvalidOperationException("Firebase:WebApiKey not configured");
    }
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

    public async Task<FirebaseUser?> SignInWithPasswordAsync(string email, string password)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.PostAsJsonAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_webApiKey}",
                new { email, password, returnSecureToken = true });

            if (!response.IsSuccessStatusCode)
                return null;

            var fbResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
            var idToken = fbResponse.GetProperty("idToken").GetString();

            if (idToken is null) return null;

            return await VerifyIdTokenAsync(idToken);
        }
        catch
        {
            return null;
        }
    }
}
