using System.Diagnostics.Metrics;

namespace Hive.HTTP.Telemetry;

internal static class HttpClientMeter
{
  private static readonly Meter Meter = new("Hive.HTTP");

  public static readonly Histogram<double> RequestDuration =
    Meter.CreateHistogram<double>(
      "hive.http.client.request.duration",
      unit: "ms",
      description: "HTTP client request duration in milliseconds");

  public static readonly Counter<long> RequestCount =
    Meter.CreateCounter<long>(
      "hive.http.client.request.count",
      description: "Total HTTP client requests");

  public static readonly Counter<long> RequestErrors =
    Meter.CreateCounter<long>(
      "hive.http.client.request.errors",
      description: "Failed HTTP client requests (non-success status)");

  public static readonly Counter<long> ResilienceRetries =
    Meter.CreateCounter<long>(
      "hive.http.client.resilience.retries",
      description: "Total retry attempts");

  public static readonly Counter<long> CircuitBreakerStateChanges =
    Meter.CreateCounter<long>(
      "hive.http.client.resilience.circuit_breaker.state",
      description: "Circuit breaker state transitions (0=closed, 1=open, 2=half-open)");
}