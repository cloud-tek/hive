namespace Hive.HTTP.Configuration;

/// <summary>
/// Configuration options for an individual HTTP client, bound from the <c>Hive:Http:{ClientName}</c> section.
/// </summary>
public class HttpClientOptions
{
  /// <summary>
  /// The configuration section key under which HTTP client options are defined.
  /// </summary>
  public const string SectionKey = "Hive:Http";

  /// <summary>
  /// The base URL for all requests made by this client.
  /// </summary>
  public string? BaseAddress { get; set; }

  /// <summary>
  /// Whether the client targets an internal service or an external API.
  /// </summary>
  public HttpClientFlavour Flavour { get; set; } = HttpClientFlavour.Internal;

  /// <summary>
  /// Resilience policy configuration (retry, circuit breaker, timeout).
  /// </summary>
  public ResilienceOptions Resilience { get; set; } = new();

  /// <summary>
  /// Authentication configuration for the client. Null if no authentication is configured.
  /// </summary>
  public AuthenticationOptions? Authentication { get; set; }

  /// <summary>
  /// Connection pooling and transport configuration.
  /// </summary>
  public SocketsHandlerOptions SocketsHandler { get; set; } = new();
}