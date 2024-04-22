using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Hive.Logging.LogzIo;

/// <summary>
/// Middleware for logging requests.
/// </summary>
internal sealed class RequestLoggingMiddleware
{
  private readonly RequestDelegate next;
  private readonly ILogger<RequestLoggingMiddleware> logger;

  public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
  {
    this.next = next ?? throw new ArgumentNullException(nameof(next));
    this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public async Task Invoke(HttpContext context)
  {
    _ = context ?? throw new ArgumentNullException(nameof(context));

    var start = Stopwatch.GetTimestamp();
    try
    {
      await next(context);
      var elapsedMs = GetElapsedMilliseconds(start, Stopwatch.GetTimestamp());

      var statusCode = context.Response?.StatusCode;
      if (statusCode < 500)
      {
        logger.LogResponse(
          context.Request.Method,
          context.Request.Path,
          context.Response!.StatusCode,
          elapsedMs);
      }
      else
      {
        logger.LogErrorResponse(
          context.Request.Method,
          context.Request.Path,
          context.Response?.StatusCode ?? 500,
          elapsedMs);
      }
    }
    catch (Exception ex) when (LogException(context, GetElapsedMilliseconds(start, Stopwatch.GetTimestamp()), ex))
    {
    }
  }

  private bool LogException(HttpContext context, double elapsedMs, Exception ex)
  {
    logger.LogRequestException(context.Request.Method, context.Request.Path, context.Response?.StatusCode ?? 500, elapsedMs, ex);

    return false;
  }

  private static double GetElapsedMilliseconds(long start, long stop)
  {
    return (stop - start) * 1000 / (double)Stopwatch.Frequency;
  }
}

internal static partial class RequestLoggingMiddlewareLoggerExtensions
{
  [LoggerMessage(2, LogLevel.Information, "HTTP {Method} {Path} responded with {StatusCode} in {Elapsed:0.0000} [ms]")]
  internal static partial void LogResponse(this ILogger logger, string method, string path, int statusCode, double elapsed);

  [LoggerMessage(1, LogLevel.Error, "HTTP {Method} {Path} responded with {StatusCode} in {Elapsed:0.0000} [ms]")]
  internal static partial void LogErrorResponse(this ILogger logger, string method, string path, int statusCode, double elapsed);

  [LoggerMessage(0, LogLevel.Error, "HTTP {Method} {Path} failed with {StatusCode} in {Elapsed:0.0000} [ms]")]
  internal static partial void LogRequestException(this ILogger logger, string method, string path, int statusCode, double elapsed, Exception exception);
}