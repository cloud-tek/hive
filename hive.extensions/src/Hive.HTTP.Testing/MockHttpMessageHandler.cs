namespace Hive.HTTP.Testing;

/// <summary>
/// An <see cref="HttpMessageHandler"/> that produces responses using a configurable factory delegate.
/// </summary>
public sealed class MockHttpMessageHandler : HttpMessageHandler
{
  private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

  /// <summary>
  /// Initializes a new instance of the <see cref="MockHttpMessageHandler"/> class.
  /// </summary>
  /// <param name="responseFactory">A delegate that produces a response for each incoming request.</param>
  public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
  {
    _responseFactory = responseFactory ?? throw new ArgumentNullException(nameof(responseFactory));
  }

  /// <inheritdoc />
  protected override Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request,
    CancellationToken cancellationToken)
  {
    var response = _responseFactory(request);
    return Task.FromResult(response);
  }
}
