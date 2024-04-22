namespace Hive.Logging.AppInsights;

/// <summary>
/// Options for Application Insights.
/// </summary>
public class Options
{
  /// <summary>
  /// The configuration section key.
  /// </summary>
  public const string SectionKey = "Hive:Logging:AppInsights";

  /// <summary>
  /// Gets or sets the instrumentation key.
  /// </summary>
  public string InstrumentationKey { get; set; } = null!;

  /// <summary>
  /// Gets or sets the request logging options.
  /// </summary>
  public RequestLoggingOptions RequestLogging { get; init; } = null!;

  /// <summary>
  /// Gets or sets the adaptive sampling options.
  /// </summary>
  public AdaptiveSamplingOptions? AdaptiveSampling { get; init; } = null!;

  /// <summary>
  /// Gets or sets the telemetry processor options.
  /// </summary>
  public class RequestLoggingOptions
  {
    /// <summary>
    /// Gets or sets a value indicating whether to enable request logging.
    /// </summary>
    public IEnumerable<string> PathsToSkip { get; set; } = Enumerable.Empty<string>();
  }

  /// <summary>
  /// Gets or sets the adaptive sampling options.
  /// </summary>
  public class AdaptiveSamplingOptions
  {
    /// <summary>
    /// Gets or sets the excluded types.
    /// </summary>
    public string ExcludedTypes { get; set; } = "Event;Exception;PageView"; // Trace

    /// <summary>
    /// Gets or sets the included types.
    /// </summary>
    public string IncludedTypes { get; set; } = null!;

    /// <summary>
    /// Gets or sets the sampling percentage.
    /// </summary>
    public double? MinSamplingPercentage { get; init; } = null!;

    /// <summary>
    /// Gets or sets the sampling percentage.
    /// </summary>
    public double? MaxSamplingPercentage { get; init; } = null!;

    /// <summary>
    /// Gets or sets the sampling percentage.
    /// </summary>
    public double? InitialSamplingPercentage { get; init; } = null!;

    /// <summary>
    ///  Gets or sets the maximum telemetry items per second.
    /// </summary>
    public double? MaxTelemetryItemsPerSecond { get; init; } = null!;

    /// <summary>
    /// Gets or sets the moving average ratio.
    /// </summary>
    public double? MovingAverageRatio { get; init; } = null!;

    /// <summary>
    /// Gets or sets the evaluation interval.
    /// </summary>
    public TimeSpan? EvaluationInterval { get; init; } = null!;

    /// <summary>
    /// Gets or sets the sampling percentage increase timeout.
    /// </summary>
    public TimeSpan? SamplingPercentageDecreaseTimeout { get; init; } = null!;

    /// <summary>
    /// Gets or sets the sampling percentage increase timeout.
    /// </summary>
    public TimeSpan? SamplingPercentageIncreaseTimeout { get; init; } = null!;

    /// <summary>
    /// Gets or sets the excluded types.
    /// </summary>
    public SamplingTelemetrySettings? Excluded { get; init; } = null!;

    /// <summary>
    /// Gets or sets the included types.
    /// </summary>
    public class SamplingTelemetrySettings
    {
      /// <summary>
      /// Gets or sets the dependency rules.
      /// </summary>
      public List<string>? DependencyRules { get; init; } = null!;

      /// <summary>
      /// Gets or sets the request rules.
      /// </summary>
      public List<string>? RequestRules { get; init; } = null!;
    }
  }
}