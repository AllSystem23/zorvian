using System.Net.Http.Json;
using System.Text.Json;
using FirebaseAdmin.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Zorvian.Application.Interfaces;

namespace Zorvian.Infrastructure.Identity;

public sealed class FirebaseAuthService : IFirebaseAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _webApiKey;
    private readonly ILogger<FirebaseAuthService> _logger;

    public FirebaseAuthService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<FirebaseAuthService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "VerifyIdTokenAsync failed");
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetUserByEmailAsync failed for {Email}", email);
            return null;
        }
    }

    public async Task UpdatePasswordAsync(string firebaseUid, string newPassword)
    {
        var args = new UserRecordArgs { Uid = firebaseUid, Password = newPassword };
        await FirebaseAuth.DefaultInstance.UpdateUserAsync(args);
    }

    public async Task UpdateEmailAsync(string firebaseUid, string newEmail)
    {
        var args = new UserRecordArgs { Uid = firebaseUid, Email = newEmail };
        await FirebaseAuth.DefaultInstance.UpdateUserAsync(args);
    }

    public async Task<FirebaseUser?> SignInWithPasswordAsync(string email, string password)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_webApiKey}");
            request.Content = JsonContent.Create(new { email, password, returnSecureToken = true });

            _logger.LogInformation("Calling Firebase REST API signInWithPassword");

            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Firebase REST API returned {StatusCode}: {Body}", response.StatusCode, body);
                return null;
            }

            var fbResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
            var localId = fbResponse.TryGetProperty("localId", out var localIdEl) ? localIdEl.GetString() : null;
            var fbEmail = fbResponse.TryGetProperty("email", out var emailEl) ? emailEl.GetString() : null;

            if (string.IsNullOrEmpty(localId) || string.IsNullOrEmpty(fbEmail))
            {
                _logger.LogWarning("Firebase REST API succeeded but missing localId or email");
                return null;
            }

            string? displayName = null;
            if (fbResponse.TryGetProperty("displayName", out var displayNameEl))
                displayName = displayNameEl.GetString();

            string? picture = null;
            if (fbResponse.TryGetProperty("photoUrl", out var photoEl))
                picture = photoEl.GetString();

            return new FirebaseUser(localId, fbEmail, displayName ?? "", picture);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignInWithPasswordAsync exception");
            return null;
        }
    }
}
