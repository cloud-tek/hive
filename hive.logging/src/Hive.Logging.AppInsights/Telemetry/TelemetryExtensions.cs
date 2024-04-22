using Hive.Logging.AppInsights.Telemetry.Initializers;
using Hive.Logging.AppInsights.Telemetry.Processors;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;

namespace Hive.Logging.AppInsights.Telemetry;

/// <summary>
/// Telemetry extensions.
/// </summary>
public static class TelemetryExtensions
{
  /// <summary>
  /// Configures the sampling for Application Insights.
  /// </summary>
  /// <param name="telemetryConfiguration"></param>
  /// <param name="options"></param>
  /// <returns><see cref="TelemetryConfiguration"/></returns>
  public static TelemetryConfiguration ConfigureSampling(this TelemetryConfiguration telemetryConfiguration, Options options)
  {
    var builder = telemetryConfiguration.TelemetryProcessorChainBuilder;

    if (options.AdaptiveSampling != null)
    {
      builder.Use(next => CreateProcessor(options, next));
      telemetryConfiguration.TelemetryInitializers.Add(new ExcludeFromAdaptiveSampling(options));
    }

    builder.Use(next => new SkippingTelemetryProcessor(
        next,
        options.RequestLogging?.PathsToSkip ?? Enumerable.Empty<string>())
        ).Build();

    return telemetryConfiguration;
  }

  /// <summary>
  /// Creates the adaptive sampling telemetry processor.
  /// </summary>
  /// <param name="options"></param>
  /// <param name="next"></param>
  /// <returns><see cref="AdaptiveSamplingTelemetryProcessor"/></returns>
  public static AdaptiveSamplingTelemetryProcessor CreateProcessor(Options options, ITelemetryProcessor next)
  {
    var processor = new AdaptiveSamplingTelemetryProcessor(next);

    if (options.AdaptiveSampling == null)
      return processor;

    processor.MaxTelemetryItemsPerSecond = options.AdaptiveSampling?.MaxTelemetryItemsPerSecond ??
                                           processor.MaxTelemetryItemsPerSecond;

    processor.ExcludedTypes = options.AdaptiveSampling?.ExcludedTypes ?? processor.ExcludedTypes;
    processor.IncludedTypes = options.AdaptiveSampling?.IncludedTypes ?? processor.IncludedTypes;
    processor.MinSamplingPercentage =
        options.AdaptiveSampling?.MinSamplingPercentage ?? processor.MinSamplingPercentage;
    processor.MaxSamplingPercentage =
        options.AdaptiveSampling?.MaxSamplingPercentage ?? processor.MaxSamplingPercentage;
    processor.InitialSamplingPercentage =
        options.AdaptiveSampling?.InitialSamplingPercentage ?? processor.InitialSamplingPercentage;
    processor.MovingAverageRatio = options.AdaptiveSampling?.MovingAverageRatio ?? processor.MovingAverageRatio;
    processor.EvaluationInterval = options.AdaptiveSampling?.EvaluationInterval ?? processor.EvaluationInterval;
    processor.SamplingPercentageDecreaseTimeout = options.AdaptiveSampling?.SamplingPercentageDecreaseTimeout ??
                                                  processor.SamplingPercentageDecreaseTimeout;
    processor.SamplingPercentageIncreaseTimeout = options.AdaptiveSampling?.SamplingPercentageIncreaseTimeout ??
                                                  processor.SamplingPercentageIncreaseTimeout;

    return processor;
  }
}