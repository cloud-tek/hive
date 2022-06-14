using Serilog.Core;
using Serilog.Events;

namespace Ion.Logging.Enrichers;

public class ExceptionMessageEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Exception != null)
        {
            logEvent.AddOrUpdateProperty(new LogEventProperty("ExceptionMessage", new ScalarValue(logEvent.Exception.Message)));
        }
    }
}
