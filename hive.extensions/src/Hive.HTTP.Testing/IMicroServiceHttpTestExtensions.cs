namespace Hive.HTTP.Testing;

public static class IMicroServiceHttpTestExtensions
{
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