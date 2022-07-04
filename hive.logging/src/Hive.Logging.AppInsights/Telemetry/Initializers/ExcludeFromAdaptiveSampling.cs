using System.Linq.Expressions;
using Hive.Logging.AppInsights.Telemetry.Sampling;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using StringToExpression.LanguageDefinitions;

namespace Hive.Logging.AppInsights.Telemetry.Initializers;

public class ExcludeFromAdaptiveSampling : ITelemetryInitializer
{
    private readonly Options options;
    private readonly List<Func<DependencyForFilter, bool>> dependencyPredicates;
    private readonly List<Func<RequestForFilter, bool>> requestPredicates;
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
            Expression<Func<T, bool>> expression = language.Parse<T>(filter);

            return expression.Compile();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(
                "Compiling rule \"{Filter}\" for {FilterType} failed! This rule will be not applied",
                filter,
                typeof(T).Name, e.ToString());
            return AlwaysFalsePredicate;
        }
    }

    internal static void ApplyDependencyRules(DependencyTelemetry dependencyTelemetry,
        List<Func<DependencyForFilter, bool>> predicates)
    {
        var shouldExclude = predicates.Any(predicate => predicate(new DependencyForFilter(dependencyTelemetry)));

        ((ISupportSampling)dependencyTelemetry).SamplingPercentage = shouldExclude ? (double?)100 : null;
    }

    internal static void ApplyRequestRules(RequestTelemetry requestTelemetry,
        List<Func<RequestForFilter, bool>> predicates)
    {
        var shouldExclude = predicates.Any(predicate => predicate(new RequestForFilter(requestTelemetry)));

        ((ISupportSampling)requestTelemetry).SamplingPercentage = shouldExclude ? (double?)100 : null;
    }

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
            Console.Error.WriteLine("Applying rules failed! Telemetry will be sampled!", e.ToString());
        }
    }
}