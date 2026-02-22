namespace Hive.HTTP.Testing;

public sealed class MockHttpMessageHandler : HttpMessageHandler
{
  private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

  public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
  {
    _responseFactory = responseFactory ?? throw new ArgumentNullException(nameof(responseFactory));
  }

  protected override Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request,
    CancellationToken cancellationToken)
  {
    var response = _responseFactory(request);
    return Task.FromResult(response);
  }
}