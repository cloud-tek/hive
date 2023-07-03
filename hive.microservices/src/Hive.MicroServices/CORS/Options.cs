namespace Hive.MicroServices.CORS;

public partial class Options
{
  public const string SectionKey = "Hive:CORS";

  /// <summary>
  /// (!) Warning
  ///
  /// Must be set to false in production
  /// </summary>
  public bool AllowAny { get; set; } = false;

  public CORSPolicy[] Policies { get; set; } = default!;
}


