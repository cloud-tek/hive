namespace Hive.Logging.AppInsights;
public class Options
{
    public const string SectionKey = "Hive:Logging:AppInsights";

    public string InstrumentationKey { get; set; } = null!;

    public RequestLoggingOptions RequestLogging { get; init; } = null!;
    public AdaptiveSamplingOptions? AdaptiveSampling { get; init; } = null!;

    public class RequestLoggingOptions
    {
        public IEnumerable<string> PathsToSkip { get; set; } = Enumerable.Empty<string>();
    }

    public class AdaptiveSamplingOptions
    {
        public string ExcludedTypes { get; set; } = "Event;Exception;PageView"; // Trace
        public string IncludedTypes { get; set; } = null!;
        public double? MinSamplingPercentage { get; init; } = null;
        public double? MaxSamplingPercentage { get; init; } = null;
        public double? InitialSamplingPercentage { get; init; } = null;
        public double? MaxTelemetryItemsPerSecond { get; init; } = null;
        public double? MovingAverageRatio { get; init; } = null;
        public TimeSpan? EvaluationInterval { get; init; } = null;
        public TimeSpan? SamplingPercentageDecreaseTimeout { get; init; } = null;
        public TimeSpan? SamplingPercentageIncreaseTimeout { get; init; } = null;

        public SamplingTelemetrySettings? Excluded { get; init; } = null;

        public class SamplingTelemetrySettings
        {
            public List<string>? DependencyRules { get; init; } = null;
            public List<string>? RequestRules { get; init; } = null;
        }
    }
}

