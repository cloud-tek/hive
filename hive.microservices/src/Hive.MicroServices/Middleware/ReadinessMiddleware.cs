using Hive.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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
      var provider = context.RequestServices.GetService<IHealthCheckStateProvider>();
      var healthChecksReady = provider is null
        || provider.GetSnapshots()
          .Where(s => s.AffectsReadiness)
          .All(s => s.IsPassingForReadiness);

      context.Response.StatusCode = service.IsReady && healthChecksReady ? 200 : 503;
      context.Response.ContentType = "application/json";

      var response = new ReadinessResponse(service, provider);

      await context.Response.WriteAsJsonAsync(response, Serialization.JsonOptions.DefaultIndented);
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