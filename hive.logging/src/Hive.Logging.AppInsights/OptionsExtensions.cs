using Hive.Logging.AppInsights.Telemetry;
using Microsoft.ApplicationInsights.Extensibility;

namespace Hive.Logging.AppInsights;

/// <summary>
/// Options for Application Insights.
/// </summary>
public static class OptionsExtensions
{
  /// <summary>
  /// Configures the sampling for Application Insights.
  /// </summary>
  /// <param name="options"></param>
  /// <returns><see cref="TelemetryConfiguration"/></returns>
  public static TelemetryConfiguration ToTelemetryConfiguration(this Options options)
  {
    return new TelemetryConfiguration()
    {
      InstrumentationKey = options.InstrumentationKey
    }.ConfigureSampling(options);
  }
}