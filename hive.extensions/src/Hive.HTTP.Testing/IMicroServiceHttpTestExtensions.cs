namespace Hive.HTTP.Testing;

/// <summary>
/// Extension methods for replacing HTTP client handlers during integration testing.
/// </summary>
public static class IMicroServiceHttpTestExtensions
{
  /// <summary>
  /// Replaces the primary HTTP message handler for the specified API client with a custom handler.
  /// </summary>
  /// <typeparam name="TApi">The Refit interface whose transport handler is being replaced.</typeparam>
  /// <param name="service">The microservice under test.</param>
  /// <param name="handler">The custom handler to use instead of <see cref="System.Net.Http.SocketsHttpHandler"/>.</param>
  /// <returns>The microservice instance for chaining.</returns>
  public static IMicroService WithTestHandler<TApi>(
    this IMicroService service,
    HttpMessageHandler handler)
    where TApi : class
  {
    ArgumentNullException.ThrowIfNull(handler);

    var clientName = typeof(TApi).Name;
    var extension = GetExtension(service);
    extension.PrimaryHandlerOverrides[clientName] = () => handler;

    return service;
  }

  /// <summary>
  /// Replaces the primary HTTP message handler for the specified API client with a mock response factory.
  /// </summary>
  /// <typeparam name="TApi">The Refit interface whose transport handler is being replaced.</typeparam>
  /// <param name="service">The microservice under test.</param>
  /// <param name="responseFactory">A delegate that produces a response for each incoming request.</param>
  /// <returns>The microservice instance for chaining.</returns>
  public static IMicroService WithMockResponse<TApi>(
    this IMicroService service,
    Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    where TApi : class
  {
    ArgumentNullException.ThrowIfNull(responseFactory);

    var clientName = typeof(TApi).Name;
    var extension = GetExtension(service);
    extension.PrimaryHandlerOverrides[clientName] = () => new MockHttpMessageHandler(responseFactory);

    return service;
  }

  private static Extension GetExtension(IMicroServiceCore service)
  {
    return service.Extensions
      .OfType<Extension>()
      .FirstOrDefault()
      ?? throw new InvalidOperationException(
        "No Hive.HTTP extension found. Call WithHttpClient<T>() before WithTestHandler<T>() or WithMockResponse<T>().");
  }
}