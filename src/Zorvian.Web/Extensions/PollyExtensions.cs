using System.Net;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Retry;

namespace Zorvian.Web.Extensions;

/// <summary>
/// Polly resilience policies: Retry + Circuit Breaker for external HTTP calls.
/// </summary>
public static class PollyExtensions
{
    /// <summary>
    /// Adds retry + circuit breaker policies to named HttpClient registrations.
    /// </summary>
    public static IServiceCollection AddZorvianResilience(this IServiceCollection services)
    {
        // Retry policy: retry 3 times with exponential backoff
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, delay, retryCount, context) =>
                {
                    // Log retry (would use ILogger in production)
                });

        // Circuit breaker: break after 5 consecutive failures, wait 30s before retry
        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, duration) =>
                {
                    // Circuit opened — log this
                },
                onReset: () =>
                {
                    // Circuit closed — log recovery
                });

        // Combined policy: retry then circuit breaker
        var policyWrap = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);

        // Named clients with resilience
        services.AddHttpClient("Firebase")
            .AddPolicyHandler(policyWrap);

        services.AddHttpClient("GoogleAI")
            .AddPolicyHandler(policyWrap);

        services.AddHttpClient("ExternalAPI")
            .AddPolicyHandler(policyWrap);

        return services;
    }
}