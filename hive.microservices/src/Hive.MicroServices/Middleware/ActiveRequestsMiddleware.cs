using Hive.MicroServices.Lifecycle;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Hive.Middleware;

/// <summary>
/// The middleware which keeps track of the active requests. Used for draining the <see cref="IMicroService"/>
/// </summary>
public class ActiveRequestsMiddleware
{
  private readonly IActiveRequestsService service;
  private readonly RequestDelegate next;

  /// <summary>
  /// Creates a new <see cref="ActiveRequestsMiddleware"/> instance
  /// </summary>
  /// <param name="service"></param>
  /// <param name="next"></param>
  /// <param name="logger"></param>
  /// <exception cref="ArgumentNullException">Thrown when any of the provided arguments is null</exception>
  public ActiveRequestsMiddleware(IActiveRequestsService service, RequestDelegate next, ILogger<ReadinessMiddleware> logger)
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
    try
    {
      service.Increment();
      await next.Invoke(context);
    }
    finally
    {
      service.Decrement();
    }
  }
}