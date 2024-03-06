using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace Hive.Logging.Enrichers;

/// <summary>
/// Adds the current activity's span, trace and parent ids to the log event.
/// </summary>
public class ActivityEnricher : ILogEventEnricher
{
  /// <summary>
  /// Enriches the log event with the current activity's span, trace and parent ids.
  /// </summary>
  /// <param name="logEvent"></param>
  /// <param name="propertyFactory"></param>
  public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
  {
    var activity = Activity.Current;

    if (activity != null)
    {
      logEvent.AddPropertyIfAbsent(new LogEventProperty("SpanId", new ScalarValue(activity.GetSpanId())));
      logEvent.AddPropertyIfAbsent(new LogEventProperty("TraceId", new ScalarValue(activity.GetTraceId())));
      logEvent.AddPropertyIfAbsent(new LogEventProperty("ParentId", new ScalarValue(activity.GetParentId())));
    }
  }
}

#pragma warning disable SA1513
internal static class ActivityExtensions
{
  public static string GetSpanId(this Activity activity)
  {
    return activity.IdFormat switch
    {
      ActivityIdFormat.Hierarchical => activity.Id,
      ActivityIdFormat.W3C => activity.SpanId.ToHexString(),
      _ => null,
    } ?? string.Empty;
  }

  public static string GetTraceId(this Activity activity)
  {
    return activity.IdFormat switch
    {
      ActivityIdFormat.Hierarchical => activity.RootId,
      ActivityIdFormat.W3C => activity.TraceId.ToHexString(),
      _ => null,
    } ?? string.Empty;
  }

  public static string GetParentId(this Activity activity)
  {
    return activity.IdFormat switch
    {
      ActivityIdFormat.Hierarchical => activity.ParentId,
      ActivityIdFormat.W3C => activity.ParentSpanId.ToHexString(),
      _ => null,
    } ?? string.Empty;
  }
}
#pragma warning restore SA1513