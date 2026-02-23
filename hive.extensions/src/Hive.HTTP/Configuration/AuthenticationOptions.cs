using System.ComponentModel.DataAnnotations;

namespace Hive.HTTP.Configuration;

/// <summary>
/// Configuration options for HTTP client authentication, bound from JSON configuration.
/// </summary>
public class AuthenticationOptions
{
  /// <summary>
  /// The authentication type. Supported values: <c>ApiKey</c>, <c>BearerToken</c>, <c>Custom</c>.
  /// </summary>
  [Required]
  public string Type { get; set; } = default!;

  /// <summary>
  /// The header name for API key authentication.
  /// </summary>
  public string? HeaderName { get; set; }

  /// <summary>
  /// The header value for API key authentication.
  /// </summary>
  public string? Value { get; set; }
}
