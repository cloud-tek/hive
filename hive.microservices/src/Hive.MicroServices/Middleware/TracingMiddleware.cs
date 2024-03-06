using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Hive.MicroServices.Middleware;

/// <summary>
/// The tracing middleware.
/// </summary>
public class TracingMiddleware
{
  private readonly RequestDelegate next;

  /// <summary>
  /// Creates a new <see cref="TracingMiddleware"/> instance
  /// </summary>
  /// <param name="service"></param>
  /// <param name="next"></param>
  /// <exception cref="ArgumentNullException">Thrown when any of the provided arguments is null</exception>
  public TracingMiddleware(IMicroService service, RequestDelegate next)
  {
    _ = service ?? throw new ArgumentNullException(nameof(service));
    this.next = next ?? throw new ArgumentNullException(nameof(next));
  }

  /// <summary>
  /// Invokes the middleware
  /// </summary>
  /// <param name="context"></param>
  /// <returns><see cref="Task"/></returns>
  public async Task InvokeAsync(HttpContext context)
  {
    if (!context.Request.Headers.TryGetValue(
            Constants.Headers.TraceParentId,
            out var requestId))
    {
      context.Request.Headers.TryGetValue(
          Constants.Headers.RequestId,
          out requestId);
    }

    var activity = new Activity(context.Request.Path);

    if (!string.IsNullOrEmpty(requestId))
    {
      activity.SetParentId(requestId!);
    }

    activity.Start();
    try
    {
      await next.Invoke(context);
    }
    finally
    {
      activity.Stop();
    }
  }
}