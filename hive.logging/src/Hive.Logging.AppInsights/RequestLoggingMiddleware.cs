using System.Diagnostics;
using System.Globalization;
using Hive.Extensions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;

namespace Hive.Logging.AppInsights;

/// <summary>
/// Middleware for logging requests.
/// </summary>
public class RequestLoggingMiddleware : IMiddleware
{
  private readonly TelemetryClient client;

  /// <summary>
  /// Initializes a new instance of the <see cref="RequestLoggingMiddleware"/> class.
  /// </summary>
  /// <param name="options"></param>
  public RequestLoggingMiddleware(Options options)
  {
    client = options.InstrumentationKey.IsNotNullOrEmpty()
        ? new TelemetryClient(options.ToTelemetryConfiguration())
        : new TelemetryClient(TelemetryConfiguration.CreateDefault());
  }

  /// <summary>
  /// Invokes the middleware.
  /// </summary>
  /// <param name="context"></param>
  /// <param name="next"></param>
  /// <returns><see cref="Task"/></returns>
  public async Task InvokeAsync(HttpContext context, RequestDelegate next)
  {
    var start = DateTimeOffset.UtcNow;
    var startTicks = Stopwatch.GetTimestamp();
    try
    {
      await next(context);
    }
    finally
    {
      var elapsedMs = GetElapsedMilliseconds(startTicks, Stopwatch.GetTimestamp());

      var telemetry = new RequestTelemetry()
      {
        Name = context.Request.Path.Value ?? "unknown",
        Timestamp = start,
        Duration = TimeSpan.FromMilliseconds(elapsedMs),
        ResponseCode = context.Response.StatusCode.ToString(CultureInfo.InvariantCulture),
        Success = context.Response.StatusCode < 400
      };

      telemetry.Properties.Add("Method", context.Request.Method);

      client.TrackRequest(telemetry);
    }
  }

  private static double GetElapsedMilliseconds(long start, long stop)
  {
    return (stop - start) * 1000 / (double)Stopwatch.Frequency;
  }
}