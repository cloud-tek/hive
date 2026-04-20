namespace Hive.HTTP;

/// <summary>
/// Indicates whether an HTTP client targets an internal service or an external API.
/// </summary>
public enum HttpClientFlavour
{
  /// <summary>
  /// Service-to-service communication within the same infrastructure.
  /// </summary>
  Internal,

  /// <summary>
  /// Communication with a third-party or external API.
  /// </summary>
  External
}