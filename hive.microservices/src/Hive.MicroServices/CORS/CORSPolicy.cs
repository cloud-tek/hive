using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Hive.MicroServices.CORS;

public class CORSPolicy
{
  public string Name { get; set; } = default!;

  // ReSharper disable once MemberCanBePrivate.Global
  public string[] AllowedMethods { get; set; } = default!;
  public string[] AllowedOrigins { get; set; } = default!;

  // ReSharper disable once MemberCanBePrivate.Global
  public string[] AllowedHeaders { get; set; } = default!;

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
