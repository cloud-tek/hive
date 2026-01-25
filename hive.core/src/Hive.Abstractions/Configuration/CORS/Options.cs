namespace Hive.Configuration.CORS;

/// <summary>
/// The CORS policy options used to configure the service
/// </summary>
public partial class Options
{
  /// <summary>
  /// The configuration section key for the CORS options
  /// </summary>
  public const string SectionKey = "Hive:CORS";

  /// <summary>
  /// (!) Warning
  ///
  /// Must be set to false in production
  /// </summary>
  public bool AllowAny { get; set; }

  /// <summary>
  /// The CORS policies to be applied
  /// </summary>
  public CORSPolicy[] Policies { get; set; } = default!;
}