namespace Hive.Configuration.CORS;

/// <summary>
/// The CORS policy
/// </summary>
public class CORSPolicy
{
  /// <summary>
  /// The name of the CORS policy
  /// </summary>
  public string Name { get; set; } = default!;

  /// <summary>
  /// Allowed method of the CORS policy
  /// </summary>
  // ReSharper disable once MemberCanBePrivate.Global
  public string[] AllowedMethods { get; set; } = default!;

  /// <summary>
  /// Allowed origins of the CORS policy
  /// </summary>
  public string[] AllowedOrigins { get; set; } = default!;

  /// <summary>
  /// Allowed headers of the CORS policy
  /// </summary>
  // ReSharper disable once MemberCanBePrivate.Global
  public string[] AllowedHeaders { get; set; } = default!;
}