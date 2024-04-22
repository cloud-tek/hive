using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Hive.MicroServices.CORS;

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

  /// <summary>
  /// Converts the <see cref="CORSPolicy"/> to a <see cref="Action"/> for a <see cref="CorsPolicyBuilder"/>
  /// </summary>
  /// <returns><see cref="Action"/></returns>
  public Action<CorsPolicyBuilder> ToCORSPolicyBuilderAction()
  {
    return new Action<CorsPolicyBuilder>((builder) =>
    {
      if (AllowedHeaders != null && AllowedHeaders.Length > 0)
      {
        builder.WithHeaders(AllowedHeaders);
      }

      if (AllowedOrigins != null && AllowedOrigins.Length > 0)
      {
        builder.WithOrigins(AllowedOrigins);
      }

      if (AllowedMethods != null && AllowedMethods.Length > 0)
      {
        builder.WithMethods(AllowedMethods);
      }
    });
  }
}