namespace Hive.MicroServices.CORS;

public class Options
{
  public const string SectionKey = "Hive:CORS";

  /// <summary>
  /// (!) Warning
  ///
  /// Must be set to false in production
  /// </summary>
  public bool AllowAny { get; set; } = false;

  public CORSPolicy[] Policies { get; set; } = default!;

  public class CORSPolicy
  {
    public string[] AllowedOrigins { get; set; } = default!;
    public string[] AllowedHeaders { get; set; } = default!;
  }
}


