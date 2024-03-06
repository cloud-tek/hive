using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Hive.Middleware;

/// <summary>
/// The readiness probe middleware, used to report if the middleware is ready to receive traffic
/// </summary>
public class ReadinessMiddleware
{
  /// <summary>
  /// The endpoint for the readiness probe
  /// </summary>
  public const string Endpoint = "/status/readiness";

  private readonly RequestDelegate next;
  private readonly IMicroService service;

  /// <summary>
  /// Initializes a new instance of the <see cref="ReadinessMiddleware"/> class.
  /// </summary>
  /// <param name="service"></param>
  /// <param name="next"></param>
  /// <param name="logger"></param>
  public ReadinessMiddleware(IMicroService service, RequestDelegate next, ILogger<ReadinessMiddleware> logger)
  {
    this.service = service ?? throw new ArgumentNullException(nameof(service));
    this.next = next ?? throw new ArgumentNullException(nameof(next));
  }

  /// <summary>
  /// Invokes the middleware
  /// </summary>
  /// <param name="context"></param>
  /// <returns><see cref="Task"/></returns>
  public async Task InvokeAsync(HttpContext context)
  {
    if (context.Request.Method == "GET" && context.Request.Path == Endpoint)
    {
      context.Response.StatusCode = service.IsReady ? 200 : 503;
      context.Response.ContentType = "application/json";

      await context.Response.WriteAsJsonAsync(new ReadinessResponse(service), Serialization.JsonOptions.DefaultIndented);
      return;
    }

    if (service.IsReady)
    {
      await next.Invoke(context);
    }
    else
    {
      context.Response.StatusCode = 503;
    }
  }
}