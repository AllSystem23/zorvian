namespace Zorvian.Application.DTOs.Auth;

public sealed record LoginStep1Result
{
    public AuthResponse? AuthResponse { get; init; }
    public MfaRequiredResponse? MfaRequiredResponse { get; init; }
    public bool IsSuccess => AuthResponse is not null || MfaRequiredResponse is not null;

    public static LoginStep1Result Completed(AuthResponse response) => new() { AuthResponse = response };
    public static LoginStep1Result MfaRequired(MfaRequiredResponse response) => new() { MfaRequiredResponse = response };
}
