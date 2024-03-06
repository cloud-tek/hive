using Microsoft.AspNetCore.Http;

namespace Hive.Middleware;

/// <summary>
/// The liveness probe middleware, used to report if the middleware is alive and to return basic <see cref="IMicroService"/> information.
/// </summary>
public class LivenessMiddleware
{
  /// <summary>
  /// The endpoint for the liveness probe
  /// </summary>
  public const string Endpoint = "/status/liveness";

  private readonly RequestDelegate next;
  private readonly LivenessResponse response;

  /// <summary>
  /// Creates a new <see cref="LivenessMiddleware"/> instance
  /// </summary>
  /// <param name="service"></param>
  /// <param name="next"></param>
  /// <exception cref="ArgumentNullException">Thrown when any of the provided arguments are null</exception>
  public LivenessMiddleware(IMicroService service, RequestDelegate next)
  {
    this.next = next ?? throw new ArgumentNullException(nameof(next));

    response = new LivenessResponse(service);
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
      context.Response.StatusCode = 200;
      await context.Response.WriteAsJsonAsync(response, Serialization.JsonOptions.DefaultIndented);
      return;
    }

    await next.Invoke(context);
  }
}