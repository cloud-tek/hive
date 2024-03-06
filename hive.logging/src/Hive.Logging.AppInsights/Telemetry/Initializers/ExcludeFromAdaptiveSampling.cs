using Hive.Logging.AppInsights.Telemetry.Sampling;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using StringToExpression.LanguageDefinitions;

namespace Hive.Logging.AppInsights.Telemetry.Initializers;

/// <summary>
/// A telemetry initializer that excludes telemetry from adaptive sampling based on the rules.
/// </summary>
public class ExcludeFromAdaptiveSampling : ITelemetryInitializer
{
  private readonly Options options;
  private readonly List<Func<DependencyForFilter, bool>> dependencyPredicates;
  private readonly List<Func<RequestForFilter, bool>> requestPredicates;

  /// <summary>
  /// Initializes a new instance of the <see cref="ExcludeFromAdaptiveSampling"/> class.
  /// </summary>
  /// <param name="options"></param>
  public ExcludeFromAdaptiveSampling(
      Options options)
  {
    this.options = options;

    dependencyPredicates = options.AdaptiveSampling?.Excluded?.DependencyRules?
                               .Select(rule => CreatePredicate<DependencyForFilter>(rule))
                               .ToList()
                           ?? new List<Func<DependencyForFilter, bool>>();
    requestPredicates = options.AdaptiveSampling?.Excluded?.DependencyRules?
                               .Select(rule => CreatePredicate<RequestForFilter>(rule))
                               .ToList()
                           ?? new List<Func<RequestForFilter, bool>>();
  }

  private static Func<T, bool> CreatePredicate<T>(string filter)
  {
    bool AlwaysFalsePredicate(T p) => false;

    if (string.IsNullOrWhiteSpace(filter))
    {
      return AlwaysFalsePredicate;
    }

    try
    {
      var language = new ODataFilterLanguage();
      var expression = language.Parse<T>(filter);

      return expression.Compile();
    }
    catch (Exception e)
    {
      // todo: replace with logging
      Console.Error.WriteLine(
          "Compiling rule \"{0}\" for {1} failed! This rule will not be applied",
          filter,
          typeof(T).Name);
      Console.Error.WriteLine(e.ToString());
      return AlwaysFalsePredicate;
    }
  }

  internal static void ApplyDependencyRules(
    DependencyTelemetry dependencyTelemetry,
    List<Func<DependencyForFilter, bool>> predicates)
  {
    var shouldExclude = predicates.Any(predicate => predicate(new DependencyForFilter(dependencyTelemetry)));

    ((ISupportSampling)dependencyTelemetry).SamplingPercentage = shouldExclude ? (double?)100 : null;
  }

  internal static void ApplyRequestRules(
    RequestTelemetry requestTelemetry,
    List<Func<RequestForFilter, bool>> predicates)
  {
    var shouldExclude = predicates.Any(predicate => predicate(new RequestForFilter(requestTelemetry)));

    ((ISupportSampling)requestTelemetry).SamplingPercentage = shouldExclude ? (double?)100 : null;
  }

  /// <summary>
  /// Initializes the telemetry.
  /// </summary>
  /// <param name="telemetry"></param>
  public void Initialize(ITelemetry telemetry)
  {
    try
    {
      switch (telemetry)
      {
        case DependencyTelemetry dependencyTelemetry:
          ApplyDependencyRules(dependencyTelemetry, dependencyPredicates);
          break;
        case RequestTelemetry requestTelemetry:
          ApplyRequestRules(requestTelemetry, requestPredicates);
          break;
      }
    }
    catch (Exception e)
    {
      // todo: replace with logging
      Console.Error.WriteLine("Applying rules failed! Telemetry will be sampled! {0}", e.ToString());
    }
  }
}