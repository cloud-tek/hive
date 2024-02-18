namespace Hive.Logging.LogzIo;

/// <summary>
/// Options for LogzIo.
/// </summary>
public class Options
{
  /// <summary>
  /// The configuration section key.
  /// </summary>
  public const string SectionKey = "Hive:Logging:LogzIo";

  /// <summary>
  /// LogzIo Token
  /// </summary>
  public string Token { get; set; } = default!;

  /// <summary>
  /// LogzIo Region : us | eu
  /// </summary>
  public string Region { get; set; } = default!;
}