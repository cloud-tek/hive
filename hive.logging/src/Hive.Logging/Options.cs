using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace Hive.Logging
{
  /// <summary>
  /// Options for the logging service.
  /// </summary>
  public class Options
  {
    /// <summary>
    /// The configuration section key.
    /// </summary>
    public const string SectionKey = "Hive:Logging";

    /// <summary>
    /// Gets or sets a value indicating whether to enable self log.
    /// </summary>
    public bool EnableSelfLog { get; set; }

    /// <summary>
    /// Gets or sets the log level.
    /// </summary>
    [Required]
    public LogLevel Level { get; set; } = LogLevel.Information;
  }
}