using Serilog.Core;
using Serilog.Events;

namespace Hive.Logging.Enrichers;

/// <summary>
/// Enriches the log event with the exception message.
/// </summary>
public class ExceptionMessageEnricher : ILogEventEnricher
{
  /// <summary>
  /// Enriches the log event with the exception message.
  /// </summary>
  /// <param name="logEvent"></param>
  /// <param name="propertyFactory"></param>
  public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
  {
    if (logEvent.Exception != null)
    {
      logEvent.AddOrUpdateProperty(new LogEventProperty("ExceptionMessage", new ScalarValue(logEvent.Exception.Message)));
    }
  }
}