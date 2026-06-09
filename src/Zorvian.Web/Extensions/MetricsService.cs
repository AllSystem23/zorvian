using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Zorvian.Web.Extensions;

/// <summary>
/// Application Performance Monitoring (APM) and metrics service (P3.4)
/// Uses System.Diagnostics.Metrics for OpenTelemetry compatibility
/// </summary>
public interface IMetricsService
{
    void RecordRequest(string endpoint, int statusCode, double durationMs);
    void RecordBusinessEvent(string eventName, Dictionary<string, object>? properties = null);
    void RecordException(Exception ex, string? context = null);
    IDisposable TrackOperation(string operationName);
    MetricsSnapshot GetSnapshot();
}

public class MetricsSnapshot
{
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public double AverageResponseTime { get; set; }
    public long ActiveOperations { get; set; }
    public DateTime SnapshotAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// In-memory metrics service. In production, this would export to
/// OpenTelemetry collector, Application Insights, Datadog, etc.
/// </summary>
public class InMemoryMetricsService : IMetricsService
{
    private static readonly Meter _meter = new("Zorvian.ERP", "1.0.0");
    private static readonly Counter<long> _requestCounter = _meter.CreateCounter<long>("zorvian.requests.total");
    private static readonly Histogram<double> _requestDuration = _meter.CreateHistogram<double>("zorvian.requests.duration_ms");
    private static readonly Counter<long> _exceptionCounter = _meter.CreateCounter<long>("zorvian.exceptions.total");
    private static readonly UpDownCounter<long> _activeOperations = _meter.CreateUpDownCounter<long>("zorvian.operations.active");

    private long _totalRequests;
    private long _successfulRequests;
    private long _failedRequests;
    private double _totalDuration;
    private long _activeOps;
    private readonly object _lock = new();

    public void RecordRequest(string endpoint, int statusCode, double durationMs)
    {
        _requestCounter.Add(1, new KeyValuePair<string, object?>("endpoint", endpoint), new KeyValuePair<string, object?>("status", statusCode));
        _requestDuration.Record(durationMs);

        lock (_lock)
        {
            _totalRequests++;
            _totalDuration += durationMs;
            if (statusCode >= 200 && statusCode < 400)
                _successfulRequests++;
            else
                _failedRequests++;
        }
    }

    public void RecordBusinessEvent(string eventName, Dictionary<string, object>? properties = null)
    {
        var counter = _meter.CreateCounter<long>($"zorvian.events.{eventName}");
        counter.Add(1);
    }

    public void RecordException(Exception ex, string? context = null)
    {
        _exceptionCounter.Add(1, new KeyValuePair<string, object?>("type", ex.GetType().Name), new KeyValuePair<string, object?>("context", context ?? "unknown"));
    }

    public IDisposable TrackOperation(string operationName)
    {
        _activeOperations.Add(1, new KeyValuePair<string, object?>("operation", operationName));
        return new OperationTracker(this, operationName);
    }

    public MetricsSnapshot GetSnapshot()
    {
        lock (_lock)
        {
            return new MetricsSnapshot
            {
                TotalRequests = _totalRequests,
                SuccessfulRequests = _successfulRequests,
                FailedRequests = _failedRequests,
                AverageResponseTime = _totalRequests > 0 ? _totalDuration / _totalRequests : 0,
                ActiveOperations = _activeOps
            };
        }
    }

    private class OperationTracker : IDisposable
    {
        private readonly InMemoryMetricsService _service;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;

        public OperationTracker(InMemoryMetricsService service, string operationName)
        {
            _service = service;
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();
            lock (_service._lock) _service._activeOps++;
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _activeOperations.Add(-1, new KeyValuePair<string, object?>("operation", _operationName));
            lock (_service._lock) _service._activeOps--;
        }
    }
}

/// <summary>
/// Middleware that tracks request metrics automatically
/// </summary>
public sealed class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMetricsService _metrics;

    public MetricsMiddleware(RequestDelegate next, IMetricsService metrics)
    {
        _next = next;
        _metrics = metrics;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RecordRequest(
                endpoint: $"{context.Request.Method} {context.Request.Path}",
                statusCode: context.Response.StatusCode,
                durationMs: stopwatch.Elapsed.TotalMilliseconds
            );
        }
    }
}

public static class MetricsMiddlewareExtensions
{
    public static IApplicationBuilder UseMetrics(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<MetricsMiddleware>();
    }
}
