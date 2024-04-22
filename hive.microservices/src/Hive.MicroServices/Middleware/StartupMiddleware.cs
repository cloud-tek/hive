using Microsoft.AspNetCore.Http;

namespace Hive.Middleware;

/// <summary>
/// The startup probe middleware, used to report if the middleware has started
/// </summary>
public class StartupMiddleware
{
  /// <summary>
  /// The endpoint for the startup probe
  /// </summary>
  public const string Endpoint = "/status/startup";

  private readonly RequestDelegate next;
  private readonly IMicroService service;

  /// <summary>
  /// Creates a new <see cref="StartupMiddleware"/> instance
  /// </summary>
  /// <param name="service"></param>
  /// <param name="next"></param>
  /// <exception cref="ArgumentNullException">Thrown when any of the provided arguments are null</exception>
  public StartupMiddleware(IMicroService service, RequestDelegate next)
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
      context.Response.StatusCode = service.IsStarted ? 200 : 503;
      context.Response.ContentType = "application/json";

      await context.Response.WriteAsJsonAsync(new StartupResponse(service), Serialization.JsonOptions.DefaultIndented);

      return;
    }

    await next.Invoke(context);
  }
}