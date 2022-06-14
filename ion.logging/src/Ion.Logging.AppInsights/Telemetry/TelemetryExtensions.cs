using Ion.Logging.AppInsights.Telemetry.Initializers;
using Ion.Logging.AppInsights.Telemetry.Processors;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using Microsoft.Extensions.Logging;


namespace Ion.Logging.AppInsights.Telemetry;

public static class TelemetryExtensions
{
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
    
    public static AdaptiveSamplingTelemetryProcessor CreateProcessor(Options options, ITelemetryProcessor next)
    {
        var processor = new AdaptiveSamplingTelemetryProcessor(next);

        if (options.AdaptiveSampling == null) return processor;

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